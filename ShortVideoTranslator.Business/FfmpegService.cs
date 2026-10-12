
using System.Diagnostics;

namespace ShortVideoTranslator.Business;

public class FfmpegService
{
    public async Task<string> ExtractAudioAsync(string videoPath)
    {
        string audioPath = Path.ChangeExtension(videoPath, ".wav");

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in new[]
        {
            "-y", "-i", videoPath,
            "-vn", "-ac", "1",
            "-ar", "16000",
            "-c:a", "pcm_s16le",
            audioPath
        })
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        string errors =
            await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(
                "FFmpeg error: " + errors);
        }

        return audioPath;
    }
}
