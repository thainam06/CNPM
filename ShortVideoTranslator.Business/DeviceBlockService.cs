namespace ShortVideoTranslator.Business;

public sealed class DeviceBlockService(IPlatformStore store, TimeProvider clock)
{
    public DateTimeOffset? Until(string? device) {
        if (!Guid.TryParse(device, out var id)) return null;
        var now = clock.GetUtcNow();
        return store.Read(s => s.DeviceBlocks.TryGetValue(id.ToString(), out var until) && until > now ? (DateTimeOffset?)until : null);
    }
    public object List() => store.Read(s => s.DeviceBlocks.Where(b => b.Value > clock.GetUtcNow()).Select(b => new { deviceId = b.Key, blockedUntil = b.Value }).ToArray());
    public DeviceBlockHistoryPage History(int page = 1, int pageSize = 20) => store.Read(s => {
        page = Math.Clamp(page, 1, 5000); pageSize = Math.Clamp(pageSize, 1, 100);
        var entries = s.Audit.Where(a => a.Action is "device.block" or "device.unblock")
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).ToArray();
        var items = entries.Skip((page - 1) * pageSize).Take(pageSize).Select(a => new DeviceBlockHistoryItem(
            a.Id, a.Resource, a.Action, a.ActorId,
            a.DeviceBlock?.ActorName ?? s.Users.FirstOrDefault(u => u.Id == a.ActorId)?.Username ?? a.ActorId,
            a.CreatedAt, a.DeviceBlock?.Seconds, a.DeviceBlock?.BlockedUntil)).ToArray();
        return new DeviceBlockHistoryPage(items, entries.Length, page, pageSize);
    });
    public DateTimeOffset Block(string actor, string device, int seconds) {
        if (!Guid.TryParse(device, out var id) || id == Guid.Empty || seconds is < 1 or > 86400) throw new PlatformException(400, "Mã thiết bị hợp lệ và thời gian từ 1 đến 86400 giây.");
        var key = id.ToString(); var now = clock.GetUtcNow();
        return store.Write(s => {
            foreach (var old in s.DeviceBlocks.Where(b => b.Value <= now).Select(b => b.Key).ToArray()) s.DeviceBlocks.Remove(old);
            if (!s.PresenceDevices.Any(d => d.DeviceId == key) && !s.DeviceBlocks.ContainsKey(key)) throw new PlatformException(404, "Thiết bị không còn trong danh sách. Tải lại và thử lại.");
            if (s.DeviceBlocks.Count >= 5000 && !s.DeviceBlocks.ContainsKey(key)) throw new PlatformException(409, "Danh sách chặn đã đầy.");
            var until = now.AddSeconds(seconds); s.DeviceBlocks[key] = until;
            PlatformService.Audit(s, actor, "device.block", key,
                new(seconds, until, s.Users.FirstOrDefault(u => u.Id == actor)?.Username ?? actor), now); return until;
        });
    }
    public void Unblock(string actor, string device) {
        if (!Guid.TryParse(device, out var id)) throw new PlatformException(400, "Mã thiết bị không hợp lệ.");
        store.Write(s => {
            if (s.DeviceBlocks.Remove(id.ToString(), out var until))
                PlatformService.Audit(s, actor, "device.unblock", id.ToString(),
                    new(null, until, s.Users.FirstOrDefault(u => u.Id == actor)?.Username ?? actor), clock.GetUtcNow());
            return true;
        });
    }
}
public record DeviceBlockHistoryItem(string Id, string DeviceId, string Action, string ActorId,
    string ActorName, DateTimeOffset CreatedAt, int? Seconds, DateTimeOffset? BlockedUntil);
public record DeviceBlockHistoryPage(DeviceBlockHistoryItem[] Items, int Total, int Page, int PageSize);
