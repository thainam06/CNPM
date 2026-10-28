using System.Diagnostics;

namespace ShortVideoTranslator.Business;

public record PreviewVoice(string Id, string Label);
public record PreviewLanguage(string Code, string Name, IReadOnlyList<PreviewVoice> Voices);

// Preview-only choices. Public narration, published assets and cache keys are unchanged.
public static class VoicePreview
{
    public static IReadOnlyList<PreviewLanguage> Languages { get; } = Array.AsReadOnly(new[] {
        Entry("vi", "Tiếng Việt", "vi-VN-HoaiMyNeural", "vi-VN-NamMinhNeural"),
        Entry("en", "English", "en-US-JennyNeural", "en-US-GuyNeural"),
        Entry("zh", "中文", "zh-CN-XiaoxiaoNeural", "zh-CN-YunxiNeural"),
        Entry("ja", "日本語", "ja-JP-NanamiNeural", "ja-JP-KeitaNeural"),
        Entry("ko", "한국어", "ko-KR-SunHiNeural", "ko-KR-InJoonNeural"),
        Entry("fr", "Français", "fr-FR-DeniseNeural", "fr-FR-HenriNeural")
    });
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PreviewLanguage Entry(string code, string name, string female, string male) =>
        new(code, name, Array.AsReadOnly(new[] { new PreviewVoice(female, "Nữ"), new PreviewVoice(male, "Nam") }));

    public static void Validate(string text, string language, string voice)
    {
        NarrationLanguages.ValidateText(text);
        NarrationLanguages.Validate(language);
        if (!Languages.Any(item => item.Code == language && item.Voices.Any(choice => choice.Id == voice)))
            throw new ArgumentException("Giọng đọc không thuộc ngôn ngữ đã chọn.");
    }

    public static async Task<byte[]> GenerateAsync(TextToSpeechOptions options, string text, string language, string voice, CancellationToken token)
    {
        Validate(text, language, voice);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        var entered = false;
        var output = Path.Combine(Path.GetTempPath(), $"smarttour-preview-{Guid.NewGuid():N}.mp3");
        try {
            await Gate.WaitAsync(budget.Token); entered = true;
            var start = new ProcessStartInfo(options.PythonExecutable) {
                UseShellExecute = false, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true
            };
            foreach (var argument in options.PythonArguments) start.ArgumentList.Add(argument);
            foreach (var argument in new[] { "-m", "edge_tts", "--voice", voice, "--text", text, "--write-media", output })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Không khởi động được công cụ nghe thử.");
            var errors = process.StandardError.ReadToEndAsync();
            var stdout = process.StandardOutput.ReadToEndAsync();
            try { await process.WaitForExitAsync(budget.Token); }
            catch (OperationCanceledException) {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(errors, stdout);
                throw;
            }
            await Task.WhenAll(errors, stdout);
            if (process.ExitCode != 0 || !File.Exists(output) || new FileInfo(output).Length == 0)
                throw new InvalidOperationException("Chưa tạo được âm thanh nghe thử.");
            return await File.ReadAllBytesAsync(output, budget.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) {
            throw new TimeoutException("Nghe thử quá thời gian cho phép.");
        }
        finally {
            if (File.Exists(output)) File.Delete(output);
            if (entered) Gate.Release();
        }
    }
}
