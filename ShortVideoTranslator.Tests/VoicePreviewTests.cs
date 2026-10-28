using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class VoicePreviewTests
{
    private sealed class PreviewApplication : TestApplication
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services => services.AddSingleton(new TextToSpeechOptions {
                PythonExecutable = OperatingSystem.IsWindows() ? "python" : "python3",
                PythonArguments = [Path.Combine(Folder, "preview_fixture.py")], TimeoutSeconds = 10
            }));
        }
    }

    [Fact]
    public void AllSixLanguagesHaveDistinctVoicesAndKeepThePublicDefault()
    {
        Assert.Equal(NarrationLanguages.Codes, VoicePreview.Languages.Select(item => item.Code));
        foreach (var item in VoicePreview.Languages) {
            Assert.Equal(2, item.Voices.Count);
            Assert.Equal(2, item.Voices.Select(voice => voice.Id).Distinct().Count());
            Assert.Equal(NarrationVoiceCatalog.Resolve(item.Code), item.Voices[0].Id);
            foreach (var voice in item.Voices) VoicePreview.Validate("Fixture", item.Code, voice.Id);
        }
    }

    [Theory]
    [InlineData("", "en", "en-US-GuyNeural")]
    [InlineData("Fixture", "de", "de-DE-ConradNeural")]
    [InlineData("Fixture", "en", "vi-VN-NamMinhNeural")]
    [InlineData("Fixture", "en", "--help")]
    public async Task InvalidInputIsRejectedBeforeStartingAnyProcess(string text, string language, string voice)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => VoicePreview.GenerateAsync(
            new() { PythonExecutable = "must-not-start" }, text, language, voice, default));
    }

    [Fact]
    public async Task PreviewRequiresEditorPermissionAndDoesNotPublishOrSaveAnyDraft()
    {
        using var app = new PreviewApplication();
        // Synthetic output checks process arguments, HTTP security and isolation, not real speech.
        var script = Path.Combine(app.Folder, "preview_fixture.py");
        Directory.CreateDirectory(app.Folder);
        await File.WriteAllTextAsync(script, "import sys,pathlib\na=sys.argv\npathlib.Path(a[a.index('--write-media')+1]).write_bytes(('fixture:'+a[a.index('--voice')+1]).encode())\n");
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/voice-preview")).StatusCode);
        var platform = app.Services.GetRequiredService<PlatformService>();
        var actor = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(actor.Id, "HttpBootstrap_2026!", "PreviewPassword!");
        var login = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "PreviewPassword!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.GetProperty("accessToken").GetString());
        var choices = await client.GetAsync("/api/admin/voice-preview"); choices.EnsureSuccessStatusCode();
        Assert.Equal("no-store", choices.Headers.CacheControl!.ToString());
        var before = app.Services.GetRequiredService<IPlatformStore>().Read(s => JsonSerializer.Serialize(new { s.TranslationDrafts, s.ApprovedTranslations }));
        var wrongVoice = await client.PostAsJsonAsync("/api/admin/voice-preview", new { text = "Fixture", language = "en", voice = "vi-VN-NamMinhNeural" });
        Assert.Equal(HttpStatusCode.BadRequest, wrongVoice.StatusCode);
        var response = await client.PostAsJsonAsync("/api/admin/voice-preview", new { text = "Fixture", language = "en", voice = "en-US-GuyNeural" });
        response.EnsureSuccessStatusCode();
        Assert.Equal("audio/mpeg", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("fixture:en-US-GuyNeural", Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync()));
        var after = app.Services.GetRequiredService<IPlatformStore>().Read(s => JsonSerializer.Serialize(new { s.TranslationDrafts, s.ApprovedTranslations }));
        Assert.Equal(before, after);
        Assert.False(Directory.Exists(Path.Combine(app.Folder, "audio")) && Directory.EnumerateFiles(Path.Combine(app.Folder, "audio")).Any());
    }
}
