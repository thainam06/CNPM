using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ShortVideoTranslator.Business;

public record TranslationResult(string Text, string Language, string Provider, bool IsMachineTranslation);
public class TranslationUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
public class TranslationOptions
{
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 25;
}

public class TranslationService(HttpClient client, TranslationOptions options)
{
    private readonly ConcurrentDictionary<(string Text, string Language), TranslationResult> _cache = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<TranslationResult> TranslateAsync(string text, string language, CancellationToken cancellationToken = default)
    {
        NarrationLanguages.ValidateText(text);
        NarrationLanguages.Validate(language);
        cancellationToken.ThrowIfCancellationRequested();
        if (language == "vi") return new(text, language, "original", false);
        if (!options.Enabled) throw new TranslationUnavailableException("Dịch đa ngôn ngữ chưa sẵn sàng: dịch vụ đang tắt.");
        var key = (text, language);
        if (_cache.TryGetValue(key, out var cached)) return cached;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached)) return cached;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
            var translated = new List<string>();
            foreach (var chunk in SplitText(text))
            {
                // MyMemory accepts at most 500 UTF-8 bytes per query, no key needed.
                var target = language == "zh" ? "zh-CN" : language;
                var uri = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(chunk)}&langpair={Uri.EscapeDataString($"vi|{target}")}&mt=1";
                using var response = await client.GetAsync(uri, timeout.Token);
                if (!response.IsSuccessStatusCode)
                    throw new TranslationUnavailableException("Dịch đa ngôn ngữ tạm thời chưa sẵn sàng: dịch vụ dịch lỗi hoặc giới hạn truy cập.");
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                var root = json.RootElement;
                if (!root.TryGetProperty("responseStatus", out var status) || status.ToString() != "200" ||
                    (root.TryGetProperty("quotaFinished", out var quota) && quota.ValueKind == JsonValueKind.True))
                    throw new TranslationUnavailableException("Dịch đa ngôn ngữ chưa sẵn sàng: MyMemory hết hạn mức hoặc không hỗ trợ yêu cầu này.");
                if (!root.TryGetProperty("responseData", out var data) ||
                    !data.TryGetProperty("translatedText", out var value) || value.ValueKind != JsonValueKind.String)
                    throw new TranslationUnavailableException("Dịch vụ không trả về bản dịch hợp lệ.");
                var result = WebUtility.HtmlDecode(value.GetString())?.Trim();
                if (string.IsNullOrWhiteSpace(result) || string.Equals(result, chunk.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new TranslationUnavailableException("Dịch vụ chưa tạo được bản dịch cho ngôn ngữ này. Vui lòng thử lại hoặc chọn Tiếng Việt.");
                translated.Add(result);
            }
            var output = string.Join(" ", translated);
            if (output.Length > 3000) throw new TranslationUnavailableException("Bản dịch vượt giới hạn 3000 ký tự của thuyết minh.");
            var translation = new TranslationResult(output, language, "MyMemory", true);
            // Bound memory use; translations are only cached for this server session.
            if (_cache.Count >= 128) _cache.Clear();
            _cache[key] = translation;
            return translation;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TranslationUnavailableException("Dịch vụ dịch phản hồi quá lâu. Vui lòng thử lại.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            throw new TranslationUnavailableException("Không kết nối được dịch vụ dịch hoặc bản dịch không hợp lệ. Vui lòng thử lại.", ex);
        }
        finally { _gate.Release(); }
    }

    public static IReadOnlyList<string> SplitText(string text)
    {
        // Split at word boundaries where possible; preserve Unicode scalar values.
        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var rune in text.EnumerateRunes())
        {
            if (Encoding.UTF8.GetByteCount(current.ToString()) + rune.Utf8SequenceLength > 480)
            {
                var buffer = current.ToString();
                var boundary = buffer.LastIndexOf(' ');
                if (boundary > 0)
                {
                    chunks.Add(buffer[..boundary]);
                    current.Clear().Append(buffer[(boundary + 1)..]);
                }
                else { chunks.Add(buffer); current.Clear(); }
            }
            current.Append(rune.ToString());
        }
        if (current.Length > 0) chunks.Add(current.ToString());
        return chunks;
    }
}
