using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class OwnerNotificationTests
{
    [Fact]
    public async Task RejectedOwnerCanResubmitOnceWithNoteThenGainAccessOnlyAfterApproval()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        var owner = platform.CreateUser("fixture", "retry_notice", "Retry fixture", "NotificationFixture!", "poi_owner", true);
        await Login(client, "retry_notice");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/owner/registration-resubmission", new { note = "Pending fixture" })).StatusCode);
        var original = platform.Store.Read(s => s.Reviews.Single(r => r.UserId == owner.Id));
        platform.Review("fixture", original.Id, false, "First rejection fixture");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/owner/registration-resubmission", new { note = " " })).StatusCode);
        var response = await client.PostAsJsonAsync("/api/owner/registration-resubmission", new { note = "  Updated information fixture  " }); response.EnsureSuccessStatusCode();
        var pending = (await response.Content.ReadFromJsonAsync<Review>())!;
        Assert.Equal("Updated information fixture", pending.ApplicantNote); Assert.Equal("pending", pending.Status);
        Assert.Equal("rejected", platform.Store.Read(s => s.Reviews.Single(r => r.Id == original.Id).Status));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/owner/places")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/owner/registration-resubmission", new { note = "Duplicate fixture" })).StatusCode);
        platform.Review("fixture", pending.Id, true, "Approved retry fixture");
        (await client.GetAsync("/api/owner/places")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/owner/registration-resubmission", new { note = "Already verified" })).StatusCode);
        var notification = platform.Store.Read(s => s.Notifications.Single(n => n.ReviewId == pending.Id));
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/owner/notifications/{notification.Id}");
        Assert.Equal("Updated information fixture", detail.GetProperty("review").GetProperty("applicantNote").GetString());
        Assert.Equal(2, platform.Store.Read(s => s.Reviews.Count(r => r.UserId == owner.Id)));
    }
    [Fact]
    public async Task NormalUserCannotUseRegistrationRetryToBecomeOwner()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        platform.CreateUser("fixture", "not_owner_notice", "User fixture", "NotificationFixture!", "user");
        await Login(client, "not_owner_notice");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/owner/registration-resubmission", new { note = "Attempt fixture" })).StatusCode);
        Assert.Empty(platform.Store.Read(s => s.Reviews));
    }
    [Fact]
    public async Task RejectedUnverifiedOwnerCanReadDecisionButCannotManagePlaces()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/owner/notifications/unread-count")).StatusCode);
        var platform = app.Services.GetRequiredService<PlatformService>(); var admin = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        var owner = platform.CreateUser(admin.Id, "pending_notice", "Synthetic pending owner", "NotificationFixture!", "poi_owner", true);
        var review = platform.Store.Read(s => s.Reviews.Single(r => r.UserId == owner.Id));
        platform.Review(admin.Id, review.Id, false, "Synthetic rejection reason");
        await Login(client, "pending_notice");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/owner/places")).StatusCode);
        var notifications = (await client.GetFromJsonAsync<OwnerNotification[]>("/api/owner/notifications"))!;
        var notification = Assert.Single(notifications); Assert.Equal(review.Id, notification.ReviewId);
        var response = await client.GetAsync($"/api/owner/notifications/{notification.Id}"); response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl!.NoStore);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rejected", detail.GetProperty("review").GetProperty("status").GetString());
        Assert.Equal("Synthetic rejection reason", detail.GetProperty("review").GetProperty("adminNote").GetString());
        Assert.False(platform.Store.Read(s => s.Notifications.Single().Read)); // GET never marks read.
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/owner/notifications/unread-count")).GetProperty("unread").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, $"/api/owner/notifications/{notification.Id}/read")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, $"/api/owner/notifications/{notification.Id}/read")).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/owner/notifications/unread-count")).GetProperty("unread").GetInt32());
    }

    [Fact]
    public async Task AnotherAccountCannotReadMarkOrFollowForeignReferences()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        var alice = platform.CreateUser("fixture", "notice_alice", "Alice fixture", "NotificationFixture!", "poi_owner", true);
        var bob = platform.CreateUser("fixture", "notice_bob", "Bob fixture", "NotificationFixture!", "poi_owner", true);
        var bobReview = platform.Store.Read(s => s.Reviews.Single(r => r.UserId == bob.Id));
        platform.Review("fixture", bobReview.Id, true, "Bob private note");
        var bobNotice = platform.Store.Read(s => s.Notifications.Single());
        var ownedByBob = platform.SavePlace("fixture", new Place(0, "Private Bob fixture", "Demo", "Private source", OwnerId: bob.Id, IsActive: false), true);
        await Login(client, "notice_alice");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/owner/notifications/{bobNotice.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, $"/api/owner/notifications/{bobNotice.Id}/read")).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/owner/notifications/unread-count")).GetProperty("unread").GetInt32());
        var importedId = PlatformService.Id();
        platform.Store.Write(s => { s.Notifications.Add(new(importedId, alice.Id, "Synthetic imported record", false, DateTimeOffset.UtcNow, bobReview.Id, ownedByBob.Id)); return true; });
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/owner/notifications/{importedId}");
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("review").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("place").ValueKind);
        Assert.False(platform.Store.Read(s => s.Notifications.Single(n => n.Id == bobNotice.Id).Read));
    }

    [Fact]
    public async Task ApprovedNewPlaceNotificationLinksAllocatedIdAndSurvivesRestart()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        var owner = platform.CreateUser("fixture", "place_notice", "Owner fixture", "NotificationFixture!", "poi_owner");
        var review = platform.Submit(owner.Id, new Place(0, "New submitted fixture", "Demo", "Test source"));
        platform.Review("fixture", review.Id, true, "Approved fixture");
        var notification = platform.Store.Read(s => s.Notifications.Single(n => n.ReviewId == review.Id));
        Assert.True(notification.PlaceId > 0);
        await Login(client, "place_notice");
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/owner/notifications/{notification.Id}");
        Assert.Equal(notification.PlaceId, detail.GetProperty("place").GetProperty("id").GetInt32());
        Assert.Equal(owner.Id, detail.GetProperty("place").GetProperty("ownerId").GetString());
        Assert.Equal("approved", detail.GetProperty("review").GetProperty("status").GetString());
        var reopened = new JsonPlatformStore(Path.Combine(app.Folder, "platform.json"));
        Assert.Equal(notification, reopened.Read(s => s.Notifications.Single(n => n.Id == notification.Id)));
    }

    [Fact]
    public async Task OldUnreadNoticeRemainsReachableBeyondLatestHundredAndLegacyRecordsStillWork()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        var owner = platform.CreateUser("fixture", "legacy_notice", "Legacy fixture", "NotificationFixture!", "poi_owner", true);
        var oldId = PlatformService.Id();
        platform.Store.Write(s => {
            s.Notifications.Add(new(oldId, owner.Id, "Old unread fixture", false, DateTimeOffset.UtcNow.AddDays(-2)));
            for (var index = 0; index < 110; index++) s.Notifications.Add(new(PlatformService.Id(), owner.Id, "Read fixture", true, DateTimeOffset.UtcNow));
            return true;
        });
        await Login(client, "legacy_notice");
        Assert.Equal(100, (await client.GetFromJsonAsync<OwnerNotification[]>("/api/owner/notifications"))!.Length);
        Assert.Equal(oldId, Assert.Single((await client.GetFromJsonAsync<OwnerNotification[]>("/api/owner/notifications?unreadOnly=true"))!).Id);
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/owner/notifications/{oldId}");
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("review").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("place").ValueKind);
    }

    [Fact]
    public async Task DeletingPlaceNotifiesOwnerOfAutomaticallyRejectedPendingSubmission()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        var owner = platform.CreateUser("fixture", "deleted_notice", "Delete fixture", "NotificationFixture!", "poi_owner");
        var place = platform.SavePlace("fixture", new Place(0, "Deletion fixture", "Demo", "Test source", OwnerId: owner.Id), true);
        var review = platform.Submit(owner.Id, place with { Description = "Proposed source" });
        platform.DeletePlace("fixture", place.Id);
        var notification = platform.Store.Read(s => s.Notifications.Single(n => n.ReviewId == review.Id));
        await Login(client, "deleted_notice");
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/owner/notifications/{notification.Id}");
        Assert.Equal("rejected", detail.GetProperty("review").GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("place").ValueKind);
    }
    private static async Task Login(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username, password = "NotificationFixture!" }); response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body.GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, string path) => client.PostAsJsonAsync(path, new { });
}
