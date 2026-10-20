using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController(PlatformService platform) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet("assignable-roles"), Permission("user:read")]
    public IActionResult AssignableRoles() => Ok(platform.Store.Read(s => s.Roles).Where(r => r.Permissions.All(p => platform.View(platform.User(Actor)!).Permissions.Contains(p))).Select(r => r.Name));
    [HttpGet("places"), Permission("poi:update")]
    public IActionResult Places() => Ok(platform.Store.Read(s => s.Places));
    [HttpPost("places"), Permission("poi:create")]
    public IActionResult CreatePlace(Place request)
    {
        if (request.IsActive && !platform.HasPermission(Actor, "poi:toggle")) return Forbid();
        return Ok(platform.SavePlace(Actor, request, true));
    }
    [HttpPut("places/{id:int}"), Permission("poi:update")]
    public IActionResult UpdatePlace(int id, Place request)
    {
        if (id != request.Id) return BadRequest(new { message = "ID địa điểm không khớp." });
        var previous = platform.Store.Read(s => s.Places.FirstOrDefault(p => p.Id == id));
        if (previous?.IsActive != request.IsActive && !platform.HasPermission(Actor, "poi:toggle")) return Forbid();
        return Ok(platform.SavePlace(Actor, request));
    }
    [HttpDelete("places/{id:int}"), Permission("poi:delete")]
    public IActionResult DeletePlace(int id) { platform.DeletePlace(Actor, id); return NoContent(); }
    [HttpGet("users"), Permission("user:read")]
    public IActionResult Users() => Ok(platform.Store.Read(s => s.Users).Select(platform.View));
    [HttpPost("users"), Permission("user:create")]
    public IActionResult CreateUser(CreateUserRequest request)
    { EnsureGrant(request.Role); return Ok(platform.CreateUser(Actor, request.Username, request.DisplayName, request.Password, request.Role)); }
    [HttpPut("users/{id}"), Permission("user:update")]
    public IActionResult UpdateUser(string id, UpdateUserRequest request)
    {
        EnsureGrant(request.Role);
        if (platform.User(id)?.Role == "super_admin" && platform.User(Actor)?.Role != "super_admin") return Forbid();
        platform.UpdateUser(Actor, id, request.Role, request.Enabled); return NoContent();
    }
    [HttpDelete("users/{id}"), Permission("user:delete")]
    public IActionResult DeleteUser(string id) { platform.DeleteUser(Actor, id); return NoContent(); }
    [HttpGet("roles"), Permission("role:read")]
    public IActionResult Roles() => Ok(new { roles = platform.Store.Read(s => s.Roles), permissions = PlatformPermissions.All });
    [HttpPost("roles"), Permission("role:create")]
    public IActionResult CreateRole(PlatformRole request) { EnsurePermissions(request.Permissions); platform.SaveRole(Actor, request); return NoContent(); }
    [HttpPut("roles/{name}"), Permission("role:update")]
    public IActionResult UpdateRole(string name, PlatformRole request)
    { if (name != request.Name) return BadRequest(); EnsurePermissions(request.Permissions); platform.SaveRole(Actor, request, false); return NoContent(); }
    [HttpDelete("roles/{name}"), Permission("role:delete")]
    public IActionResult DeleteRole(string name) { platform.DeleteRole(Actor, name); return NoContent(); }
    [HttpGet("reviews"), Permission("content:moderate")]
    public IActionResult Reviews() => Ok(platform.Store.Read(s => s.Reviews.OrderByDescending(r => r.CreatedAt).ToArray()));
    [HttpPost("reviews/{id}/decision"), Permission("poi:approve")]
    public IActionResult Review(string id, ReviewDecision request) { platform.Review(Actor, id, request.Approve, request.AdminNote); return NoContent(); }
    [HttpGet("audit"), Permission("audit:read")]
    public IActionResult Audit([FromQuery] int limit = 100) => Ok(platform.Store.Read(s => s.Audit.OrderByDescending(a => a.CreatedAt).Take(Math.Clamp(limit, 1, 500)).ToArray()));
    [HttpGet("analytics"), Permission("analytics:view")]
    public IActionResult Analytics() => Ok(platform.Store.Read(s => new {
        places = s.Places.Count, activePlaces = s.Places.Count(p => p.IsActive), users = s.Users.Count,
        pendingReviews = s.Reviews.Count(r => r.Status == "pending"), events = s.Events.Count(e => e.Kind != "heartbeat"),
        consentedOnlineDevices = s.AnalyticsHeartbeats.Where(e => e.LastSeen > DateTimeOffset.UtcNow.AddMinutes(-5)).Select(e => e.DeviceId).Distinct().Count(),
        pendingAnalytics = s.PendingAnalytics.Count,
        byPlace = s.Events.Where(e => e.Kind != "heartbeat" && e.PlaceId != null).GroupBy(e => e.PlaceId).Select(g => new { placeId = g.Key, count = g.Count() }).ToArray(),
        byKind = s.Events.Where(e => e.Kind != "heartbeat").GroupBy(e => e.Kind).Select(g => new { kind = g.Key, count = g.Count() }).ToArray()
    }));
    [HttpGet("analytics/export"), Permission("analytics:export")]
    public IActionResult Export() => File(System.Text.Encoding.UTF8.GetBytes("createdAt,kind,placeId,language\n" +
        string.Join('\n', platform.Store.Read(s => s.Events.Where(e => e.Kind != "heartbeat").ToArray()).Select(e => $"{e.CreatedAt:O},{e.Kind},{e.PlaceId},{e.Language}"))), "text/csv", "smarttour-events.csv");
    [HttpGet("backup"), Permission("system:backup")]
    public IActionResult Backup() => Ok(platform.Store.Read(s => new { s.Places, s.Menus, s.Version, s.UpdatedAt }));
    [HttpGet("places/{id:int}/menu"), Permission("menu:read")]
    public IActionResult Menus(int id) => Ok(platform.Store.Read(s => s.Menus.Where(m => m.PlaceId == id).ToArray()));
    [HttpPost("places/{id:int}/menu"), Permission("menu:create")]
    public IActionResult AddMenu(int id, MenuItem item) => Ok(platform.SaveMenu(Actor, item with { PlaceId = id }, true));
    [HttpPut("places/{id:int}/menu/{itemId}"), Permission("menu:update")]
    public IActionResult UpdateMenu(int id, string itemId, MenuItem item) => Ok(platform.SaveMenu(Actor, item with { PlaceId = id, Id = itemId }, false));
    [HttpDelete("places/{id:int}/menu/{itemId}"), Permission("menu:delete")]
    public IActionResult DeleteMenu(int id, string itemId)
    {
        platform.Store.Write(s => { if (s.Menus.RemoveAll(m => m.PlaceId == id && m.Id == itemId) == 0) throw new PlatformException(404, "Không tìm thấy món ăn."); PlatformService.Audit(s, Actor, "menu.delete", itemId); return true; });
        return NoContent();
    }
    private void EnsureGrant(string role)
    {
        var target = platform.Store.Read(s => s.Roles.FirstOrDefault(r => r.Name == role)) ?? throw new PlatformException(400, "Vai trò không tồn tại.");
        EnsurePermissions(target.Permissions);
    }
    private void EnsurePermissions(string[] permissions)
    {
        var own = platform.View(platform.User(Actor)!).Permissions;
        if (permissions.Any(p => !own.Contains(p))) throw new PlatformException(403, "Không thể cấp quyền cao hơn quyền của bạn.");
    }
}
public record CreateUserRequest(string Username, string DisplayName, string Password, string Role);
public record UpdateUserRequest(string Role, bool Enabled);
public record ReviewDecision(bool Approve, string AdminNote);
