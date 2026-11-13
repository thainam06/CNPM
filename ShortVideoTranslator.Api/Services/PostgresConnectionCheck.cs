using System.Security.Authentication;
using Npgsql;
using ShortVideoTranslator.Data;
namespace ShortVideoTranslator.Api.Services;

public record PostgresCheckResult(bool Ready, string Code, string Message, int? Places = null, int? Users = null, string? SqlState = null, string? Stage = null);
public static class PostgresConnectionCheck
{
    // Standalone diagnostic: no bootstrap, import, hosted workers or committed writes.
    public static PostgresCheckResult Check(Func<string> connection)
    {
        var stage = "configuration";
        try {
            using var store = new PostgresPlatformStore(connection());
            stage = "connect-and-read";
            var counts = store.Read(s => (Places: s.Places.Count, Users: s.Users.Count));
            stage = "write-probe";
            store.ProbeWrite(); // Transaction always rolls back.
            return new(true, "READY", "Supabase PostgreSQL read/write check passed (write rolled back).", counts.Places, counts.Users);
        } catch (Exception error) { return Failure(error) with { Stage = stage }; }
    }
    public static PostgresCheckResult Failure(Exception error)
    {
        // Never return exception messages: they may contain credentials/row data.
        for (Exception? current = error; current != null; current = current.InnerException) {
            if (current is AuthenticationException) return new(false, "TLS", "Check the pooler hostname and Root Certificate file. Keep SSL Mode=VerifyFull.");
            if (current is FileNotFoundException) return new(false, "FILE", "A connection or CA certificate file is missing inside the container.");
            if (current is PostgresException postgres) {
                var sqlState = System.Text.RegularExpressions.Regex.IsMatch(postgres.SqlState, "^[A-Z0-9]{5}$") ? postgres.SqlState : null;
                // Known pooler errors may share XX000. Classify only fixed phrases,
                // never echo the raw server message, detail, SQL or connection string.
                if (postgres.MessageText.Contains("Tenant or user not found", StringComparison.OrdinalIgnoreCase))
                    return new(false, "POOLER_PROJECT", "Copy the exact Session pooler host and username from this project's Connect panel; the pooler could not identify the project/user.", SqlState: sqlState);
                if (postgres.MessageText.Contains("Circuit breaker open", StringComparison.OrdinalIgnoreCase))
                    return new(false, "POOLER_BLOCKED", "Pooler temporarily blocked backend connections. Check Supabase status and saved database credentials, then allow the pooler to recover before retrying.", SqlState: sqlState);
                if (postgres.MessageText.Contains("MaxClientsInSessionMode", StringComparison.OrdinalIgnoreCase))
                    return new(false, "CAPACITY", "The Session pooler is full. Check active clients and pool limits before retrying.", SqlState: sqlState);
                PostgresCheckResult result = postgres.SqlState switch {
                "28P01" or "28000" => new(false, "AUTH", "Check the Session pooler username and database password in the private env file."),
                "42P01" or "3F000" or "42703" => new(false, "SCHEMA", "Required smarttour_demo tables are missing. Check the configured project and migrations 002/005."),
                "42501" => new(false, "PERMISSION", "The backend database role cannot read/write the SmartTour tables."),
                "25006" => new(false, "READ_ONLY", "Database is read-only. Check Supabase database/disk status and use the primary endpoint; do not delete data."),
                "57P03" => new(false, "UNAVAILABLE", "Database is not accepting connections yet. Check Supabase project status and wait for startup/resume."),
                "57014" => new(false, "TIMEOUT", "PostgreSQL cancelled the query. Check database load and statement timeout."),
                "3D000" => new(false, "DATABASE_NAME", "Database name does not exist. Check Database=postgres against the Connect panel."),
                "53300" => new(false, "CAPACITY", "Database connection capacity reached. Retry after other connections close."),
                _ => new(false, "DATABASE", "PostgreSQL rejected the check. Share this code; keep credentials private.")
                };
                return result with { SqlState = sqlState };
            }
        }
        if (error is InvalidDataException) return new(false, "DATA", "SmartTour metadata is missing or incompatible. Check the selected project; do not reimport or delete existing data.");
        if (error is NpgsqlException or TimeoutException) return new(false, "CONNECT", "Cannot reach PostgreSQL. Check internet, Supabase project status (resume if paused), and Session pooler host/port.");
        if (error is ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
            return new(false, "CONFIG", "Check the private connection configuration. Do not paste passwords into chat.");
        return new(false, "CHECK_FAILED", "Database check failed. Share this code without credentials.");
    }
}
