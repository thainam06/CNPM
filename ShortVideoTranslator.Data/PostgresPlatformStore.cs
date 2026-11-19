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
    internal static readonly Dictionary<string, string[]> Columns = new() {
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
    internal static string ExpectedType(string table, string column) =>
        column is "settings" or "metadata" or "device_info" or "details" ? "jsonb"
        : column is "enabled" or "is_cover" or "consent" ? "boolean"
        : column is "latitude" or "longitude" ? "double precision"
        : column is "started_at" or "last_seen_at" or "ended_at" or "created_at" ? "timestamp with time zone"
        : column == "ordinal" ? "integer"
        : column == "place_id" || (column == "id" && table is "places" or "place_images") ? "bigint" : "text";
    public object CheckSchema()
    {
        using var db=source.OpenConnection(); using var tx=db.BeginTransaction(IsolationLevel.RepeatableRead);
        using (var readOnly=new NpgsqlCommand("SET TRANSACTION READ ONLY",db,tx)) readOnly.ExecuteNonQuery();
        var errors=new List<string>(); var found=new Dictionary<string,HashSet<string>>();
        // Display views do not add physical storage tables to the six-table contract.
        using(var cmd=new NpgsqlCommand("SELECT c.table_name,c.column_name,c.data_type,c.is_nullable,c.column_default,c.is_identity FROM information_schema.columns c JOIN information_schema.tables t USING(table_catalog,table_schema,table_name) WHERE c.table_schema='smarttour_demo' AND t.table_type='BASE TABLE'",db,tx))
        using(var reader=cmd.ExecuteReader()) while(reader.Read()) {
            var table=reader.GetString(0); var column=reader.GetString(1);
            if(!found.ContainsKey(table))found[table]=[]; found[table].Add(column);
            if(Columns.TryGetValue(table,out var cols)) {
                if(!cols.Contains(column)&&reader.GetString(3)=="NO"&&reader.IsDBNull(4)&&reader.GetString(5)=="NO")
                    errors.Add($"{table}.{column}: cột bắt buộc ngoài mapping");
                if(cols.Contains(column)) {
                    var expected=ExpectedType(table,column);
                    if(reader.GetString(2)!=expected)errors.Add($"{table}.{column}: cần kiểu {expected}");
                }
            }
        }
        foreach(var (table,cols) in Columns) foreach(var col in cols)
            if(!found.TryGetValue(table,out var present)||!present.Contains(col))errors.Add($"Thiếu {table}.{col}");
        foreach(var table in found.Keys.Where(t=>!Columns.ContainsKey(t)))errors.Add($"Bảng ngoài thiết kế 6 bảng: {table}");
        var primaryKeys=new HashSet<string>();
        using(var cmd=new NpgsqlCommand("SELECT c.relname,pg_get_constraintdef(k.oid) FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid WHERE k.connamespace=to_regnamespace('smarttour_demo') AND k.contype='p'",db,tx))
        using(var reader=cmd.ExecuteReader())while(reader.Read())
            if(reader.GetString(1)=="PRIMARY KEY (id)")primaryKeys.Add(reader.GetString(0));
        foreach(var table in Columns.Keys)if(!primaryKeys.Contains(table))errors.Add($"{table}: cần PRIMARY KEY (id)");
        var foreignKeys=new HashSet<string>();
        using(var cmd=new NpgsqlCommand("SELECT c.relname,a.attname,pc.relname,pa.attname FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid JOIN pg_class pc ON pc.oid=k.confrelid JOIN pg_attribute a ON a.attrelid=k.conrelid AND a.attnum=k.conkey[1] JOIN pg_attribute pa ON pa.attrelid=k.confrelid AND pa.attnum=k.confkey[1] WHERE k.connamespace=to_regnamespace('smarttour_demo') AND k.contype='f' AND cardinality(k.conkey)=1 AND pc.relnamespace=k.connamespace",db,tx))
        using(var reader=cmd.ExecuteReader())while(reader.Read())foreignKeys.Add($"{reader.GetString(0)}.{reader.GetString(1)}->{reader.GetString(2)}.{reader.GetString(3)}");
        foreach(var key in new[]{"places.owner_id->users.id","place_images.place_id->places.id","place_narrations.place_id->places.id","activity_logs.user_id->users.id","activity_logs.place_id->places.id"})
            if(!foreignKeys.Contains(key))errors.Add("Thiếu FK "+key);
        using(var cmd=new NpgsqlCommand("SELECT c.relname,k.conname FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid WHERE k.connamespace=to_regnamespace('smarttour_demo') AND k.contype='f' AND NOT k.condeferrable",db,tx))
        using(var reader=cmd.ExecuteReader()) while(reader.Read())errors.Add($"{reader.GetString(0)}.{reader.GetString(1)}: cần chạy migration 005 (FK deferrable)");
        if(errors.Count!=0)return new {compatible=false,initialized=false,empty=false,errors=errors.ToArray(),counts=new Dictionary<string,int>()};
        var tables=Load(db,tx);var counts=tables.ToDictionary(p=>p.Key,p=>p.Value!.AsArray().Count);
        var initialized=tables["activity_logs"]!.AsArray().OfType<JsonObject>().Any(r=>(string?)r["id"]==PostgresStateCodec.StateId);
        if(initialized) {try {PostgresStateCodec.Decode(tables);}catch(Exception ex) when(ex is InvalidDataException or System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException or NullReferenceException){errors.Add("Dữ liệu đã có nhưng không khớp định dạng adapter; không import ghi đè.");}}
        return new {compatible=errors.Count==0,initialized,empty=counts.Values.All(c=>c==0),errors=errors.ToArray(),counts};
    }
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
        var expected = System.Text.Json.JsonSerializer.SerializeToNode(state, json)!.AsObject();
        var actual = System.Text.Json.JsonSerializer.SerializeToNode(restored, json)!.AsObject();
        var mismatched = expected.Where(p => !JsonNode.DeepEquals(p.Value, actual[p.Key])).Select(p => p.Key).ToArray();
        if (mismatched.Length != 0)
            // Report collection names only, never credentials or private record values.
            throw new InvalidDataException($"Đối chiếu dữ liệu nhập PostgreSQL không khớp ({string.Join(", ", mismatched)}). Đã rollback.");
        tx.Commit();
    }
    // Same public catalog contract as SQLite/JSON: never expose hidden or old-region POIs.
    public IReadOnlyList<Place> GetAll() => Read(s => s.Places.Where(p => p.IsActive && TourRegion.Includes(p)).Select(p => {
        var revisions = s.ApprovedTranslations.Where(t => t.SourceHash == PlatformService.ContentHash(p.Description))
            .ToDictionary(t => t.Language,t => t.Revision);
        return (p.LocationVerified ? p : p with { Latitude=null, Longitude=null }) with {
            NarrationRevisions=revisions.Count == 0 ? null : revisions
        };
    }).ToArray());
    public Place? GetById(int id) => GetAll().FirstOrDefault(p => p.Id == id);
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
