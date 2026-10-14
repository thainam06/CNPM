using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
[Route("api/localizations")]
public class LocalizationsController(PlaceService places, NarrationAssetService assets, AudioTaskWorker tasks) : ControllerBase
{
    [HttpPost("prepare-hotset"), EnableRateLimiting("speech")]
    public async Task<IActionResult> Hotset(WarmupRequest request, CancellationToken token)
    {
        NarrationLanguages.Validate(request.Language);
        if (request.PlaceIds.Length is < 1 or > 10) return BadRequest(new { message = "Hotset cần 1–10 địa điểm." });
        var selected = request.PlaceIds.Distinct().Select(places.GetById).ToArray();
        if (selected.Any(p => p == null)) return NotFound(new { message = "Địa điểm không còn công khai. Tải lại danh sách." });
        var result = new List<object>(); var ready = 0;
        // Only the first three are mandatory; remaining entries are deferred to background prefetch.
        foreach (var place in selected.Take(3)) {
            try { var asset = await assets.GenerateAsync(place!.Description, request.Language, token); result.Add(asset); ready++; }
            catch (Exception ex) when (ex is TranslationUnavailableException or InvalidOperationException or TimeoutException) {
                return StatusCode(503, new { ready = false, cached = result, required = Math.Min(3, selected.Length), message = "Chưa chuẩn bị đủ nội dung/audio cho ngôn ngữ này. " + (ex is TranslationUnavailableException ? ex.Message : "Edge-TTS chưa sẵn sàng.") });
            }
        }
        return Ok(new { ready = true, cached = result, required = ready, deferredIds = selected.Skip(3).Select(p => p!.Id) });
    }
    [HttpPost("{id:int}"), EnableRateLimiting("speech")]
    public async Task<IActionResult> Prepare(int id, LocalizationRequest request, CancellationToken token)
    {
        var place = places.GetById(id);
        if (place == null) return NotFound();
        try { return Ok(await assets.GenerateAsync(place.Description, request.Language, token)); }
        catch (TranslationUnavailableException ex) { return StatusCode(503, new { message = ex.Message }); }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException) { return StatusCode(503, new { message = "Edge-TTS chưa sẵn sàng. Vui lòng thử lại." }); }
    }
    [HttpPost("warmup"), Authorize, Permission("content:publish")]
    public IActionResult Warmup(WarmupRequest request)
    {
        NarrationLanguages.Validate(request.Language);
        if (request.PlaceIds.Length is < 1 or > 10) return BadRequest(new { message = "Chọn 1–10 địa điểm." });
        var result = new List<object>();
        foreach (var id in request.PlaceIds.Distinct()) {
            try { result.Add(tasks.Enqueue(id, [request.Language], User.FindFirstValue(ClaimTypes.NameIdentifier)!)); }
            catch (PlatformException ex) { result.Add(new { placeId = id, status = "skipped", message = ex.Message }); }
        }
        return Accepted(result);
    }
}
public record LocalizationRequest(string Language);
public record WarmupRequest(int[] PlaceIds, string Language);
