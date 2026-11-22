using System.Security.Claims;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Services;

public sealed class DeviceBlockMiddleware(RequestDelegate next)
{
    public static string? Device(HttpContext context) => context.Request.Headers["X-SmartTour-Device"].FirstOrDefault() ?? context.Request.Cookies["smarttour-presence-device"];
    public async Task InvokeAsync(HttpContext context, DeviceBlockService blocks, PlatformService platform) {
        var path = context.Request.Path;
        var exempt = !path.StartsWithSegments("/api") || path.StartsWithSegments("/api/health") || path == "/api/presence/access" || path == "/api/presence/leave" || path.StartsWithSegments("/api/auth");
        if (!exempt && blocks.Until(Device(context)) is { } until) {
            var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Administrators retain access so a demo block cannot lock out management.
            if (actor == null || !platform.HasPermission(actor, "user:update")) {
                context.Response.StatusCode = 429; context.Response.Headers.CacheControl = "no-store";
                var seconds = Math.Max(1, (int)Math.Ceiling((until - DateTimeOffset.UtcNow).TotalSeconds));
                context.Response.Headers.RetryAfter = seconds.ToString();
                await context.Response.WriteAsJsonAsync(new { code = "DEVICE_BLOCKED", message = "Trình duyệt này đang bị tạm chặn truy cập.", blockedUntil = until, retryAfterSeconds = seconds }); return;
            }
        }
        await next(context);
    }
}
