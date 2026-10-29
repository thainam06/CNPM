namespace ShortVideoTranslator.Business;

public record PresenceDevice(string DeviceId, DeviceDescriptor Descriptor, DateTimeOffset FirstSeen, DateTimeOffset LastSeen, string? SessionId = null);
public record PresenceCount(int Online, int WindowSeconds, DateTimeOffset CheckedAt);
public record PresencePage(int Online, int WindowSeconds, DateTimeOffset CheckedAt, int Page, int PageSize, IReadOnlyList<PresenceDevice> Items);

// Short-lived operational presence, separate from consented view/play analytics.
public sealed class PresenceService(IPlatformStore store, TimeProvider clock)
{
    /// <summary>
    //// hết hạn sau bao nhiêu giây mới được tính là offline
    /// </summary>
    public const int WindowSeconds = 45;
    private static string Id(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id.ToString() : throw new PlatformException(400, "Mã phiên truy cập không hợp lệ.");
    private static void Cleanup(PlatformState s, DateTimeOffset now)
    {
        s.PresenceDevices.RemoveAll(d => d.LastSeen <= now.AddSeconds(-WindowSeconds));
        foreach (var key in s.PresenceDepartures.Where(p => p.Value <= now).Select(p => p.Key).ToArray()) s.PresenceDepartures.Remove(key);
    }
    private static int Online(PlatformState s, DateTimeOffset now) => s.PresenceDevices.Where(d => d.LastSeen > now.AddSeconds(-WindowSeconds)).Select(d => d.DeviceId).Distinct().Count();
    public PresenceCount Heartbeat(string deviceId, DeviceDescriptor? descriptor, string? sessionId = null)
    {
        var key = Id(deviceId); var session = Id(sessionId ?? deviceId);
        descriptor ??= new();
        if (!descriptor.IsValid()) throw new PlatformException(400, "Thông tin trình duyệt không hợp lệ.");
        var now = clock.GetUtcNow();
        return store.Write(s => {
            Cleanup(s, now);
            // A heartbeat arriving after the leave request cannot resurrect a closed tab.
            if (s.PresenceDepartures.ContainsKey(key + ":" + session)) return new PresenceCount(Online(s, now), WindowSeconds, now);
            var index = s.PresenceDevices.FindIndex(d => d.DeviceId == key && (d.SessionId ?? d.DeviceId) == session);
            if (index < 0) {
                if (s.PresenceDevices.Count >= 5000) throw new PlatformException(503, "Bảng truy cập đang đầy. Vui lòng thử lại sau.");
                s.PresenceDevices.Add(new(key, descriptor, now, now, session));
            } else s.PresenceDevices[index] = s.PresenceDevices[index] with { Descriptor = descriptor, LastSeen = now };
            return new PresenceCount(Online(s, now), WindowSeconds, now);
        });
    }
    public PresenceCount Count()
    {
        var now = clock.GetUtcNow();
        return store.Read(s => new PresenceCount(Online(s, now), WindowSeconds, now));
    }
    public void Leave(string deviceId, string sessionId)
    {
        var key = Id(deviceId); var session = Id(sessionId); var now = clock.GetUtcNow();
        store.Write(s => {
            Cleanup(s, now);
            s.PresenceDevices.RemoveAll(d => d.DeviceId == key && (d.SessionId ?? d.DeviceId) == session);
            if (s.PresenceDepartures.Count >= 10000) s.PresenceDepartures.Remove(s.PresenceDepartures.MinBy(p => p.Value).Key);
            s.PresenceDepartures[key + ":" + session] = now.AddSeconds(WindowSeconds);
            return true;
        });
    }
    public PresencePage Devices(int page, int pageSize)
    {
        if (page < 1 || page > 100000 || pageSize < 1 || pageSize > 100) throw new PlatformException(400, "Trang hoặc số dòng không hợp lệ.");
        var now = clock.GetUtcNow();
        return store.Read(s => {
            var active = s.PresenceDevices.Where(d => d.LastSeen > now.AddSeconds(-WindowSeconds)).GroupBy(d => d.DeviceId)
                .Select(group => group.OrderByDescending(d => d.LastSeen).First() with { FirstSeen = group.Min(d => d.FirstSeen), SessionId = null })
                .OrderByDescending(d => d.LastSeen).ThenBy(d => d.DeviceId).ToArray();
            return new PresencePage(active.Length, WindowSeconds, now, page, pageSize, active.Skip((page - 1) * pageSize).Take(pageSize).ToArray());
        });
    }
}
