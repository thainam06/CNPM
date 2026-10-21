using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Tests;

public class TestApplication : WebApplicationFactory<Program>
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "SmartTourHttpTests", Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
            ["Storage:Directory"] = Folder, ["Admin:BootstrapPassword"] = "HttpBootstrap_2026!", ["Audio:WorkersEnabled"] = "false", ["Analytics:WorkerEnabled"] = "false"
            , ["Security:JwtSecret"] = "test-only-signing-secret-32-characters-minimum"
        }));
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (Directory.Exists(Folder)) Directory.Delete(Folder, true); }
}
public class AdminIntegrationTests
{
    [Fact]
    public async Task ContentEtagAndDeltaRemoveHiddenAndDeletedPlaces()
    {
        using var app = new TestApplication(); using var client = app.CreateClient(); await LoginAdmin(client);
        var snapshot = await client.GetAsync("/api/content?language=ja");
        var body = await snapshot.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("full", body.GetProperty("syncMode").GetString());
        Assert.All(body.GetProperty("items").EnumerateArray(), item => {
            Assert.True(item.GetProperty("fallback").GetBoolean());
            Assert.Equal("vi", item.GetProperty("contentLanguage").GetString());
        });
        using var conditional = new HttpRequestMessage(HttpMethod.Get, "/api/content?language=ja");
        conditional.Headers.IfNoneMatch.Add(snapshot.Headers.ETag!);
        Assert.Equal(HttpStatusCode.NotModified, (await client.SendAsync(conditional)).StatusCode);
        var cursor = body.GetProperty("updatedAt").GetDateTimeOffset();
        var places = (await client.GetFromJsonAsync<Place[]>("/api/admin/places"))!;
        (await Send(client, $"/api/admin/places/{places[0].Id}", places[0] with { IsActive = false }, HttpMethod.Put)).EnsureSuccessStatusCode();
        (await Send(client, $"/api/admin/places/{places[1].Id}", new { }, HttpMethod.Delete)).EnsureSuccessStatusCode();
        var delta = await client.GetFromJsonAsync<JsonElement>($"/api/content?since={Uri.EscapeDataString(cursor.ToString("O"))}");
        Assert.Equal("delta", delta.GetProperty("syncMode").GetString());
        Assert.Contains(places[0].Id, delta.GetProperty("removedIds").EnumerateArray().Select(p => p.GetInt32()));
        Assert.Contains(places[1].Id, delta.GetProperty("removedIds").EnumerateArray().Select(p => p.GetInt32()));
        var old = await client.GetFromJsonAsync<JsonElement>($"/api/content?since={Uri.EscapeDataString(cursor.AddDays(-31).ToString("O"))}");
        Assert.Equal("full", old.GetProperty("syncMode").GetString());
    }
    [Fact]
    public async Task UiBundlesNeverClaimUnavailableLanguageIsTranslated()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var supported = await client.GetFromJsonAsync<JsonElement>("/api/ui-bundles/ja");
        Assert.Equal("ready", supported.GetProperty("status").GetString());
        var unknown = await client.GetFromJsonAsync<JsonElement>("/api/ui-bundles/unknown");
        Assert.Equal("unavailable", unknown.GetProperty("status").GetString());
        Assert.Equal("en", unknown.GetProperty("locale").GetString());
    }
    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, object body, HttpMethod? method = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var message = new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        message.Headers.Add("X-CSRF-Token", csrf.GetProperty("token").GetString()); return await client.SendAsync(message);
    }
    private static async Task LoginAdmin(HttpClient client)
    {
        (await Send(client, "/api/auth/login", new { username = "admin", password = "HttpBootstrap_2026!" })).EnsureSuccessStatusCode();
        (await Send(client, "/api/auth/change-password", new { currentPassword = "HttpBootstrap_2026!", newPassword = "HttpChanged_2026!" })).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task AuthenticationCsrfAndCookieFlagsAreEnforced()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/places")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "HttpBootstrap_2026!" })).StatusCode);
        var login = await Send(client, "/api/auth/login", new { username = "admin", password = "HttpBootstrap_2026!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), header => header.Contains("SmartTour.Session") && header.Contains("httponly", StringComparison.OrdinalIgnoreCase) && header.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/places")).StatusCode);
        (await Send(client, "/api/auth/change-password", new { currentPassword = "HttpBootstrap_2026!", newPassword = "HttpChanged_2026!" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/places")).StatusCode);
        Assert.DoesNotContain("passwordHash", await (await client.GetAsync("/api/admin/users")).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task AdminCrudPersistsAndPublicCatalogExcludesHiddenPlace()
    {
        using var app = new TestApplication(); using var client = app.CreateClient(); await LoginAdmin(client);
        var created = await Send(client, "/api/admin/places", new Place(0, "HTTP test place", "Demo", "Test content", IsActive: false));
        created.EnsureSuccessStatusCode(); var place = (await created.Content.ReadFromJsonAsync<Place>())!;
        Assert.DoesNotContain((await client.GetFromJsonAsync<Place[]>("/api/places"))!, item => item.Id == place.Id);
        var updatedResponse = await Send(client, $"/api/admin/places/{place.Id}", place with { Name = "Updated HTTP POI", Description = "Updated narration", Address = "Demo address, not verified", IsActive = true }, HttpMethod.Put);
        updatedResponse.EnsureSuccessStatusCode(); var updated = (await updatedResponse.Content.ReadFromJsonAsync<Place>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, $"/api/admin/places/{place.Id}", place, HttpMethod.Put)).StatusCode);
        var published = (await client.GetFromJsonAsync<Place>($"/api/places/{place.Id}"))!;
        Assert.Equal("Updated HTTP POI", published.Name); Assert.Equal("Updated narration", published.Description);
        Assert.Equal(updated.Address, published.Address);
        var persisted = new ShortVideoTranslator.Data.JsonPlatformStore(Path.Combine(app.Folder, "platform.json"));
        Assert.Equal("Updated narration", persisted.GetById(place.Id)!.Description);
        var hiddenResponse = await Send(client, $"/api/admin/places/{place.Id}", updated with { IsActive = false }, HttpMethod.Put);
        hiddenResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/places/{place.Id}")).StatusCode);
        Assert.Contains((await client.GetFromJsonAsync<Place[]>("/api/admin/places"))!, item => item.Id == place.Id && !item.IsActive);
        var hidden = (await hiddenResponse.Content.ReadFromJsonAsync<Place>())!;
        (await Send(client, $"/api/admin/places/{place.Id}", hidden with { IsActive = true }, HttpMethod.Put)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/places/{place.Id}")).StatusCode);
        (await Send(client, $"/api/admin/places/{place.Id}", new { }, HttpMethod.Delete)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/places/{place.Id}")).StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<Place[]>("/api/admin/places"))!, item => item.Id == place.Id);
        Assert.Null(new ShortVideoTranslator.Data.JsonPlatformStore(Path.Combine(app.Folder, "platform.json")).GetById(place.Id));
    }
    [Fact]
    public async Task OwnerCannotReadAdminOrSubmitBeforeReview()
    {
        using var app = new TestApplication(); using var admin = app.CreateClient(); using var owner = app.CreateClient(); await LoginAdmin(admin);
        (await Send(owner, "/api/auth/register-owner", new { username = "httpowner", displayName = "Test owner", password = "OwnerPassword_2026!" })).EnsureSuccessStatusCode();
        (await Send(owner, "/api/auth/login", new { username = "httpowner", password = "OwnerPassword_2026!" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/places")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/owner/places")).StatusCode);
        var reviews = (await admin.GetFromJsonAsync<Review[]>("/api/admin/reviews"))!;
        (await Send(admin, $"/api/admin/reviews/{reviews.Single().Id}/decision", new { approve = true, adminNote = "Test verified" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/owner/places")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(owner, "/api/owner/submissions", new Place(1, "Attack", "Test", "Test"))).StatusCode);
        var submitted = await Send(owner, "/api/owner/submissions", new Place(0, "Owner HTTP test", "Demo", "Test")); submitted.EnsureSuccessStatusCode();
        var review = (await submitted.Content.ReadFromJsonAsync<Review>())!;
        (await Send(admin, $"/api/admin/reviews/{review.Id}/decision", new { approve = true, adminNote = "Approved" })).EnsureSuccessStatusCode();
        Assert.Contains((await owner.GetFromJsonAsync<Place[]>("/api/owner/places"))!, place => place.Name == "Owner HTTP test");
        Assert.Equal(2, (await owner.GetFromJsonAsync<OwnerNotification[]>("/api/owner/notifications"))!.Length);
    }
    [Fact]
    public async Task ConsentAndMissingExternalResourcesAreReportedHonestly()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/analytics/events", new { consent = false, deviceId = Guid.NewGuid().ToString(), kind = "view", language = "vi" })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/maps/pack-manifest")).StatusCode);
        await LoginAdmin(client);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Send(client, "/api/ai/enhance-description", new { text = "Test source" })).StatusCode);
        var job = await Send(client, "/api/admin/audio-tasks", new { placeId = 1, languages = new[] { "vi" } }); job.EnsureSuccessStatusCode();
        var task = (await job.Content.ReadFromJsonAsync<AudioTaskState>())!;
        (await Send(client, $"/api/admin/audio-tasks/{task.Id}/pause", new { })).EnsureSuccessStatusCode();
        (await Send(client, $"/api/admin/audio-tasks/{task.Id}/resume", new { })).EnsureSuccessStatusCode();
        (await Send(client, $"/api/admin/audio-tasks/{task.Id}/cancel", new { })).EnsureSuccessStatusCode();
        Assert.Equal("cancelled", (await client.GetFromJsonAsync<AudioTaskState[]>("/api/admin/audio-tasks"))!.Single().Status);
    }
    [Fact]
    public async Task ContentEditorCannotPublishGrantSuperAdminOrReadAccounts()
    {
        using var app = new TestApplication(); using var admin = app.CreateClient(); using var editor = app.CreateClient(); await LoginAdmin(admin);
        (await Send(admin, "/api/admin/roles", new PlatformRole("editor", ["poi:read", "poi:create", "poi:update"]))).EnsureSuccessStatusCode();
        (await Send(admin, "/api/admin/users", new { username = "editor", displayName = "Editor", password = "EditorPassword_2026!", role = "editor" })).EnsureSuccessStatusCode();
        (await Send(editor, "/api/auth/login", new { username = "editor", password = "EditorPassword_2026!" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await editor.GetAsync("/api/admin/places")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await editor.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editor, "/api/admin/places", new Place(0, "Test", "Demo", "Test", IsActive: true))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editor, "/api/admin/users", new { username = "attack", displayName = "Attack", password = "AttackPassword_2026!", role = "super_admin" })).StatusCode);
    }
    [Fact]
    public async Task BearerTokenAndRotatingRefreshAreValidated()
    {
        using var app = new TestApplication(); using var admin = app.CreateClient(); await LoginAdmin(admin);
        using var mobile = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var response = await mobile.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "HttpChanged_2026!" }); response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        mobile.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await mobile.GetAsync("/api/admin/places")).StatusCode);
        var refresh = token.GetProperty("refreshToken").GetString();
        (await mobile.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = refresh })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await mobile.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = refresh })).StatusCode);
        (await Send(admin, "/api/auth/change-password", new { currentPassword = "HttpChanged_2026!", newPassword = "AnotherTestPassword!" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await mobile.GetAsync("/api/admin/places")).StatusCode);
    }
}
