using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

// One table per collection preserves the existing Business contract. Nested records
// remain JSON payloads; analytics columns and indexes are available directly in SQL.
public sealed class SqlitePlatformStore : IPlatformStore, IPlaceRepository, IDeviceActivityReader
{
    private readonly string connectionString;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] Collections = typeof(PlatformState).GetProperties()
        .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToArray();

    public SqlitePlatformStore(string directory)
    {
        Directory.CreateDirectory(directory);
        connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "smarttour.db"), DefaultTimeout = 30, Pooling = false }.ToString();
        using var db = Open();
        Execute(db, null, "PRAGMA journal_mode=WAL;");
        using var tx = db.BeginTransaction();
        Execute(db, tx, "CREATE TABLE IF NOT EXISTS metadata (id INTEGER PRIMARY KEY CHECK(id=1), payload TEXT NOT NULL CHECK(json_valid(payload))); ");
        foreach (var table in Collections)
        {
            var columns = table is "events" or "pendingAnalytics"
                ? ", deviceId TEXT GENERATED ALWAYS AS (json_extract(payload,'$.deviceId')) VIRTUAL, kind TEXT GENERATED ALWAYS AS (json_extract(payload,'$.kind')) VIRTUAL, createdAt TEXT GENERATED ALWAYS AS (json_extract(payload,'$.createdAt')) VIRTUAL"
                : table == "analyticsHeartbeats" ? ", deviceId TEXT GENERATED ALWAYS AS (json_extract(payload,'$.deviceId')) VIRTUAL, lastSeen TEXT GENERATED ALWAYS AS (json_extract(payload,'$.lastSeen')) VIRTUAL"
                : table == "analyticsDevices" ? ", deviceId TEXT GENERATED ALWAYS AS (json_extract(payload,'$.deviceId')) VIRTUAL" : "";
            Execute(db, tx, $"CREATE TABLE IF NOT EXISTS [{table}] (ordinal INTEGER PRIMARY KEY, payload TEXT NOT NULL CHECK(json_valid(payload)){columns});");
        }
        Execute(db, tx, "CREATE INDEX IF NOT EXISTS ix_events_device ON events(deviceId); CREATE INDEX IF NOT EXISTS ix_heartbeats_seen ON analyticsHeartbeats(lastSeen); CREATE UNIQUE INDEX IF NOT EXISTS ix_heartbeats_device ON analyticsHeartbeats(deviceId);");
        Execute(db, tx, "CREATE UNIQUE INDEX IF NOT EXISTS ix_devices_device ON analyticsDevices(deviceId);");
        using var exists = db.CreateCommand(); exists.Transaction = tx; exists.CommandText = "SELECT count(*) FROM metadata";
        if (Convert.ToInt32(exists.ExecuteScalar()) == 0)
        {
            // Run legacy seed migrations on a private copy; the original JSON is kept intact.
            var staging = Path.Combine(directory, "migration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                var legacy = Path.Combine(directory, "platform.json");
                var copy = Path.Combine(staging, "platform.json");
                if (File.Exists(legacy)) File.Copy(legacy, copy);
                var seed = new JsonPlatformStore(copy).Read(s => s);
                Save(db, tx, seed, null);
            }
            finally { Directory.Delete(staging, true); }
        }
        tx.Commit();
    }
    private SqliteConnection Open() { var db = new SqliteConnection(connectionString); db.Open(); return db; }
    private static void Execute(SqliteConnection db, SqliteTransaction? tx, string sql)
    { using var cmd = db.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
    private static PlatformState Load(SqliteConnection db, SqliteTransaction tx)
    {
        using var cmd = db.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = "SELECT payload FROM metadata WHERE id=1";
        var root = JsonNode.Parse((string)cmd.ExecuteScalar()!)!.AsObject();
        foreach (var table in Collections)
        {
            cmd.CommandText = $"SELECT payload FROM [{table}] ORDER BY ordinal";
            using var reader = cmd.ExecuteReader(); var rows = new JsonArray();
            while (reader.Read()) rows.Add(JsonNode.Parse(reader.GetString(0)));
            root[table] = rows;
        }
        return root.Deserialize<PlatformState>(Json)!;
    }
    private static void Save(SqliteConnection db, SqliteTransaction tx, PlatformState state, PlatformState? previous)
    {
        var root = JsonSerializer.SerializeToNode(state, Json)!.AsObject();
        var old = previous == null ? null : JsonSerializer.SerializeToNode(previous, Json)!.AsObject();
        foreach (var table in Collections)
        {
            var rows = root[table]!.AsArray();
            if (old == null || !JsonNode.DeepEquals(rows, old[table]))
            {
                // Preserve unchanged rows, especially long event histories on each heartbeat.
                using var trim = db.CreateCommand(); trim.Transaction = tx;
                trim.CommandText = $"DELETE FROM [{table}] WHERE ordinal >= $count";
                trim.Parameters.AddWithValue("$count", rows.Count); trim.ExecuteNonQuery();
                using var insert = db.CreateCommand(); insert.Transaction = tx;
                insert.CommandText = $"INSERT INTO [{table}] (ordinal,payload) VALUES ($id,$payload) ON CONFLICT(ordinal) DO UPDATE SET payload=excluded.payload";
                var id = insert.Parameters.Add("$id", SqliteType.Integer); var payload = insert.Parameters.Add("$payload", SqliteType.Text);
                var oldRows = old?[table]?.AsArray();
                // Delete changed keys before reordering rows with unique device indexes.
                using var remove = db.CreateCommand(); remove.Transaction = tx;
                remove.CommandText = $"DELETE FROM [{table}] WHERE ordinal=$id";
                var removeId = remove.Parameters.Add("$id", SqliteType.Integer);
                if (oldRows != null) for (var i = 0; i < Math.Min(rows.Count, oldRows.Count); i++) {
                    if (!JsonNode.DeepEquals(rows[i], oldRows[i])) { removeId.Value = i; remove.ExecuteNonQuery(); }
                }
                for (var i = 0; i < rows.Count; i++) {
                    if (oldRows != null && i < oldRows.Count && JsonNode.DeepEquals(rows[i], oldRows[i])) continue;
                    id.Value = i; payload.Value = rows[i]!.ToJsonString(); insert.ExecuteNonQuery();
                }
            }
            root.Remove(table);
        }
        using var meta = db.CreateCommand(); meta.Transaction = tx;
        meta.CommandText = "INSERT INTO metadata(id,payload) VALUES(1,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload";
        meta.Parameters.AddWithValue("$payload", root.ToJsonString()); meta.ExecuteNonQuery();
    }
    public T Read<T>(Func<PlatformState, T> query)
    {
        using var db = Open(); using var tx = db.BeginTransaction(deferred: true);
        return query(Load(db, tx));
    }
    public T Write<T>(Func<PlatformState, T> change)
    {
        // Immediate transaction serializes read-modify-write across connections, avoiding lost updates.
        using var db = Open(); using var tx = db.BeginTransaction();
        var previous = Load(db, tx);
        var next = JsonSerializer.Deserialize<PlatformState>(JsonSerializer.Serialize(previous, Json), Json)!;
        var result = change(next); Save(db, tx, next, previous); tx.Commit(); return result;
    }
    private static T[] Collection<T>(SqliteConnection db, SqliteTransaction tx, string table)
    {
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        // Callers pass a source-code table constant, never request input.
        cmd.CommandText = $"SELECT payload FROM [{table}] ORDER BY ordinal";
        using var reader = cmd.ExecuteReader(); var items = new List<T>();
        while (reader.Read()) items.Add(JsonSerializer.Deserialize<T>(reader.GetString(0), Json)!);
        return items.ToArray();
    }
    public IReadOnlyList<Place> GetAll()
    {
        using var db = Open(); using var tx = db.BeginTransaction(deferred: true);
        var places = Collection<Place>(db, tx, "places");
        var translations = Collection<ApprovedTranslation>(db, tx, "approvedTranslations");
        return places.Where(p => p.IsActive && TourRegion.Includes(p)).Select(p => {
        var revisions = translations.Where(t => t.SourceHash == PlatformService.ContentHash(p.Description)).ToDictionary(t => t.Language, t => t.Revision);
        return (p.LocationVerified ? p : p with { Latitude = null, Longitude = null }) with { NarrationRevisions = revisions.Count == 0 ? null : revisions };
        }).ToArray();
    }
    public Place? GetById(int id) => GetAll().FirstOrDefault(p => p.Id == id);

    public DeviceActivityPage Devices(DateTimeOffset now, int page, int pageSize)
    {
        using var db = Open(); using var tx = db.BeginTransaction(deferred: true);
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "SELECT count(DISTINCT deviceId) FROM analyticsHeartbeats WHERE julianday(lastSeen)>julianday($cutoff)";
        cmd.Parameters.AddWithValue("$cutoff", now.AddMinutes(-5).ToString("O"));
        var online = Convert.ToInt32(cmd.ExecuteScalar());
        cmd.CommandText = "SELECT count(DISTINCT deviceId) FROM events";
        var total = Convert.ToInt32(cmd.ExecuteScalar());
        cmd.CommandText = """
            SELECT e.deviceId, min(e.createdAt), max(e.createdAt),
              sum(CASE WHEN e.kind='view' THEN 1 ELSE 0 END), sum(CASE WHEN e.kind='play' THEN 1 ELSE 0 END),
              CASE WHEN julianday(max(h.lastSeen))>julianday($cutoff) THEN 1 ELSE 0 END,
              json_extract(d.payload,'$.descriptor.browser'), json_extract(d.payload,'$.descriptor.operatingSystem'), json_extract(d.payload,'$.descriptor.type')
            FROM events e LEFT JOIN analyticsHeartbeats h ON h.deviceId=e.deviceId
            LEFT JOIN analyticsDevices d ON d.deviceId=e.deviceId
            GROUP BY e.deviceId ORDER BY max(julianday(e.createdAt)) DESC, e.deviceId LIMIT $limit OFFSET $offset
            """;
        cmd.Parameters.AddWithValue("$limit", pageSize); cmd.Parameters.AddWithValue("$offset", (page - 1) * pageSize);
        using var reader = cmd.ExecuteReader(); var items = new List<DeviceActivity>();
        while (reader.Read()) items.Add(new(reader.GetString(0), DateTimeOffset.Parse(reader.GetString(1)), DateTimeOffset.Parse(reader.GetString(2)), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt32(5) == 1,
            new(reader.IsDBNull(6) ? "Other" : reader.GetString(6), reader.IsDBNull(7) ? "Other" : reader.GetString(7), reader.IsDBNull(8) ? "Unknown" : reader.GetString(8))));
        return new("SQLite", online, total, page, pageSize, now, items);
    }
}
