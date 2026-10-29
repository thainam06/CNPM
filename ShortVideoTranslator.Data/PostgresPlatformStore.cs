using System.Data;
using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

public sealed class PostgresPlatformStore : IPlatformStore, IPlaceRepository, IDeviceActivityReader, IDisposable
{
    private readonly NpgsqlDataSource source;
    private const long WriteLock = 831794209;
    private static readonly Dictionary<string, string[]> Columns = new() {
        ["users"] = ["id","username","display_name","password_hash","role","enabled","settings"],
        ["places"] = ["id","owner_id","name","address","latitude","longitude","introduction","settings"],
        ["place_images"] = ["id","place_id","url","caption","ordinal","is_cover","metadata"],
        ["place_narrations"] = ["id","place_id","language_code","narration_text","audio_path","status","version","metadata"],
        ["visitor_sessions"] = ["id","device_id","device_info","started_at","last_seen_at","ended_at"],
        ["activity_logs"] = ["id","kind","user_id","device_id","place_id","consent","details","created_at"]
    };
    public PostgresPlatformStore(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Thiếu cấu hình kết nối PostgreSQL.");
        // Never log connection strings or include server error detail containing row data.
        var options = new NpgsqlConnectionStringBuilder(connectionString) {
            IncludeErrorDetail = false, PersistSecurityInfo = false, ApplicationName = "SmartTourGuide",
            Timeout = 15, CommandTimeout = 30, MaxPoolSize = 10
        };
        source = NpgsqlDataSource.Create(options.ConnectionString);
    }
    public void Dispose() => source.Dispose();
    private static void Lock(NpgsqlConnection db, NpgsqlTransaction tx)
    {
        using var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", db, tx);
        cmd.Parameters.AddWithValue("key", WriteLock); cmd.ExecuteNonQuery();
    }
    private static JsonObject Load(NpgsqlConnection db, NpgsqlTransaction tx)
    {
        var select = string.Join(",", Columns.Keys.Select(t => $"'{t}',COALESCE((SELECT jsonb_agg(to_jsonb(t)) FROM smarttour_demo.{t} t),'[]'::jsonb)"));
        using var cmd = new NpgsqlCommand($"SELECT jsonb_build_object({select})::text", db, tx);
        return JsonNode.Parse((string)cmd.ExecuteScalar()!)!.AsObject();
    }
    private static string Key(JsonObject row) => row["id"]!.ToJsonString();
    private static void Delete(NpgsqlConnection db, NpgsqlTransaction tx, string table, JsonObject row)
    {
        using var cmd = new NpgsqlCommand($"DELETE FROM smarttour_demo.{table} WHERE id=(jsonb_populate_record(NULL::smarttour_demo.{table},@row)).id", db, tx);
        cmd.Parameters.AddWithValue("row", NpgsqlDbType.Jsonb, row.ToJsonString()); cmd.ExecuteNonQuery();
    }
    private static void Save(NpgsqlConnection db, NpgsqlTransaction tx, JsonObject next, JsonObject previous)
    {
        var removed = new Dictionary<string, JsonObject[]>(); var changed = new Dictionary<string, JsonObject[]>();
        foreach (var table in Columns.Keys) {
            var oldRows = previous[table]!.AsArray().OfType<JsonObject>().ToDictionary(Key);
            var newRows = next[table]!.AsArray().OfType<JsonObject>().ToDictionary(Key);
            removed[table] = oldRows.Where(p => !newRows.ContainsKey(p.Key)).Select(p => p.Value).ToArray();
            changed[table] = newRows.Where(p => !oldRows.TryGetValue(p.Key, out var old) || !JsonNode.DeepEquals(old, p.Value)).Select(p => p.Value).ToArray();
        }
        using (var defer = new NpgsqlCommand("SET CONSTRAINTS ALL DEFERRED", db, tx)) defer.ExecuteNonQuery();
        foreach (var table in Columns.Keys.Reverse()) {
            foreach (var row in removed[table]) Delete(db, tx, table, row);
            // Remove changed image/narration keys before reordering unique ordinals/published versions.
            if (table is "place_images" or "place_narrations")
                foreach (var row in changed[table]) Delete(db, tx, table, row);
        }
        foreach (var (table, columns) in Columns) {
            if (changed[table].Length == 0) continue;
            var names = string.Join(',', columns);
            var assignments = string.Join(',', columns.Where(c => c != "id").Select(c => $"{c}=EXCLUDED.{c}"));
            using var cmd = new NpgsqlCommand($"INSERT INTO smarttour_demo.{table}({names}) SELECT {names} FROM jsonb_populate_recordset(NULL::smarttour_demo.{table},@rows) ON CONFLICT(id) DO UPDATE SET {assignments}", db, tx);
            cmd.Parameters.AddWithValue("rows", NpgsqlDbType.Jsonb, new JsonArray(changed[table].Select(r => r.DeepClone()).ToArray()).ToJsonString());
            cmd.ExecuteNonQuery();
        }
    }
    public T Read<T>(Func<PlatformState,T> query)
    {
        using var db = source.OpenConnection(); using var tx = db.BeginTransaction(IsolationLevel.RepeatableRead);
        var result = query(PostgresStateCodec.Decode(Load(db, tx))); tx.Commit(); return result;
    }
    public T Write<T>(Func<PlatformState,T> change)
    {
        using var db = source.OpenConnection(); using var tx = db.BeginTransaction(); Lock(db, tx);
        var before = Load(db, tx); var state = PostgresStateCodec.Decode(before); var result = change(state);
        Save(db, tx, PostgresStateCodec.Encode(state), before); tx.Commit(); return result;
    }
    // Only import into an empty target. Never overwrite cloud records silently.
    public void Import(PlatformState state)
    {
        using var db = source.OpenConnection(); using var tx = db.BeginTransaction(); Lock(db, tx);
        var previous = Load(db, tx);
        if (previous.Any(p => p.Value!.AsArray().Count != 0)) throw new InvalidOperationException("Database đích đã có dữ liệu. Không ghi đè; hãy sao lưu và đối chiếu trước.");
        Save(db, tx, PostgresStateCodec.Encode(state), previous);
        var restored = PostgresStateCodec.Decode(Load(db, tx));
        var json = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        if (!JsonNode.DeepEquals(System.Text.Json.JsonSerializer.SerializeToNode(state, json), System.Text.Json.JsonSerializer.SerializeToNode(restored, json)))
            throw new InvalidDataException("Đối chiếu dữ liệu nhập PostgreSQL không khớp. Đã rollback.");
        tx.Commit();
    }
    public IReadOnlyList<Place> GetAll() => Read(s => s.Places.ToArray());
    public Place? GetById(int id) => Read(s => s.Places.FirstOrDefault(p => p.Id == id));
    public DeviceActivityPage Devices(DateTimeOffset now, int page, int pageSize) => Read(s => {
        var active = s.AnalyticsHeartbeats.Where(h => h.LastSeen > now.AddMinutes(-5)).Select(h => h.DeviceId).ToHashSet();
        var groups = s.Events.GroupBy(e => e.DeviceId).ToArray();
        var items = groups.Select(g => new DeviceActivity(g.Key,g.Min(e => e.CreatedAt),g.Max(e => e.CreatedAt),
            g.LongCount(e => e.Kind == "view"),g.LongCount(e => e.Kind == "play"),active.Contains(g.Key),
            s.AnalyticsDevices.FirstOrDefault(d => d.DeviceId == g.Key)?.Descriptor ?? new()))
            .OrderByDescending(d => d.LastSeen).ThenBy(d => d.DeviceId).Skip((page-1)*pageSize).Take(pageSize).ToArray();
        return new DeviceActivityPage("PostgreSQL",active.Count,groups.Length,page,pageSize,now,items);
    });
    public void ProbeWrite()
    {
        using var db = source.OpenConnection(); using var tx = db.BeginTransaction();
        using var cmd = new NpgsqlCommand("UPDATE smarttour_demo.activity_logs SET details=details WHERE id=@id", db, tx);
        cmd.Parameters.AddWithValue("id", PostgresStateCodec.StateId);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidDataException("Thiếu metadata nền tảng.");
        tx.Rollback();
    }
}
