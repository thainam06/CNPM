using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class PresenceTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "smarttour-presence-" + Guid.NewGuid().ToString("N"));
    private class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    [Fact]
    public void TabsDeduplicateAndInactiveDevicesExpireWithoutAClosingRequest()
    {
        var store = new SqlitePlatformStore(folder); var clock = new Clock(); var service = new PresenceService(store, clock);
        var device = Guid.NewGuid().ToString();
        Assert.Equal(0, service.Count().Online);
        Assert.Equal(1, service.Heartbeat(device, new("Chrome", "Windows", "Desktop")).Online);
        Assert.Equal(1, service.Heartbeat(device.ToUpperInvariant(), null).Online);
        Assert.Equal(2, service.Heartbeat(Guid.NewGuid().ToString(), null).Online);
        Assert.Empty(store.Read(s => s.Events)); // Does not fabricate analytics consent or view/play events.
        clock.Now = clock.Now.AddSeconds(121);
        Assert.Equal(0, service.Count().Online); Assert.Empty(service.Devices(1, 25).Items);
        service.Heartbeat(Guid.NewGuid().ToString(), null);
        Assert.Single(store.Read(s => s.PresenceDevices));
    }
    [Fact]
    public void PresencePersistsAndInvalidInputCannotChangeIt()
    {
        var store = new SqlitePlatformStore(folder); var clock = new Clock(); var service = new PresenceService(store, clock);
        var device = Guid.NewGuid().ToString(); service.Heartbeat(device, new("Safari", "iOS", "Mobile"));
        var restored = new PresenceService(new SqlitePlatformStore(folder), clock);
        Assert.Equal("iOS", restored.Devices(1, 25).Items.Single().Descriptor.OperatingSystem);
        Assert.Throws<PlatformException>(() => service.Heartbeat("invalid", null));
        Assert.Throws<PlatformException>(() => service.Heartbeat(Guid.NewGuid().ToString(), new("injected-browser")));
        Assert.Throws<PlatformException>(() => service.Devices(0, 25));
        Assert.Equal(1, service.Count().Online);
    }
    [Fact]
    public async Task ConcurrentDevicesAreCountedOncePerBrowser()
    {
        var store = new SqlitePlatformStore(folder); var clock = new Clock(); var service = new PresenceService(store, clock);
        var ids = Enumerable.Range(0, 12).Select(_ => Guid.NewGuid().ToString()).ToArray();
        await Task.WhenAll(ids.SelectMany(id => new[] { Task.Run(() => service.Heartbeat(id, null)), Task.Run(() => service.Heartbeat(id, null)) }));
        Assert.Equal(12, service.Count().Online);
        Assert.Equal(5, service.Devices(2, 5).Items.Count);
    }
    [Fact]
    public async Task PublicCounterWorksWithoutAnalyticsConsentButDeviceListRequiresAdmin()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var device = Guid.NewGuid().ToString();
        var response = await client.PostAsJsonAsync("/api/presence/heartbeat", new { deviceId = device, device = new { browser = "Chrome", operatingSystem = "Windows", type = "Desktop" } });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(device, body); Assert.DoesNotContain("descriptor", body);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/presence")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/presence")).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "HttpBootstrap_2026!" });
        var token = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        (await client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "HttpBootstrap_2026!", newPassword = "Presence_Test2026!" })).EnsureSuccessStatusCode();
        login = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "Presence_Test2026!" });
        token = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        var table = await client.GetFromJsonAsync<JsonElement>("/api/admin/presence");
        Assert.Equal(1, table.GetProperty("online").GetInt32());
        Assert.Equal(device, table.GetProperty("items")[0].GetProperty("deviceId").GetString());
    }
    [Fact]
    public void ClosingOneTabKeepsOtherTabOnlineAndLateHeartbeatCannotReviveClosedTab()
    {
        var store = new SqlitePlatformStore(folder); var clock = new Clock(); var service = new PresenceService(store, clock);
        var device = Guid.NewGuid().ToString(); var a = Guid.NewGuid().ToString(); var b = Guid.NewGuid().ToString();
        service.Heartbeat(device, null, a); service.Heartbeat(device, null, b);
        Assert.Equal(1, service.Count().Online); Assert.Single(service.Devices(1, 25).Items);
        service.Leave(device, a); Assert.Equal(1, service.Count().Online);
        service.Heartbeat(device, null, a); Assert.Single(store.Read(s => s.PresenceDevices));
        service.Leave(device, b); Assert.Equal(0, service.Count().Online);
        service.Heartbeat(device, null, b); Assert.Equal(0, service.Count().Online);
        var resumed = Guid.NewGuid().ToString(); service.Heartbeat(device, null, resumed);
        Assert.Equal(1, service.Count().Online);
        clock.Now = clock.Now.AddSeconds(46); Assert.Equal(0, service.Count().Online);
    }
    [Fact]
    public void LeaveArrivingBeforeFirstHeartbeatStillClosesThatSession()
    {
        var service = new PresenceService(new SqlitePlatformStore(folder), new Clock());
        var device = Guid.NewGuid().ToString(); var session = Guid.NewGuid().ToString();
        service.Leave(device, session); service.Heartbeat(device, null, session);
        Assert.Equal(0, service.Count().Online);
    }
}
