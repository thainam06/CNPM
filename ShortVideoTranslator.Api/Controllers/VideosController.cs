
using Microsoft.AspNetCore.Mvc;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
[Route("api/videos")]
public class VideosController : ControllerBase
{
    
[HttpPost("extract-audio")]
[Consumes("multipart/form-data")]
[RequestSizeLimit(55 * 1024 * 1024)]
public async Task<IActionResult> ExtractAudio(
    [FromForm] IFormFile file,
    [FromServices]
    ShortVideoTranslator.Business.FfmpegService ffmpeg)
{
    if (file == null || file.Length == 0)
        return BadRequest("Vui lòng chọn video.");

    if (file.Length > 50 * 1024 * 1024 ||
        Path.GetExtension(file.FileName).ToLowerInvariant() != ".mp4")
        return BadRequest("Chỉ nhận MP4 tối đa 50 MB.");

    string folder = Path.Combine(
        Path.GetTempPath(),
        "ShortVideoTranslator",
        "audio-tests",
        Guid.NewGuid().ToString("N"));

    Directory.CreateDirectory(folder);

    string videoPath = Path.Combine(folder, "input.mp4");

    await using (var stream =
        System.IO.File.Create(videoPath))
    {
        await file.CopyToAsync(stream);
    }

    string audioPath =
        await ffmpeg.ExtractAudioAsync(videoPath);

    byte[] audioBytes =
        await System.IO.File.ReadAllBytesAsync(audioPath);

    Directory.Delete(folder, recursive: true);

    return File(
        audioBytes,
        "audio/wav",
        "extracted-audio.wav");
}
    
[HttpPost("transcribe")]
[Consumes("multipart/form-data")]
[RequestSizeLimit(55 * 1024 * 1024)]
public async Task<IActionResult> Transcribe(
    [FromForm] IFormFile file,
    [FromServices]
    ShortVideoTranslator.Business.FfmpegService ffmpeg,
    [FromServices]
    ShortVideoTranslator.Business.SpeechService speech)
{
    if (file == null || file.Length == 0)
        return BadRequest("Vui lòng chọn video.");

    if (file.Length > 50 * 1024 * 1024 ||
        Path.GetExtension(file.FileName).ToLowerInvariant() != ".mp4")
        return BadRequest("Chỉ hỗ trợ MP4 tối đa 50 MB.");

    string folder = Path.Combine(
        Path.GetTempPath(),
        "ShortVideoTranslator",
        Guid.NewGuid().ToString("N"));

    Directory.CreateDirectory(folder);

    try
    {
        string videoPath = Path.Combine(folder, "input.mp4");

        await using (var stream =
            System.IO.File.Create(videoPath))
        {
            await file.CopyToAsync(stream);
        }

        string audioPath =
            await ffmpeg.ExtractAudioAsync(videoPath);

        var segments =
            await speech.TranscribeAsync(audioPath);

        return Ok(new
        {
            message = "Nhận diện giọng nói hoàn tất",
            language = "vi",
            segments
        });
    }
    finally
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }
}

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(55 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Vui lòng chọn video.");

        if (file.Length > 50 * 1024 * 1024)
            return BadRequest("Video vượt quá 50 MB.");

        if (Path.GetExtension(file.FileName)
            .ToLowerInvariant() != ".mp4")
            return BadRequest("Chỉ hỗ trợ MP4.");

        string folder = Path.Combine(
            Path.GetTempPath(),
            "ShortVideoTranslator",
            "uploads");

        Directory.CreateDirectory(folder);

        string fileName =
            Guid.NewGuid().ToString("N") + ".mp4";

        string path = Path.Combine(folder, fileName);

        await using var stream =
            System.IO.File.Create(path);

        await file.CopyToAsync(stream);

        return Ok(new
        {
            message = "Upload video thành công!",
            fileName,
            size = file.Length
        });
    }
}
