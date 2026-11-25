using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class DeviceBlockTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    [Fact]
    public void HistoryPreservesActorDurationAndDeadlineAcrossStoresAndExpiry()
    {
        var folder = Path.Combine(Path.GetTempPath(), "block-history-" + Guid.NewGuid().ToString("N"));
        try {
            var store = new SqlitePlatformStore(folder); var clock = new Clock();
            var device = Guid.NewGuid().ToString();
            store.Write(s => {
                s.Users.Add(new("operator", "admin-demo", "Admin", "hash", "super_admin"));
                s.Audit.Add(new("legacy", "operator", "device.block", device, clock.Now.AddHours(-1)));
                PlatformService.Audit(s, "operator", "poi.update", "1");
                return true;
            });
            new PresenceService(store, clock).Heartbeat(device, null);
            var service = new DeviceBlockService(store, clock);
            var until = service.Block("operator", device, 30);
            clock.Now = clock.Now.AddSeconds(2);
            service.Unblock("operator", device);
            service.Unblock("operator", device); // A repeated no-op must not add a history row.
            store.Write(s => { s.Users.Clear(); return true; });
            var restarted = new DeviceBlockService(new SqlitePlatformStore(folder), clock);
            var history = restarted.History(1, 1);
            Assert.Equal(3, history.Total);
            Assert.Equal("device.unblock", history.Items.Single().Action);
            Assert.Equal("admin-demo", history.Items.Single().ActorName);
            Assert.Equal(until, history.Items.Single().BlockedUntil);
            var blocked = restarted.History(2, 1).Items.Single();
            Assert.Equal(30, blocked.Seconds); Assert.Equal(until, blocked.BlockedUntil);
            Assert.Null(restarted.History(3, 1).Items.Single().Seconds);
            clock.Now = until.AddSeconds(1);
            Assert.Equal(3, restarted.History().Total);
            var decoded = PostgresStateCodec.Decode(PostgresStateCodec.Encode(store.Read(s => s)));
            Assert.Equal(store.Read(s => s.Audit.ToArray()), decoded.Audit.ToArray());
            // Older PostgreSQL rows have no deviceBlock key.
            var legacySnapshot = PostgresStateCodec.Encode(store.Read(s => s));
            foreach (var row in legacySnapshot["activity_logs"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>())
                if (row["details"] is System.Text.Json.Nodes.JsonObject details) details.Remove("deviceBlock");
            Assert.All(PostgresStateCodec.Decode(legacySnapshot).Audit, a => Assert.Null(a.DeviceBlock));
        } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    [Fact]
    public void BlocksPersistExpireAndRejectInvalidDurations()
    {
        var folder = Path.Combine(Path.GetTempPath(), "device-block-" + Guid.NewGuid().ToString("N"));
        try {
            var store = new SqlitePlatformStore(folder); var clock = new Clock();
            var device = Guid.NewGuid().ToString(); new PresenceService(store, clock).Heartbeat(device, null);
            var blocks = new DeviceBlockService(store, clock);
            Assert.Throws<PlatformException>(() => blocks.Block("admin", device, 0));
            Assert.Throws<PlatformException>(() => blocks.Block("admin", device, 86401));
            var until = blocks.Block("admin", device, 30);
            var restarted = new DeviceBlockService(new SqlitePlatformStore(folder), clock);
            Assert.Equal(until, restarted.Until(device));
            var decoded = PostgresStateCodec.Decode(PostgresStateCodec.Encode(store.Read(s => s)));
            Assert.Equal(until, decoded.DeviceBlocks[device]);
            clock.Now = until; Assert.Null(restarted.Until(device));
            blocks.Block("admin", device, 20); blocks.Unblock("admin", device); Assert.Null(blocks.Until(device));
        } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    [Fact]
    public async Task AdminBlocksOnlyTheSelectedBrowserAndCanUnblockIt()
    {
        using var app = new TestApplication(); using var admin = app.CreateClient(); using var target = app.CreateClient(); using var other = app.CreateClient();
        var device = Guid.NewGuid().ToString(); target.DefaultRequestHeaders.Add("X-SmartTour-Device", device);
        (await target.PostAsJsonAsync("/api/presence/heartbeat", new { deviceId = device })).EnsureSuccessStatusCode();
        target.DefaultRequestHeaders.Authorization = new("Bearer", "invalid");
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.PostAsJsonAsync("/api/admin/presence/blocks", new { deviceId = device, seconds = 30 })).StatusCode);
        target.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/admin/presence/blocks/history")).StatusCode);
        async Task Login(string password) {
            var response = await admin.PostAsJsonAsync("/api/auth/token", new { username = "admin", password }); response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(); admin.DefaultRequestHeaders.Authorization = new("Bearer", json.GetProperty("accessToken").GetString());
        }
        await Login("HttpBootstrap_2026!");
        (await admin.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "HttpBootstrap_2026!", newPassword = "BlockTest_2026!" })).EnsureSuccessStatusCode();
        await Login("BlockTest_2026!");
        (await admin.PostAsJsonAsync("/api/admin/presence/blocks", new { deviceId = device, seconds = 30 })).EnsureSuccessStatusCode();
        var blocked = await target.GetAsync("/api/places"); Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.NotNull(blocked.Headers.RetryAfter);
        var problem = await blocked.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("DEVICE_BLOCKED", problem.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/places")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await target.GetAsync("/api/presence/access")).StatusCode);
        admin.DefaultRequestHeaders.Add("X-SmartTour-Device", device);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/presence")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/presence/blocks", new { deviceId = device, seconds = 30 })).StatusCode);
        (await admin.DeleteAsync($"/api/admin/presence/blocks/{device}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await target.GetAsync("/api/places")).StatusCode);
        var history = await admin.GetFromJsonAsync<JsonElement>("/api/admin/presence/blocks/history?pageSize=1");
        Assert.Equal(2, history.GetProperty("total").GetInt32());
        Assert.Equal("device.unblock", history.GetProperty("items")[0].GetProperty("action").GetString());
        var blockHistory = await admin.GetFromJsonAsync<JsonElement>("/api/admin/presence/blocks/history?page=2&pageSize=1");
        Assert.Equal(30, blockHistory.GetProperty("items")[0].GetProperty("seconds").GetInt32());
    }
}
