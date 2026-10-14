using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class OwnerIdentityTests
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Current { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Current;
    }
    [Fact]
    public void IdentityIsEncryptedRejectsTamperingAndExpiresAfter180Days()
    {
        var folder = Path.Combine(Path.GetTempPath(), "SmartTourIdentityTests", Guid.NewGuid().ToString("N"));
        try {
            var path = Path.Combine(folder, "platform.json"); var store = new JsonPlatformStore(path);
            var platform = new PlatformService(store);
            var owner = platform.CreateUser("test", "identity_owner", "Test owner", "TestPassword!", "poi_owner", true);
            var clock = new TestClock(); var identities = new OwnerIdentityService(new EphemeralDataProtectionProvider(), store, clock);
            const string syntheticNumber = "111111111111";
            identities.Save(owner.Id, syntheticNumber);
            Assert.DoesNotContain(syntheticNumber, File.ReadAllText(path));
            Assert.Equal(syntheticNumber, identities.Read(owner.Id));
            store.Write(s => { s.OwnerIdentities[0] = s.OwnerIdentities[0] with { Ciphertext = "v1:tampered" }; return true; });
            Assert.Null(identities.Read(owner.Id));
            identities.Save(owner.Id, syntheticNumber); clock.Current = clock.Current.AddDays(180);
            Assert.Null(identities.Read(owner.Id)); Assert.Equal(1, identities.RedactExpired()); Assert.Empty(store.Read(s => s.OwnerIdentities));
        } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    private static async Task SignIn(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username, password = "TestPassword!" }); response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new("Bearer", body.GetProperty("accessToken").GetString());
    }
    [Fact]
    public async Task OwnerIdentityIsScopedMaskedAndAbsentFromUserList()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        var first = platform.CreateUser("test", "identity_first", "Test first", "TestPassword!", "poi_owner", true);
        var second = platform.CreateUser("test", "identity_second", "Test second", "TestPassword!", "poi_owner", true);
        await SignIn(client, "identity_first");
        (await client.PutAsJsonAsync("/api/owner/identity", new { number = "111111111111", userId = second.Id })).EnsureSuccessStatusCode();
        var own = await client.GetFromJsonAsync<JsonElement>("/api/owner/identity");
        Assert.Equal("********1111", own.GetProperty("masked").GetString());
        Assert.DoesNotContain("111111111111", own.ToString());
        Assert.Equal(first.Id, Assert.Single(platform.Store.Read(s => s.OwnerIdentities)).UserId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/admin/users/{first.Id}/identity")).StatusCode);
        await SignIn(client, "identity_second");
        Assert.False((await client.GetFromJsonAsync<JsonElement>("/api/owner/identity")).GetProperty("present").GetBoolean());
        var admin = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(admin.Id, "HttpBootstrap_2026!", "TestPassword!"); await SignIn(client, "admin");
        var revealed = await client.GetAsync($"/api/admin/users/{first.Id}/identity");
        Assert.Equal("no-store", revealed.Headers.CacheControl!.ToString());
        Assert.Contains("111111111111", await revealed.Content.ReadAsStringAsync());
        var users = await (await client.GetAsync("/api/admin/users")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("111111111111", users); Assert.DoesNotContain("ciphertext", users, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(platform.Store.Read(s => s.Audit), a => a.Action == "identity.read");
    }
}
