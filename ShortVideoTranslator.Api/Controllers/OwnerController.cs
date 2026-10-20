using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Authorize, Route("api/owner")]
public class OwnerController(PlatformService platform) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet("registration-status")]
    public IActionResult Status() => Ok(new { user = platform.View(platform.User(Actor)!), reviews = platform.Store.Read(s => s.Reviews.Where(r => r.UserId == Actor && r.Kind == "registration").ToArray()) });
    [HttpPost("registration-resubmission"), Permission("owner:access"), EnableRateLimiting("auth")]
    public IActionResult ResubmitRegistration(RegistrationResubmissionRequest request) => Ok(platform.ResubmitRegistration(Actor, request.Note));
    [HttpGet("places"), Permission("owner:access")]
    public IActionResult Places() { platform.RequireOwner(Actor); return Ok(platform.Store.Read(s => s.Places.Where(p => p.OwnerId == Actor).ToArray())); }
    [HttpGet("submissions"), Permission("owner:access")]
    public IActionResult Submissions() { platform.RequireOwner(Actor); return Ok(platform.Store.Read(s => s.Reviews.Where(r => r.UserId == Actor && r.Kind == "submission").OrderByDescending(r => r.CreatedAt).ToArray())); }
    [HttpPost("submissions"), Permission("owner:submit_poi")]
    public IActionResult Submit(Place request) => Ok(platform.Submit(Actor, request));
    [HttpGet("notifications")]
    public IActionResult Notifications([FromQuery] bool unreadOnly = false)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(platform.Store.Read(s => s.Notifications.Where(n => n.UserId == Actor && (!unreadOnly || !n.Read)).OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Take(100).ToArray()));
    }
    [HttpGet("notifications/unread-count")]
    public IActionResult UnreadNotifications()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(platform.Store.Read(s => new { unread = s.Notifications.Count(n => n.UserId == Actor && !n.Read) }));
    }
    [HttpGet("notifications/{id}")]
    public IActionResult Notification(string id)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(platform.Store.Read(s => {
            var notification = s.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == Actor)
                ?? throw new PlatformException(404, "Không tìm thấy thông báo.");
            // Check the owner on every related resource, even for old/imported notifications.
            var review = s.Reviews.FirstOrDefault(r => r.Id == notification.ReviewId && r.UserId == Actor);
            var place = s.Places.FirstOrDefault(p => p.Id == notification.PlaceId && p.OwnerId == Actor);
            return new { notification, review, place };
        }));
    }
    [HttpPost("notifications/{id}/read")]
    public IActionResult ReadNotification(string id)
    {
        platform.Store.Write(s => { var index = s.Notifications.FindIndex(n => n.Id == id && n.UserId == Actor); if (index < 0) throw new PlatformException(404, "Không tìm thấy thông báo."); s.Notifications[index] = s.Notifications[index] with { Read = true }; return true; }); return NoContent();
    }
    [HttpGet("analytics"), Permission("analytics:view_own")]
    public IActionResult Analytics()
    {
        platform.RequireOwner(Actor);
        return Ok(platform.Store.Read(s => s.Places.Where(p => p.OwnerId == Actor).Select(p => new { placeId = p.Id, name = p.Name, events = s.Events.Count(e => e.Kind != "heartbeat" && e.PlaceId == p.Id) }).ToArray()));
    }
    [HttpGet("places/{id:int}/menu"), Permission("menu:read")]
    public IActionResult Menu(int id) { EnsureOwned(id); return Ok(platform.Store.Read(s => s.Menus.Where(m => m.PlaceId == id).ToArray())); }
    [HttpPost("places/{id:int}/menu"), Permission("menu:create")]
    public IActionResult CreateMenu(int id, MenuItem item) { EnsureOwned(id); return Ok(platform.SaveMenu(Actor, item with { PlaceId = id }, true)); }
    [HttpPut("places/{id:int}/menu/{itemId}"), Permission("menu:update")]
    public IActionResult UpdateMenu(int id, string itemId, MenuItem item) { EnsureOwned(id); return Ok(platform.SaveMenu(Actor, item with { PlaceId = id, Id = itemId }, false)); }
    private void EnsureOwned(int id)
    {
        platform.RequireOwner(Actor);
        if (!platform.Store.Read(s => s.Places.Any(p => p.Id == id && p.OwnerId == Actor))) throw new PlatformException(403, "Chỉ được quản lý món ăn của địa điểm thuộc bạn.");
    }
}
public record RegistrationResubmissionRequest(string Note);
