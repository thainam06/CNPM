using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class AudioTaskIntegrationTests
{
    [Fact]
    public async Task HiddenNarrationIsAbsentFromPublicManifestAndMediaNeedsPermission()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        const string source = "Private synthetic draft";
        var key = NarrationAssetService.KeyFor(source, "vi");
        byte[] bytes = [1, 2, 3]; var folder = Path.Combine(app.Folder, "audio"); Directory.CreateDirectory(folder);
        var asset = new NarrationAsset(key, source, "vi", $"/api/narration/audio/{key}", Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.Length, DateTimeOffset.UtcNow);
        await File.WriteAllTextAsync(Path.Combine(folder, key + ".json"), JsonSerializer.Serialize(asset));
        await File.WriteAllBytesAsync(Path.Combine(folder, key + ".mp3"), bytes);
        var publicManifest = await (await client.GetAsync("/api/narration/pack-manifest?language=vi")).Content.ReadAsStringAsync();
        Assert.DoesNotContain(source, publicManifest); Assert.DoesNotContain(key, publicManifest);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(asset.AudioUrl)).StatusCode);
        await AdminLogin(app, client);
        var media = await client.GetAsync(asset.AudioUrl);
        Assert.Equal(HttpStatusCode.OK, media.StatusCode); Assert.True(media.Headers.CacheControl!.NoStore);
    }
    [Fact]
    public async Task SameLengthCorruptAudioIsNeverAdvertisedAsReady()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        const string source = "Synthetic test source";
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"v1:vi:{source}")));
        byte[] bytes = [1, 2, 3]; var folder = Path.Combine(app.Folder, "audio"); Directory.CreateDirectory(folder);
        var asset = new NarrationAsset(key, source, "vi", $"/api/narration/audio/{key}", Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.Length, DateTimeOffset.UtcNow);
        await File.WriteAllTextAsync(Path.Combine(folder, key + ".json"), JsonSerializer.Serialize(asset));
        await File.WriteAllBytesAsync(Path.Combine(folder, key + ".mp3"), [3, 2, 1]);
        var service = app.Services.GetRequiredService<NarrationAssetService>();
        Assert.Null(service.Find(source, "vi")); Assert.Empty(service.Manifest("vi"));
    }
    private class RecoveringApplication : TestApplication
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Audio:WorkersEnabled"] = "true" }));
        }
    }
    private static async Task AdminLogin(TestApplication app, HttpClient client)
    {
        var platform = app.Services.GetRequiredService<PlatformService>();
        var admin = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(admin.Id, "HttpBootstrap_2026!", "TestPassword!");
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "TestPassword!" });
        response.EnsureSuccessStatusCode(); var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body.GetProperty("accessToken").GetString());
    }
    [Fact]
    public async Task AudioTaskControlsPersistAndRejectInvalidTransitions()
    {
        using var app = new TestApplication(); using var client = app.CreateClient(); await AdminLogin(app, client);
        var response = await client.PostAsJsonAsync("/api/admin/audio-tasks", new { placeId = 1, languages = new[] { "vi", "en" } }); response.EnsureSuccessStatusCode();
        var task = (await response.Content.ReadFromJsonAsync<AudioTaskState>())!;
        Assert.Equal("en", task.Languages[0]);
        (await client.PostAsJsonAsync($"/api/admin/audio-tasks/{task.Id}/pause", new { })).EnsureSuccessStatusCode();
        Assert.Equal("paused", new JsonPlatformStore(Path.Combine(app.Folder, "platform.json")).Read(s => s.AudioTasks.Single().Status));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/admin/audio-tasks/{task.Id}/pause", new { })).StatusCode);
        (await client.PostAsJsonAsync($"/api/admin/audio-tasks/{task.Id}/resume", new { })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/admin/audio-tasks/{task.Id}/cancel", new { })).EnsureSuccessStatusCode();
        Assert.Equal("cancelled", app.Services.GetRequiredService<IPlatformStore>().Read(s => s.AudioTasks.Single().Status));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/admin/audio-tasks/{task.Id}/resume", new { })).StatusCode);
    }
    [Fact]
    public async Task RestartRecoversRunningTaskAndFinishesFromCachedAssets()
    {
        using var app = new RecoveringApplication();
        var store = new JsonPlatformStore(Path.Combine(app.Folder, "platform.json"));
        var place = store.GetById(1)!; var id = PlatformService.Id(); var now = DateTimeOffset.UtcNow;
        // Cached byte fixtures exercise worker recovery, not real TTS or MP3 decoding.
        var folder = Path.Combine(app.Folder, "audio"); Directory.CreateDirectory(folder);
        foreach (var language in new[] { "en", "vi" }) {
            var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"v1:{language}:{place.Description}")));
            byte[] bytes = [1, 2, 3];
            var asset = new NarrationAsset(key, "Synthetic cached text", language, $"/api/narration/audio/{key}", Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.Length, now);
            await File.WriteAllBytesAsync(Path.Combine(folder, key + ".mp3"), bytes);
            await File.WriteAllTextAsync(Path.Combine(folder, key + ".json"), JsonSerializer.Serialize(asset));
        }
        store.Write(s => { s.AudioTasks.Add(new(id, place.Id, ["en", "vi"], "running", 0, null, "test.restart", now, now, PlatformService.ContentHash(place.Description))); return true; });
        using var client = app.CreateClient(); var live = app.Services.GetRequiredService<IPlatformStore>();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline && live.Read(s => s.AudioTasks.Single(t => t.Id == id).Status) != "completed") await Task.Delay(100);
        var completed = live.Read(s => s.AudioTasks.Single(t => t.Id == id));
        Assert.Equal("completed", completed.Status); Assert.Equal(2, completed.Completed);
        Assert.Equal(PlatformService.ContentHash(place.Description), live.Read(s => s.Places.First(p => p.Id == place.Id).ReadyEnglishHash));
    }
}
