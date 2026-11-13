using System.Security.Authentication;
using Npgsql;
using ShortVideoTranslator.Data;
namespace ShortVideoTranslator.Api.Services;

public record PostgresCheckResult(bool Ready, string Code, string Message, int? Places = null, int? Users = null);
public static class PostgresConnectionCheck
{
    // Standalone diagnostic: no bootstrap, import, hosted workers or committed writes.
    public static PostgresCheckResult Check(Func<string> connection)
    {
        try {
            using var store = new PostgresPlatformStore(connection());
            var counts = store.Read(s => (Places: s.Places.Count, Users: s.Users.Count));
            store.ProbeWrite(); // Transaction always rolls back.
            return new(true, "READY", "Supabase PostgreSQL read/write check passed (write rolled back).", counts.Places, counts.Users);
        } catch (Exception error) { return Failure(error); }
    }
    public static PostgresCheckResult Failure(Exception error)
    {
        // Never return exception messages: they may contain credentials/row data.
        for (Exception? current = error; current != null; current = current.InnerException) {
            if (current is AuthenticationException) return new(false, "TLS", "Check the pooler hostname and Root Certificate file. Keep SSL Mode=VerifyFull.");
            if (current is FileNotFoundException) return new(false, "FILE", "A connection or CA certificate file is missing inside the container.");
            if (current is PostgresException postgres) return postgres.SqlState switch {
                "28P01" or "28000" => new(false, "AUTH", "Check the Session pooler username and database password in the private env file."),
                "42P01" or "3F000" => new(false, "SCHEMA", "Required smarttour_demo tables are missing. Check the configured project and migrations 002/005."),
                "42501" => new(false, "PERMISSION", "The backend database role cannot read/write the SmartTour tables."),
                "53300" => new(false, "CAPACITY", "Database connection capacity reached. Retry after other connections close."),
                _ => new(false, "DATABASE", "PostgreSQL rejected the check. Share this code; keep credentials private.")
            };
        }
        if (error is InvalidDataException) return new(false, "DATA", "SmartTour metadata is missing or incompatible. Check the selected project; do not reimport or delete existing data.");
        if (error is NpgsqlException or TimeoutException) return new(false, "CONNECT", "Cannot reach PostgreSQL. Check internet, Supabase project status (resume if paused), and Session pooler host/port.");
        if (error is ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
            return new(false, "CONFIG", "Check the private connection configuration. Do not paste passwords into chat.");
        return new(false, "CHECK_FAILED", "Database check failed. Share this code without credentials.");
    }
}
