using Microsoft.AspNetCore.Mvc;
using ShortVideoTranslator.Api.Security;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;
namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
public class SystemStatusController(SystemReadinessService readiness, PlaceService places, NarrationAssetService assets, MapPackService maps, AiAdvisorService advisor, WeatherContextService weather, IConfiguration config) : ControllerBase
{
    [HttpGet("/health"), HttpGet("/health/ready"), HttpGet("/api/health/ready")]
    public IActionResult Ready()
    {
        Response.Headers.CacheControl = "no-store"; var snapshot = readiness.Check();
        return StatusCode(snapshot.Ready ? 200 : 503, new { status = snapshot.Ready ? "ready" : "not-ready", snapshot.CheckedAt });
    }
    [HttpGet("/api/admin/system-status"), Permission("system:logs")]
    public IActionResult Detail()
    {
        Response.Headers.CacheControl = "no-store";
        var snapshot = readiness.Check(); var catalog = places.GetAll(); var map = maps.ReadVerified();
        var audio = NarrationLanguages.Codes.Select(language => new { language, ready = catalog.Count(p => assets.Find(p.Description, language) != null), total = catalog.Count }).ToArray();
        var checks = new[] {
            new { name = "Kho dữ liệu", state = snapshot.Ready ? "Sẵn sàng" : "Chưa sẵn sàng", detail = snapshot.Message },
            new { name = "Địa điểm công khai", state = catalog.Count > 0 ? "Có dữ liệu" : "Chưa có dữ liệu", detail = $"{catalog.Count} địa điểm thuộc Quận 1." },
            new { name = "Hàng đợi tạo MP3", state = config.GetValue("Audio:WorkersEnabled", true) ? "Được bật" : "Đang tắt", detail = "Cấu hình worker; số audio có sẵn được kiểm tra checksum bên dưới. Không thay thế thử tạo MP3 mới." },
            new { name = "Hàng đợi thống kê", state = config.GetValue("Analytics:WorkerEnabled", true) ? "Được bật" : "Đang tắt", detail = "Batch tối đa 25 sự kiện, worker mỗi 2 giây; heartbeat tách khỏi lượt xem/nghe. Hàng đợi lưu bền trong kho JSON." },
            new { name = "Bản đồ offline", state = map != null ? "Đã xác minh" : "Chưa sẵn sàng", detail = map != null ? $"Gói {map.Version}, {map.Bytes:N0} byte; checksum hợp lệ." : "Thiếu hoặc hỏng gói PMTiles/metadata. Có thể dùng bản đồ online khi có mạng." },
            new { name = "AI Advisor", state = advisor.Providers.Length > 0 ? "Đã cấu hình" : "Chưa cấu hình", detail = advisor.Providers.Length > 0 ? string.Join(" → ", advisor.Providers) + "; kết nối chỉ được xác minh khi tạo bản nháp." : "Cần cấu hình Gemini hoặc ProxyPal ở backend. Không trả bản nháp giả." },
            new { name = "Thời tiết cho AI", state = weather.Configured ? "Đã cấu hình" : "Chưa cấu hình", detail = "OpenWeather tùy chọn; chỉ gọi khi chọn dùng thời tiết, với tọa độ địa điểm đã xác minh." }
        };
        return Ok(new { snapshot.CheckedAt, coreReady = snapshot.Ready, checks, audio, optionalServicesBlockCore = false });
    }
}
