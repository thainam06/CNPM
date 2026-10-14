
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Business;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Api.Services;
using System.Security.Claims;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
[Route("api/narration")]
public class NarrationController : ControllerBase
{
    private readonly TextToSpeechService _ttsService;
    private readonly TranslationService _translation;
    private readonly ILogger<NarrationController> _logger;
    private readonly NarrationAssetService _assets;
    private readonly PlaceService _places;
    private readonly PlatformService _platform;

    public NarrationController(TextToSpeechService ttsService, TranslationService translation, ILogger<NarrationController> logger, NarrationAssetService assets, PlaceService places, PlatformService platform)
    {
        _ttsService = ttsService;
        _translation = translation;
        _logger = logger;
        _assets = assets;
        _places = places; _platform = platform;
    }

    [HttpPost("speech")]
    [EnableRateLimiting("speech")]
    public async Task<IActionResult> GenerateSpeech(
        [FromBody] SpeechRequest request, CancellationToken cancellationToken)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new
            {
                message = "Nội dung thuyết minh không được để trống."
            });
        }

        if (request.Text.Length > 3000)
        {
            return BadRequest(new
            {
                message = "Nội dung tối đa 3000 ký tự."
            });
        }

        try
        {
            var language = request.Language ?? "vi";
            var asset = await _assets.GenerateAsync(request.Text, language, cancellationToken);
            Response.Headers["X-Audio-Url"] = asset.AudioUrl;
            return PhysicalFile(_assets.AudioPath(asset.Key)!, "audio/mpeg", "narration.mp3", enableRangeProcessing: true);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (TranslationUnavailableException ex)
        {
            _logger.LogWarning(ex, "Translation unavailable");
            return StatusCode(503, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Edge-TTS generation failed");

            return StatusCode(503, new
            {
                message = "Không thể tạo MP3 bằng Edge-TTS. Kiểm tra Python, edge-tts và kết nối Internet rồi thử lại."
            });
        }

    }

    [HttpPost("translate")]
    [EnableRateLimiting("speech")]
    public async Task<IActionResult> Translate([FromBody] SpeechRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var language = request.Language ?? "vi";
            NarrationLanguages.ValidateText(request.Text); NarrationLanguages.Validate(language);
            var cached = _assets.Find(request.Text, language);
            return Ok(cached == null ? await _translation.TranslateAsync(request.Text, language, cancellationToken)
                : new TranslationResult(cached.Text, language, language == "vi" ? "original" : "MyMemory (cached audio)", language != "vi"));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return new EmptyResult(); }
        catch (TranslationUnavailableException ex)
        {
            _logger.LogWarning(ex, "Translation unavailable");
            return StatusCode(503, new { message = ex.Message });
        }
    }

    [HttpGet("audio/{key}")]
    public IActionResult Audio(string key)
    {
        var asset = _assets.FindByKey(key); if (asset == null) return NotFound();
        var isPublic = _places.GetAll().Any(p => NarrationAssetService.KeyFor(p.Description, asset.Language) == key);
        if (!isPublic && !_platform.HasPermission(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", "poi:update")) return NotFound();
        var path = _assets.AudioPath(key)!;
        Response.Headers.CacheControl = isPublic ? "public,max-age=31536000,immutable" : "private,no-store";
        return PhysicalFile(path, "audio/mpeg", enableRangeProcessing: true);
    }

    [HttpGet("pack-manifest")]
    public IActionResult Manifest([FromQuery] string language = "vi")
    {
        try {
            NarrationLanguages.Validate(language);
            // A public manifest must never enumerate drafts or arbitrary /speech request text.
            var files = _places.GetAll().Select(p => _assets.Find(p.Description, language)).Where(p => p != null).Cast<NarrationAsset>().ToArray();
            return Ok(new { language, totalFiles = files.Length, totalBytes = files.Sum(f => f.Bytes), files });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }
}

public class SpeechRequest
{
    public string Text { get; set; } = "";
    public string? Language { get; set; } = "vi";
}
