using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Api.Controllers;

namespace ShortVideoTranslator.Tests;

public class RuntimeIntegrationTests
{
    [Fact]
    public async Task RuntimeNeedsSeparateConsentAndRejectsInaccurateOrInvalidLocation()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var sample = new RuntimeLocationRequest(false, Guid.NewGuid().ToString(), 10.761234, 106.704567, 20);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/runtime/location", sample)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/runtime/location", sample with { Consent = true, Accuracy = 150 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/runtime/location", sample with { Consent = true, Latitude = 91 })).StatusCode);
        Assert.Empty(app.Services.GetRequiredService<IPlatformStore>().Read(s => s.RuntimeLocations));
    }
    [Fact]
    public async Task RuntimeRoundsCoordinatesDoesNotMixAnalyticsAndRevokesLateRequests()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var sample = new RuntimeLocationRequest(true, Guid.NewGuid().ToString(), 10.761234, 106.704567, 20);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/runtime/location", sample)).StatusCode);
        var store = app.Services.GetRequiredService<IPlatformStore>();
        var item = Assert.Single(store.Read(s => s.RuntimeLocations));
        Assert.Equal(10.761, item.Latitude); Assert.Equal(106.705, item.Longitude);
        Assert.Empty(store.Read(s => s.Events));
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/runtime/location", sample)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/runtime/forget", new { sample.SessionId })).StatusCode);
        Assert.Empty(store.Read(s => s.RuntimeLocations));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/runtime/location", sample with { SessionId = sample.SessionId.ToUpperInvariant() })).StatusCode);
    }
    [Fact]
    public async Task RuntimeWindowIsRestrictedAndHasBoundedHours()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/runtime/window")).StatusCode);
        var platform = app.Services.GetRequiredService<PlatformService>();
        platform.CreateUser("test", "window_user", "Test account", "TestUserPassword!", "user");
        var login = await client.PostAsJsonAsync("/api/auth/token", new { username = "window_user", password = "TestUserPassword!" });
        var token = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/runtime/window")).StatusCode);
        var admin = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(admin.Id, "HttpBootstrap_2026!", "TestAdminPassword!");
        login = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "TestAdminPassword!" });
        token = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/runtime/window?hours=24")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/runtime/window?hours=25")).StatusCode);
    }
}
