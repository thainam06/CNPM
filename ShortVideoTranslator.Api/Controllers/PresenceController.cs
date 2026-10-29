using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
public sealed class PresenceController(PresenceService presence) : ControllerBase
{
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
