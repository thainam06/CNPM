namespace ShortVideoTranslator.Business;

public static class PageViewStatistics
{
    public static int LastFiveHours(IEnumerable<AnalyticsEvent> events, DateTimeOffset now) =>
        events.Count(e => e.Kind == "page_view" && e.CreatedAt >= now.AddHours(-5) && e.CreatedAt <= now);
}
