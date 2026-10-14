using System.Text.Json;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

// Single-process demo persistence. Mutations use a detached snapshot + atomic replacement.
// A failed validation/write cannot partially change the live state.
public sealed class JsonPlatformStore : IPlatformStore, IPlaceRepository
{
    private readonly object _sync = new();
    private readonly string _path;
    private PlatformState _state;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public JsonPlatformStore(string path)
    {
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var existingFile = File.Exists(_path);
        _state = existingFile
            ? JsonSerializer.Deserialize<PlatformState>(File.ReadAllText(_path), Json) ?? throw new InvalidDataException("Dữ liệu quản trị không hợp lệ.")
            : new() { Places = new DemoPlaceRepository().GetAll().Select(p => p with { UpdatedAt = DateTimeOffset.UtcNow }).ToList(), Roles = PlatformPermissions.Defaults.ToList(), SampleDataVersion = 4 };
        // Scope migration preserves every old record, owner, review, menu and credential.
        // New IDs prevent saved audio/catalog links from referring to another attraction.
        if (existingFile && _state.SampleDataVersion < 3)
        {
            var now = DateTimeOffset.UtcNow;
            _state.Places = _state.Places.Select(p => p with { Region = "district4", UpdatedAt = now }).ToList();
            _state.Reviews = _state.Reviews.Select(r => r.Place == null ? r : r with { Place = r.Place with { Region = "district4" } }).ToList();
            var next = Math.Max(_state.NextPlaceId, _state.Places.Select(p => p.Id).Concat(_state.DeletedPlaces.Select(p => p.Id)).DefaultIfEmpty(0).Max() + 1);
            foreach (var sample in new DemoPlaceRepository().GetAll())
                _state.Places.Add(sample with { Id = next++, UpdatedAt = now });
            _state.NextPlaceId = next;
            _state.SampleDataVersion = 4; _state.Version++; _state.UpdatedAt = now;
            Write(_ => true); // Persist once, atomically; restart must not seed duplicates.
        }
        if (_state.SampleDataVersion < 4)
        {
            var now = DateTimeOffset.UtcNow;
            var next = Math.Max(_state.NextPlaceId, _state.Places.Select(p => p.Id).Concat(_state.DeletedPlaces.Select(p => p.Id)).DefaultIfEmpty(0).Max() + 1);
            foreach (var sample in new DemoPlaceRepository().GetAll().Skip(3))
                if (!_state.Places.Any(p => TourRegion.Includes(p) && p.Name == sample.Name))
                    _state.Places.Add(sample with { Id = next++, UpdatedAt = now });
            _state.NextPlaceId = next; _state.SampleDataVersion = 4;
            _state.Version++; _state.UpdatedAt = now; Write(_ => true);
        }
    }
    private static PlatformState Copy(PlatformState state) => JsonSerializer.Deserialize<PlatformState>(JsonSerializer.Serialize(state, Json), Json)!;
    public T Read<T>(Func<PlatformState, T> query) { lock (_sync) return query(Copy(_state)); }
    public T Write<T>(Func<PlatformState, T> change)
    {
        lock (_sync)
        {
            var next = Copy(_state);
            var result = change(next);
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(next, Json));
            File.Move(temporary, _path, true);
            _state = next;
            return result;
        }
    }
    public IReadOnlyList<Place> GetAll() => Read(s => s.Places.Where(p => p.IsActive && TourRegion.Includes(p)).Select(PublicPlace).ToArray());
    public Place? GetById(int id) => GetAll().FirstOrDefault(p => p.Id == id);
    private static Place PublicPlace(Place p) => p.LocationVerified ? p : p with { Latitude = null, Longitude = null };
}
