
using System.Diagnostics;

namespace ShortVideoTranslator.Business;

public class TextToSpeechService
{
    private readonly TextToSpeechOptions _options;
    public TextToSpeechService(TextToSpeechOptions? options = null) => _options = options ?? new();

    public async Task<byte[]> GenerateSpeechAsync(
        string text,
        string language = "vi",
        CancellationToken cancellationToken = default)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        try
        {
            try { return await GenerateSpeechOnceAsync(text, language, budget.Token); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("NoAudioReceived", StringComparison.Ordinal))
            {
                // Edge's online endpoint occasionally returns an empty stream. Retry once.
                await Task.Delay(750, budget.Token);
                return await GenerateSpeechOnceAsync(text, language, budget.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new TimeoutException("Tạo âm thanh quá thời gian cho phép."); }
    }

    private async Task<byte[]> GenerateSpeechOnceAsync(string text, string language, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Nội dung không được để trống.");
        if (text.Length > 3000)
            throw new ArgumentException("Nội dung tối đa 3000 ký tự.");
        cancellationToken.ThrowIfCancellationRequested();

        string voice = language switch
        {
            "vi" => "vi-VN-HoaiMyNeural",
            "en" => "en-US-JennyNeural",
            "zh" => "zh-CN-XiaoxiaoNeural",
            "ja" => "ja-JP-NanamiNeural",
            "ko" => "ko-KR-SunHiNeural",
            "fr" => "fr-FR-DeniseNeural",
            _ => throw new ArgumentException("Ngôn ngữ chưa được hỗ trợ.")
        };

        string outputPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.mp3");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _options.PythonExecutable,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            foreach (var argument in _options.PythonArguments)
                processInfo.ArgumentList.Add(argument);
            processInfo.ArgumentList.Add("-m");
            processInfo.ArgumentList.Add("edge_tts");
            processInfo.ArgumentList.Add("--voice");
            processInfo.ArgumentList.Add(voice);
            processInfo.ArgumentList.Add("--text");
            processInfo.ArgumentList.Add(text);
            processInfo.ArgumentList.Add("--write-media");
            processInfo.ArgumentList.Add(outputPath);

            using var process = new Process
            {
                StartInfo = processInfo
            };

            process.Start();

            Task<string> errorsTask =
                process.StandardError.ReadToEndAsync();

            Task<string> outputTask =
                process.StandardOutput.ReadToEndAsync();

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(errorsTask, outputTask);
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException("Tạo âm thanh quá thời gian cho phép.");
            }

            string errors = await errorsTask;
            await outputTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException("Edge-TTS lỗi: " + errors);

            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Không tạo được MP3.");

            byte[] audio = await File.ReadAllBytesAsync(outputPath, cancellationToken);

            if (audio.Length == 0)
                throw new InvalidOperationException("File âm thanh rỗng.");

            return audio;
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}

public class TextToSpeechOptions
{
    public string PythonExecutable { get; set; } = OperatingSystem.IsWindows() ? "py" : "python3";
    public string[] PythonArguments { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 90;
}
