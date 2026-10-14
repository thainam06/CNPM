using System.Net;
using System.Text;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class TranslationServiceTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return respond(request, token); }
    }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task VietnameseDoesNotContactProvider()
    {
        var handler = new FakeHandler((_, _) => throw new InvalidOperationException("Should not call provider"));
        var service = new TranslationService(new HttpClient(handler), new());
        var result = await service.TranslateAsync("Nội dung tiếng Việt", "vi");
        Assert.Equal("Nội dung tiếng Việt", result.Text);
        Assert.False(result.IsMachineTranslation);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData("en")][InlineData("zh")][InlineData("ja")][InlineData("ko")][InlineData("fr")]
    public async Task TranslatesBeforeSpeechAndCachesSuccessfulResult(string language)
    {
        var handler = new FakeHandler((request, _) => {
            var uri = Uri.UnescapeDataString(request.RequestUri!.Query);
            Assert.Contains("langpair=vi|" + (language == "zh" ? "zh-CN" : language), uri);
            return Task.FromResult(Json("""{"responseStatus":200,"responseData":{"translatedText":"Real &amp; translated text"}}"""));
        });
        var service = new TranslationService(new HttpClient(handler), new());
        var result = await service.TranslateAsync("Nội dung tiếng Việt", language);
        Assert.Equal("Real & translated text", result.Text);
        Assert.Equal(language, result.Language);
        Assert.True(result.IsMachineTranslation);
        Assert.Equal(result, await service.TranslateAsync("Nội dung tiếng Việt", language));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("{\"responseStatus\":429,\"responseData\":{\"translatedText\":\"QUOTA EXCEEDED\"}}")]
    [InlineData("{\"responseStatus\":200,\"quotaFinished\":true,\"responseData\":{\"translatedText\":\"quota\"}}")]
    [InlineData("{\"responseStatus\":200,\"responseData\":{\"translatedText\":\"\"}}")]
    [InlineData("{\"responseStatus\":200,\"responseData\":{\"translatedText\":\"Xin chào\"}}")]
    [InlineData("{\"responseStatus\":200}")]
    [InlineData("not json")]
    public async Task ProviderFailureDoesNotBecomeFakeTranslationOrCachedSuccess(string response)
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(Json(response)));
        var service = new TranslationService(new HttpClient(handler), new());
        await Assert.ThrowsAsync<TranslationUnavailableException>(() => service.TranslateAsync("Xin chào", "en"));
        await Assert.ThrowsAsync<TranslationUnavailableException>(() => service.TranslateAsync("Xin chào", "en"));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task DisabledProviderReportsUnavailableButVietnameseStillWorks()
    {
        var service = new TranslationService(new HttpClient(), new() { Enabled = false });
        await Assert.ThrowsAsync<TranslationUnavailableException>(() => service.TranslateAsync("Xin chào", "en"));
        Assert.Equal("Xin chào", (await service.TranslateAsync("Xin chào", "vi")).Text);
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        var handler = new FakeHandler(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return Json("{}"); });
        var service = new TranslationService(new HttpClient(handler), new());
        using var cancel = new CancellationTokenSource();
        var task = service.TranslateAsync("Xin chào", "en", cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task ProviderTimeoutIsReportedAsUnavailable()
    {
        var handler = new FakeHandler((_, _) => throw new TaskCanceledException());
        var service = new TranslationService(new HttpClient(handler), new());
        await Assert.ThrowsAsync<TranslationUnavailableException>(() => service.TranslateAsync("Xin chào", "en"));
    }

    [Fact]
    public async Task NetworkFailureIsReportedAsUnavailable()
    {
        var handler = new FakeHandler((_, _) => throw new HttpRequestException());
        var service = new TranslationService(new HttpClient(handler), new());
        await Assert.ThrowsAsync<TranslationUnavailableException>(() => service.TranslateAsync("Xin chào", "en"));
    }

    [Fact]
    public void ChunksFitProviderLimitAndPreserveUnicode()
    {
        var text = string.Join(' ', Enumerable.Repeat("Thuyết minh tiếng Việt 🧭", 100));
        var chunks = TranslationService.SplitText(text);
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.InRange(Encoding.UTF8.GetByteCount(chunk), 1, 500));
        Assert.Equal(text, string.Join(' ', chunks));
        var noSpaces = string.Concat(Enumerable.Repeat("🧭", 400));
        Assert.Equal(noSpaces, string.Concat(TranslationService.SplitText(noSpaces)));
    }

    [Theory]
    [InlineData("", "vi")][InlineData(" ", "en")][InlineData("Xin chào", "de")]
    public async Task InvalidInputIsRejected(string text, string language)
    {
        var service = new TranslationService(new HttpClient(), new());
        await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateAsync(text, language));
    }

    [Fact]
    public async Task OversizedInputIsRejected()
    {
        var service = new TranslationService(new HttpClient(), new());
        await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateAsync(new string('a', 3001), "vi"));
    }

    [Fact]
    public async Task SpeechCancellationDoesNotStartPython()
    {
        var service = new TextToSpeechService(new() { PythonExecutable = "does-not-exist" });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateSpeechAsync("Xin chào", "vi", new CancellationToken(true)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateSpeechAsync("Xin chào", "de"));
    }
}
