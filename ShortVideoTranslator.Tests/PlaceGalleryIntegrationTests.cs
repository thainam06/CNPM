using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class PlaceGalleryIntegrationTests
{
    [Fact]
    public async Task GalleryCrudValidatesAtomicallyRejectsStaleUpdateAndPersistsDeletion()
    {
        using var app = new TestApplication(); using var client = app.CreateClient();
        var platform = app.Services.GetRequiredService<PlatformService>();
        var actor = platform.Authenticate("admin", "HttpBootstrap_2026!")!;
        platform.ChangePassword(actor.Id, "HttpBootstrap_2026!", "TestPassword!");
        var token = await (await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "TestPassword!" })).Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        // External image URLs are validation fixtures; tests never download them.
        var photo = new PlacePhoto("https://example.com/photo.jpg", "Test angle", "Synthetic attribution", "https://example.com/source");
        var create = await client.PostAsJsonAsync("/api/admin/places", new Place(0, "Gallery CRUD fixture", "Demo", "Test source", Images: [photo]));
        create.EnsureSuccessStatusCode(); var place = (await create.Content.ReadFromJsonAsync<Place>())!;
        Assert.Equal(photo, (await client.GetFromJsonAsync<Place>($"/api/places/{place.Id}"))!.Images!.Single());
        var invalid = await client.PutAsJsonAsync($"/api/admin/places/{place.Id}", place with { Name = "Must not persist", Images = [photo with { Url = "javascript:alert(1)" }] });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(place.Name, (await client.GetFromJsonAsync<Place>($"/api/places/{place.Id}"))!.Name);
        var update = await client.PutAsJsonAsync($"/api/admin/places/{place.Id}", place with { Name = "Updated fixture", Images = [photo with { Caption = "Changed angle" }] });
        update.EnsureSuccessStatusCode(); var changed = (await update.Content.ReadFromJsonAsync<Place>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/admin/places/{place.Id}", place)).StatusCode);
        var persisted = new JsonPlatformStore(Path.Combine(app.Folder, "platform.json")).GetById(place.Id)!;
        Assert.Equal("Updated fixture", persisted.Name); Assert.Equal("Changed angle", persisted.Images!.Single().Caption);
        (await client.PutAsJsonAsync($"/api/admin/places/{place.Id}", changed with { IsActive = false, Images = [] })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/places/{place.Id}")).StatusCode);
        (await client.DeleteAsync($"/api/admin/places/{place.Id}")).EnsureSuccessStatusCode();
        var reopened = new JsonPlatformStore(Path.Combine(app.Folder, "platform.json"));
        Assert.DoesNotContain(reopened.Read(s => s.Places), p => p.Id == place.Id);
        Assert.Contains(reopened.Read(s => s.DeletedPlaces), p => p.Id == place.Id);
    }
    [Fact]
    public void PhotoMigrationPreservesCustomGalleryAndDoesNotReturnRemovedPhotosOnRestart()
    {
        var folder = Path.Combine(Path.GetTempPath(), "SmartTourGalleryTests", Guid.NewGuid().ToString("N"));
        try {
            var path = Path.Combine(folder, "platform.json"); var store = new JsonPlatformStore(path);
            store.Write(s => { s.SampleDataVersion = 4; s.Places[0] = s.Places[0] with { Images = [] }; s.Places[1] = s.Places[1] with { Images = null }; return true; });
            var migrated = new JsonPlatformStore(path);
            Assert.Empty(migrated.GetById(1)!.Images!); Assert.NotEmpty(migrated.GetById(2)!.Images!);
            migrated.Write(s => { s.Places[1] = s.Places[1] with { Images = [] }; return true; });
            Assert.Empty(new JsonPlatformStore(path).GetById(2)!.Images!);
        } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
