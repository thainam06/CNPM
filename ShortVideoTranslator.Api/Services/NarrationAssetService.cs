using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Services;

public record NarrationAsset(string Key, string Text, string Language, string AudioUrl, string Sha256, long Bytes, DateTimeOffset CreatedAt, string? Provider = null);
public class NarrationAssetService(TranslationService translation, TextToSpeechService tts, IWebHostEnvironment environment, IConfiguration configuration, TranslationReviewService? reviews = null)
{
    private readonly SemaphoreSlim _capacity = new(3, 3);
    private readonly SemaphoreSlim[] _locks = Enumerable.Range(0, 32).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    public string Folder => Path.Combine(configuration["Storage:Directory"] ?? Path.Combine(environment.ContentRootPath, "App_Data"), "audio");
    public static string KeyFor(string text, string language, string? revision = null) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(revision) ? $"v1:{language}:{text}" : $"v2:{language}:{revision}:{text}")));
    public string CurrentRevision(string text, string language) => reviews?.Current(text, language)?.Revision ?? "";
    public TranslationResult? ReviewedText(string text, string language) => reviews?.Current(text, language) is { } item ? new(item.Text, language, "reviewed", false) : null;
    public string CurrentKeyFor(string text, string language) => KeyFor(text, language, CurrentRevision(text, language));
    public NarrationAsset? FindByKey(string key)
    {
        if (AudioPath(key) is not { } audio) return null;
        try {
            var asset = JsonSerializer.Deserialize<NarrationAsset>(File.ReadAllText(Path.Combine(Folder, key + ".json")));
            return asset != null && NarrationLanguages.Codes.Contains(asset.Language) && ValidAsset(asset, audio, key) ? asset : null;
        } catch (Exception ex) when (ex is IOException or JsonException) { return null; }
    }
    public NarrationAsset? Find(string text, string language)
    {
        var key = CurrentKeyFor(text, language);
        var metadata = Path.Combine(Folder, key + ".json");
        try {
            if (AudioPath(key) is not { } audio || !File.Exists(metadata)) return null;
            var asset = JsonSerializer.Deserialize<NarrationAsset>(File.ReadAllText(metadata));
            var reviewed = reviews?.Current(text, language);
            return asset?.Language == language && (reviewed == null || (asset.Text == reviewed.Text && asset.Provider == "reviewed")) && ValidAsset(asset, audio, key) ? asset : null;
        } catch (Exception ex) when (ex is IOException or JsonException) { return null; }
    }
    public Task<NarrationAsset> GenerateAsync(string text, string language, CancellationToken token)
    {
        NarrationLanguages.ValidateText(text); NarrationLanguages.Validate(language);
        return GenerateCoreAsync(text, language, token, reviews?.Current(text, language));
    }
    public Task<NarrationAsset> PrepareReviewedAsync(string source, TranslationDraft draft, CancellationToken token) =>
        GenerateCoreAsync(source, draft.Language, token, new(draft.SourceHash, draft.Language, draft.Text, draft.Version, draft.ActorId, DateTimeOffset.UtcNow));
    private async Task<NarrationAsset> GenerateCoreAsync(string text, string language, CancellationToken token, ApprovedTranslation? reviewed)
    {
        NarrationLanguages.ValidateText(text); NarrationLanguages.Validate(language);
        var key = KeyFor(text, language, reviewed?.Revision);
        var gate = _locks[Convert.ToByte(key[..2], 16) % _locks.Length];
        await gate.WaitAsync(token);
        try
        {
            var metadata = Path.Combine(Folder, key + ".json");
            var audio = Path.Combine(Folder, key + ".mp3");
            if (File.Exists(metadata) && File.Exists(audio))
            {
                try {
                    var cached = JsonSerializer.Deserialize<NarrationAsset>(await File.ReadAllTextAsync(metadata, token));
                    if (cached?.Language == language && (reviewed == null || (cached.Text == reviewed.Text && cached.Provider == "reviewed")) && ValidAsset(cached, audio, key)) return cached;
                } catch (Exception ex) when (ex is IOException or JsonException) { /* Regenerate incomplete/corrupt cache, never loop on it. */ }
            }
            await _capacity.WaitAsync(token);
            try
            {
                var content = reviewed == null ? await translation.TranslateAsync(text, language, token) : new TranslationResult(reviewed.Text, language, "reviewed", false);
                var bytes = await tts.GenerateSpeechAsync(content.Text, language, token);
                token.ThrowIfCancellationRequested();
                var asset = new NarrationAsset(key, content.Text, language, $"/api/narration/audio/{key}",
                    Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.LongLength, DateTimeOffset.UtcNow, content.Provider);
                Directory.CreateDirectory(Folder);
                await File.WriteAllBytesAsync(audio + ".tmp", bytes, token);
                File.Move(audio + ".tmp", audio, true);
                await File.WriteAllTextAsync(metadata + ".tmp", JsonSerializer.Serialize(asset), token);
                File.Move(metadata + ".tmp", metadata, true);
                return asset;
            }
            finally { _capacity.Release(); }
        }
        finally { gate.Release(); }
    }
    public string? AudioPath(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length != 64 || key.Any(c => !Uri.IsHexDigit(c))) return null;
        var path = Path.Combine(Folder, key + ".mp3"); return File.Exists(path) ? path : null;
    }
    private static bool ValidAsset(NarrationAsset asset, string path, string key)
    {
        if (asset.Key != key || string.IsNullOrWhiteSpace(asset.Text) || asset.Text.Length > 3000 || asset.Bytes <= 0 || new FileInfo(path).Length != asset.Bytes) return false;
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream)) == asset.Sha256;
    }
    public NarrationAsset[] Manifest(string language)
    {
        NarrationLanguages.Validate(language);
        if (!Directory.Exists(Folder)) return [];
        return Directory.EnumerateFiles(Folder, "*.json").Select(path => {
            try { return JsonSerializer.Deserialize<NarrationAsset>(File.ReadAllText(path)); }
            catch (Exception ex) when (ex is JsonException or IOException) { return null; }
        }).Where(asset => {
            if (asset?.Language != language || AudioPath(asset.Key) is not { } path) return false;
            try { return ValidAsset(asset, path, asset.Key); } catch (IOException) { return false; }
        }).Cast<NarrationAsset>().ToArray();
    }
}
