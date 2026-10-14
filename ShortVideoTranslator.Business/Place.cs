namespace ShortVideoTranslator.Business;

public record Place(int Id, string Name, string Category, string Description,
    bool IsDemo = true, string? Address = null, double? Latitude = null, double? Longitude = null,
    string? SourceUrl = null, bool IsActive = true, string? OwnerId = null,
    bool LocationVerified = false, string? ImageUrl = null, string? ImageNote = null,
    int TriggerRadius = 30, int AudioPriority = 0, DateTimeOffset? UpdatedAt = null,
    string? ImageSourceUrl = null, int ImageFocusY = 25,
    string? AddressSourceUrl = null, string? LocationSourceUrl = null,
    string AudioStatus = "missing", bool ActivationRequested = false, string? ReadyEnglishHash = null,
    string Region = "district1");

public static class TourRegion
{
    public const string Current = "district1";
    public static bool Includes(Place place) => place.Region == Current;
}

public interface IPlaceRepository
{
    IReadOnlyList<Place> GetAll();
    Place? GetById(int id);
}

public class PlaceService(IPlaceRepository repository)
{
    public IReadOnlyList<Place> GetAll() => repository.GetAll();
    public Place? GetById(int id) => repository.GetById(id);
}
