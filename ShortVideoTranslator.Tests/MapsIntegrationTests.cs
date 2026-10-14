using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ShortVideoTranslator.Api.Services;

namespace ShortVideoTranslator.Tests;

public class MapsIntegrationTests
{
    [Fact]
    public async Task MissingOrCorruptPackNeverReportsReady()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/maps/pack-manifest")).StatusCode);
        var options = await client.GetFromJsonAsync<JsonElement>("/api/maps/offline-options");
        Assert.False(options.GetProperty("ready").GetBoolean());
        var folder = Path.Combine(app.Folder, "maps"); Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "pack.json"), "invalid json");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/maps/pack-manifest")).StatusCode);
    }
    [Fact]
    public async Task VerifiedManifestRangeVersionAndPathGuardsAreEnforced()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var folder = Path.Combine(app.Folder, "maps"); Directory.CreateDirectory(folder);
        // A header fixture tests serving guards; it is not a renderable geographic dataset.
        var bytes = new byte[127]; "PMTiles"u8.CopyTo(bytes); bytes[7] = 3;
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var descriptor = new MapPackDescriptor("test-v1", "https://example.com/source.pmtiles", "Test fixture", "Test source",
            [106.68, 10.75, 106.73, 10.78], hash, bytes.Length);
        await File.WriteAllBytesAsync(Path.Combine(folder, "district1.pmtiles"), bytes);
        await File.WriteAllTextAsync(Path.Combine(folder, "pack.json"), JsonSerializer.Serialize(descriptor));
        var manifest = await client.GetFromJsonAsync<JsonElement>("/api/maps/pack-manifest");
        Assert.Equal(hash, manifest.GetProperty("sha256").GetString());
        using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, manifest.GetProperty("url").GetString());
        rangeRequest.Headers.Range = new(0, 7);
        var range = await client.SendAsync(rangeRequest);
        Assert.Equal(HttpStatusCode.PartialContent, range.StatusCode);
        Assert.Equal(bytes[..8], await range.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/maps/files/district1.pmtiles?v=wrong")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/maps/files/platform.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/maps/files/%2e%2e%2fplatform.json")).StatusCode);
        var original = await client.GetAsync("/api/maps/pack-manifest");
        await File.WriteAllTextAsync(Path.Combine(folder, "pack.json"), JsonSerializer.Serialize(descriptor with { Version = "test-v2" }));
        using var conditional = new HttpRequestMessage(HttpMethod.Get, "/api/maps/pack-manifest");
        conditional.Headers.IfNoneMatch.Add(original.Headers.ETag!);
        var changed = await client.SendAsync(conditional);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.NotEqual(original.Headers.ETag, changed.Headers.ETag);
        bytes[20] = 1; await File.WriteAllBytesAsync(Path.Combine(folder, "district1.pmtiles"), bytes);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/maps/pack-manifest")).StatusCode);
    }
}
