using System.Collections.Concurrent;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Services;

public class AudioTaskWorker(IPlatformStore store, PlatformService platform, NarrationAssetService assets, ILogger<AudioTaskWorker> logger, IConfiguration configuration) : BackgroundService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new();
    public AudioTaskState Enqueue(int placeId, string[] languages, string actor)
    {
        if (languages.Length is < 1 or > 6) throw new PlatformException(400, "Chọn 1–6 ngôn ngữ.");
        foreach (var language in languages) NarrationLanguages.Validate(language);
        return store.Write(s => {
            if (!s.Places.Any(p => p.Id == placeId)) throw new PlatformException(404, "Không tìm thấy địa điểm.");
            if (s.AudioTasks.Any(t => t.PlaceId == placeId && t.Status is "queued" or "running" or "paused")) throw new PlatformException(409, "Địa điểm này đã có tác vụ chưa hoàn tất.");
            s.AudioTasks.RemoveAll(t => t.UpdatedAt < DateTimeOffset.UtcNow.AddDays(-14));
            var source = s.Places.First(p => p.Id == placeId);
            var task = new AudioTaskState(PlatformService.Id(), placeId, languages.Distinct().OrderBy(l => l == "en" ? 0 : 1).ToArray(), "queued", 0, null, actor, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, PlatformService.ContentHash(source.Description));
            s.AudioTasks.Add(task); PlatformService.Audit(s, actor, "audio.enqueue", task.Id); return task;
        });
    }
    public void Control(string id, string action, string actor)
    {
        store.Write(s => {
            var index = s.AudioTasks.FindIndex(t => t.Id == id); if (index < 0) throw new PlatformException(404, "Không tìm thấy tác vụ.");
            var task = s.AudioTasks[index];
            var status = action switch {
                "pause" when task.Status is "queued" or "running" => "paused",
                "resume" when task.Status == "paused" => "queued",
                "cancel" when task.Status is "queued" or "running" or "paused" => "cancelled",
                _ => throw new PlatformException(409, "Thao tác không phù hợp với trạng thái tác vụ.")
            };
            s.AudioTasks[index] = task with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            PlatformService.Audit(s, actor, "audio." + action, id); return true;
        });
        if (action != "resume" && _running.TryGetValue(id, out var cancellation)) cancellation.Cancel();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Audio:WorkersEnabled", true)) return;
        store.Write(s => {
            s.AudioTasks = s.AudioTasks.Select(t => t.Status == "running" ? t with { Status = "queued", UpdatedAt = DateTimeOffset.UtcNow } : t).ToList(); return true;
        });
        while (!stoppingToken.IsCancellationRequested)
        {
            var pending = store.Read(s => s.Places.Where(p => p.ActivationRequested && !s.AudioTasks.Any(t => t.PlaceId == p.Id && (t.Status is "queued" or "running" or "paused" || t.SourceHash == PlatformService.ContentHash(p.Description)))).Select(p => p.Id).ToArray());
            foreach (var placeId in pending) {
                try { Enqueue(placeId, ["en", "vi", "zh", "ja", "ko", "fr"], "system.publish"); }
                catch (PlatformException ex) when (ex.StatusCode is 404 or 409) { }
            }
            var batch = store.Read(s => s.AudioTasks.Where(t => t.Status == "queued").Take(3).ToArray());
            if (batch.Length > 0) await Task.WhenAll(batch.Select(task => Process(task.Id, stoppingToken)));
            else await Task.Delay(1000, stoppingToken);
        }
    }
    private async Task Process(string id, CancellationToken stoppingToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        if (!_running.TryAdd(id, cancellation)) return;
        try
        {
            var task = store.Write(s => {
                var index = s.AudioTasks.FindIndex(t => t.Id == id);
                if (index < 0 || s.AudioTasks[index].Status != "queued") return null;
                s.AudioTasks[index] = s.AudioTasks[index] with { Status = "running", UpdatedAt = DateTimeOffset.UtcNow };
                return s.AudioTasks[index];
            });
            if (task == null) return;
            var place = store.Read(s => s.Places.FirstOrDefault(p => p.Id == task.PlaceId)) ?? throw new PlatformException(404, "Địa điểm đã bị xóa.");
            if (task.SourceHash != null && task.SourceHash != PlatformService.ContentHash(place.Description)) throw new PlatformException(409, "Nội dung đã thay đổi; cần tạo tác vụ mới.");
            for (var i = task.Completed; i < task.Languages.Length; i++)
            {
                if (store.Read(s => s.AudioTasks.First(t => t.Id == id).Status) != "running") return;
                await assets.GenerateAsync(place.Description, task.Languages[i], cancellation.Token);
                if (!store.Read(s => s.Places.Any(p => p.Id == place.Id && PlatformService.ContentHash(p.Description) == PlatformService.ContentHash(place.Description)))) throw new PlatformException(409, "Nội dung đã thay đổi trong lúc tạo audio. Tạo tác vụ mới.");
                if (task.Languages[i] == "en") platform.MarkEnglishReady(place.Id, PlatformService.ContentHash(place.Description));
                Update(id, t => t.Status == "running" ? t with { Completed = i + 1, UpdatedAt = DateTimeOffset.UtcNow } : t);
            }
            Update(id, t => t.Status == "running" ? t with { Status = "completed", UpdatedAt = DateTimeOffset.UtcNow } : t);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Audio task {TaskId} failed", id);
            var message = exception is TranslationUnavailableException or PlatformException ? exception.Message : "Edge-TTS chưa tạo được audio. Kiểm tra kết nối hoặc thử tạo tác vụ mới.";
            Update(id, t => t.Status == "running" ? t with { Status = "failed", Error = message, UpdatedAt = DateTimeOffset.UtcNow } : t);
        }
        finally { _running.TryRemove(id, out _); }
    }
    private void Update(string id, Func<AudioTaskState, AudioTaskState> update) => store.Write(s => {
        var index = s.AudioTasks.FindIndex(t => t.Id == id); if (index >= 0) s.AudioTasks[index] = update(s.AudioTasks[index]); return true;
    });
}
