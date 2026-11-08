using System.Data;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Npgsql;

namespace ShortVideoTranslator.Data;

public record SixTableMigrationReport(bool Success, string Code, string Mode, bool Initialized,
    string? ArchiveSchema, IReadOnlyDictionary<string, long> Counts,
    IReadOnlyList<string> Blockers, IReadOnlyList<string> Warnings);

// Explicit maintenance operation, never called by normal web startup or import.
public static class PostgresSixTableMigration
{
    public const string ArchiveSchema = "smarttour_demo_archive15";
    private static readonly string[] ExtraTables = ["roles", "menu_items", "owner_requests", "place_proposals",
        "notifications", "audio_assets", "audio_tasks", "audit_logs", "runtime_sessions"];
    private static readonly Dictionary<string, string[]> Core = PostgresPlatformStore.Columns;
    private static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    public static SixTableMigrationReport Run(string connectionString, bool apply = false,
        bool backupConfirmed = false, bool acceptCompactSnapshot = false)
    {
        if (apply && !backupConfirmed)
            return new(false, "BACKUP_REQUIRED", "apply", false, null, new Dictionary<string, long>(),
                ["Create and verify an external backup, stop all writers, then set BackupConfirmed=true."], []);
        var options = new NpgsqlConnectionStringBuilder(connectionString)
        {
            IncludeErrorDetail = false, PersistSecurityInfo = false, Pooling = false,
            Timeout = 15, CommandTimeout = 60, ApplicationName = "SmartTourSixTableMigration"
        };
        using var db = new NpgsqlConnection(options.ConnectionString);
        db.Open();
        // Apply reads only after exclusive table locks. READ COMMITTED ensures
        // writes that finished while locks were being acquired are included.
        using var tx = db.BeginTransaction(apply ? IsolationLevel.ReadCommitted : IsolationLevel.RepeatableRead);
        if (!apply) Execute(db, tx, "SET TRANSACTION READ ONLY");
        Execute(db, tx, "SET LOCAL lock_timeout = '10s'; SET LOCAL statement_timeout = '60s'");
        var tables = new List<string>();
        using (var cmd = new NpgsqlCommand("SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='smarttour_demo' AND c.relkind IN ('r','p') ORDER BY c.relname", db, tx))
        using (var reader = cmd.ExecuteReader()) while (reader.Read()) tables.Add(reader.GetString(0));
        var blockers = new List<string>();
        var warnings = new List<string>();
        var counts = new Dictionary<string, long>();
        var initialized = false;
        SixTableMigrationReport Finish(string code, bool success)
        {
            tx.Rollback();
            return new(success, code, apply ? "apply" : "plan", initialized, null, counts, blockers, warnings);
        }
        var compact = tables.ToHashSet().SetEquals(Core.Keys);
        if (!compact && !tables.ToHashSet().SetEquals(Core.Keys.Concat(ExtraTables)))
        {
            blockers.Add("Expected exactly the six-table design or the known 15-table design from migration 004. No changes made.");
            return Finish("UNSUPPORTED_SCHEMA", false);
        }
        if (apply && !compact)
        {
            Execute(db, tx, "SELECT pg_advisory_xact_lock(831794209)");
            Execute(db, tx, "LOCK TABLE " + string.Join(',', tables.Select(t => "smarttour_demo." + Quote(t))) + " IN ACCESS EXCLUSIVE MODE");
        }
        using (var visibility = new NpgsqlCommand("SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='smarttour_demo' AND c.relkind IN ('r','p') AND (row_security_active(c.oid) OR NOT has_table_privilege(c.oid,'SELECT'))", db, tx))
            if ((long)visibility.ExecuteScalar()! > 0)
            {
                blockers.Add("The current role cannot see every source row. Ask the database owner to inspect/migrate; do not disable RLS or grant public access.");
                return Finish("VISIBILITY_REQUIRED", false);
            }
        foreach (var table in tables)
        {
            using var count = new NpgsqlCommand("SELECT count(*) FROM smarttour_demo." + Quote(table), db, tx);
            counts[table] = (long)count.ExecuteScalar()!;
        }
        ValidateColumns(db, tx, compact, blockers);
        if (blockers.Count > 0) return Finish("COLUMN_MISMATCH", false);
        var original = ReadCore(db, tx, "smarttour_demo");
        var markers = original["activity_logs"]!.AsArray().OfType<JsonObject>()
            .Count(row => (string?)row["id"] == PostgresStateCodec.StateId);
        initialized = markers == 1;
        if (initialized)
        {
            try { PostgresStateCodec.Decode(original); }
            catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException or NullReferenceException)
            { blockers.Add("Existing compact snapshot is incompatible with the backend codec; manual mapping is required."); }
        }
        else if (Core.Keys.Any(t => counts[t] != 0))
            blockers.Add("Nonempty core tables have no unique supported backend state envelope. Do not bootstrap or import over them.");
        if (blockers.Count > 0) return Finish("SNAPSHOT_REQUIRED", false);
        if (compact) return Finish("ALREADY_COMPACT", true);

