using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Services;

public class OwnerIdentityService(IDataProtectionProvider provider, IPlatformStore store, TimeProvider time)
{
    private readonly IDataProtector _protector = provider.CreateProtector("SmartTour.OwnerIdentity.v1");
    public void Save(string userId, string value)
    {
        value = value.Trim();
        if (!Regex.IsMatch(value, "^[0-9]{12}$")) throw new PlatformException(400, "Thông tin xác minh cần 12 chữ số, hoặc để trống bằng thao tác xóa.");
        var record = new OwnerIdentity(userId, "v1:" + _protector.Protect(value), time.GetUtcNow().AddDays(180));
        store.Write(s => {
            if (!s.Users.Any(u => u.Id == userId && u.Enabled && u.Role == "poi_owner")) throw new PlatformException(403, "Chỉ chủ quán quản lý giấy tờ của chính mình.");
            s.OwnerIdentities.RemoveAll(p => p.UserId == userId); s.OwnerIdentities.Add(record);
            PlatformService.Audit(s, userId, "identity.save", userId); return true;
        });
    }
    public string? Read(string userId)
    {
        var item = store.Read(s => s.OwnerIdentities.FirstOrDefault(p => p.UserId == userId));
        if (item == null || item.ExpiresAt <= time.GetUtcNow() || !item.Ciphertext.StartsWith("v1:", StringComparison.Ordinal)) return null;
        try { return _protector.Unprotect(item.Ciphertext[3..]); }
        catch (CryptographicException) { return null; }
    }
    public DateTimeOffset? ExpiresAt(string userId) => store.Read(s => s.OwnerIdentities.FirstOrDefault(p => p.UserId == userId)?.ExpiresAt);
    public void Delete(string userId) => store.Write(s => {
        s.OwnerIdentities.RemoveAll(p => p.UserId == userId); PlatformService.Audit(s, userId, "identity.delete", userId); return true;
    });
    public int RedactExpired() => store.Write(s => s.OwnerIdentities.RemoveAll(p => p.ExpiresAt <= time.GetUtcNow()));
}
