using System.Text.Json;
using ShortVideoTranslator.Business;
namespace ShortVideoTranslator.Api.Services;

public record StorageReadiness(bool Ready, DateTimeOffset CheckedAt, string Message);
public sealed class SystemReadinessService(IConfiguration config, IWebHostEnvironment environment, IPlatformStore store, TimeProvider clock)
{
    private readonly object gate = new();
    private StorageReadiness? cached;
    public StorageReadiness Check()
    {
        lock (gate) {
            if (cached != null && clock.GetUtcNow() - cached.CheckedAt < TimeSpan.FromSeconds(15)) return cached;
            try {
                var folder = config["Storage:Directory"] ?? Path.Combine(environment.ContentRootPath, "App_Data");
                var snapshotFile = Path.Combine(folder, "platform.json");
                if (store is not ShortVideoTranslator.Data.SqlitePlatformStore) {
                using var json = JsonDocument.Parse(File.ReadAllText(snapshotFile));
                if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("places", out var records) || records.ValueKind != JsonValueKind.Array || !json.RootElement.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number) throw new InvalidDataException();
                if ((File.GetAttributes(snapshotFile) & FileAttributes.ReadOnly) != 0) throw new UnauthorizedAccessException();
                }
                store.Read(s => s.Version);
                // Own unique probe only; never mutate the platform snapshot or its catalog version.
                var probe = Path.Combine(folder, $"readiness-{Guid.NewGuid():N}.tmp");
                using (var file = new FileStream(probe, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose)) {
                    file.WriteByte(1); file.Flush(true); file.Position = 0;
                    if (file.ReadByte() != 1) throw new IOException();
                }
                return cached = new(true, clock.GetUtcNow(), store is ShortVideoTranslator.Data.SqlitePlatformStore ? "SQLite đọc được; thư mục lưu trữ ghi được." : "Kho JSON đọc được; thư mục lưu trữ ghi được. Demo một tiến trình.");
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException) {
                return cached = new(false, clock.GetUtcNow(), "Kho dữ liệu chưa sẵn sàng: kiểm tra database/tệp dữ liệu, quyền đọc/ghi và dung lượng trên máy chủ.");
            }
        }
    }
}
