using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public sealed class TranslationReviewTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "SmartTourReviewTests", Guid.NewGuid().ToString("N"));
    private JsonPlatformStore Store() => new(Path.Combine(folder, "platform.json"));
    [Fact]
    public void DraftRemainsPrivateAndStaleEditorCannotOverwrite()
    {
        var store = Store(); var service = new TranslationReviewService(store); var place = store.GetById(1)!;
        var hash = PlatformService.ContentHash(place.Description);
        var draft = service.Save("editor", 1, "en", "Reviewed English example.", hash, null);
        Assert.Null(service.Current(place.Description, "en")); Assert.Null(store.GetById(1)!.NarrationRevisions);
        Assert.Equal(409, Assert.Throws<PlatformException>(() => service.Save("other", 1, "en", "Overwrite", hash, null)).StatusCode);
        var next = service.Save("editor", 1, "en", "Updated English example.", hash, draft.Version);
        Assert.NotEqual(draft.Version, next.Version); Assert.Equal(409, Assert.Throws<PlatformException>(() => service.Check(1, "en", draft.Version)).StatusCode);
    }
    [Fact]
    public void PublishChangesCatalogRevisionAndWithdrawReturnsToMachineLane()
    {
        var store = Store(); var service = new TranslationReviewService(store); var place = store.GetById(1)!;
        var version = store.Read(s => s.Version);
        var draft = service.Save("editor", 1, "en", "English example.", PlatformService.ContentHash(place.Description), null);
        service.Publish("reviewer", 1, "en", draft.Version, draft.Version);
        Assert.Equal(draft.Version, store.GetById(1)!.NarrationRevisions!["en"]); Assert.True(store.Read(s => s.Version) > version);
        Assert.Equal("English example.", new TranslationReviewService(Store()).Current(place.Description, "en")!.Text);
        service.Withdraw("reviewer", 1, "en", draft.Version);
        Assert.Null(store.GetById(1)!.NarrationRevisions); Assert.Null(service.Current(place.Description, "en"));
        Assert.Equal(409, Assert.Throws<PlatformException>(() => service.Withdraw("reviewer", 1, "en", draft.Version)).StatusCode);
    }
    [Fact]
    public void ChangedSourceInvalidatesReviewedTextAndRejectsOldDraft()
    {
        var store = Store(); var service = new TranslationReviewService(store); var place = store.GetById(1)!;
        var draft = service.Save("editor", 1, "en", "English example.", PlatformService.ContentHash(place.Description), null);
        service.Publish("reviewer", 1, "en", draft.Version, draft.Version);
        store.Write(s => { s.Places[0] = s.Places[0] with { Description = "Nguồn đã thay đổi." }; return true; });
        Assert.Null(store.GetById(1)!.NarrationRevisions); Assert.Null(service.Current("Nguồn đã thay đổi.", "en"));
        Assert.Equal(409, Assert.Throws<PlatformException>(() => service.Publish("reviewer", 1, "en", draft.Version, draft.Version)).StatusCode);
    }
    [Fact]
    public void OfficialPublicationCannotLoseItsOnlyEnglishAudioThroughWithdrawal()
    {
        var store = Store(); var service = new TranslationReviewService(store); var place = store.GetById(1)!;
        var draft = service.Save("editor", 1, "en", "English example.", PlatformService.ContentHash(place.Description), null);
        service.Publish("reviewer", 1, "en", draft.Version, draft.Version);
        store.Write(s => { s.Places[0] = s.Places[0] with { IsDemo = false, IsActive = true }; return true; });
        Assert.Equal(409, Assert.Throws<PlatformException>(() => service.Withdraw("reviewer", 1, "en", draft.Version)).StatusCode);
        Assert.NotNull(service.Current(place.Description, "en"));
        store.Write(s => { s.Places[0] = s.Places[0] with { IsActive = false, ReadyEnglishHash = draft.SourceHash, AudioStatus = "ready" }; return true; });
        service.Withdraw("reviewer", 1, "en", draft.Version);
        Assert.Null(service.Current(place.Description, "en"));
        var hidden = store.Read(s => s.Places.Single(p => p.Id == 1));
        Assert.Null(hidden.ReadyEnglishHash); Assert.Equal("missing", hidden.AudioStatus);
    }
    [Fact]
    public void VietnameseAndUntranslatedCopiesAreRejected()
    {
        var store = Store(); var service = new TranslationReviewService(store); var place = store.GetById(1)!; var hash = PlatformService.ContentHash(place.Description);
        Assert.Equal(400, Assert.Throws<PlatformException>(() => service.Save("editor", 1, "vi", "Test", hash, null)).StatusCode);
        Assert.Equal(400, Assert.Throws<PlatformException>(() => service.Save("editor", 1, "en", place.Description, hash, null)).StatusCode);
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
