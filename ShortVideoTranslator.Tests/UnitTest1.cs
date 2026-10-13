
using ShortVideoTranslator.Business;
using Xunit;

namespace ShortVideoTranslator.Tests;

public class SpeechSegmentTests
{
    [Fact]
    public void SpeechSegment_ShouldStoreCorrectText()
    {
        var segment = new SpeechSegment(
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(5),
            "Xin chào mọi người");

        Assert.Equal(
            "Xin chào mọi người",
            segment.Text);
    }

    [Fact]
    public void SpeechSegment_ShouldStoreCorrectTime()
    {
        var segment = new SpeechSegment(
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(7),
            "Đây là video thử nghiệm");

        Assert.Equal(
            TimeSpan.FromSeconds(2),
            segment.Start);

        Assert.Equal(
            TimeSpan.FromSeconds(7),
            segment.End);
    }

    [Fact]
    public void SpeechSegment_ShouldSupportVietnamese()
    {
        var segment = new SpeechSegment(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(3),
            "Hệ thống dịch phụ đề tự động");

        Assert.Contains("phụ đề", segment.Text);
    }
}
