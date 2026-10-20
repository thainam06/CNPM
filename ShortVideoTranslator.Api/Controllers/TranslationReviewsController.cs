using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/admin/translations"), Permission("poi:update")]
public sealed class TranslationReviewsController(IPlatformStore store, TranslationReviewService reviews, NarrationAssetService assets, PlatformService platform) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public IActionResult List() { Response.Headers.CacheControl = "no-store"; return Ok(store.Read(s => new { places = s.Places.Where(TourRegion.Includes).ToArray(), drafts = s.TranslationDrafts.ToArray() })); }
    [HttpGet("{placeId:int}/{language}")]
    public IActionResult Detail(int placeId, string language)
    {
        NarrationLanguages.Validate(language); Response.Headers.CacheControl = "no-store";
        var place = store.Read(s => s.Places.FirstOrDefault(p => p.Id == placeId)); if (place == null) return NotFound();
        var draft = store.Read(s => s.TranslationDrafts.FirstOrDefault(t => t.PlaceId == placeId && t.Language == language));
        return Ok(new { place, sourceHash = PlatformService.ContentHash(place.Description), draft, published = reviews.Current(place.Description, language), audio = assets.Find(place.Description, language) });
    }
    [HttpPut("{placeId:int}/{language}")]
    public IActionResult Save(int placeId, string language, SaveTranslationRequest request) => Ok(reviews.Save(Actor, placeId, language, request.Text, request.SourceHash, request.ExpectedVersion));
    [HttpPost("{placeId:int}/{language}/approve"), Permission("content:moderate"), Permission("content:publish")]
    public async Task<IActionResult> Approve(int placeId, string language, TranslationVersionRequest request, CancellationToken token)
    {
        var draft = reviews.Check(placeId, language, request.Version);
        var source = store.Read(s => s.Places.Single(p => p.Id == placeId).Description);
        try {
            // Prepare before publication. Failure leaves the currently published revision untouched.
            var audio = await assets.PrepareReviewedAsync(source, draft, token); token.ThrowIfCancellationRequested();
            if (!platform.HasPermission(Actor, "poi:update") || !platform.HasPermission(Actor, "content:moderate") || !platform.HasPermission(Actor, "content:publish")) return StatusCode(403, new { message = "Quyền duyệt đã bị thu hồi." });
            if (audio.Key != NarrationAssetService.KeyFor(source, language, draft.Version) || audio.Text != draft.Text) return Conflict(new { message = "MP3 không khớp bản nháp." });
            var published = reviews.Publish(Actor, placeId, language, request.Version, draft.Version);
            if (language == "en") platform.MarkEnglishReady(placeId, draft.SourceHash);
            return Ok(new { published, audio });
        } catch (Exception ex) when (ex is InvalidOperationException or TimeoutException or IOException or System.ComponentModel.Win32Exception) { return StatusCode(503, new { message = "Chưa tạo được MP3 cho bản dịch. Bản đang công khai được giữ nguyên; thử lại sau." }); }
    }
    [HttpPost("{placeId:int}/{language}/withdraw"), Permission("content:moderate"), Permission("content:publish")]
    public IActionResult Withdraw(int placeId, string language, TranslationVersionRequest request)
    {
        var source = store.Read(s => s.Places.FirstOrDefault(p => p.Id == placeId)?.Description);
        var fallbackReady = source != null && assets.FindByKey(NarrationAssetService.KeyFor(source, "en")) != null;
        reviews.Withdraw(Actor, placeId, language, request.Version, fallbackReady); return NoContent();
    }
}
public record SaveTranslationRequest(string Text, string SourceHash, string? ExpectedVersion);
public record TranslationVersionRequest(string Version);
