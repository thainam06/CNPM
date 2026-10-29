using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class DeviceExpiryTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void ConsentedSqlDevicesExpireAtFiveMinutesWithoutDeletingHistoryAndForgetPersists()
    {
        var folder = Path.Combine(Path.GetTempPath(), "smarttour-expiry-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SqlitePlatformStore(folder);
            var clock = new Clock();
            var queue = new AnalyticsQueue(store, clock);
            var device = Guid.NewGuid().ToString();
            var descriptor = new DeviceDescriptor("Safari", "iOS", "Mobile");
            queue.Enqueue(new(true, device,
                [new(Guid.NewGuid().ToString(), "heartbeat", null, "vi", clock.Now),
                 new(Guid.NewGuid().ToString(), "view", null, "vi", clock.Now),
                 new(Guid.NewGuid().ToString(), "play", null, "vi", clock.Now)], descriptor));
            queue.Drain();
            var service = new DeviceActivityService(store, clock);
            clock.Now = clock.Now.AddMinutes(5).AddSeconds(-1);
            Assert.Equal(1, service.Get(1, 25).Online);
            clock.Now = clock.Now.AddSeconds(1);
            var offline = service.Get(1, 25);
            Assert.Equal(0, offline.Online);
            var item = Assert.Single(offline.Items);
            Assert.False(item.Online);
            Assert.Equal(1, item.Views);
            Assert.Equal(1, item.Plays);
            Assert.Equal(descriptor, item.Descriptor);
            queue.Forget(device);
            var restarted = new SqlitePlatformStore(folder);
            Assert.Equal(0, restarted.Devices(clock.Now, 1, 25).Total);
            var restartedQueue = new AnalyticsQueue(restarted, clock);
            Assert.Equal(409, Assert.Throws<PlatformException>(() => restartedQueue.Enqueue(new(true, device,
                [new(Guid.NewGuid().ToString(), "heartbeat", null, "vi", clock.Now)], descriptor))).StatusCode);
            Assert.Empty(restarted.Read(s => s.AnalyticsDevices));
            Assert.Empty(restarted.Read(s => s.AnalyticsHeartbeats));
            Assert.Empty(restarted.Read(s => s.PendingAnalytics));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
