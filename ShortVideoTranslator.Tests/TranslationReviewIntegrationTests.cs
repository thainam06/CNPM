using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class TranslationReviewIntegrationTests
{
    private static async Task<string> Login(TestApplication app, HttpClient client)
    {
        var platform = app.Services.GetRequiredService<PlatformService>();
        var admin = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(admin.Id, "HttpBootstrap_2026!", "TestPassword!");
        await Token(client, "admin", "TestPassword!"); return admin.Id;
    }
    private static async Task Token(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username, password }); response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
    }
    private static async Task<TranslationDraft> Save(HttpClient client, Place place) =>
        (await (await client.PutAsJsonAsync($"/api/admin/translations/{place.Id}/en", new {
            text = "Reviewed English fixture.", sourceHash = PlatformService.ContentHash(place.Description), expectedVersion = (string?)null
        })).Content.ReadFromJsonAsync<TranslationDraft>())!;
    private static async Task<NarrationAsset> CachedFixture(TestApplication app, Place place, string? revision)
    {
        // Deliberately synthetic bytes: tests publication/ACL/cache logic, not MP3 decoding or real TTS.
        byte[] bytes = [1, 2, 3]; var folder = Path.Combine(app.Folder, "audio"); Directory.CreateDirectory(folder);
        var key = NarrationAssetService.KeyFor(place.Description, "en", revision);
        var asset = new NarrationAsset(key, revision == null ? "Machine fixture." : "Reviewed English fixture.", "en", $"/api/narration/audio/{key}",
            Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.Length, DateTimeOffset.UtcNow, revision == null ? "fixture" : "reviewed");
        await File.WriteAllBytesAsync(Path.Combine(folder, key + ".mp3"), bytes);
        await File.WriteAllTextAsync(Path.Combine(folder, key + ".json"), JsonSerializer.Serialize(asset)); return asset;
    }
    [Fact]
    public async Task EditorMaySaveButNeedsModerationAndPublicationPermissionsToApprove()
    {
        using var app = new TestApplication(); using var admin = app.CreateClient(); using var editor = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await editor.GetAsync("/api/admin/translations")).StatusCode);
        var actor = await Login(app, admin); var platform = app.Services.GetRequiredService<PlatformService>();
        platform.SaveRole(actor, new PlatformRole("translator", ["poi:update"]));
        platform.CreateUser(actor, "translator", "Test translator", "TranslatorPassword!", "translator");
        await Token(editor, "translator", "TranslatorPassword!");
        var place = app.Services.GetRequiredService<IPlatformStore>().Read(s => s.Places.Single(p => p.Id == 1));
        var draft = await Save(editor, place);
        Assert.Equal("draft", draft.Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await editor.PostAsJsonAsync("/api/admin/translations/1/en/approve", new { version = draft.Version })).StatusCode);
        Assert.Null(app.Services.GetRequiredService<TranslationReviewService>().Current(place.Description, "en"));
    }
    [Fact]
    public async Task ApprovalSwitchesPublicManifestAndRejectsStaleClientThenWithdrawalRestoresMachineLane()
    {
        using var app = new TestApplication(); using var admin = app.CreateClient(); using var visitor = app.CreateClient(); await Login(app, admin);
        var place = app.Services.GetRequiredService<IPlatformStore>().Read(s => s.Places.Single(p => p.Id == 1));
        var original = await CachedFixture(app, place, null); var draft = await Save(admin, place);
        var reviewed = await CachedFixture(app, place, draft.Version);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(reviewed.AudioUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync(original.AudioUrl)).StatusCode);
        (await admin.PostAsJsonAsync("/api/admin/translations/1/en/approve", new { version = draft.Version })).EnsureSuccessStatusCode();
        Assert.Equal(draft.Version, (await visitor.GetFromJsonAsync<Place>("/api/places/1"))!.NarrationRevisions!["en"]);
        var manifest = await (await visitor.GetAsync("/api/narration/pack-manifest?language=en")).Content.ReadAsStringAsync();
        Assert.Contains(reviewed.Key, manifest); Assert.DoesNotContain(original.Key, manifest);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(original.AudioUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync(reviewed.AudioUrl)).StatusCode);
        var privateAudio = await admin.GetAsync(original.AudioUrl); Assert.True(privateAudio.Headers.CacheControl!.NoStore);
        Assert.Equal(HttpStatusCode.Conflict, (await visitor.PostAsJsonAsync("/api/narration/translate", new { text = place.Description, language = "en", revision = "" })).StatusCode);
        var result = await (await visitor.PostAsJsonAsync("/api/narration/translate", new { text = place.Description, language = "en", revision = draft.Version })).Content.ReadFromJsonAsync<TranslationResult>();
        Assert.Equal("reviewed", result!.Provider); Assert.False(result.IsMachineTranslation);
        (await admin.PostAsJsonAsync("/api/admin/translations/1/en/withdraw", new { version = draft.Version })).EnsureSuccessStatusCode();
        Assert.Null((await visitor.GetFromJsonAsync<Place>("/api/places/1"))!.NarrationRevisions);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync(original.AudioUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(reviewed.AudioUrl)).StatusCode);
    }
    private class UnavailableTtsApplication : TestApplication
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services => services.AddSingleton(new TextToSpeechOptions { PythonExecutable = Path.Combine(Folder, "missing-python.exe"), TimeoutSeconds = 1 }));
        }
    }
    [Fact]
    public async Task FailedTtsKeepsExistingPublicationAndDraftForRetry()
    {
        using var app = new UnavailableTtsApplication(); using var admin = app.CreateClient(); await Login(app, admin);
        var place = app.Services.GetRequiredService<IPlatformStore>().Read(s => s.Places.Single(p => p.Id == 1)); var original = await CachedFixture(app, place, null);
        var draft = await Save(admin, place);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await admin.PostAsJsonAsync("/api/admin/translations/1/en/approve", new { version = draft.Version })).StatusCode);
        Assert.Null(app.Services.GetRequiredService<TranslationReviewService>().Current(place.Description, "en"));
        Assert.Equal(original.Key, app.Services.GetRequiredService<NarrationAssetService>().Find(place.Description, "en")!.Key);
        Assert.Equal("draft", app.Services.GetRequiredService<TranslationReviewService>().Check(1, "en", draft.Version).Status);
    }
}
