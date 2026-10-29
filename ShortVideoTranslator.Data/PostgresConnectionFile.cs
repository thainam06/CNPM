using System.Text.Json;

namespace ShortVideoTranslator.Data;

public static class PostgresConnectionFile
{
    public static string Read(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("Chưa cấu hình PostgreSQL. Chạy scripts/Configure-Supabase.ps1 trước.");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("connectionString").GetString()
            ?? throw new InvalidOperationException("Cấu hình PostgreSQL trống.");
    }
}
