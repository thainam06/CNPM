using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
public class OwnerIdentityController(OwnerIdentityService identities, PlatformService platform) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpPut("api/owner/identity"), Permission("owner:access")]
    public IActionResult Save(IdentityRequest request) { identities.Save(Actor, request.Number); Response.Headers.CacheControl = "no-store"; return NoContent(); }
    [HttpGet("api/owner/identity"), Permission("owner:access")]
    public IActionResult Own()
    {
        var number = identities.Read(Actor); Response.Headers.CacheControl = "no-store";
        return Ok(new { present = number != null, masked = number == null ? null : "********" + number[^4..], expiresAt = identities.ExpiresAt(Actor) });
    }
    [HttpDelete("api/owner/identity"), Permission("owner:access")]
    public IActionResult Delete() { identities.Delete(Actor); return NoContent(); }
    [HttpGet("api/admin/users/{id}/identity"), Permission("user:read"), Permission("poi:approve")]
    public IActionResult Reveal(string id)
    {
        if (platform.User(id)?.Role != "poi_owner") return NotFound();
        var number = identities.Read(id); Response.Headers.CacheControl = "no-store";
        platform.Store.Write(s => { PlatformService.Audit(s, Actor, "identity.read", id); return true; });
        return Ok(new { present = number != null, number, expiresAt = identities.ExpiresAt(id) });
    }
}
public record IdentityRequest(string Number);
