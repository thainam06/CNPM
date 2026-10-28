using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class NarrationVoiceCatalogTests
{
    [Theory]
    [InlineData("vi", "vi-VN-HoaiMyNeural")]
    [InlineData("en", "en-US-JennyNeural")]
    [InlineData("zh", "zh-CN-XiaoxiaoNeural")]
    [InlineData("ja", "ja-JP-NanamiNeural")]
    [InlineData("ko", "ko-KR-SunHiNeural")]
    [InlineData("fr", "fr-FR-DeniseNeural")]
    public void SupportedLanguagesKeepTheirExistingSpeechVoice(string language, string voice)
    {
        Assert.Equal(voice, NarrationVoiceCatalog.Resolve(language));
    }

    [Fact]
    public void EveryAcceptedNarrationLanguageHasAVoice()
    {
        foreach (var language in NarrationLanguages.Codes)
            Assert.False(string.IsNullOrWhiteSpace(NarrationVoiceCatalog.Resolve(language)));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("")]
    public void UnsupportedLanguageCannotSilentlyUseVietnameseVoice(string language)
    {
        Assert.Throws<ArgumentException>(() => NarrationVoiceCatalog.Resolve(language));
    }
}
