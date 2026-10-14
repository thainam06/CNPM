using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ShortVideoTranslator.Api.Services;

public record MapPackDescriptor(string Version, string SourceUrl, string License, string Attribution,
    double[] Bounds, string Sha256, long Bytes, int MinZoom = 0, int MaxZoom = 15);
public class MapPackService(IConfiguration configuration, IWebHostEnvironment environment)
{
    public string Folder => Path.Combine(configuration["Storage:Directory"] ?? Path.Combine(environment.ContentRootPath, "App_Data"), "maps");
    public string ArchivePath => Path.Combine(Folder, "district1.pmtiles");
    public MapPackDescriptor? ReadVerified()
    {
        try {
            var descriptor = JsonSerializer.Deserialize<MapPackDescriptor>(File.ReadAllText(Path.Combine(Folder, "pack.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.Version) || string.IsNullOrWhiteSpace(descriptor.License) ||
                string.IsNullOrWhiteSpace(descriptor.Attribution) || !Uri.TryCreate(descriptor.SourceUrl, UriKind.Absolute, out var source) || source.Scheme != "https" ||
                descriptor.Bounds is not { Length: 4 } bounds || bounds.Any(b => !double.IsFinite(b)) || bounds[0] >= bounds[2] || bounds[1] >= bounds[3] ||
                bounds[0] < -180 || bounds[2] > 180 || bounds[1] < -90 || bounds[3] > 90 || descriptor.Bytes is < 127 or > 104857600) return null;
            using var archive = File.OpenRead(ArchivePath);
            if (archive.Length != descriptor.Bytes) return null;
            var header = new byte[8]; archive.ReadExactly(header);
            if (Encoding.ASCII.GetString(header, 0, 7) != "PMTiles" || header[7] != 3) return null;
            archive.Position = 0;
            return Convert.ToHexStringLower(SHA256.HashData(archive)) == descriptor.Sha256 ? descriptor : null;
        } catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return null; }
    }
}
