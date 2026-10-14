using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Services;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/maps")]
public class MapsController(MapPackService packs) : ControllerBase
{
    private string Folder => packs.Folder;
    [HttpGet("offline-options")]
    public IActionResult Options() => Ok(new { ready = packs.ReadVerified() != null,
        message = packs.ReadVerified() != null ? "Gói Quận 1 đã được xác minh; tải về thiết bị để dùng offline." : "Gói bản đồ chưa sẵn sàng: cần PMTiles v3 và pack.json có nguồn, giấy phép, phạm vi và checksum đúng." });
    [HttpGet("pack-manifest")]
    public IActionResult Manifest()
    {
        var pack = packs.ReadVerified();
        if (pack == null) return StatusCode(503, new { message = "Gói bản đồ offline chưa có hoặc không khớp định dạng/checksum/nguồn." });
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(pack, new JsonSerializerOptions(JsonSerializerDefaults.Web))));
        Response.Headers.CacheControl = "no-cache"; Response.Headers.ETag = $"\"{fingerprint}\"";
        if (Request.Headers.IfNoneMatch.Contains(Response.Headers.ETag.ToString())) return StatusCode(304);
        return Ok(new { scope = "district1", fingerprint, pack.Bytes, pack.Sha256, pack.Version, pack.SourceUrl, pack.License, pack.Attribution, pack.Bounds,
            pack.MinZoom, pack.MaxZoom, url = $"/api/maps/files/district1.pmtiles?v={pack.Sha256}" });
    }
    [HttpGet("files/{name}")]
    public IActionResult FileResource(string name)
    {
        if (!Regex.IsMatch(name, "^[a-zA-Z0-9_-]+\\.(pmtiles|json|pbf|png)$")) return NotFound();
        var path = Path.GetFullPath(Path.Combine(Folder, name));
        if (!path.StartsWith(Path.GetFullPath(Folder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path)) return NotFound();
        if (name.EndsWith(".pmtiles")) {
            var pack = packs.ReadVerified();
            if (name != "district1.pmtiles" || pack == null || (Request.Query.ContainsKey("v") && Request.Query["v"] != pack.Sha256)) return NotFound();
            Response.Headers.ETag = $"\"{pack.Sha256}\"";
        }
        Response.Headers.CacheControl = Request.Query.ContainsKey("v") ? "public,max-age=31536000,immutable" : "no-cache";
        return PhysicalFile(path, name.EndsWith(".json") ? "application/json" : "application/octet-stream", enableRangeProcessing: true);
    }
}
