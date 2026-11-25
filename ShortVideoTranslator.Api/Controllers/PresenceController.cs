using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
public sealed class PresenceController(PresenceService presence, DeviceBlockService blocks) : ControllerBase
{
    [HttpGet("/api/presence/access")]
    public IActionResult Access() { Response.Headers.CacheControl = "no-store"; return Ok(new { blockedUntil = blocks.Until(ShortVideoTranslator.Api.Services.DeviceBlockMiddleware.Device(HttpContext)) }); }
    [HttpGet("/api/admin/presence/blocks"), Permission("user:update")]
    public IActionResult Blocks() => Ok(blocks.List());
    [HttpGet("/api/admin/presence/blocks/history"), Permission("user:update")]
    public IActionResult BlockHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    { Response.Headers.CacheControl = "no-store"; return Ok(blocks.History(page, pageSize)); }
    [HttpPost("/api/admin/presence/blocks"), Permission("user:update")]
    public IActionResult Block(DeviceBlockRequest request) {
        var own = ShortVideoTranslator.Api.Services.DeviceBlockMiddleware.Device(HttpContext);
        if (Guid.TryParse(own, out var ownId) && Guid.TryParse(request.DeviceId, out var target) && ownId == target) return BadRequest(new { message = "Không tự chặn trình duyệt đang quản trị." });
        return Ok(new { blockedUntil = blocks.Block(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, request.DeviceId, request.Seconds) });
    }
    [HttpDelete("/api/admin/presence/blocks/{deviceId}"), Permission("user:update")]
    public IActionResult Unblock(string deviceId) { blocks.Unblock(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, deviceId); return NoContent(); }
    [HttpPost("/api/presence/heartbeat"), EnableRateLimiting("runtime")]
    public IActionResult Heartbeat(PresenceRequest request)
    { Response.Headers.CacheControl = "no-store"; presence.Heartbeat(request.DeviceId, request.Device, request.SessionId); return NoContent(); }

    [HttpPost("/api/presence/leave"), EnableRateLimiting("runtime")]
    public IActionResult Leave(PresenceLeave request)
    { Response.Headers.CacheControl = "no-store"; presence.Leave(request.DeviceId, request.SessionId); return NoContent(); }

    [HttpGet("/api/admin/presence"), Permission("analytics:view")]
    public IActionResult Devices([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    { Response.Headers.CacheControl = "no-store"; return Ok(presence.Devices(page, pageSize)); }
}
public record PresenceRequest(string DeviceId, DeviceDescriptor? Device = null, string? SessionId = null);
public record PresenceLeave(string DeviceId, string SessionId);
public record DeviceBlockRequest(string DeviceId, int Seconds);
