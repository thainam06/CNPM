using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Security;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/media")]
public class MediaController(IWebHostEnvironment environment) : ControllerBase
{
    private string Folder => Path.Combine(environment.ContentRootPath, "App_Data", "media");
    [HttpPost, Permission("poi:update"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is < 12 or > 5 * 1024 * 1024) return BadRequest(new { message = "Ảnh tối đa 5 MB." });
        using var memory = new MemoryStream(); await file.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var extension = bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png" :
            bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg" :
            System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? ".webp" : null;
        if (extension == null) return BadRequest(new { message = "Chỉ nhận PNG, JPEG hoặc WebP; không nhận SVG/HTML." });
        Directory.CreateDirectory(Folder); var name = Guid.NewGuid().ToString("N") + extension;
        await System.IO.File.WriteAllBytesAsync(Path.Combine(Folder, name), bytes, cancellationToken);
        return Ok(new { url = "/api/media/" + name });
    }
    [HttpGet("{name}")]
    public IActionResult Get(string name)
    {
        if (!Regex.IsMatch(name, "^[a-f0-9]{32}\\.(png|jpg|webp)$")) return NotFound();
        var path = Path.Combine(Folder, name); if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = "public,max-age=86400";
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(path, name.EndsWith(".png") ? "image/png" : name.EndsWith(".jpg") ? "image/jpeg" : "image/webp");
    }
}
