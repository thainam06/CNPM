using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/admin/voice-preview"), Permission("poi:update")]
public sealed class VoicePreviewController(TextToSpeechOptions options) : ControllerBase
{
    [HttpGet]
    public IActionResult Choices()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(VoicePreview.Languages);
    }

    [HttpPost, EnableRateLimiting("speech")]
    public async Task<IActionResult> Preview(VoicePreviewRequest request, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try {
            var audio = await VoicePreview.GenerateAsync(options, request.Text, request.Language, request.Voice, token);
            return File(audio, "audio/mpeg");
        }
        catch (Exception error) when (error is InvalidOperationException or TimeoutException or IOException or System.ComponentModel.Win32Exception) {
            return StatusCode(503, new { message = "Chưa nghe thử được. Kiểm tra Python, edge-tts và kết nối Internet rồi thử lại." });
        }
    }
}
public record VoicePreviewRequest(string Text, string Language, string Voice);
