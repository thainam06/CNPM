using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class PageViewStatisticsTests
{
    [Fact]
    public void RollingWindowCountsOnlyWebsiteOpensIncludingBoundaryAndExcludingFuture()
    {
        var now = DateTimeOffset.Parse("2026-11-19T14:00:00+07:00");
        var events = new[] {
            new AnalyticsEvent("1","a","page_view",null,"vi",now.AddHours(-5)),
            new AnalyticsEvent("2","a","page_view",null,"vi",now),
            new AnalyticsEvent("3","b","page_view",null,"vi",now.AddHours(-5).AddTicks(-1)),
            new AnalyticsEvent("4","b","page_view",null,"vi",now.AddSeconds(1)),
            new AnalyticsEvent("5","a","view",1,"vi",now),
            new AnalyticsEvent("6","a","heartbeat",null,"vi",now)
        };
        Assert.Equal(2, PageViewStatistics.LastFiveHours(events, now));
        Assert.Equal(0, PageViewStatistics.LastFiveHours(events, now.AddHours(6)));
    }
}
