using ShortVideoTranslator.Business;
namespace ShortVideoTranslator.Api.Services;
public record AiQuotaSnapshot(int Used, int InFlight, int? Limit, int? Remaining, DateTimeOffset ResetsAt);

// In-flight reservations are released on failure/restart. Only completed drafts are charged.
public sealed class AiQuotaService(IPlatformStore store, TimeProvider clock)
{
    private readonly object gate = new();
    private readonly Dictionary<string, int> pending = [];
    private string Key(string actor) => $"{actor}:{clock.GetUtcNow():yyyy-MM-dd}";
    public AiQuotaSnapshot Usage(string actor, bool admin)
    {
        lock (gate) {
            var key = Key(actor); var used = store.Read(s => s.AiUsage.GetValueOrDefault(key)); var active = pending.GetValueOrDefault(key);
            return new(used, active, admin ? null : 10, admin ? null : Math.Max(0, 10 - used - active), new DateTimeOffset(clock.GetUtcNow().UtcDateTime.Date.AddDays(1), TimeSpan.Zero));
        }
    }
    public Reservation Reserve(string actor, bool admin)
    {
        lock (gate) {
            var key = Key(actor);
            if (!admin && store.Read(s => s.AiUsage.GetValueOrDefault(key)) + pending.GetValueOrDefault(key) >= 10)
                throw new PlatformException(429, "Đã hết lượt AI hoặc đang có yêu cầu xử lý. Chủ quán có 10 bản nháp thành công/ngày UTC.");
            pending[key] = pending.GetValueOrDefault(key) + 1; return new(this, key);
        }
    }
    private void Finish(string key, bool success)
    {
        lock (gate) {
            try {
                if (success) store.Write(s => {
                    s.AiUsage[key] = s.AiUsage.GetValueOrDefault(key) + 1;
                    var cutoff = clock.GetUtcNow().AddDays(-7).ToString("yyyy-MM-dd");
                    foreach (var old in s.AiUsage.Keys.Where(k => k.Length >= 10 && string.CompareOrdinal(k[^10..], cutoff) < 0).ToArray()) s.AiUsage.Remove(old);
                    return true;
                });
            } finally { if (pending.GetValueOrDefault(key) <= 1) pending.Remove(key); else pending[key]--; }
        }
    }
    public sealed class Reservation(AiQuotaService owner, string key) : IDisposable
    {
        private bool finished;
        public void Commit() { if (finished) return; finished = true; owner.Finish(key, true); }
        public void Dispose() { if (finished) return; finished = true; owner.Finish(key, false); }
    }
}
