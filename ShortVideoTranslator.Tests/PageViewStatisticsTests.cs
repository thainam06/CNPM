using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class PageViewStatisticsTests
{
    [Fact]
    public void FiveRollingTotalsShareOneTimestampAndIncludeEachBoundary()
    {
        var now = DateTimeOffset.Parse("2026-11-26T08:00:00Z");
        var events = Enumerable.Range(1, 6).Select(age => new AnalyticsEvent(age.ToString(), "a", "page_view", null, "vi", now.AddHours(-age)))
            .Append(new("future", "a", "page_view", null, "vi", now.AddSeconds(1)))
            .Append(new("poi", "a", "view", 1, "vi", now));
        var totals = PageViewStatistics.FirstFiveHours(events, now);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, totals.Select(t => t.Hours));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, totals.Select(t => t.PageViews));
        Assert.All(PageViewStatistics.FirstFiveHours([], now), t => Assert.Equal(0, t.PageViews));
    }
    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 2)]
    [InlineData(24, 3)]
    public void SelectableHoursChangeTheRollingWindow(int hours, int expected)
    {
        var now = DateTimeOffset.UtcNow;
        var events = new[] { 1, 4, 24, 25 }.Select(age => new AnalyticsEvent(age.ToString(), "a", "page_view", null, "vi", now.AddHours(-age)));
        Assert.Equal(expected, PageViewStatistics.LastHours(events, now, hours));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    public void RejectsOutOfRangeHours(int hours) => Assert.Equal(400,
        Assert.Throws<PlatformException>(() => PageViewStatistics.LastHours([], DateTimeOffset.UtcNow, hours)).StatusCode);

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
