using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class AiIntegrationTests
{
    private class StubHandler(string body, HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? SentBody { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; SentBody = await request.Content!.ReadAsStringAsync(token);
            return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
    private class StubClients(StubHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };
    }
    private class AiApplication(StubHandler handler) : TestApplication
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Ai:ApiKey"] = "synthetic-test-key", ["Ai:Model"] = "test-model"
            }));
            builder.ConfigureTestServices(services => { services.RemoveAll<IHttpClientFactory>(); services.AddSingleton<IHttpClientFactory>(new StubClients(handler)); });
        }
    }
    private static async Task OwnerLogin(TestApplication app, HttpClient client)
    {
        app.Services.GetRequiredService<PlatformService>().CreateUser("test", "ai_owner", "Test AI owner", "TestPassword!", "poi_owner");
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username = "ai_owner", password = "TestPassword!" });
        response.EnsureSuccessStatusCode(); var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body.GetProperty("accessToken").GetString());
    }
    [Fact]
    public async Task MissingAiConfigurationIsExplicitlyUnavailable()
    {
        using var app = new TestApplication(); using var client = app.CreateClient(); await OwnerLogin(app, client);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nội dung demo." })).StatusCode);
    }
    [Theory]
    [InlineData("{\"candidates\":[]}", HttpStatusCode.OK)]
    [InlineData("not json", HttpStatusCode.OK)]
    [InlineData("{}", HttpStatusCode.TooManyRequests)]
    public async Task EmptyMalformedAndQuotaProviderResponsesReturn503(string responseBody, HttpStatusCode providerStatus)
    {
        var handler = new StubHandler(responseBody, providerStatus);
        using var app = new AiApplication(handler); using var client = app.CreateClient(); await OwnerLogin(app, client);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nội dung demo." })).StatusCode);
    }
    [Fact]
    public async Task ValidProviderDraftRequiresReviewAndOwnerCannotExceedTenDailyRequests()
    {
        var handler = new StubHandler("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Synthetic provider response for tests only.\"}]}}]}", HttpStatusCode.OK);
        using var app = new AiApplication(handler); using var client = app.CreateClient(); await OwnerLogin(app, client);
        for (var i = 0; i < 10; i++) {
            var response = await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nội dung demo." }); response.EnsureSuccessStatusCode();
            Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requiresReview").GetBoolean());
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/ai/enhance-description", new { text = "Nội dung demo." })).StatusCode);
        Assert.Equal(10, handler.Calls);
        Assert.Contains("systemInstruction", handler.SentBody);
        var usage = await client.GetFromJsonAsync<JsonElement>("/api/ai/usage"); Assert.Equal(10, usage.GetProperty("used").GetInt32());
    }
}
