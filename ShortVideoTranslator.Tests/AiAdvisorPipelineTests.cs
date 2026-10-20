using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class AiAdvisorPipelineTests
{
    // Synthetic provider payloads; no real model, key, weather or external calls in this suite.
    private const string ProxyDraft = "{\"choices\":[{\"message\":{\"content\":\"Synthetic reviewed-later draft.\"}}]}";
    private const string GeminiDraft = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Synthetic reviewed-later draft.\"}]}}]}";
    private record Sent(string Host, string Path, string Body, string? Authorization, bool GoogleKey);
    private class Handler(bool geminiFails = true, bool proxyFails = false, bool staleWeather = false, bool cancelGemini = false) : HttpMessageHandler
    {
        public ConcurrentQueue<Sent> Requests { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri!;
            Requests.Enqueue(new(uri.Host, uri.AbsolutePath, request.Content == null ? "" : await request.Content.ReadAsStringAsync(token), request.Headers.Authorization?.ToString(), request.Headers.Contains("x-goog-api-key")));
            if (uri.Host == "generativelanguage.googleapis.com" && cancelGemini) throw new OperationCanceledException("Synthetic per-provider timeout.");
            if (uri.Host == "api.openweathermap.org") return Json(JsonSerializer.Serialize(new { dt = DateTimeOffset.UtcNow.AddHours(staleWeather ? -2 : 0).ToUnixTimeSeconds(), main = new { temp = 30 }, weather = new[] { new { description = "Synthetic weather fixture" } } }));
            if (uri.Host == "generativelanguage.googleapis.com") return geminiFails ? Json("{}", HttpStatusCode.TooManyRequests) : Json(GeminiDraft);
            return proxyFails ? Json("{}", HttpStatusCode.ServiceUnavailable) : Json(ProxyDraft);
        }
        private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
    private class Clients(Handler handler) : IHttpClientFactory { public HttpClient CreateClient(string name) => new(handler, false); }
    private class PipelineApp(Handler handler, Dictionary<string, string?>? extra = null) : TestApplication
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => {
                var values = new Dictionary<string, string?> { ["Ai:Provider"] = "Auto", ["Ai:ApiKey"] = "fake-gemini-key", ["Ai:Model"] = "test-model", ["Ai:ProxyPal:ApiKey"] = "fake-proxy-key", ["Ai:ProxyPal:Model"] = "test/proxy-model", ["Ai:ProxyPal:BaseUrl"] = "http://localhost:8317/v1", ["Weather:ApiKey"] = "fake-weather-key" };
                foreach (var pair in extra ?? []) values[pair.Key] = pair.Value;
                config.AddInMemoryCollection(values);
            });
            builder.ConfigureTestServices(services => { services.RemoveAll<IHttpClientFactory>(); services.AddSingleton<IHttpClientFactory>(new Clients(handler)); });
        }
    }
    private static async Task<string> Owner(PipelineApp app, HttpClient client)
    {
        var user = app.Services.GetRequiredService<PlatformService>().CreateUser("test", "pipeline_owner", "Fixture", "TestPassword!", "poi_owner");
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username = "pipeline_owner", password = "TestPassword!" }); response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString()); return user.Id;
    }
    [Fact]
    public async Task FallbackUsesProxyProtocolAndChargesOnlyOneSuccessfulDraft()
    {
        var handler = new Handler(); using var app = new PipelineApp(handler); using var client = app.CreateClient(); await Owner(app, client);
        var response = await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn demo", name = "Tên fixture", category = "Demo", address = "Địa chỉ fixture" }); response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ProxyPal", result.GetProperty("provider").GetString()); Assert.True(result.GetProperty("requiresReview").GetBoolean());
        Assert.Contains("Gemini", result.GetProperty("warnings")[0].GetString()); Assert.Equal(1, result.GetProperty("quota").GetProperty("used").GetInt32());
        Assert.Equal(2, handler.Requests.Count); var proxy = handler.Requests.Last();
        Assert.Equal("/v1/chat/completions", proxy.Path); Assert.Equal("Bearer fake-proxy-key", proxy.Authorization); Assert.False(proxy.GoogleKey);
        using var proxyBody = JsonDocument.Parse(proxy.Body);
        using var suppliedSource = JsonDocument.Parse(proxyBody.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
        Assert.Equal("Tên fixture", suppliedSource.RootElement.GetProperty("source").GetProperty("name").GetString());
    }
    [Fact]
    public async Task PerProviderCancellationFallsBackWithoutChargingTwice()
    {
        var handler = new Handler(cancelGemini: true); using var app = new PipelineApp(handler); using var client = app.CreateClient(); await Owner(app, client);
        var response = await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn demo" }); response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("ProxyPal", result.GetProperty("provider").GetString());
        Assert.Equal(1, result.GetProperty("quota").GetProperty("used").GetInt32());
    }
    [Fact]
    public async Task AdminIsUnlimitedAndReservationsDoNotSurviveServiceRestart()
    {
        var handler = new Handler(); using var app = new PipelineApp(handler); using var client = app.CreateClient(); var owner = await Owner(app, client);
        var quota = app.Services.GetRequiredService<AiQuotaService>();
        for (var i = 0; i < 12; i++) { using var completed = quota.Reserve("synthetic-admin", true); completed.Commit(); }
        Assert.Null(quota.Usage("synthetic-admin", true).Limit); Assert.Equal(12, quota.Usage("synthetic-admin", true).Used);
        using var unfinished = quota.Reserve(owner, false);
        var recovered = new AiQuotaService(app.Services.GetRequiredService<IPlatformStore>(), TimeProvider.System);
        Assert.Equal(0, recovered.Usage(owner, false).InFlight); Assert.Equal(0, recovered.Usage(owner, false).Used);
        Assert.Equal(12, recovered.Usage("synthetic-admin", true).Used);
    }
    [Fact]
    public async Task AllProviderFailuresAndMissingConfigDoNotConsumeQuota()
    {
        var handler = new Handler(proxyFails: true); using var app = new PipelineApp(handler); using var client = app.CreateClient(); await Owner(app, client);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn demo" })).StatusCode);
        var usage = await client.GetFromJsonAsync<JsonElement>("/api/ai/usage"); Assert.Equal(0, usage.GetProperty("used").GetInt32()); Assert.Equal(0, usage.GetProperty("inFlight").GetInt32()); Assert.Equal(10, usage.GetProperty("remaining").GetInt32());
        Assert.DoesNotContain("fake-", usage.ToString());
    }
    [Fact]
    public async Task WeatherUsesOwnedVerifiedPoiIsCachedAndNotRequestedWithoutOptIn()
    {
        var handler = new Handler(geminiFails: false); using var app = new PipelineApp(handler); using var client = app.CreateClient(); var owner = await Owner(app, client);
        var store = app.Services.GetRequiredService<IPlatformStore>();
        store.Write(s => { s.Places[0] = s.Places[0] with { OwnerId = owner }; return true; });
        (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn demo", placeId = 1 })).EnsureSuccessStatusCode();
        Assert.DoesNotContain(handler.Requests, p => p.Host == "api.openweathermap.org");
        for (var i = 0; i < 2; i++) {
            var response = await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn demo", placeId = 1, useWeather = true }); response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("OpenWeather", body.GetProperty("weather").GetProperty("provider").GetString());
        }
        Assert.Single(handler.Requests, p => p.Host == "api.openweathermap.org");
        var before = handler.Requests.Count;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn demo", placeId = 2, useWeather = true })).StatusCode);
        Assert.Equal(before, handler.Requests.Count);
    }
    [Fact]
    public async Task StaleWeatherIsOmittedWithWarningInsteadOfInventedContext()
    {
        var handler = new Handler(geminiFails: false, staleWeather: true); using var app = new PipelineApp(handler); using var client = app.CreateClient(); var owner = await Owner(app, client);
        app.Services.GetRequiredService<IPlatformStore>().Write(s => { s.Places[0] = s.Places[0] with { OwnerId = owner }; return true; });
        var result = await (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn demo", placeId = 1, useWeather = true })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, result.GetProperty("weather").ValueKind); Assert.NotEmpty(result.GetProperty("warnings").EnumerateArray());
        Assert.DoesNotContain("Synthetic weather fixture", handler.Requests.Last().Body);
    }
    [Theory]
    [InlineData("http://remote.example/v1")]
    [InlineData("https://user:password@example.com/v1")]
    [InlineData("https://example.com/v1?key=secret")]
    public async Task InvalidProxyDestinationIsNotCalled(string url)
    {
        var handler = new Handler(); using var app = new PipelineApp(handler, new() { ["Ai:Provider"] = "ProxyPal", ["Ai:ProxyPal:BaseUrl"] = url }); using var client = app.CreateClient(); await Owner(app, client);
        var usage = await client.GetFromJsonAsync<JsonElement>("/api/ai/usage"); Assert.False(usage.GetProperty("ready").GetBoolean());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nguồn" })).StatusCode); Assert.Empty(handler.Requests);
    }
    [Fact]
    public async Task ConcurrentReservationsCannotExceedOwnerLimitAndFailureReleasesCapacity()
    {
        var handler = new Handler(); using var app = new PipelineApp(handler); using var client = app.CreateClient(); var owner = await Owner(app, client);
        var quota = app.Services.GetRequiredService<AiQuotaService>();
        var holds = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => quota.Reserve(owner, false))));
        Assert.Equal(429, Assert.Throws<PlatformException>(() => quota.Reserve(owner, false)).StatusCode);
        Assert.Equal(0, quota.Usage(owner, false).Used); Assert.Equal(10, quota.Usage(owner, false).InFlight);
        holds[0].Dispose(); using var retry = quota.Reserve(owner, false); retry.Commit();
        foreach (var hold in holds) hold.Dispose();
        Assert.Equal(1, quota.Usage(owner, false).Used); Assert.Equal(0, quota.Usage(owner, false).InFlight);
    }
}
