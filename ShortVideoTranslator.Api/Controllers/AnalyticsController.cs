using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/analytics"), EnableRateLimiting("speech")]
public class AnalyticsController(IPlatformStore store) : ControllerBase
{
    [HttpPost("events")]
    public IActionResult Collect(AnalyticsRequest request)
    {
        if (!request.Consent) return BadRequest(new { message = "Chỉ ghi nhận thống kê khi người dùng đồng ý." });
        if (!Guid.TryParse(request.DeviceId, out _) || !new[] { "view", "play" }.Contains(request.Kind) || !NarrationLanguages.Codes.Contains(request.Language)) return BadRequest();
        store.Write(s => {
            if (request.PlaceId.HasValue && !s.Places.Any(p => p.Id == request.PlaceId && p.IsActive)) throw new PlatformException(404, "Địa điểm không tồn tại.");
            s.Events.Add(new(PlatformService.Id(), request.DeviceId, request.Kind, request.PlaceId, request.Language, DateTimeOffset.UtcNow));
            s.Events.RemoveAll(e => e.CreatedAt < DateTimeOffset.UtcNow.AddDays(-30));
            if (s.Events.Count > 10000) s.Events.RemoveRange(0, s.Events.Count - 10000);
            return true;
        });
        return Accepted();
    }
    [HttpPost("forget")]
    public IActionResult Forget(ForgetRequest request)
    {
        if (!Guid.TryParse(request.DeviceId, out _)) return BadRequest();
        store.Write(s => { s.Events.RemoveAll(e => e.DeviceId == request.DeviceId); return true; }); return NoContent();
    }
}
public record AnalyticsRequest(bool Consent, string DeviceId, string Kind, int? PlaceId, string Language);
public record ForgetRequest(string DeviceId);
