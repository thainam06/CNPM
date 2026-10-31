using System.Data;
using Npgsql;

namespace ShortVideoTranslator.Api.Services;

public record ReviewSection(string Name, IReadOnlyList<Dictionary<string, object?>> Rows);
public record PoolerInfo(string? Host, int Port, string? Database, string? User);
public record SupabaseReviewResult(bool Success, string Code, DateTimeOffset CheckedAt,
    IReadOnlyList<string> Issues, IReadOnlyList<ReviewSection> Sections,
    PoolerInfo? Connection = null, string? Stage = null, string? SqlState = null);

// Standalone SELECT-only inspection. Never instantiates the store, imports,
// bootstraps accounts, starts workers or runs the diagnostic write probe.
public static class SupabaseReview
{
    private static readonly Dictionary<string, string[]> RequiredColumns = new()
    {
        ["users"] = ["id", "username", "display_name", "password_hash", "role", "enabled", "settings"],
        ["places"] = ["id", "owner_id", "name", "address", "latitude", "longitude", "introduction", "settings"],
        ["place_images"] = ["id", "place_id", "url", "caption", "ordinal", "is_cover", "metadata"],
        ["place_narrations"] = ["id", "place_id", "language_code", "narration_text", "audio_path", "status", "version", "metadata"],
        ["visitor_sessions"] = ["id", "device_id", "device_info", "started_at", "last_seen_at", "ended_at"],
        ["activity_logs"] = ["id", "kind", "user_id", "device_id", "place_id", "consent", "details", "created_at"]
    };
    private static readonly Dictionary<string, string[]> RequiredTypes = new()
    {
        ["users"] = ["text", "text", "text", "text", "text", "bool", "jsonb"],
        ["places"] = ["int8", "text", "text", "text", "float8", "float8", "text", "jsonb"],
        ["place_images"] = ["int8", "int8", "text", "text", "int4", "bool", "jsonb"],
        ["place_narrations"] = ["text", "int8", "text", "text", "text", "text", "text", "jsonb"],
        ["visitor_sessions"] = ["text", "text", "jsonb", "timestamptz", "timestamptz", "timestamptz"],
        ["activity_logs"] = ["text", "text", "text", "text", "int8", "bool", "jsonb", "timestamptz"]
    };

    public static SupabaseReviewResult Run(Func<string> connection, bool includeConnectionInfo = false)
    {
        var stage = "configuration";
        try
        {
            var options = new NpgsqlConnectionStringBuilder(connection())
            {
                IncludeErrorDetail = false, PersistSecurityInfo = false, Pooling = false,
                Timeout = 15, CommandTimeout = 15, ApplicationName = "SmartTourReadOnlyReview"
            };
            stage = "connect";
            using var db = new NpgsqlConnection(options.ConnectionString);
            db.Open();
            using var tx = db.BeginTransaction(IsolationLevel.RepeatableRead);
            using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL statement_timeout = '15s'", db, tx))
                readOnly.ExecuteNonQuery();
            var sections = new List<ReviewSection>();
            stage = "schema";
            ReadSections(db, tx, Resource("01_check_schema.sql"), "schema", sections);
            var issues = new List<string>();
            if (sections[0].Rows.Count != 6 || sections[0].Rows.Any(r => !Equals(r["result"], "FOUND")))
                issues.Add("Expected exactly six visible tables in smarttour_demo; inspect schema and permissions.");
            foreach (var (table, columns) in RequiredColumns)
            {
                var actual = sections[1].Rows.Where(r => Equals(r["table_name"], table))
                    .Select(r => r["column_name"]?.ToString()).ToArray();
                if (!actual.SequenceEqual(columns)) issues.Add($"Column contract mismatch: {table}.");
                var types = sections[1].Rows.Where(r => Equals(r["table_name"], table))
                    .Select(r => r["udt_name"]?.ToString()).ToArray();
                if (!types.SequenceEqual(RequiredTypes[table])) issues.Add($"Column type mismatch: {table}.");
            }
            // Do not query unknown/missing columns after a failed schema inspection.
            if (issues.Count == 0)
            {
                stage = "data";
                var counts = string.Join(" UNION ALL ", RequiredColumns.Keys.Select(table =>
                    $"SELECT '{table}' AS table_name, count(*) AS row_count FROM smarttour_demo.{table}"));
                ReadSections(db, tx, counts, "counts", sections);
                stage = "integrity";
                ReadSections(db, tx, Resource("03_check_integrity.sql"), "integrity", sections);
                foreach (var row in sections.Single(s => s.Name == "integrity-1").Rows)
                    if (Convert.ToInt64(row["violations"]) != 0)
                        issues.Add($"Integrity violations: {row["check_name"]}.");
                var foreignKeys = sections.Single(s => s.Name == "integrity-2").Rows.Single();
                if (Convert.ToInt64(foreignKeys["foreign_keys"]) != 5 || Convert.ToInt64(foreignKeys["non_deferrable_foreign_keys"]) != 0)
                    issues.Add("Expected five deferrable foreign keys; inspect migrations 002/005.");
                if (Convert.ToInt64(sections.Single(s => s.Name == "integrity-3").Rows.Single()["backend_state_envelopes"]) != 1)
                    issues.Add("Backend state envelope missing; a schema-only database is not an imported application store.");
            }
            ReadSections(db, tx, "SELECT current_setting('transaction_read_only') AS transaction_read_only", "transaction", sections);
            tx.Rollback();
            return new(issues.Count == 0, issues.Count == 0 ? "REVIEW_OK" : "REVIEW_ISSUES", DateTimeOffset.UtcNow,
                issues, sections, includeConnectionInfo ? new(options.Host, options.Port, options.Database, options.Username) : null);
        }
        catch (Exception error)
        {
            var safe = PostgresConnectionCheck.Failure(error);
            return new(false, safe.Code, DateTimeOffset.UtcNow, [safe.Message], [], Stage: stage, SqlState: safe.SqlState);
        }
    }

    private static string Resource(string name)
    {
        using var stream = typeof(SupabaseReview).Assembly.GetManifestResourceStream("SmartTour.Review." + name)
            ?? throw new FileNotFoundException();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void ReadSections(NpgsqlConnection db, NpgsqlTransaction tx, string sql, string name, List<ReviewSection> output)
    {
        using var cmd = new NpgsqlCommand(sql, db, tx);
        using var reader = cmd.ExecuteReader();
        var index = 0;
        do
        {
            if (reader.FieldCount == 0) continue;
            var rows = new List<Dictionary<string, object?>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }
            output.Add(new(name + "-" + ++index, rows));
        } while (reader.NextResult());
    }
}
