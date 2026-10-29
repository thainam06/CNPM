using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Services;

public class PlatformMaintenanceWorker(IPlatformStore store, OwnerIdentityService identities, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Audio:WorkersEnabled", true)) return;
        var nextCleanup = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (!stoppingToken.IsCancellationRequested) {
            var now = DateTimeOffset.UtcNow;
            var cleanup = now >= nextCleanup;
            if (cleanup || store.Read(s => s.AudioTasks.Any(t => t.Status == "running"))) {
                store.Write(s => {
                    for (var i = 0; i < s.AudioTasks.Count; i++)
                        if (s.AudioTasks[i].Status == "running") s.AudioTasks[i] = s.AudioTasks[i] with { HeartbeatAt = now };
                    if (cleanup) {
                        s.AudioTasks.RemoveAll(t => t.Status is "completed" or "failed" or "cancelled" && t.UpdatedAt < now.AddDays(-14));
                        s.DeletedPlaces.RemoveAll(p => p.DeletedAt < now.AddDays(-30));
                        s.RefreshSessions.RemoveAll(r => r.ExpiresAt <= now);
                        s.Events.RemoveAll(e => e.CreatedAt < now.AddDays(-30));
                        s.PresenceDevices.RemoveAll(d => d.LastSeen <= now.AddSeconds(-PresenceService.WindowSeconds));
                        s.RuntimeLocations.RemoveAll(p => p.LastSeen < now.AddHours(-24));
                        foreach (var key in s.RuntimeRevocations.Where(p => p.Value <= now).Select(p => p.Key).ToArray()) s.RuntimeRevocations.Remove(key);
                    }
                    return true;
                });
            }
            if (cleanup) { identities.RedactExpired(); nextCleanup = now.AddHours(1); }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