        using (var archive = new NpgsqlCommand("SELECT to_regnamespace('smarttour_demo_archive15') IS NOT NULL", db, tx))
            if ((bool)archive.ExecuteScalar()!) blockers.Add("Archive schema already exists; it will not be overwritten or deleted.");
        foreach (var table in ExtraTables.Where(t => t != "roles"))
            if (counts[table] > 0) blockers.Add($"{table} has workflow rows requiring an explicit mapping; migration stopped.");
        // 004 seeds empty-permission role names from users. Any richer role data
        // needs reconciliation rather than silently replacing the active permissions.
        using (var roles = new NpgsqlCommand("SELECT count(*) FROM smarttour_demo.roles r WHERE cardinality(r.permissions) <> 0 OR r.is_system OR NOT EXISTS (SELECT 1 FROM smarttour_demo.users u WHERE u.role=r.name)", db, tx))
            if ((long)roles.ExecuteScalar()! != 0) blockers.Add("roles contains more than migration-004 seed names; reconcile permissions first.");
        using (var external = new NpgsqlCommand("""
            SELECT
              (SELECT count(*) FROM pg_constraint k JOIN pg_class c ON c.oid=k.confrelid
               WHERE k.contype='f' AND c.relnamespace=to_regnamespace('smarttour_demo')
                 AND k.connamespace<>c.relnamespace)
              + (SELECT count(*) FROM pg_depend d JOIN pg_rewrite r ON r.oid=d.objid
                 JOIN pg_class v ON v.oid=r.ev_class JOIN pg_class c ON c.oid=d.refobjid
                 WHERE d.classid='pg_rewrite'::regclass AND d.refclassid='pg_class'::regclass
                   AND c.relnamespace=to_regnamespace('smarttour_demo') AND c.relkind IN ('r','p')
                   AND v.relnamespace<>c.relnamespace)
            """, db, tx))
            if ((long)external.ExecuteScalar()! > 0) blockers.Add("Other schemas depend on the old tables; review those clients before migration.");
        warnings.Add("Expanded 004 columns and all 15 original tables remain in the archive. Runtime uses the existing compact JSONB snapshot, not those expanded columns.");
        warnings.Add("Pause every writer and inspect external clients/functions. This transaction does not replace a verified external backup.");
        if (!initialized) warnings.Add("Empty schema only: import an authoritative source separately before starting the backend. No account or fixture data will be generated.");
        if (blockers.Count > 0) return Finish("MAPPING_REQUIRED", false);
        if (!apply) return Finish("PLAN_READY", true);
        if (initialized && !acceptCompactSnapshot)
        {
            blockers.Add("Confirm AcceptCompactSnapshot=true only after verifying the existing compact snapshot is authoritative; expanded-column edits are preserved in the archive, not applied to runtime.");
            return Finish("SNAPSHOT_CONFIRMATION_REQUIRED", false);
        }

