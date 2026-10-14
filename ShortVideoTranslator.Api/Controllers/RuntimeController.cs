using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/runtime")]
public class RuntimeController(IPlatformStore store) : ControllerBase
{
    [HttpPost("location"), EnableRateLimiting("runtime")]
    public IActionResult Ingest(RuntimeLocationRequest request)
    {
        if (!request.Consent) return BadRequest(new { message = "Chia sẻ vị trí cần đồng ý riêng, độc lập với thống kê lượt nghe." });
        if (!Guid.TryParseExact(request.SessionId, "D", out var sessionGuid) || !double.IsFinite(request.Latitude) || !double.IsFinite(request.Longitude) ||
            !double.IsFinite(request.Accuracy) || request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180 || request.Accuracy is < 0 or > 100) return BadRequest();
        var sessionId = sessionGuid.ToString("D");
        store.Write(s => {
            var now = DateTimeOffset.UtcNow;
            if (s.RuntimeRevocations.TryGetValue(sessionId, out var revokedUntil) && revokedUntil > now) throw new PlatformException(409, "Phiên chia sẻ đã bị thu hồi; bật lại để tạo phiên mới.");
            var latest = s.RuntimeLocations.Where(p => p.SessionId == sessionId).MaxBy(p => p.LastSeen);
            if (latest?.LastSeen > now.AddSeconds(-5)) throw new PlatformException(429, "Chờ ít nhất 5 giây giữa hai cập nhật vị trí.");
            var hour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
            var item = new RuntimeLocation(sessionId, hour, Math.Round(request.Latitude, 3), Math.Round(request.Longitude, 3), request.Accuracy, now);
            var index = s.RuntimeLocations.FindIndex(p => p.SessionId == sessionId && p.Hour == hour);
            if (index < 0) s.RuntimeLocations.Add(item); else s.RuntimeLocations[index] = item;
            s.RuntimeLocations.RemoveAll(p => p.LastSeen < now.AddHours(-24));
            if (s.RuntimeLocations.Count > 5000) s.RuntimeLocations = s.RuntimeLocations.OrderByDescending(p => p.LastSeen).Take(5000).ToList();
            return true;
        });
        return Accepted(new { precision = "rounded-3-decimals", retentionHours = 24 });
    }
    [HttpPost("forget"), EnableRateLimiting("runtime")]
    public IActionResult Forget(RuntimeForgetRequest request)
    {
        if (!Guid.TryParseExact(request.SessionId, "D", out var sessionGuid)) return BadRequest();
        var sessionId = sessionGuid.ToString("D");
        store.Write(s => {
            s.RuntimeLocations.RemoveAll(p => p.SessionId == sessionId);
            s.RuntimeRevocations[sessionId] = DateTimeOffset.UtcNow.AddHours(24);
            foreach (var key in s.RuntimeRevocations.Where(p => p.Value <= DateTimeOffset.UtcNow).Select(p => p.Key).ToArray()) s.RuntimeRevocations.Remove(key);
            if (s.RuntimeRevocations.Count > 10000) foreach (var key in s.RuntimeRevocations.OrderBy(p => p.Value).Take(s.RuntimeRevocations.Count - 10000).Select(p => p.Key).ToArray()) s.RuntimeRevocations.Remove(key);
            return true;
        });
        return NoContent();
    }
    [HttpGet("window"), Permission("system:logs")]
    public IActionResult Window([FromQuery] int hours = 1)
    {
        if (hours is < 1 or > 24) return BadRequest(new { message = "Chọn cửa sổ từ 1 đến 24 giờ." });
        Response.Headers.CacheControl = "no-store";
        return Ok(store.Read(s => new { hours, retentionHours = 24,
            positions = s.RuntimeLocations.Where(p => p.LastSeen >= DateTimeOffset.UtcNow.AddHours(-hours)).OrderByDescending(p => p.LastSeen).Take(500).ToArray(),
            hourlyCounts = s.RuntimeLocations.Where(p => p.LastSeen >= DateTimeOffset.UtcNow.AddHours(-hours)).GroupBy(p => p.Hour).Select(g => new { hour = g.Key, sessions = g.Select(p => p.SessionId).Distinct().Count() }).ToArray() }));
    }
}
public record RuntimeLocationRequest(bool Consent, string SessionId, double Latitude, double Longitude, double Accuracy);
public record RuntimeForgetRequest(string SessionId);
