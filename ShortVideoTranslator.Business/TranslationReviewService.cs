namespace ShortVideoTranslator.Business;

public record TranslationDraft(string Id, int PlaceId, string Language, string SourceHash, string Text,
    string Version, string Status, string ActorId, DateTimeOffset UpdatedAt, string? PublishedRevision = null);
public record ApprovedTranslation(string SourceHash, string Language, string Text, string Revision, string ReviewerId, DateTimeOffset ApprovedAt);

// Reviewed text is scoped to the exact Vietnamese source and language, like the TTS cache.
public sealed class TranslationReviewService(IPlatformStore store)
{
    public ApprovedTranslation? Current(string source, string language) => store.Read(s => s.ApprovedTranslations.FirstOrDefault(t => t.SourceHash == PlatformService.ContentHash(source) && t.Language == language));
    public TranslationDraft Save(string actor, int placeId, string language, string text, string sourceHash, string? expectedVersion)
    {
        NarrationLanguages.Validate(language); NarrationLanguages.ValidateText(text);
        if (language == "vi") throw new PlatformException(400, "Sửa bản gốc tiếng Việt trong phần Địa điểm.");
        return store.Write(s => {
            var place = s.Places.FirstOrDefault(p => p.Id == placeId) ?? throw new PlatformException(404, "Không tìm thấy địa điểm.");
            if (sourceHash != PlatformService.ContentHash(place.Description)) throw new PlatformException(409, "Bản gốc vừa thay đổi. Tải lại trước khi sửa bản dịch.");
            if (text.Trim() == place.Description.Trim()) throw new PlatformException(400, "Bản dịch không được giữ nguyên toàn bộ nội dung tiếng Việt.");
            var previous = s.TranslationDrafts.FirstOrDefault(t => t.PlaceId == placeId && t.Language == language);
            if (previous?.Version != expectedVersion) throw new PlatformException(409, "Bản nháp đã được người khác sửa. Tải lại trước khi lưu.");
            var draft = new TranslationDraft(previous?.Id ?? PlatformService.Id(), placeId, language, sourceHash, text.Trim(), PlatformService.Id(), "draft", actor, DateTimeOffset.UtcNow);
            s.TranslationDrafts.RemoveAll(t => t.PlaceId == placeId && t.Language == language); s.TranslationDrafts.Add(draft);
            PlatformService.Audit(s, actor, "translation.save", draft.Id); return draft;
        });
    }
    public TranslationDraft Check(int placeId, string language, string version) => store.Read(s => Check(s, placeId, language, version));
    private static TranslationDraft Check(PlatformState state, int placeId, string language, string version)
    {
        var place = state.Places.FirstOrDefault(p => p.Id == placeId) ?? throw new PlatformException(404, "Không tìm thấy địa điểm.");
        var draft = state.TranslationDrafts.FirstOrDefault(t => t.PlaceId == placeId && t.Language == language) ?? throw new PlatformException(404, "Chưa có bản nháp.");
        if (draft.Version != version || draft.SourceHash != PlatformService.ContentHash(place.Description)) throw new PlatformException(409, "Bản nháp hoặc nguồn đã thay đổi. Tải lại trước khi duyệt.");
        if (draft.Status != "draft") throw new PlatformException(409, "Bản nháp này đã được duyệt.");
        return draft;
    }
    public ApprovedTranslation Publish(string actor, int placeId, string language, string version, string preparedRevision) => store.Write(s => {
        var draft = Check(s, placeId, language, version);
        if (preparedRevision != draft.Version) throw new PlatformException(409, "MP3 không khớp phiên bản bản nháp.");
        var approved = new ApprovedTranslation(draft.SourceHash, language, draft.Text, preparedRevision, actor, DateTimeOffset.UtcNow);
        s.ApprovedTranslations.RemoveAll(t => t.SourceHash == draft.SourceHash && t.Language == language); s.ApprovedTranslations.Add(approved);
        s.TranslationDrafts[s.TranslationDrafts.FindIndex(t => t.Id == draft.Id)] = draft with { Status = "approved", PublishedRevision = preparedRevision };
        TouchCatalog(s, draft.SourceHash); PlatformService.Audit(s, actor, "translation.publish", draft.Id); return approved;
    });
    public void Withdraw(string actor, int placeId, string language, string revision, bool fallbackEnglishReady = false) => store.Write(s => {
        var place = s.Places.FirstOrDefault(p => p.Id == placeId) ?? throw new PlatformException(404, "Không tìm thấy địa điểm.");
        var hash = PlatformService.ContentHash(place.Description);
        var current = s.ApprovedTranslations.FirstOrDefault(t => t.SourceHash == hash && t.Language == language);
        if (current == null || current.Revision != revision) throw new PlatformException(409, "Phiên bản công khai đã thay đổi. Tải lại.");
        if (language == "en" && !fallbackEnglishReady && s.Places.Any(p => p.IsActive && !p.IsDemo && PlatformService.ContentHash(p.Description) == hash))
            throw new PlatformException(409, "Bản tiếng Anh này đang bảo đảm điều kiện công khai. Hãy ẩn địa điểm chính thức dùng nội dung này trước khi thu hồi, hoặc chuẩn bị bản tiếng Anh thay thế.");
        if (language == "en" && !fallbackEnglishReady)
            s.Places = s.Places.Select(p => PlatformService.ContentHash(p.Description) == hash ? p with { ReadyEnglishHash = null, AudioStatus = "missing" } : p).ToList();
        s.ApprovedTranslations.Remove(current); TouchCatalog(s, hash); PlatformService.Audit(s, actor, "translation.withdraw", revision); return true;
    });
    private static void TouchCatalog(PlatformState state, string hash)
    {
        var now = DateTimeOffset.UtcNow;
        state.Places = state.Places.Select(p => PlatformService.ContentHash(p.Description) == hash ? p with { UpdatedAt = now } : p).ToList();
        state.Version++; state.UpdatedAt = now;
    }
}