        Execute(db, tx, "ALTER SCHEMA smarttour_demo RENAME TO " + ArchiveSchema);
        foreach (var name in new[] { "002_smarttour_compact.sql", "005_six_table_backend.sql" })
            Execute(db, tx, SchemaSql(name));
        foreach (var (table, columns) in Core)
        {
            var names = string.Join(',', columns.Select(Quote));
            Execute(db, tx, $"INSERT INTO smarttour_demo.{Quote(table)} ({names}) SELECT {names} FROM {ArchiveSchema}.{Quote(table)}");
        }
        foreach (var table in new[] { "places", "place_images" })
            Execute(db, tx, $"SELECT setval(pg_get_serial_sequence('smarttour_demo.{table}','id'), greatest(coalesce(max(id),1),1), coalesce(max(id)>=1,false)) FROM smarttour_demo.{table}");
        var restored = ReadCore(db, tx, "smarttour_demo");
        if (!JsonNode.DeepEquals(original, restored)) throw new InvalidDataException("Migration verification failed; transaction rolled back.");
        if (initialized) PostgresStateCodec.Decode(restored);
        var finalErrors = new List<string>();
        ValidateColumns(db, tx, true, finalErrors);
        if (finalErrors.Count > 0) throw new InvalidDataException("Target schema verification failed; transaction rolled back.");
        tx.Commit();
        return new(true, "MIGRATED", "apply", initialized, ArchiveSchema, counts, [], warnings);
    }

    private static void ValidateColumns(NpgsqlConnection db, NpgsqlTransaction tx, bool exact, List<string> errors)
    {
        var columns = new Dictionary<string, Dictionary<string, string>>();
        using var command = new NpgsqlCommand("SELECT table_name,column_name,data_type FROM information_schema.columns WHERE table_schema='smarttour_demo'", db, tx);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var table = reader.GetString(0);
            if (!columns.ContainsKey(table)) columns[table] = [];
            columns[table][reader.GetString(1)] = reader.GetString(2);
        }
        foreach (var (table, expected) in Core)
        {
            if (!columns.TryGetValue(table, out var actual)) { errors.Add("Missing table: " + table); continue; }
            foreach (var column in expected)
                if (!actual.TryGetValue(column, out var type) || type != PostgresPlatformStore.ExpectedType(table, column))
                    errors.Add($"Column/type mismatch: {table}.{column}");
            if (exact && actual.Count != expected.Length) errors.Add("Extra columns: " + table);
        }
    }
    private static JsonObject ReadCore(NpgsqlConnection db, NpgsqlTransaction tx, string schema)
    {
        var items = Core.Select(pair => $"'{pair.Key}',(SELECT coalesce(jsonb_agg(to_jsonb(r)),'[]'::jsonb) FROM (SELECT {string.Join(',', pair.Value.Select(Quote))} FROM {Quote(schema)}.{Quote(pair.Key)} ORDER BY id) r)");
        using var command = new NpgsqlCommand("SELECT jsonb_build_object(" + string.Join(',', items) + ")::text", db, tx);
        return JsonNode.Parse((string)command.ExecuteScalar()!)!.AsObject();
    }
    private static void Execute(NpgsqlConnection db, NpgsqlTransaction tx, string sql)
    {
        using var command = new NpgsqlCommand(sql, db, tx);
        command.ExecuteNonQuery();
    }
    private static string SchemaSql(string name)
    {
        using var stream = typeof(PostgresSixTableMigration).Assembly.GetManifestResourceStream("SmartTour.Migration." + name)
            ?? throw new FileNotFoundException("Embedded schema resource missing.");
        using var reader = new StreamReader(stream);
        // The outer migration owns the only transaction; do not allow embedded
        // schema scripts to commit the archive before copy/verification succeeds.
        return Regex.Replace(reader.ReadToEnd(), @"^(BEGIN|COMMIT);\s*$", "", RegexOptions.Multiline);
    }
}
