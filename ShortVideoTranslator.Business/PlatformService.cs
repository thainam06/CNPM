using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ShortVideoTranslator.Business;

public class PlatformService(IPlatformStore store)
{
    private static string RefreshHash(string token) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    public string CreateRefresh(string userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        store.Write(s => {
            var user = s.Users.First(u => u.Id == userId);
            s.RefreshSessions.RemoveAll(r => r.ExpiresAt <= DateTimeOffset.UtcNow);
            var old = s.RefreshSessions.Where(r => r.UserId == userId).OrderByDescending(r => r.ExpiresAt).Skip(4).Select(r => r.Hash).ToHashSet();
            s.RefreshSessions.RemoveAll(r => old.Contains(r.Hash));
            s.RefreshSessions.Add(new(RefreshHash(token), userId, user.SecurityStamp, DateTimeOffset.UtcNow.AddDays(7))); return true;
        }); return token;
    }
    public (PlatformUser User, string Token) RotateRefresh(string token)
    {
        if (token.Length > 200) throw new PlatformException(401, "Phiên làm mới không hợp lệ.");
        var hash = RefreshHash(token); var next = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return store.Write(s => {
            var session = s.RefreshSessions.FirstOrDefault(r => r.Hash == hash) ?? throw new PlatformException(401, "Phiên làm mới hết hạn hoặc đã được sử dụng.");
            var user = s.Users.FirstOrDefault(u => u.Id == session.UserId && u.Enabled);
            if (session.ExpiresAt <= DateTimeOffset.UtcNow || user == null || session.SecurityStamp != user.SecurityStamp) throw new PlatformException(401, "Phiên làm mới đã bị thu hồi.");
            s.RefreshSessions.Remove(session);
            s.RefreshSessions.Add(new(RefreshHash(next), user.Id, user.SecurityStamp, DateTimeOffset.UtcNow.AddDays(7)));
            return (user, next);
        });
    }
    public void RevokeRefresh(string token) => store.Write(s => s.RefreshSessions.RemoveAll(r => r.Hash == RefreshHash(token)));
    public IPlatformStore Store => store;
    public static string ContentHash(string text) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
    public void MarkEnglishReady(int id, string sourceHash) => store.Write(s => {
        var index = s.Places.FindIndex(p => p.Id == id);
        if (index < 0 || ContentHash(s.Places[index].Description) != sourceHash) return false;
        var place = s.Places[index];
        if (place.ReadyEnglishHash == sourceHash && !place.ActivationRequested) return false;
        s.Places[index] = place with { ReadyEnglishHash = sourceHash, AudioStatus = "ready", IsActive = place.IsActive || place.ActivationRequested, ActivationRequested = false, UpdatedAt = DateTimeOffset.UtcNow };
        s.Version++; s.UpdatedAt = DateTimeOffset.UtcNow; return true;
    });
    public static string Id() => Guid.NewGuid().ToString("N");
    public static string HashPassword(string password)
    {
        if (password.Length < 7 || password.Length > 128) throw new PlatformException(400, "Mật khẩu cần từ 7 đến 128 ký tự.");
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2:210000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
    public static bool VerifyPassword(string password, string stored)
    {
        try
        {
            var parts = stored.Split(':');
            if (parts.Length != 4 || parts[0] != "pbkdf2" || password.Length > 128) return false;
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[2]), int.Parse(parts[1]), HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(parts[3]));
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException) { return false; }
    }
    public PlatformUser? User(string id) => store.Read(s => s.Users.FirstOrDefault(u => u.Id == id));
    public bool HasPermission(string id, string permission) => store.Read(s =>
        s.Users.FirstOrDefault(u => u.Id == id) is { Enabled: true, MustChangePassword: false } user &&
        s.Roles.FirstOrDefault(r => r.Name == user.Role)?.Permissions.Contains(permission) == true);
    public UserView View(PlatformUser user) => store.Read(s => new UserView(user.Id, user.Username, user.DisplayName,
        user.Role, user.Verified, user.Enabled, user.MustChangePassword, s.Roles.FirstOrDefault(r => r.Name == user.Role)?.Permissions ?? []));
    public void Bootstrap(string username, string password)
    {
        var hash = HashPassword(password);
        store.Write(s => {
            if (!s.Users.Any(u => u.Role == "super_admin"))
                s.Users.Add(new(Id(), Username(username), "Quản trị viên", hash, "super_admin", true, true, true, Id(), DateTimeOffset.UtcNow));
            return true;
        });
    }
    public static string Username(string value)
    {
        value = value.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(value, "^[a-z0-9_.@-]{3,80}$")) throw new PlatformException(400, "Tên đăng nhập cần 3–80 ký tự Latin, số hoặc . _ @ -.");
        return value;
    }
    public PlatformUser? Authenticate(string username, string password)
    {
        var user = store.Read(s => s.Users.FirstOrDefault(u => u.Username == username.Trim().ToLowerInvariant() && u.Enabled));
        // Constant work for an unknown account to avoid a cheap username timing oracle.
        var dummy = "pbkdf2:210000:AAAAAAAAAAAAAAAAAAAAAA==:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
        return VerifyPassword(password, user?.PasswordHash ?? dummy) ? user : null;
    }
    public void ChangePassword(string userId, string oldPassword, string newPassword)
    {
        var hash = HashPassword(newPassword);
        store.Write(s => {
            var index = s.Users.FindIndex(u => u.Id == userId);
            if (index < 0 || !VerifyPassword(oldPassword, s.Users[index].PasswordHash)) throw new PlatformException(400, "Mật khẩu hiện tại không đúng.");
            s.Users[index] = s.Users[index] with { PasswordHash = hash, MustChangePassword = false, SecurityStamp = Id() };
            Audit(s, userId, "password.change", userId); return true;
        });
    }
    public UserView CreateUser(string actor, string username, string displayName, string password, string role, bool ownerRegistration = false)
    {
        var hash = HashPassword(password); username = Username(username);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100) throw new PlatformException(400, "Tên hiển thị cần 1–100 ký tự.");
        var user = store.Write(s => {
            if (s.Users.Any(u => u.Username == username)) throw new PlatformException(409, "Tên đăng nhập đã tồn tại.");
            if (!s.Roles.Any(r => r.Name == role)) throw new PlatformException(400, "Vai trò không tồn tại.");
            var created = new PlatformUser(Id(), username, displayName.Trim(), hash, role, !ownerRegistration, true, false, Id(), DateTimeOffset.UtcNow);
            s.Users.Add(created);
            if (ownerRegistration) s.Reviews.Add(new(Id(), "registration", created.Id, "pending", null, CreatedAt: DateTimeOffset.UtcNow));
            Audit(s, actor, ownerRegistration ? "owner.register" : "user.create", created.Id); return created;
        });
        return View(user);
    }
    public void UpdateUser(string actor, string id, string role, bool enabled)
    {
        store.Write(s => {
            var index = s.Users.FindIndex(u => u.Id == id);
            if (index < 0) throw new PlatformException(404, "Không tìm thấy tài khoản.");
            if (!s.Roles.Any(r => r.Name == role)) throw new PlatformException(400, "Vai trò không tồn tại.");
            var previous = s.Users[index];
            if (previous.Role == "super_admin" && (!enabled || role != "super_admin") && s.Users.Count(u => u.Role == "super_admin" && u.Enabled) <= 1)
                throw new PlatformException(409, "Phải giữ ít nhất một super admin đang hoạt động.");
            if (actor == id && !enabled) throw new PlatformException(409, "Không thể tự khóa tài khoản.");
            s.Users[index] = previous with { Role = role, Enabled = enabled, SecurityStamp = Id() };
            Audit(s, actor, "user.update", id); return true;
        });
    }
    public void DeleteUser(string actor, string id)
    {
        store.Write(s => {
            var user = s.Users.FirstOrDefault(u => u.Id == id) ?? throw new PlatformException(404, "Không tìm thấy tài khoản.");
            if (actor == id || user.Role == "super_admin") throw new PlatformException(409, "Không thể xóa chính mình hoặc super admin.");
            if (s.Places.Any(p => p.OwnerId == id)) throw new PlatformException(409, "Tài khoản đang sở hữu địa điểm; chuyển chủ trước khi xóa.");
            s.Users.Remove(user); s.Reviews.RemoveAll(r => r.UserId == id); s.Notifications.RemoveAll(n => n.UserId == id);
            s.OwnerIdentities.RemoveAll(p => p.UserId == id);
            Audit(s, actor, "user.delete", id); return true;
        });
    }
    public void SaveRole(string actor, PlatformRole role, bool create = true)
    {
        if (!Regex.IsMatch(role.Name, "^[a-z_]{3,40}$") || role.Permissions.Any(p => !PlatformPermissions.All.Contains(p)))
            throw new PlatformException(400, "Tên vai trò hoặc quyền không hợp lệ.");
        store.Write(s => {
            var previous = s.Roles.FirstOrDefault(r => r.Name == role.Name);
            if (create && previous != null) throw new PlatformException(409, "Vai trò đã tồn tại. Dùng thao tác cập nhật.");
            if (!create && previous == null) throw new PlatformException(404, "Vai trò không tồn tại.");
            if (previous?.BuiltIn == true) throw new PlatformException(409, "Không sửa các vai trò hệ thống; hãy tạo vai trò mới.");
            if (previous != null) s.Roles.Remove(previous);
            s.Roles.Add(role with { BuiltIn = false, Permissions = role.Permissions.Distinct().ToArray() });
            Audit(s, actor, "role.save", role.Name); return true;
        });
    }
    public void DeleteRole(string actor, string name) => store.Write(s => {
        var role = s.Roles.FirstOrDefault(r => r.Name == name) ?? throw new PlatformException(404, "Không tìm thấy vai trò.");
        if (role.BuiltIn || s.Users.Any(u => u.Role == name)) throw new PlatformException(409, "Vai trò hệ thống hoặc đang được sử dụng.");
        s.Roles.Remove(role); Audit(s, actor, "role.delete", name); return true;
    });
    public static void ValidatePlace(Place p)
    {
        if (p.Region is not ("district1" or "district4")) throw new PlatformException(400, "Khu vực không hợp lệ.");
        if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 150 || string.IsNullOrWhiteSpace(p.Category) || p.Category.Length > 80)
            throw new PlatformException(400, "Tên địa điểm và danh mục không hợp lệ.");
        if (string.IsNullOrWhiteSpace(p.Description) || p.Description.Length > 3000) throw new PlatformException(400, "Thuyết minh cần 1–3000 ký tự.");
        if ((p.Latitude is null) != (p.Longitude is null) || p.Latitude is < -90 or > 90 || p.Longitude is < -180 or > 180 ||
            (p.Latitude.HasValue && !double.IsFinite(p.Latitude.Value)) || (p.Longitude.HasValue && !double.IsFinite(p.Longitude.Value)))
            throw new PlatformException(400, "Tọa độ không hợp lệ; cần đủ vĩ độ và kinh độ.");
        if (p.TriggerRadius is < 10 or > 1000 || p.AudioPriority is < 0 or > 100) throw new PlatformException(400, "Bán kính cần 10–1000 mét, ưu tiên 0–100.");
        if (p.ImageFocusY is < 0 or > 100) throw new PlatformException(400, "Trọng tâm ảnh cần từ 0 đến 100 phần trăm.");
        foreach (var url in new[] { p.SourceUrl, p.ImageUrl, p.ImageSourceUrl, p.AddressSourceUrl, p.LocationSourceUrl })
            if (!string.IsNullOrWhiteSpace(url) && !(url.StartsWith("/api/media/", StringComparison.Ordinal) && url == p.ImageUrl) &&
                (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != "https"))
                throw new PlatformException(400, "Nguồn và ảnh cần URL HTTPS hợp lệ.");
        if (!p.IsDemo && string.IsNullOrWhiteSpace(p.SourceUrl)) throw new PlatformException(400, "Địa điểm chính thức cần nguồn kiểm chứng.");
        if (p.LocationVerified && (p.Latitude is null || string.IsNullOrWhiteSpace(p.SourceUrl))) throw new PlatformException(400, "Xác minh tọa độ cần tọa độ và nguồn dữ liệu.");
        if (!string.IsNullOrWhiteSpace(p.ImageUrl) && string.IsNullOrWhiteSpace(p.ImageNote)) throw new PlatformException(400, "Ảnh cần ghi chú nguồn và mức độ xác thực.");
        if (p.Address?.Length > 300 || p.ImageNote?.Length > 500 || p.SourceUrl?.Length > 2000 || p.ImageUrl?.Length > 2000 || p.ImageSourceUrl?.Length > 2000)
            throw new PlatformException(400, "Địa chỉ, ghi chú hoặc URL quá dài.");
    }
    public Place SavePlace(string actor, Place place, bool create = false)
    {
        ValidatePlace(place);
        return store.Write(s => SavePlaceInState(s, actor, place, create));
    }
    private static Place SavePlaceInState(PlatformState s, string actor, Place place, bool create)
    {
        if (place.OwnerId != null && !s.Users.Any(u => u.Id == place.OwnerId && u.Verified && u.Enabled && u.Role == "poi_owner"))
            throw new PlatformException(400, "Chủ địa điểm phải là chủ quán đã được duyệt.");
        var index = s.Places.FindIndex(p => p.Id == place.Id);
        if (!create && index < 0) throw new PlatformException(404, "Không tìm thấy địa điểm.");
        if (!create && place.UpdatedAt != s.Places[index].UpdatedAt) throw new PlatformException(409, "Nội dung đã được người khác sửa. Tải lại trước khi lưu.");
        var previous = create ? null : s.Places[index];
        var hash = ContentHash(place.Description);
        var ready = previous?.ReadyEnglishHash == hash;
        var requested = place.IsActive;
        var nextId = Math.Max(s.NextPlaceId, s.Places.Select(p => p.Id).DefaultIfEmpty(0).Max() + 1);
        var saved = place with { Id = create ? nextId : place.Id, UpdatedAt = DateTimeOffset.UtcNow,
            ReadyEnglishHash = ready ? hash : null, AudioStatus = ready ? "ready" : "missing",
            IsActive = requested && (place.IsDemo || ready), ActivationRequested = requested && !place.IsDemo && !ready };
        if (create) s.NextPlaceId = nextId + 1;
        if (create) s.Places.Add(saved); else s.Places[index] = saved;
        s.Version++; s.UpdatedAt = DateTimeOffset.UtcNow;
        Audit(s, actor, create ? "poi.create" : "poi.update", saved.Id.ToString()); return saved;
    }
    public void DeletePlace(string actor, int id) => store.Write(s => {
        if (s.Places.RemoveAll(p => p.Id == id) == 0) throw new PlatformException(404, "Không tìm thấy địa điểm.");
        s.NextPlaceId = Math.Max(s.NextPlaceId, id + 1);
        s.DeletedPlaces.RemoveAll(p => p.DeletedAt < DateTimeOffset.UtcNow.AddDays(-30));
        s.DeletedPlaces.Add(new(id, DateTimeOffset.UtcNow));
        s.Menus.RemoveAll(m => m.PlaceId == id);
        foreach (var review in s.Reviews.Where(r => r.Status == "pending" && r.Place?.Id == id).ToArray())
        {
            s.Reviews.Remove(review); s.Reviews.Add(review with { Status = "rejected", AdminNote = "Địa điểm đã bị xóa.", ReviewedAt = DateTimeOffset.UtcNow });
        }
        s.Version++; s.UpdatedAt = DateTimeOffset.UtcNow; Audit(s, actor, "poi.delete", id.ToString()); return true;
    });
    public void RequireOwner(string id)
    {
        if (User(id) is not { Enabled: true, Verified: true } || !HasPermission(id, "owner:access")) throw new PlatformException(403, "Chủ quán cần được Admin duyệt trước khi quản lý nội dung.");
    }
    public Review Submit(string actor, Place place)
    {
        RequireOwner(actor); ValidatePlace(place);
        return store.Write(s => {
            if (place.Id != 0 && !s.Places.Any(p => p.Id == place.Id && p.OwnerId == actor)) throw new PlatformException(403, "Chỉ được đề xuất thay đổi địa điểm của bạn.");
            var previous = s.Places.FirstOrDefault(p => p.Id == place.Id);
            if (previous != null && previous.UpdatedAt != place.UpdatedAt) throw new PlatformException(409, "Địa điểm đã thay đổi. Vui lòng tải lại.");
            if (s.Reviews.Any(r => r.Status == "pending" && r.Kind == "submission" && r.UserId == actor && r.Place?.Id == place.Id)) throw new PlatformException(409, "Địa điểm này đã có đề xuất đang chờ duyệt.");
            var review = new Review(Id(), "submission", actor, "pending", place with { OwnerId = actor, IsActive = false }, CreatedAt: DateTimeOffset.UtcNow, ExpectedPlaceVersion: previous?.UpdatedAt);
            s.Reviews.Add(review); Audit(s, actor, "submission.create", review.Id); return review;
        });
    }
    public void Review(string actor, string id, bool approve, string note)
    {
        if (note.Length > 1000 || (!approve && string.IsNullOrWhiteSpace(note))) throw new PlatformException(400, "Từ chối cần lý do; ghi chú tối đa 1000 ký tự.");
        store.Write(s => {
            var index = s.Reviews.FindIndex(r => r.Id == id);
            if (index < 0) throw new PlatformException(404, "Không tìm thấy yêu cầu.");
            var review = s.Reviews[index];
            if (review.Status != "pending") throw new PlatformException(409, "Yêu cầu đã được xử lý.");
            if (approve && review.Kind == "registration")
            {
                var userIndex = s.Users.FindIndex(u => u.Id == review.UserId);
                if (userIndex < 0) throw new PlatformException(404, "Tài khoản đã bị xóa.");
                s.Users[userIndex] = s.Users[userIndex] with { Verified = true };
            }
            if (approve && review.Place is { } place)
            {
                if (place.Id != 0 && s.Places.FirstOrDefault(p => p.Id == place.Id)?.UpdatedAt != review.ExpectedPlaceVersion)
                    throw new PlatformException(409, "Địa điểm đã thay đổi từ lúc gửi đề xuất; yêu cầu chủ quán gửi lại.");
                SavePlaceInState(s, actor, place with { IsActive = true }, place.Id == 0);
            }
            s.Reviews[index] = review with { Status = approve ? "approved" : "rejected", AdminNote = note.Trim(), ReviewedAt = DateTimeOffset.UtcNow };
            s.Notifications.Add(new(Id(), review.UserId, $"Yêu cầu {review.Kind} đã {(approve ? "được duyệt" : "bị từ chối")}. {note}", false, DateTimeOffset.UtcNow));
            Audit(s, actor, approve ? "review.approve" : "review.reject", id); return true;
        });
    }
    public MenuItem SaveMenu(string actor, MenuItem item, bool create)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 100 || item.Description.Length > 500 || item.Price is < 0 or > 100_000_000)
            throw new PlatformException(400, "Tên món, mô tả hoặc giá không hợp lệ.");
        return store.Write(s => {
            if (!s.Places.Any(p => p.Id == item.PlaceId)) throw new PlatformException(404, "Địa điểm không tồn tại.");
            var saved = item with { Id = create ? Id() : item.Id };
            var index = s.Menus.FindIndex(m => m.Id == saved.Id && m.PlaceId == saved.PlaceId);
            if (!create && index < 0) throw new PlatformException(404, "Món ăn không tồn tại.");
            if (create) s.Menus.Add(saved); else s.Menus[index] = saved;
            Audit(s, actor, "menu.save", saved.Id); return saved;
        });
    }
    public static void Audit(PlatformState state, string actor, string action, string resource)
    {
        state.Audit.Add(new(Id(), actor, action, resource, DateTimeOffset.UtcNow));
        if (state.Audit.Count > 5000) state.Audit.RemoveRange(0, state.Audit.Count - 5000);
    }
}
