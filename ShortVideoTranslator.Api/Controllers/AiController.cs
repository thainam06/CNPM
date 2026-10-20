using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController, Authorize, Route("api/ai")]
public class AiController(PlatformService platform, AiAdvisorService advisor, AiQuotaService quota, WeatherContextService weather) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool Admin => platform.HasPermission(Actor, "poi:update");
    private void RequireAccess() { if (!Admin) platform.RequireOwner(Actor); }
    [HttpGet("usage")]
    public IActionResult Usage()
    {
        RequireAccess(); Response.Headers.CacheControl = "no-store";
        var snapshot = quota.Usage(Actor, Admin);
        return Ok(new { ready = advisor.Providers.Length > 0, configurationOnly = true, provider = string.Join(" → ", advisor.Providers), providers = advisor.Providers, weatherReady = weather.Configured,
            snapshot.Used, snapshot.InFlight, snapshot.Limit, snapshot.Remaining, snapshot.ResetsAt, quota = snapshot });
    }
    [HttpPost("enhance-description")]
    public async Task<IActionResult> Enhance(AiRequest request, CancellationToken token)
    {
        RequireAccess(); Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 3000 || request.Name?.Length > 150 || request.Category?.Length > 80 || request.Address?.Length > 300) return BadRequest(new { message = "Nguồn cần 1–3000 ký tự; tên/danh mục/địa chỉ đúng giới hạn biểu mẫu." });
        Place? place = null;
        if (request.PlaceId is > 0) {
            place = platform.Store.Read(s => s.Places.FirstOrDefault(p => p.Id == request.PlaceId));
            if (place == null) return NotFound(new { message = "Không tìm thấy địa điểm." });
            if (!Admin && place.OwnerId != Actor) return Forbid();
        }
        if (advisor.Providers.Length == 0) return StatusCode(503, new { message = "AI chưa sẵn sàng. Cần cấu hình Gemini hoặc ProxyPal ở backend; không tạo nội dung giả." });
        using var reservation = quota.Reserve(Actor, Admin);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token); budget.CancelAfter(TimeSpan.FromSeconds(30));
        try {
            var warnings = new List<string>(); WeatherContext? context = null;
            if (request.UseWeather) {
                if (place != null) context = await weather.ReadAsync(place, budget.Token);
                if (context == null) warnings.Add("Không có thời tiết đã xác minh; bản nháp không dùng ngữ cảnh thời tiết.");
            }
            var draft = await advisor.GenerateAsync(new { name = request.Name, category = request.Category, address = request.Address, description = request.Text, isDemo = request.IsDemo }, context, warnings, budget.Token);
            budget.Token.ThrowIfCancellationRequested(); RequireAccess(); reservation.Commit();
            return Ok(new { draft.Text, draft.Provider, draft.RequiresReview, draft.Warnings, draft.Weather, quota = quota.Usage(Actor, Admin) });
        } catch (AiUnavailableException ex) { return StatusCode(503, new { message = ex.Message }); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return StatusCode(503, new { message = "AI hết thời gian 30 giây. Lượt dùng chưa bị trừ." }); }
    }
}
public record AiRequest(string Text, string? Name = null, string? Category = null, string? Address = null, int? PlaceId = null, bool UseWeather = false, bool IsDemo = true);
