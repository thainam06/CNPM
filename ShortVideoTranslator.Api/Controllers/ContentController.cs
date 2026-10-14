using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Api.Services;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
[Route("api/content")]
[Route("api/v1/poi")]
public class ContentController(IPlatformStore store, PlaceService places, NarrationAssetService assets) : ControllerBase
{
    [HttpGet]
    public IActionResult Load([FromQuery] string language = "vi", [FromQuery] DateTimeOffset? since = null)
    {
        NarrationLanguages.Validate(language);
        // Snapshot and public projection use the same lock/version; do not mix concurrent edits.
        return store.Read(s => {
            var etag = $"\"content-{s.Version}-{language}\"";
            Response.Headers.ETag = etag;
            if (Request.Headers.IfNoneMatch.Contains(etag)) return (IActionResult)StatusCode(304);
            var delta = since.HasValue && since >= DateTimeOffset.UtcNow.AddDays(-30) && since <= s.UpdatedAt;
            var catalog = places.GetAll().Where(p => !delta || (p.UpdatedAt ?? DateTimeOffset.MinValue) > since).Select(p => {
                var target = assets.Find(p.Description, language);
                var selected = target ?? (language == "vi" ? null : assets.Find(p.Description, "en"));
                return new { place = p, text = selected?.Text ?? p.Description, requestedLanguage = language,
                    contentLanguage = selected?.Language ?? "vi", fallback = language != (selected?.Language ?? "vi"), audioUrl = selected?.AudioUrl };
            }).ToArray();
            var removed = delta ? s.DeletedPlaces.Where(p => p.DeletedAt > since).Select(p => p.Id)
                .Concat(s.Places.Where(p => (!p.IsActive || !TourRegion.Includes(p)) && p.UpdatedAt > since).Select(p => p.Id)).Distinct().ToArray() : [];
            return Ok(new { region = TourRegion.Current, version = s.Version, updatedAt = s.UpdatedAt, syncMode = delta ? "delta" : "full", items = catalog, removedIds = removed });
        });
    }
}
