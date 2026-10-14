using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Route("api/admin/audio-tasks"), Permission("poi:update")]
public class AudioTasksController(AudioTaskWorker worker, IPlatformStore store, PlatformService platform) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public IActionResult List() => Ok(store.Read(s => s.AudioTasks.OrderByDescending(t => t.CreatedAt).Take(100).ToArray()));
    [HttpPost]
    public IActionResult Enqueue(AudioTaskRequest request) => Ok(worker.Enqueue(request.PlaceId, request.Languages, Actor));
    [HttpPost("{id}/{command}")]
    public IActionResult Control(string id, string command) { worker.Control(id, command, Actor); return NoContent(); }
    [HttpGet("stream")]
    public async Task Stream(CancellationToken token)
    {
        Response.ContentType = "text/event-stream"; Response.Headers.CacheControl = "no-store";
        while (!token.IsCancellationRequested && platform.HasPermission(Actor, "poi:update") &&
            platform.User(Actor)?.SecurityStamp == User.FindFirstValue("stamp") &&
            (!long.TryParse(User.FindFirstValue("expires_at"), out var expiry) || expiry > DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
        {
            var json = JsonSerializer.Serialize(store.Read(s => s.AudioTasks.OrderByDescending(t => t.CreatedAt).Take(100).ToArray()), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await Response.WriteAsync($"data: {json}\n\n", token); await Response.Body.FlushAsync(token);
            await Task.Delay(2000, token);
        }
    }
}
public record AudioTaskRequest(int PlaceId, string[] Languages);
