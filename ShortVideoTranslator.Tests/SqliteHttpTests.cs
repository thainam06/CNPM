using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Api.Services;

namespace ShortVideoTranslator.Tests;

public class SqliteHttpTests
{
    private class SqlApp : TestApplication
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:Provider"] = "Sqlite" }));
        }
    }
    [Fact]
    public async Task PublicBatchFeedsProtectedSqlTableAndHealth()
    {
        using var app = new SqlApp(); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/devices")).StatusCode);
        var device = Guid.NewGuid().ToString();
        var batch = new AnalyticsBatch(true, device, [new(Guid.NewGuid().ToString(), "heartbeat", null, "vi", DateTimeOffset.UtcNow)]);
        (await client.PostAsJsonAsync("/api/analytics/batch", batch)).EnsureSuccessStatusCode();
        app.Services.GetRequiredService<AnalyticsQueue>().Drain();
        var platform = app.Services.GetRequiredService<ShortVideoTranslator.Business.PlatformService>();
        var admin = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(admin.Id, "HttpBootstrap_2026!", "SqlChanged_2026!");
        var login = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "SqlChanged_2026!" });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        var devices = await client.GetFromJsonAsync<JsonElement>("/api/admin/devices");
        Assert.Equal("SQLite", devices.GetProperty("storage").GetString());
        Assert.Equal(1, devices.GetProperty("online").GetInt32());
        Assert.Equal(device, devices.GetProperty("items")[0].GetProperty("deviceId").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/admin/devices?page=0")).StatusCode);
        (await client.GetAsync("/api/health/ready")).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/analytics/forget", new { deviceId = device })).EnsureSuccessStatusCode();
        devices = await client.GetFromJsonAsync<JsonElement>("/api/admin/devices");
        Assert.Equal(0, devices.GetProperty("total").GetInt32());
    }
}
