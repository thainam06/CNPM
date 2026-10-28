using System.Text.Json;
using Microsoft.Data.Sqlite;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class SqliteStoreTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "smarttour-sql-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    [Fact]
    public void MigrationPreservesEveryRecordAndOriginalJsonAndDoesNotRepeat()
    {
        var json = new JsonPlatformStore(Path.Combine(folder, "platform.json"));
        new PlatformService(json).Bootstrap("admin", "TestPassword2026!");
        var original = File.ReadAllBytes(Path.Combine(folder, "platform.json"));
        var sql = new SqlitePlatformStore(folder);
        Assert.Equivalent(json.Read(s => s), sql.Read(s => s));
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(folder, "platform.json")));
        sql.Write(s => { s.Version += 10; return true; });
        Assert.Equal(json.Read(s => s.Version) + 10, new SqlitePlatformStore(folder).Read(s => s.Version));
    }
    [Fact]
    public void FailedMutationRollsBack()
    {
        var sql = new SqlitePlatformStore(folder); var before = sql.Read(s => s.Version);
        Assert.Throws<InvalidOperationException>(() => sql.Write<bool>(s => { s.Version++; throw new InvalidOperationException(); }));
        Assert.Equal(before, new SqlitePlatformStore(folder).Read(s => s.Version));
    }
    [Fact]
    public async Task ConcurrentConnectionsDoNotLoseUpdates()
    {
        var sql = new SqlitePlatformStore(folder); var second = new SqlitePlatformStore(folder);
        var before = sql.Read(s => s.Version);
        await Task.WhenAll(Enumerable.Range(0, 32).Select(i => Task.Run(() => (i % 2 == 0 ? sql : second).Write(s => ++s.Version))));
        Assert.Equal(before + 32, sql.Read(s => s.Version));
    }
    [Fact]
    public void SqlDeviceCountsDeduplicateExpireAndRespectForget()
    {
        var sql = new SqlitePlatformStore(folder); var now = DateTimeOffset.UtcNow;
        var queue = new AnalyticsQueue(sql, TimeProvider.System);
        var devices = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToArray();
        foreach (var device in devices)
        {
            var batch = new AnalyticsBatch(true, device, [new(Guid.NewGuid().ToString(), "heartbeat", null, "vi", now)]);
            queue.Enqueue(batch); queue.Enqueue(batch);
        }
        queue.Drain();
        JsonElement Snapshot(DateTimeOffset time) => JsonSerializer.SerializeToElement(sql.Devices(time, 1, 25), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(3, Snapshot(now).GetProperty("online").GetInt32());
        Assert.Equal(3, Snapshot(now).GetProperty("total").GetInt32());
        Assert.Equal(0, Snapshot(now.AddMinutes(6)).GetProperty("online").GetInt32());
        queue.Forget(devices[0]);
        Assert.Equal(2, Snapshot(now).GetProperty("total").GetInt32());
        Assert.Throws<PlatformException>(() => queue.Enqueue(new(true, devices[0], [new(Guid.NewGuid().ToString(), "heartbeat", null, "vi", now)])));
        using var db = new SqliteConnection($"Data Source={Path.Combine(folder, "smarttour.db")};Pooling=False"); db.Open();
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT count(DISTINCT deviceId) FROM events";
        Assert.Equal(2L, cmd.ExecuteScalar());
    }
    [Fact]
    public void DeviceMetadataSurvivesReorderingAndForgetRemovesIt()
    {
        var sql = new SqlitePlatformStore(folder); var queue = new AnalyticsQueue(sql, TimeProvider.System);
        var a = Guid.NewGuid().ToString(); var b = Guid.NewGuid().ToString();
        void Send(string device, string browser) => queue.Enqueue(new(true, device,
            [new(Guid.NewGuid().ToString(), "heartbeat", null, "vi", DateTimeOffset.UtcNow)], new(browser, "Windows", "Desktop")));
        Send(a, "Chrome"); Send(b, "Firefox"); queue.Drain();
        Send(a, "Edge"); queue.Drain();
        var report = new DeviceActivityService(sql, TimeProvider.System).Get(1, 25);
        Assert.Equal(2, report.Online);
        Assert.Equal("Edge", report.Items.Single(d => d.DeviceId == a).Descriptor.Browser);
        Assert.Equal("Firefox", new SqlitePlatformStore(folder).Devices(DateTimeOffset.UtcNow, 1, 25).Items.Single(d => d.DeviceId == b).Descriptor.Browser);
        queue.Forget(a);
        Assert.DoesNotContain(sql.Read(s => s.AnalyticsDevices), d => d.DeviceId == a);
        Assert.Throws<PlatformException>(() => Send(b, "invalid browser"));
    }
    [Fact]
    public void PublicCatalogIgnoresAnalyticsAndRowUpdatesDoNotRewriteUnchangedEvents()
    {
        var sql = new SqlitePlatformStore(folder); var device = Guid.NewGuid().ToString();
        sql.Write(s => { s.Events.Add(new(Guid.NewGuid().ToString(), device, "view", null, "vi", DateTimeOffset.UtcNow)); return true; });
        using var db = new SqliteConnection($"Data Source={Path.Combine(folder, "smarttour.db")};Pooling=False"); db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TRIGGER preserve_history BEFORE DELETE ON events BEGIN SELECT RAISE(ABORT,'unchanged history rewritten'); END";
        cmd.ExecuteNonQuery();
        sql.Write(s => { s.Events.Add(new(Guid.NewGuid().ToString(), device, "play", null, "vi", DateTimeOffset.UtcNow)); return true; });
        Assert.Equal(2, sql.Read(s => s.Events.Count));
        Assert.Equal(8, sql.GetAll().Count);
    }
}
