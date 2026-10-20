using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class SystemReadinessTests
{
    private class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private class StatusApp : TestApplication
    {
        public Clock Clock { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(Clock); });
        }
    }
    [Fact]
    public async Task PublicReadinessAliasesDoNotExposePathsOrOptionalConfiguration()
    {
        using var app = new StatusApp(); using var client = app.CreateClient();
        foreach (var path in new[] { "/health", "/health/ready", "/api/health/ready" }) {
            var response = await client.GetAsync(path); response.EnsureSuccessStatusCode(); Assert.True(response.Headers.CacheControl!.NoStore);
            var body = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain(app.Folder, body); Assert.DoesNotContain("provider", body);
            Assert.Equal("ready", JsonDocument.Parse(body).RootElement.GetProperty("status").GetString());
        }
        Assert.Empty(Directory.EnumerateFiles(app.Folder, "readiness-*.tmp"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/system-status")).StatusCode);
    }
    [Fact]
    public async Task MalformedStorageBecomes503AfterBoundedReadinessCacheExpires()
    {
        using var app = new StatusApp(); using var client = app.CreateClient();
        (await client.GetAsync("/health/ready")).EnsureSuccessStatusCode();
        var path = Path.Combine(app.Folder, "platform.json"); var saved = await File.ReadAllTextAsync(path);
        try {
            await File.WriteAllTextAsync(path, "not json"); app.Clock.Now = app.Clock.Now.AddSeconds(16);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
        } finally { await File.WriteAllTextAsync(path, saved); }
        app.Clock.Now = app.Clock.Now.AddSeconds(16); (await client.GetAsync("/health/ready")).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task AdminDiagnosticsPreservesCatalogVersionAndOwnerIsDenied()
    {
        using var app = new StatusApp(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>(); var actor = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(actor.Id, "HttpBootstrap_2026!", "TestPassword!");
        await Login(client, "admin", "TestPassword!"); var version = platform.Store.Read(s => s.Version);
        var response = await client.GetAsync("/api/admin/system-status"); response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.True(body.GetProperty("coreReady").GetBoolean()); Assert.False(body.GetProperty("optionalServicesBlockCore").GetBoolean());
        Assert.Equal(6, body.GetProperty("audio").GetArrayLength()); Assert.Equal(version, platform.Store.Read(s => s.Version));
        platform.CreateUser(actor.Id, "status_owner", "Owner fixture", "TestPassword!", "poi_owner"); await Login(client, "status_owner", "TestPassword!");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/system-status")).StatusCode);
    }
    private static async Task Login(HttpClient client, string username, string password)
    {
        var token = await (await client.PostAsJsonAsync("/api/auth/token", new { username, password })).Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
    }
}
