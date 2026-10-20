namespace ShortVideoTranslator.Business;

public record PlatformUser(string Id, string Username, string DisplayName, string PasswordHash,
    string Role, bool Verified = false, bool Enabled = true, bool MustChangePassword = false,
    string SecurityStamp = "", DateTimeOffset? CreatedAt = null);
public record UserView(string Id, string Username, string DisplayName, string Role, bool Verified,
    bool Enabled, bool MustChangePassword, string[] Permissions);
public record PlatformRole(string Name, string[] Permissions, bool BuiltIn = false);
public record Review(string Id, string Kind, string UserId, string Status, Place? Place,
    string AdminNote = "", DateTimeOffset? CreatedAt = null, DateTimeOffset? ReviewedAt = null,
    DateTimeOffset? ExpectedPlaceVersion = null, string ApplicantNote = "");
public record OwnerNotification(string Id, string UserId, string Message, bool Read, DateTimeOffset CreatedAt,
    string? ReviewId = null, int? PlaceId = null);
public record AuditEntry(string Id, string ActorId, string Action, string Resource, DateTimeOffset CreatedAt);
public record RefreshSession(string Hash, string UserId, string SecurityStamp, DateTimeOffset ExpiresAt);
public record MenuItem(string Id, int PlaceId, string Name, string Description, decimal? Price);
public record AnalyticsEvent(string Id, string DeviceId, string Kind, int? PlaceId, string Language, DateTimeOffset CreatedAt);
public record AnalyticsHeartbeat(string DeviceId, DateTimeOffset LastSeen);
public record AudioTaskState(string Id, int PlaceId, string[] Languages, string Status, int Completed,
    string? Error, string RequestedBy, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string? SourceHash = null, DateTimeOffset? HeartbeatAt = null);
public record DeletedPlace(int Id, DateTimeOffset DeletedAt);
public record RuntimeLocation(string SessionId, DateTimeOffset Hour, double Latitude, double Longitude, double Accuracy, DateTimeOffset LastSeen);
public record OwnerIdentity(string UserId, string Ciphertext, DateTimeOffset ExpiresAt);

public class PlatformState
{
    public List<TranslationDraft> TranslationDrafts { get; set; } = [];
    public List<ApprovedTranslation> ApprovedTranslations { get; set; } = [];
    public List<Place> Places { get; set; } = [];
    public List<DeletedPlace> DeletedPlaces { get; set; } = [];
    public int NextPlaceId { get; set; } = 4;
    public List<PlatformUser> Users { get; set; } = [];
    public List<PlatformRole> Roles { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
    public List<OwnerNotification> Notifications { get; set; } = [];
    public List<AuditEntry> Audit { get; set; } = [];
    public List<MenuItem> Menus { get; set; } = [];
    public List<AnalyticsEvent> Events { get; set; } = [];
    public List<AnalyticsEvent> PendingAnalytics { get; set; } = [];
    public List<AnalyticsHeartbeat> AnalyticsHeartbeats { get; set; } = [];
    public Dictionary<string, DateTimeOffset> AnalyticsRevocations { get; set; } = [];
    public List<RuntimeLocation> RuntimeLocations { get; set; } = [];
    public List<OwnerIdentity> OwnerIdentities { get; set; } = [];
    public Dictionary<string, DateTimeOffset> RuntimeRevocations { get; set; } = [];
    public List<AudioTaskState> AudioTasks { get; set; } = [];
    public List<RefreshSession> RefreshSessions { get; set; } = [];
    public Dictionary<string, int> AiUsage { get; set; } = [];
    public long Version { get; set; } = 1;
    public int SampleDataVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public interface IPlatformStore
{
    T Read<T>(Func<PlatformState, T> query);
    T Write<T>(Func<PlatformState, T> change);
}

public class PlatformException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public static class PlatformPermissions
{
    public static readonly string[] All = ["poi:read", "poi:create", "poi:update", "poi:delete", "poi:approve", "poi:toggle",
        "menu:read", "menu:create", "menu:update", "menu:delete", "user:read", "user:create", "user:update", "user:delete",
        "role:read", "role:create", "role:update", "role:delete", "analytics:view", "analytics:export", "analytics:view_own",
        "audit:read", "audit:manage", "system:config", "system:logs", "system:backup", "owner:register", "owner:access",
        "owner:submit_poi", "owner:manage_own_poi", "content:moderate", "content:publish"];
    public static PlatformRole[] Defaults => [
        new("super_admin", All, true),
        new("admin", All.Where(p => !p.StartsWith("role:") && !p.StartsWith("system:") && p != "audit:manage").ToArray(), true),
        new("poi_owner", ["poi:read", "owner:access", "owner:submit_poi", "owner:manage_own_poi", "menu:read", "menu:create", "menu:update", "analytics:view_own"], true),
        new("user", ["poi:read", "menu:read", "owner:register"], true)
    ];
}
