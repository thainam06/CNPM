using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/ui-bundles")]
public class UiBundlesController : ControllerBase
{
    private static readonly byte[] BundleBytes = ReadContent();
    private static readonly string SourceHash = Convert.ToHexStringLower(SHA256.HashData(BundleBytes));
    private static byte[] ReadContent() { using var stream = typeof(Place).Assembly.GetManifestResourceStream("SmartTour.PublicUi.json")!; using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray(); }
    [HttpGet("{locale}")]
    public IActionResult Get(string locale)
    {
        using var document = JsonDocument.Parse(BundleBytes);
        var supported = document.RootElement.TryGetProperty(locale, out var bundle);
        if (!supported) bundle = document.RootElement.GetProperty("en");
        Response.Headers.ETag = $"\"ui-{SourceHash}-{locale}\"";
        return Ok(new { locale = supported ? locale : "en", requestedLocale = locale, status = supported ? "ready" : "unavailable", sourceHash = SourceHash, @namespace = "public-ui", strings = bundle.Clone() });
    }
}
