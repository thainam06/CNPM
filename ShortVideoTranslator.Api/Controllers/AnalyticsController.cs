using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Api.Services;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/analytics"), EnableRateLimiting("runtime")]
public class AnalyticsController(AnalyticsQueue queue) : ControllerBase
{
    [HttpPost("batch")]
    public IActionResult Batch(AnalyticsBatch request) => Accepted(new { queued = queue.Enqueue(request) });
    [HttpPost("events")]
    public IActionResult Collect(AnalyticsRequest request)
    {
        if (!request.Consent) return BadRequest(new { message = "Chỉ ghi nhận thống kê khi người dùng đồng ý." });
        if (!Guid.TryParse(request.DeviceId, out _) || !new[] { "view", "play" }.Contains(request.Kind) || !NarrationLanguages.Codes.Contains(request.Language)) return BadRequest();
        queue.Enqueue(new(true, request.DeviceId, [new(Guid.NewGuid().ToString(), request.Kind, request.PlaceId, request.Language, DateTimeOffset.UtcNow)]));
        return Accepted();
    }
    [HttpPost("forget")]
    public IActionResult Forget(ForgetRequest request)
    {
        if (!Guid.TryParse(request.DeviceId, out _)) return BadRequest();
        queue.Forget(request.DeviceId); return NoContent();
    }
}
public record AnalyticsRequest(bool Consent, string DeviceId, string Kind, int? PlaceId, string Language);
public record ForgetRequest(string DeviceId);
