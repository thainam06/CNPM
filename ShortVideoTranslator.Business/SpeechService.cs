
using Whisper.net;

namespace ShortVideoTranslator.Business;

public class SpeechService
{
    private readonly string _modelPath;


    public SpeechService()
    {
        _modelPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "models",
            "ggml-base.bin");

        _modelPath = Path.GetFullPath(_modelPath);
    }


    public async Task<List<SpeechSegment>>
        TranscribeAsync(string audioPath)
    {
        if (!File.Exists(_modelPath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy mô hình Whisper",
                _modelPath);
        }

        var result = new List<SpeechSegment>();

        using var factory =
            WhisperFactory.FromPath(_modelPath);

        using var processor = factory
            .CreateBuilder()
            .WithLanguage("vi")
            .Build();

        await using var audioStream =
            File.OpenRead(audioPath);

        await foreach (var segment in
            processor.ProcessAsync(audioStream))
        {
            result.Add(new SpeechSegment(
                segment.Start,
                segment.End,
                segment.Text.Trim()));
        }

        return result;
    }
}

public record SpeechSegment(
    TimeSpan Start,
    TimeSpan End,
    string Text);
