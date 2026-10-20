using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class AnalyticsBatchTests
{
    private static AnalyticsInput Input(string kind = "view", int? placeId = 1) => new(Guid.NewGuid().ToString(), kind, placeId, "vi", DateTimeOffset.UtcNow);
    [Fact]
    public async Task BatchIsAtomicConsentRequiredAndRetriesDoNotInflateMetrics()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var device = Guid.NewGuid().ToString(); var input = Input();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/analytics/batch", new AnalyticsBatch(false, device, [input]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/analytics/batch", new AnalyticsBatch(true, device, [input, Input(placeId: 99999)]))).StatusCode);
        var store = app.Services.GetRequiredService<IPlatformStore>(); var queue = app.Services.GetRequiredService<AnalyticsQueue>();
        Assert.Empty(store.Read(s => s.PendingAnalytics));
        var batch = new AnalyticsBatch(true, device, [input, Input("heartbeat", null)]);
        (await client.PostAsJsonAsync("/api/analytics/batch", batch)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/analytics/batch", batch)).EnsureSuccessStatusCode();
        Assert.Equal(2, store.Read(s => s.PendingAnalytics.Count));
        queue.Drain();
        (await client.PostAsJsonAsync("/api/analytics/batch", batch)).EnsureSuccessStatusCode(); queue.Drain();
        Assert.Equal(1, store.Read(s => s.Events.Count(e => e.Kind != "heartbeat")));
        Assert.Single(store.Read(s => s.AnalyticsHeartbeats));
    }
    [Fact]
    public async Task ForgetErasesQueuedAndProcessedDataAndRejectsLateRequestsAfterRestart()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var device = Guid.NewGuid().ToString(); var queue = app.Services.GetRequiredService<AnalyticsQueue>();
        queue.Enqueue(new(true, device, [Input()])); queue.Drain(); queue.Enqueue(new(true, device, [Input("play")]));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/analytics/forget", new { deviceId = device })).StatusCode);
        var restarted = new JsonPlatformStore(Path.Combine(app.Folder, "platform.json"));
        Assert.Empty(restarted.Read(s => s.Events)); Assert.Empty(restarted.Read(s => s.PendingAnalytics)); Assert.Empty(restarted.Read(s => s.AnalyticsHeartbeats));
        var restoredQueue = new AnalyticsQueue(restarted, TimeProvider.System);
        Assert.Equal(409, Assert.Throws<PlatformException>(() => restoredQueue.Enqueue(new(true, device, [Input()]))).StatusCode);
        restoredQueue.Enqueue(new(true, Guid.NewGuid().ToString(), [Input()])); restoredQueue.Drain();
        Assert.Single(restarted.Read(s => s.Events));
    }
    [Fact]
    public void AcceptedQueueSurvivesRestartAndRejectsExpiredOrOversizedBatches()
    {
        var folder = Path.Combine(Path.GetTempPath(), "SmartTourAnalytics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            var path = Path.Combine(folder, "platform.json"); var store = new JsonPlatformStore(path);
            var queue = new AnalyticsQueue(store, TimeProvider.System); var device = Guid.NewGuid().ToString();
            queue.Enqueue(new(true, device, [Input()]));
            var restored = new JsonPlatformStore(path); new AnalyticsQueue(restored, TimeProvider.System).Drain();
            Assert.Single(restored.Read(s => s.Events)); Assert.Empty(restored.Read(s => s.PendingAnalytics));
            Assert.Equal(400, Assert.Throws<PlatformException>(() => queue.Enqueue(new(true, device, Enumerable.Range(0, 26).Select(_ => Input()).ToArray()))).StatusCode);
            Assert.Equal(400, Assert.Throws<PlatformException>(() => queue.Enqueue(new(true, device, [Input() with { OccurredAt = DateTimeOffset.UtcNow.AddDays(-2) }]))).StatusCode);
        } finally { Directory.Delete(folder, true); }
    }
}
