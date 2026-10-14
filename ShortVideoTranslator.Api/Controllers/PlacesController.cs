using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
[Route("api/places")]
public class PlacesController(PlaceService places, IPlatformStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<Place>> GetAll() => Ok(places.GetAll());

    [HttpGet("{id:int}")]
    public ActionResult<Place> GetById(int id) => places.GetById(id) is { } place
        ? Ok(place) : NotFound(new { message = "Không tìm thấy địa điểm." });

    [HttpGet("snapshot")]
    public IActionResult Snapshot()
    {
        var version = store.Read(s => s.Version);
        var etag = $"\"places-{version}\"";
        Response.Headers.ETag = etag;
        if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(304);
        return Ok(new { version, places = places.GetAll(), updatedAt = store.Read(s => s.UpdatedAt) });
    }

    [HttpGet("{id:int}/menu")]
    public IActionResult Menu(int id) => places.GetById(id) == null ? NotFound() : Ok(store.Read(s => s.Menus.Where(m => m.PlaceId == id).ToArray()));

    [HttpGet("nearby")]
    public IActionResult Nearby([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] int radius = 1500)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || latitude is < -90 or > 90 || longitude is < -180 or > 180 || radius is < 1 or > 10000) return BadRequest();
        var result = places.GetAll().Where(p => p.Latitude != null && p.Longitude != null)
            .Select(p => new { place = p, distance = Distance(latitude, longitude, p.Latitude!.Value, p.Longitude!.Value) }).Where(p => p.distance <= radius).OrderBy(p => p.distance).Take(10);
        return Ok(result);
    }
    private static double Distance(double lat1, double lon1, double lat2, double lon2)
    {
        const double radians = Math.PI / 180;
        var a = Math.Pow(Math.Sin((lat2 - lat1) * radians / 2), 2) + Math.Cos(lat1 * radians) * Math.Cos(lat2 * radians) * Math.Pow(Math.Sin((lon2 - lon1) * radians / 2), 2);
        return 6371000 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
