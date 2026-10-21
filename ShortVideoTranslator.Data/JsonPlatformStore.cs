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
        if (_state.SampleDataVersion < 5)
        {
            var now = DateTimeOffset.UtcNow;
            _state.Places = _state.Places.Select(p => {
                var sample = new DemoPlaceRepository().GetAll().FirstOrDefault(d => d.Name == p.Name && d.ImageUrl == p.ImageUrl);
                return TourRegion.Includes(p) && p.IsDemo && p.Images == null && sample?.Images is { Length: > 0 }
                    ? p with { Images = sample.Images, UpdatedAt = now } : p;
            }).ToList();
            _state.SampleDataVersion = 5; _state.Version++; _state.UpdatedAt = now; Write(_ => true);
        }
        if (_state.SampleDataVersion < 6)
        {
            var now = DateTimeOffset.UtcNow; var changed = false;
            _state.Places = _state.Places.Select(p => {
                var narration = DemoNarrations.Previous.FirstOrDefault(item => item.Name == p.Name && item.PreviousText == p.Description);
                // Upgrade untouched system samples only. Keep owner/editor text, visibility and all media.
                if (!TourRegion.Includes(p) || !p.IsDemo || p.OwnerId != null || narration == null) return p;
                changed = true;
                return p with { Description = narration.Texts["vi"], SourceUrl = narration.SourceUrl,
                    ReadyEnglishHash = null, AudioStatus = "missing", UpdatedAt = now };
            }).ToList();
            _state.SampleDataVersion = 6;
            if (changed) { _state.Version++; _state.UpdatedAt = now; }
            Write(_ => true);
        }
        if (_state.SampleDataVersion < 7)
        {
            var now = DateTimeOffset.UtcNow;
            var next = Math.Max(_state.NextPlaceId, _state.Places.Select(p => p.Id)
                .Concat(_state.DeletedPlaces.Select(p => p.Id)).DefaultIfEmpty(0).Max() + 1);
            var changed = false;
            foreach (var sample in AdditionalDemoPlaces.All)
            {
                // Keep matching editor/owner records, including intentionally hidden ones.
                if (_state.Places.Any(p => TourRegion.Includes(p) && p.Name == sample.Name)) continue;
                _state.Places.Add(sample with { Id = next++, UpdatedAt = now });
                changed = true;
            }
            _state.NextPlaceId = next; _state.SampleDataVersion = 7;
            if (changed) { _state.Version++; _state.UpdatedAt = now; }
            Write(_ => true);
        }
        if (_state.SampleDataVersion < 8)
        {
            var now = DateTimeOffset.UtcNow; var changed = false;
            _state.Places = _state.Places.Select(p => {
                var revised = DemoNarrations.All.FirstOrDefault(n => n.Name == p.Name &&
                    n.PreviousText.ReplaceLineEndings("\n") == p.Description.ReplaceLineEndings("\n"));
                var previous = DemoNarrations.Previous.FirstOrDefault(n => n.Name == p.Name);
                if (!TourRegion.Includes(p) || !p.IsDemo || p.OwnerId != null || revised == null ||
                    (p.SourceUrl != previous?.SourceUrl && p.SourceUrl != revised.SourceUrl)) return p;
                changed = true;
                return p with { Description = revised.Texts["vi"], SourceUrl = revised.SourceUrl,
                    ReadyEnglishHash = null, AudioStatus = "missing", NarrationRevisions = null, UpdatedAt = now };
            }).ToList();
            _state.SampleDataVersion = 8;
            if (changed) { _state.Version++; _state.UpdatedAt = now; }
            Write(_ => true);
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
    public IReadOnlyList<Place> GetAll() => Read(s => s.Places.Where(p => p.IsActive && TourRegion.Includes(p)).Select(p => {
        var hash = PlatformService.ContentHash(p.Description);
        var revisions = s.ApprovedTranslations.Where(t => t.SourceHash == hash).ToDictionary(t => t.Language, t => t.Revision);
        return PublicPlace(p) with { NarrationRevisions = revisions.Count == 0 ? null : revisions };
    }).ToArray());
    public Place? GetById(int id) => GetAll().FirstOrDefault(p => p.Id == id);
    private static Place PublicPlace(Place p) => p.LocationVerified ? p : p with { Latitude = null, Longitude = null };
}
