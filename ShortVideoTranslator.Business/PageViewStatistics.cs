namespace ShortVideoTranslator.Business;

public static class PageViewStatistics
{
    public static PageViewHourCount[] FirstFiveHours(IEnumerable<AnalyticsEvent> events, DateTimeOffset now)
    {
        var recent = events.Where(e => e.Kind == "page_view" && e.CreatedAt >= now.AddHours(-5) && e.CreatedAt <= now).ToArray();
        return Enumerable.Range(1, 5).Select(hours => new PageViewHourCount(hours, LastHours(recent, now, hours))).ToArray();
    }
    public static int LastFiveHours(IEnumerable<AnalyticsEvent> events, DateTimeOffset now) => LastHours(events, now, 5);

    public static int LastHours(IEnumerable<AnalyticsEvent> events, DateTimeOffset now, int hours)
    {
        if (hours is < 1 or > 24) throw new PlatformException(400, "Số giờ thống kê phải từ 1 đến 24.");
        var start = now.AddHours(-hours);
        return events.Count(e => e.Kind == "page_view" && e.CreatedAt >= start && e.CreatedAt <= now);
    }
}
public record PageViewHourCount(int Hours, int PageViews);
