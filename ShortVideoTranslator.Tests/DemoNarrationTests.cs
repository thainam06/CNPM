using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class DemoNarrationTests
{
    [Fact]
    public async Task EverySampleHasSixRealTextsWithoutCallingRemoteTranslation()
    {
        using var client = new HttpClient(new NoNetwork());
        var catalog = new DemoNarrations();
        var translations = new TranslationService(client, new() { Enabled = false }, catalog);
        foreach (var place in new DemoPlaceRepository().GetAll()) {
            Assert.InRange(place.Description.Length, 800, 3000); Assert.Contains("\n\n", place.Description);
            foreach (var language in NarrationLanguages.Codes) {
                var result = await translations.TranslateAsync(place.Description, language);
                NarrationLanguages.ValidateText(result.Text);
                if (language == "vi") Assert.Equal(place.Description, result.Text);
                else { Assert.NotEqual(place.Description, result.Text); Assert.Equal(DemoNarrations.Provider, result.Provider); Assert.True(result.IsMachineTranslation); }
            }
            // A source edited by Admin must not silently fall back to our old script.
            Assert.Null(catalog.Find(place.Description + " Đã sửa.", "en"));
            await Assert.ThrowsAsync<TranslationUnavailableException>(() => translations.TranslateAsync(place.Description + " Đã sửa.", "en"));
        }
    }
    [Fact]
    public void UpgradeChangesOnlyUntouchedSystemSamplesAndInvalidatesOldAudioReadiness()
    {
        var folder = Path.Combine(Path.GetTempPath(), "SmartTourNarrations", Guid.NewGuid().ToString("N"));
        try {
            var path = Path.Combine(folder, "platform.json"); var store = new JsonPlatformStore(path);
            store.Write(s => {
                s.SampleDataVersion = 5;
                s.Places = s.Places.Select(p => p with { Description = DemoNarrations.All.Single(n => n.Name == p.Name).PreviousText, ReadyEnglishHash = "old", AudioStatus = "ready" }).ToList();
                s.Places[0] = s.Places[0] with { IsActive = false, ImageUrl = "/api/media/keep.jpg", Images = [] };
                s.Places[1] = s.Places[1] with { Description = "Nội dung do thành viên biên tập." };
                s.Places[2] = s.Places[2] with { OwnerId = "owner-fixture" };
                s.Places[3] = s.Places[3] with { IsDemo = false };
                s.Places[4] = s.Places[4] with { Region = "district4" };
                return true;
            });
            var before = store.Read(s => s.Places); var migrated = new JsonPlatformStore(path);
            var after = migrated.Read(s => s.Places);
            Assert.Equal(DemoNarrations.All[0].Texts["vi"], after[0].Description);
            Assert.False(after[0].IsActive); Assert.Empty(after[0].Images!); Assert.Equal(before[0].ImageUrl, after[0].ImageUrl);
            Assert.Null(after[0].ReadyEnglishHash); Assert.Equal("missing", after[0].AudioStatus);
            for (var index = 1; index < before.Count; index++) Assert.Equivalent(before[index], after[index]);
            var version = migrated.Read(s => s.Version);
            Assert.Equal(6, migrated.Read(s => s.SampleDataVersion));
            Assert.Equal(version, new JsonPlatformStore(path).Read(s => s.Version));
        } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    private sealed class NoNetwork : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => throw new InvalidOperationException("Sample translations must not call the network.");
    }
}
