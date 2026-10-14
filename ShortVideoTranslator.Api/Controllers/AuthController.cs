using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Api.Security;

namespace ShortVideoTranslator.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(PlatformService platform, IAntiforgery antiforgery, JwtTokenService tokens, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("csrf")]
    public IActionResult Csrf()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(platform.View(platform.User(User.FindFirstValue(ClaimTypes.NameIdentifier)!)!));

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = platform.Authenticate(request.Username, request.Password);
        if (user == null) return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
        await SignIn(user);
        platform.Store.Write(s => { PlatformService.Audit(s, user.Id, "auth.login", user.Id); return true; });
        return Ok(platform.View(user));
    }
    [HttpPost("register-owner")]
    [EnableRateLimiting("auth")]
    public IActionResult Register(RegisterRequest request) => Ok(platform.CreateUser("public", request.Username, request.DisplayName, request.Password, "poi_owner", true));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout() {
        if (Request.Cookies["SmartTour.Refresh"] is { } token) platform.RevokeRefresh(token);
        Response.Cookies.Delete("SmartTour.Refresh", new CookieOptions { Path = "/api/auth" });
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return NoContent();
    }

    [HttpPost("refresh"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh()
    {
        var raw = Request.Cookies["SmartTour.Refresh"] ?? throw new PlatformException(401, "Không có phiên làm mới.");
        var rotated = platform.RotateRefresh(raw); SetRefreshCookie(rotated.Token);
        await SignIn(rotated.User, false); return Ok(platform.View(rotated.User));
    }
    [HttpPost("token"), Consumes("application/json"), EnableRateLimiting("auth")]
    public IActionResult Token(LoginRequest request)
    {
        var user = platform.Authenticate(request.Username, request.Password);
        if (user == null) return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
        return Ok(new { accessToken = tokens.Create(user), refreshToken = platform.CreateRefresh(user.Id), expiresIn = 1800, user = platform.View(user) });
    }
    [HttpPost("refresh-token"), Consumes("application/json"), EnableRateLimiting("auth")]
    public IActionResult RefreshToken(RefreshTokenRequest request)
    {
        var rotated = platform.RotateRefresh(request.RefreshToken);
        return Ok(new { accessToken = tokens.Create(rotated.User), refreshToken = rotated.Token, expiresIn = 1800 });
    }

    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        platform.ChangePassword(id, request.CurrentPassword, request.NewPassword);
        var user = platform.User(id)!;
        await SignIn(user); return Ok(platform.View(user));
    }
    private Task SignIn(PlatformUser user, bool refresh = true) {
        if (refresh) SetRefreshCookie(platform.CreateRefresh(user.Id));
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.Username),
            new("stamp", user.SecurityStamp), new("expires_at", DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString())], CookieAuthenticationDefaults.AuthenticationScheme)),
        new AuthenticationProperties { IsPersistent = false, AllowRefresh = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });
    }
    private void SetRefreshCookie(string token) => Response.Cookies.Append("SmartTour.Refresh", token, new CookieOptions {
        HttpOnly = true, Secure = !environment.IsDevelopment(), SameSite = SameSiteMode.Strict, Path = "/api/auth", Expires = DateTimeOffset.UtcNow.AddDays(7)
    });
}
public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string DisplayName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record RefreshTokenRequest(string RefreshToken);
