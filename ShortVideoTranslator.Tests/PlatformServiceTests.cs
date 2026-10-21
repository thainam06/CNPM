using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class PlatformServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "SmartTourTests", Guid.NewGuid().ToString("N"));
    private readonly JsonPlatformStore _store;
    private readonly PlatformService _service;
    public PlatformServiceTests() { _store = new(Path.Combine(_folder, "platform.json")); _service = new(_store); }
    private string Admin()
    {
        _service.Bootstrap("admin", "TestBootstrap_2026!");
        var user = _service.Authenticate("admin", "TestBootstrap_2026!")!;
        _service.ChangePassword(user.Id, "TestBootstrap_2026!", "TestChanged_2026!"); return user.Id;
    }
    [Fact]
    public void PasswordHashIsSaltedAndRejectsWrongPassword()
    {
        var first = PlatformService.HashPassword("LongTestPassword!");
        Assert.NotEqual(first, PlatformService.HashPassword("LongTestPassword!"));
        Assert.True(PlatformService.VerifyPassword("LongTestPassword!", first));
        Assert.False(PlatformService.VerifyPassword("IncorrectPassword!", first));
    }
    [Fact]
    public void SevenCharacterPasswordWorksAndShorterPasswordIsRejected()
    {
        var hash = PlatformService.HashPassword("1234567");
        Assert.True(PlatformService.VerifyPassword("1234567", hash));
        Assert.False(PlatformService.VerifyPassword("1234568", hash));
        Assert.Throws<PlatformException>(() => PlatformService.HashPassword("123456"));
    }
    [Fact]
    public void PasswordChangeRevokesStampAndUnlocksInitialAdmin()
    {
        _service.Bootstrap("admin", "TestBootstrap_2026!");
        var user = _service.Authenticate("admin", "TestBootstrap_2026!")!;
        Assert.False(_service.HasPermission(user.Id, "poi:delete"));
        _service.ChangePassword(user.Id, "TestBootstrap_2026!", "TestChanged_2026!");
        Assert.NotEqual(user.SecurityStamp, _service.User(user.Id)!.SecurityStamp);
        Assert.True(_service.HasPermission(user.Id, "poi:delete"));
    }
    [Fact]
    public void StorePersistsAndFailedWriteRollsBack()
    {
        var original = _store.Read(s => s.Places[0]);
        Assert.Throws<InvalidOperationException>(() => _store.Write<int>(s => { s.Places.Clear(); throw new InvalidOperationException(); }));
        Assert.Equivalent(original, _store.Read(s => s.Places[0]));
        _service.SavePlace(Admin(), new(0, "Test place", "Demo", "Demo content"), true);
        Assert.Equal(9, new JsonPlatformStore(Path.Combine(_folder, "platform.json")).Read(s => s.Places.Count));
    }
    [Fact]
    public void ConcurrentEditorMustReloadBeforeSaving()
    {
        var actor = Admin(); var source = _store.Read(s => s.Places[0]);
        _service.SavePlace(actor, source with { Name = "First edit" });
        var error = Assert.Throws<PlatformException>(() => _service.SavePlace(actor, source with { Name = "Stale edit" }));
        Assert.Equal(409, error.StatusCode); Assert.Equal("First edit", _store.Read(s => s.Places[0].Name));
    }
    [Fact]
    public void OwnerCannotSubmitUntilApprovedOrEditSomeoneElsePlace()
    {
        var admin = Admin(); var owner = _service.CreateUser("public", "testowner", "Test owner", "OwnerPassword_2026!", "poi_owner", true);
        Assert.Throws<PlatformException>(() => _service.Submit(owner.Id, new(0, "Test", "Demo", "Demo")));
        var review = _store.Read(s => s.Reviews.Single()); _service.Review(admin, review.Id, true, "Test approval");
        Assert.Throws<PlatformException>(() => _service.Submit(owner.Id, _store.Read(s => s.Places[0])));
        var submission = _service.Submit(owner.Id, new(0, "Owner test", "Demo", "Demo"));
        Assert.DoesNotContain(_store.GetAll(), p => p.Name == "Owner test");
        _service.Review(admin, submission.Id, true, "Approved");
        Assert.Equal(owner.Id, _store.GetAll().Single(p => p.Name == "Owner test").OwnerId);
        Assert.Equal(2, _store.Read(s => s.Notifications.Count));
        Assert.Throws<PlatformException>(() => _service.Review(admin, submission.Id, true, "Duplicate"));
    }
    [Fact]
    public void LastSuperAdminAndBuiltInRolesAreProtected()
    {
        var admin = Admin();
        Assert.Throws<PlatformException>(() => _service.UpdateUser(admin, admin, "user", true));
        Assert.Throws<PlatformException>(() => _service.DeleteUser(admin, admin));
        Assert.Throws<PlatformException>(() => _service.SaveRole(admin, new("super_admin", []), false));
    }
    [Fact]
    public void UnverifiedCoordinatesStayOutOfPublicCatalog()
    {
        var actor = Admin(); var source = _store.Read(s => s.Places[0]);
        _service.SavePlace(actor, source with { Latitude = 10, Longitude = 106, LocationVerified = false });
        Assert.Null(_store.GetById(1)!.Latitude);
        Assert.Throws<PlatformException>(() => _service.SavePlace(actor, source with { LocationVerified = true, Latitude = null }));
        Assert.Throws<PlatformException>(() => _service.SavePlace(actor, source with { IsDemo = false, SourceUrl = null }));
    }
    [Fact]
    public void RoleCannotBeOverwrittenThroughCreate()
    {
        var admin = Admin(); _service.SaveRole(admin, new("editor", ["poi:read"]));
        Assert.Throws<PlatformException>(() => _service.SaveRole(admin, new("editor", ["poi:delete"])));
        Assert.Equal(new[] { "poi:read" }, _store.Read(s => s.Roles.Single(r => r.Name == "editor").Permissions));
    }
    [Fact]
    public void SourcedPhotoMigrationPreservesEditorsImageAndContent()
    {
        var actor = Admin(); var place = _store.Read(s => s.Places[0]);
        _service.SavePlace(actor, place with { ImageUrl = "https://example.com/custom.jpg", ImageNote = "Owner-provided test image" });
        var reloaded = new JsonPlatformStore(Path.Combine(_folder, "platform.json"));
        Assert.Equal("https://example.com/custom.jpg", reloaded.GetById(place.Id)!.ImageUrl);
        Assert.All(new DemoPlaceRepository().GetAll(), p => { Assert.NotNull(p.ImageUrl); Assert.NotNull(p.ImageSourceUrl); Assert.NotNull(p.ImageNote); });
    }
    [Fact]
    public void PublicationRequiresEnglishForCurrentContentAndIgnoresForgedReadiness()
    {
        var actor = Admin();
        var place = _service.SavePlace(actor, new(0, "Verified test", "Test", "Original content", IsDemo: false,
            SourceUrl: "https://example.com/source", ReadyEnglishHash: PlatformService.ContentHash("Original content"), AudioStatus: "ready"), true);
        Assert.False(place.IsActive); Assert.True(place.ActivationRequested);
        _service.MarkEnglishReady(place.Id, "wrong-content-hash");
        Assert.Null(_store.GetById(place.Id));
        _service.MarkEnglishReady(place.Id, PlatformService.ContentHash(place.Description));
        place = _store.GetById(place.Id)!; Assert.True(place.IsActive);
        var updated = _service.SavePlace(actor, place with { Description = "Changed content" });
        Assert.False(updated.IsActive); Assert.Null(updated.ReadyEnglishHash); Assert.True(updated.ActivationRequested);
        _service.MarkEnglishReady(place.Id, PlatformService.ContentHash(place.Description));
        Assert.Null(_store.GetById(place.Id));
    }
    [Fact]
    public void DeletedIdsAreNeverReusedAndTombstonesPersist()
    {
        var actor = Admin();
        var first = _service.SavePlace(actor, new(0, "First", "Demo", "Demo"), true);
        _service.DeletePlace(actor, first.Id);
        var next = _service.SavePlace(actor, new(0, "Second", "Demo", "Demo"), true);
        Assert.True(next.Id > first.Id);
        Assert.Contains(new JsonPlatformStore(Path.Combine(_folder, "platform.json")).Read(s => s.DeletedPlaces), p => p.Id == first.Id);
    }
    [Fact]
    public void DistrictMigrationPreservesEditedRecordsAndUsersAndDoesNotDuplicateOnRestart()
    {
        var admin = Admin();
        var edited = _store.Read(s => s.Places[0]) with { Name = "Member's edited place", ImageUrl = "https://example.com/member.jpg" };
        _store.Write(s => { s.Places[0] = edited; s.SampleDataVersion = 2; s.NextPlaceId = 42; return true; });
        var migrated = new JsonPlatformStore(Path.Combine(_folder, "platform.json"));
        Assert.Equal(8, migrated.GetAll().Count);
        Assert.All(migrated.GetAll(), p => { Assert.Equal("district1", p.Region); Assert.True(p.Id >= 42); });
        Assert.Null(migrated.GetById(edited.Id));
        var archived = migrated.Read(s => s.Places.Single(p => p.Id == edited.Id));
        Assert.Equal(edited.Name, archived.Name); Assert.Equal(edited.Description, archived.Description);
        Assert.Equal(edited.ImageUrl, archived.ImageUrl); Assert.Equal("district4", archived.Region);
        Assert.Equal(admin, migrated.Read(s => s.Users.Single().Id));
        Assert.Equal(16, new JsonPlatformStore(Path.Combine(_folder, "platform.json")).Read(s => s.Places.Count));
    }
    [Fact]
    public void ExtraDistrictOneSamplesDoNotOverwriteTheThreeExistingRecordsOrDuplicateOnRestart()
    {
        var actor = Admin(); var existing = _store.Read(s => s.Places[0]);
        existing = _service.SavePlace(actor, existing with { Name = "Edited district one record" });
        _store.Write(s => { s.Places = s.Places.Take(3).ToList(); s.SampleDataVersion = 3; s.NextPlaceId = 42; return true; });
        var path = Path.Combine(_folder, "platform.json"); var expanded = new JsonPlatformStore(path);
        Assert.Equivalent(existing, expanded.GetById(existing.Id)); Assert.Equal(8, expanded.GetAll().Count);
        Assert.Contains(expanded.GetAll(), p => p.Id == 42 && p.Name == "Phố đi bộ Nguyễn Huệ");
        Assert.Contains(expanded.GetAll(), p => p.Id == 43 && p.Name == "Nhà hát Thành phố");
        Assert.Equal(8, new JsonPlatformStore(path).GetAll().Count);
    }
    [Fact]
    public void NewAttractionsKeepEditedHiddenPlacesAndNeverReuseDeletedIdsOrReseedAfterDeletion()
    {
        var actor = Admin();
        var park = _store.Read(s => s.Places.Single(p => p.Name == "Công viên Tao Đàn"))
            with { Id = 123, Description = "Member-edited park content", IsActive = false, ImageUrl = "/api/media/member.jpg", Images = [] };
        _store.Write(s => {
            s.Places = s.Places.Take(5).Append(park).ToList();
            s.Places[0] = s.Places[0] with { Description = "Member-edited market content" };
            s.Menus.Add(new("keep-menu", s.Places[0].Id, "Fixture dish", "Synthetic menu", null));
            s.DeletedPlaces.Add(new(124, DateTimeOffset.UtcNow)); s.NextPlaceId = 100;
            s.SampleDataVersion = 6; return true;
        });
        var before = _store.Read(s => s);
        var path = Path.Combine(_folder, "platform.json"); var migrated = new JsonPlatformStore(path);
        var after = migrated.Read(s => s);
        Assert.Equal(8, after.Places.Count);
        foreach (var oldPlace in before.Places) Assert.Equivalent(oldPlace, after.Places.Single(p => p.Id == oldPlace.Id));
        Assert.Equivalent(before.Users, after.Users); Assert.Equivalent(before.Menus, after.Menus);
        Assert.Equivalent(before.Roles, after.Roles); Assert.Equivalent(before.DeletedPlaces, after.DeletedPlaces);
        Assert.All(after.Places.Where(p => !before.Places.Any(old => old.Id == p.Id)), p => Assert.True(p.Id > 124));
        Assert.Equal(before.Version + 1, after.Version);
        Assert.Equivalent(after, new JsonPlatformStore(path).Read(s => s));
        var service = new PlatformService(migrated);
        service.DeletePlace(actor, after.Places.Single(p => p.Name == "Nhà thờ Đức Bà Sài Gòn").Id);
        var restarted = new JsonPlatformStore(path);
        Assert.DoesNotContain(restarted.Read(s => s.Places), p => p.Name == "Nhà thờ Đức Bà Sài Gòn");
        Assert.Equal(8, restarted.Read(s => s.SampleDataVersion));
    }
    public void Dispose() { if (Directory.Exists(_folder)) Directory.Delete(_folder, true); }
}
