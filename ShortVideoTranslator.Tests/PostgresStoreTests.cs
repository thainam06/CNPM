using Npgsql;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public sealed class DisposablePostgresFactAttribute : FactAttribute
{
    public DisposablePostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMARTTOUR_TEST_POSTGRES")))
            Skip = "Requires isolated PostgreSQL; run scripts/Test-Postgres.ps1.";
    }
}
public sealed class PostgresStoreTests
{
    [DisposablePostgresFact]
    public async Task ImportRestartConcurrentWritesRollbackAndDeletePreserveExistingFeatures()
    {
        var connection = Environment.GetEnvironmentVariable("SMARTTOUR_TEST_POSTGRES")!;
        var options = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("smarttour_tests",options.Database); Assert.Equal("127.0.0.1",options.Host);
        var folder = Path.Combine(Path.GetTempPath(), "smarttour-pg-"+Guid.NewGuid().ToString("N"));
        try {
            var sqlite = new SqlitePlatformStore(folder); new PlatformService(sqlite).Bootstrap("admin","TestOnly123!");
            var original = sqlite.Read(s => s);
            using var store = new PostgresPlatformStore(connection); store.Import(original);
            Assert.Equivalent(original,store.Read(s => s),strict:true);
            Assert.Throws<InvalidOperationException>(() => store.Import(original));
            var before = store.Read(s => s.Version);
            using var second = new PostgresPlatformStore(connection);
            await Task.WhenAll(Enumerable.Range(0,12).Select(i => Task.Run(() => (i%2==0?store:second).Write(s => ++s.Version))));
            Assert.Equal(before+12,store.Read(s => s.Version));
            Assert.Throws<InvalidOperationException>(() => store.Write<bool>(s => { s.Users.Clear(); throw new InvalidOperationException(); }));
            Assert.Single(store.Read(s => s.Users));
            var platform = new PlatformService(store); var actor = store.Read(s => s.Users[0].Id);
            var place = store.Read(s => s.Places[0]);
            store.Write(s => { s.Events.Add(new("fixture-event","fixture-browser","view",place.Id,"vi",DateTimeOffset.UtcNow)); return true; });
            platform.DeletePlace(actor,place.Id);
            Assert.Null(store.GetById(place.Id));
            Assert.Contains(store.Read(s => s.Events),e => e.Id == "fixture-event" && e.PlaceId == place.Id);
            var remaining = store.Read(s => s.Places[0]);
            platform.SavePlace(actor,remaining with { Description=remaining.Description+" Test edit.", IsActive=false });
            Assert.EndsWith("Test edit.",second.GetById(remaining.Id)!.Description);
            var device = Guid.NewGuid().ToString(); var tab = Guid.NewGuid().ToString();
            var presence = new PresenceService(store,TimeProvider.System);
            presence.Heartbeat(device,new("Chrome","Windows","Desktop"),tab);
            Assert.Equal(1,presence.Count().Online); presence.Leave(device,tab);
            presence.Heartbeat(device,null,tab); Assert.Equal(0,presence.Count().Online);
            var user = platform.User(actor)!;
            Assert.True(PlatformService.VerifyPassword("TestOnly123!",user.PasswordHash));
            store.ProbeWrite();
        } finally { if (Directory.Exists(folder)) Directory.Delete(folder,true); }
    }
}
