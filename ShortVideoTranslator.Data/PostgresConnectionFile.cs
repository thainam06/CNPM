using System.Text.Json;
using Npgsql;

namespace ShortVideoTranslator.Data;

public static class PostgresConnectionFile
{
    public static string WithRootCertificate(string connectionString, string? certificatePath)
    {
        if (string.IsNullOrWhiteSpace(certificatePath)) return connectionString;
        if (!File.Exists(certificatePath))
            throw new FileNotFoundException("The configured PostgreSQL CA certificate file is missing.");
        var options = new NpgsqlConnectionStringBuilder(connectionString)
        {
            RootCertificate = Path.GetFullPath(certificatePath),
            SslMode = SslMode.VerifyFull
        };
        return options.ConnectionString;
    }

    public static string Read(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("Chưa cấu hình PostgreSQL. Chạy scripts/Configure-Supabase.ps1 trước.");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("connectionString").GetString()
            ?? throw new InvalidOperationException("Cấu hình PostgreSQL trống.");
    }
}
