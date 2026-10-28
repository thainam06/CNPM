namespace ShortVideoTranslator.Business;

public record DeviceDescriptor(string Browser = "Other", string OperatingSystem = "Other", string Type = "Unknown");
public record AnalyticsDevice(string DeviceId, DeviceDescriptor Descriptor, DateTimeOffset UpdatedAt);
public record DeviceActivity(string DeviceId, DateTimeOffset FirstSeen, DateTimeOffset LastSeen,
    long Views, long Plays, bool Online, DeviceDescriptor Descriptor);
public record DeviceActivityPage(string Storage, int Online, int Total, int Page, int PageSize,
    DateTimeOffset CheckedAt, IReadOnlyList<DeviceActivity> Items);
public interface IDeviceActivityReader
{
    DeviceActivityPage Devices(DateTimeOffset now, int page, int pageSize);
}

public sealed class DeviceActivityService(IPlatformStore store, TimeProvider clock)
{
    public DeviceActivityPage Get(int page, int pageSize)
    {
        if (page < 1 || page > 100000 || pageSize < 1 || pageSize > 100)
            throw new PlatformException(400, "Trang phải từ 1 đến 100000; số dòng từ 1 đến 100.");
        var now = clock.GetUtcNow();
        if (store is IDeviceActivityReader reader) return reader.Devices(now, page, pageSize);
        return store.Read(s => {
            var active = s.AnalyticsHeartbeats.Where(h => h.LastSeen > now.AddMinutes(-5)).Select(h => h.DeviceId).ToHashSet();
            var groups = s.Events.GroupBy(e => e.DeviceId).ToArray();
            var items = groups.Select(g => new DeviceActivity(g.Key, g.Min(e => e.CreatedAt), g.Max(e => e.CreatedAt),
                g.LongCount(e => e.Kind == "view"), g.LongCount(e => e.Kind == "play"), active.Contains(g.Key),
                s.AnalyticsDevices.FirstOrDefault(d => d.DeviceId == g.Key)?.Descriptor ?? new()))
                .OrderByDescending(d => d.LastSeen).ThenBy(d => d.DeviceId).Skip((page - 1) * pageSize).Take(pageSize).ToArray();
            return new DeviceActivityPage("JSON", active.Count, groups.Length, page, pageSize, now, items);
        });
    }
}
