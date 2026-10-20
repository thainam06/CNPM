using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Services;

public record AnalyticsInput(string Id, string Kind, int? PlaceId, string Language, DateTimeOffset OccurredAt);
public record AnalyticsBatch(bool Consent, string DeviceId, AnalyticsInput[] Events);

// Pending batches live in the same atomic store as events: restart cannot lose accepted data.
public sealed class AnalyticsQueue(IPlatformStore store, TimeProvider clock)
{
    public int Enqueue(AnalyticsBatch batch)
    {
        var now = clock.GetUtcNow();
        if (!batch.Consent || !Guid.TryParse(batch.DeviceId, out _) || batch.Events is not { Length: > 0 and <= 25 })
            throw new PlatformException(400, "Cần đồng ý thống kê, mã thiết bị và từ 1 đến 25 sự kiện.");
        var device = Guid.Parse(batch.DeviceId).ToString();
        if (batch.Events.Any(e => e == null || !Guid.TryParse(e.Id, out _) || e.Kind is not ("view" or "play" or "heartbeat") || !NarrationLanguages.Codes.Contains(e.Language) || e.OccurredAt < now.AddHours(-24) || e.OccurredAt > now.AddMinutes(2) || (e.Kind == "heartbeat" && e.PlaceId != null)))
            throw new PlatformException(400, "Sự kiện không hợp lệ hoặc đã quá hạn 24 giờ.");
        return store.Write(s => {
            Cleanup(s, now);
            if (s.AnalyticsRevocations.ContainsKey(device)) throw new PlatformException(409, "Phiên thống kê đã thu hồi. Chỉ tạo phiên mới khi đồng ý lại.");
            if (batch.Events.Any(e => e.PlaceId.HasValue && !s.Places.Any(p => p.Id == e.PlaceId && p.IsActive && TourRegion.Includes(p)))) throw new PlatformException(404, "Địa điểm không tồn tại.");
            var pending = batch.Events.Select(e => new AnalyticsEvent(Guid.Parse(e.Id).ToString(), device, e.Kind, e.PlaceId, e.Language, e.OccurredAt))
                .DistinctBy(e => e.Id).Where(e => !s.Events.Any(old => old.Id == e.Id) && !s.PendingAnalytics.Any(old => old.Id == e.Id)).ToArray();
            if (s.PendingAnalytics.Count + pending.Length > 1000) throw new PlatformException(503, "Hàng đợi thống kê đầy. Thử lại sau.");
            s.PendingAnalytics.AddRange(pending); return pending.Length;
        });
    }
    public void Forget(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out var id)) throw new PlatformException(400, "Mã thiết bị không hợp lệ.");
        var device = id.ToString(); var now = clock.GetUtcNow();
        store.Write(s => {
            Cleanup(s, now); s.AnalyticsRevocations[device] = now.AddDays(31);
            s.Events.RemoveAll(e => e.DeviceId == device); s.PendingAnalytics.RemoveAll(e => e.DeviceId == device);
            s.AnalyticsHeartbeats.RemoveAll(e => e.DeviceId == device); return true;
        });
    }
    public void Drain()
    {
        var now = clock.GetUtcNow();
        if (!store.Read(s => s.PendingAnalytics.Count > 0)) return;
        store.Write(s => {
            Cleanup(s, now);
            foreach (var item in s.PendingAnalytics.Take(100).ToArray()) {
                s.PendingAnalytics.Remove(item);
                if (s.AnalyticsRevocations.ContainsKey(item.DeviceId) || item.CreatedAt < now.AddHours(-24)) continue;
                // Retain heartbeat IDs for deduplication, but exclude them from view/play metrics.
                if (!s.Events.Any(e => e.Id == item.Id)) s.Events.Add(item);
                if (item.CreatedAt > now.AddMinutes(-5)) {
                    var previous = s.AnalyticsHeartbeats.FirstOrDefault(h => h.DeviceId == item.DeviceId);
                    if (previous == null || previous.LastSeen < item.CreatedAt) {
                        s.AnalyticsHeartbeats.RemoveAll(h => h.DeviceId == item.DeviceId);
                        s.AnalyticsHeartbeats.Add(new(item.DeviceId, item.CreatedAt));
                    }
                }
            }
            if (s.Events.Count > 10000) s.Events = s.Events.OrderByDescending(e => e.CreatedAt).Take(10000).ToList();
            return true;
        });
    }
    private static void Cleanup(PlatformState s, DateTimeOffset now)
    {
        s.Events.RemoveAll(e => e.CreatedAt < now.AddDays(-30));
        s.PendingAnalytics.RemoveAll(e => e.CreatedAt < now.AddHours(-24));
        s.AnalyticsHeartbeats.RemoveAll(e => e.LastSeen < now.AddMinutes(-5));
        foreach (var key in s.AnalyticsRevocations.Where(e => e.Value <= now).Select(e => e.Key).ToArray()) s.AnalyticsRevocations.Remove(key);
    }
}

public sealed class AnalyticsWorker(AnalyticsQueue queue, ILogger<AnalyticsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do {
            try { queue.Drain(); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { logger.LogWarning("Chưa ghi được thống kê; giữ hàng đợi để thử lại."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
