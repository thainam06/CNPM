using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Api.Security;

var builder = WebApplication.CreateBuilder(args);
// OpenWeather authenticates in its query string; never log that URL/API key.
builder.Logging.AddFilter("System.Net.Http.HttpClient.WeatherContext", LogLevel.None);
// Options are resolved after test/host configuration has been applied.
builder.Services.AddSingleton(services => {
    var configuration = services.GetRequiredService<IConfiguration>();
    var secret = configuration["Security:JwtSecret"];
    if (string.IsNullOrEmpty(secret)) {
        if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Cần Security__JwtSecret tối thiểu 32 ký tự ở production.");
        var folder = configuration["Storage:Directory"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(folder); var path = Path.Combine(folder, "jwt-secret.txt");
        if (!File.Exists(path)) File.WriteAllText(path, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        secret = File.ReadAllText(path);
    }
    if (secret.Length < 32) throw new InvalidOperationException("JWT secret cần tối thiểu 32 ký tự.");
    return new JwtTokenService(secret);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod().AllowCredentials();
    });
});


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton(_ => new JsonPlatformStore(Path.Combine(builder.Configuration["Storage:Directory"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data"), "platform.json")));
builder.Services.AddSingleton<IPlatformStore>(s => s.GetRequiredService<JsonPlatformStore>());
builder.Services.AddSingleton<IPlaceRepository>(s => s.GetRequiredService<JsonPlatformStore>());
builder.Services.AddSingleton<PlatformService>();
builder.Services.AddSingleton<TranslationReviewService>();
builder.Services.AddAntiforgery(options => {
    options.HeaderName = "X-CSRF-Token";
    options.Cookie.Name = "SmartTour.Csrf";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddAuthentication("SmartTour").AddPolicyScheme("SmartTour", "Cookie or Bearer", options => {
    options.ForwardDefaultSelector = context => context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? JwtBearerDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options => {
    options.Cookie.Name = "SmartTour.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return context.Response.WriteAsJsonAsync(new { message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập." }); };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return context.Response.WriteAsJsonAsync(new { message = "Bạn không có quyền truy cập." }); };
    options.Events.OnValidatePrincipal = context => {
        var user = context.HttpContext.RequestServices.GetRequiredService<PlatformService>().User(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
        if (user is not { Enabled: true } || user.SecurityStamp != context.Principal?.FindFirstValue("stamp")) context.RejectPrincipal();
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<JwtTokenService>((options, tokens) => {
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new() {
        ValidateIssuer = true, ValidIssuer = JwtTokenService.Issuer, ValidateAudience = true, ValidAudience = JwtTokenService.Audience,
        ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = tokens.Key,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.FromSeconds(10), NameClaimType = ClaimTypes.Name
    };
    options.Events = new JwtBearerEvents { OnTokenValidated = context => {
        var user = context.HttpContext.RequestServices.GetRequiredService<PlatformService>().User(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
        if (user is not { Enabled: true } || user.SecurityStamp != context.Principal?.FindFirstValue("stamp")) context.Fail("Phiên đã bị thu hồi.");
        return Task.CompletedTask;
    }};
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("speech", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("runtime", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = async (context, token) => {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Quá nhiều yêu cầu. Vui lòng chờ rồi thử lại." }, token);
    };
});
builder.Services.AddScoped<ShortVideoTranslator.Business.PlaceService>();
builder.Services.AddSingleton(builder.Configuration.GetSection("Translation").Get<ShortVideoTranslator.Business.TranslationOptions>() ?? new());
builder.Services.AddSingleton<ISampleNarrationCatalog, DemoNarrations>();
builder.Services.AddHttpClient("Translation", client => client.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddSingleton(services => new ShortVideoTranslator.Business.TranslationService(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("Translation"),
    services.GetRequiredService<ShortVideoTranslator.Business.TranslationOptions>(), services.GetRequiredService<ISampleNarrationCatalog>()));
builder.Services.AddScoped<
    ShortVideoTranslator.Business.FfmpegService>();
builder.Services.AddScoped<
    ShortVideoTranslator.Business.SpeechService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSingleton<
    ShortVideoTranslator.Business.TextToSpeechService>();
builder.Services.AddHttpClient("AiAdvisor", client => { client.Timeout = TimeSpan.FromSeconds(30); client.MaxResponseContentBufferSize = 128 * 1024; }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("WeatherContext", client => { client.Timeout = TimeSpan.FromSeconds(5); client.MaxResponseContentBufferSize = 64 * 1024; }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<AiAdvisorService>();
builder.Services.AddSingleton<AiQuotaService>();
builder.Services.AddSingleton<WeatherContextService>();
builder.Services.AddSingleton<SystemReadinessService>();
builder.Services.AddSingleton<AnalyticsQueue>();
if (builder.Configuration.GetValue("Analytics:WorkerEnabled", true)) builder.Services.AddHostedService<AnalyticsWorker>();
builder.Services.AddSingleton(builder.Configuration.GetSection("TextToSpeech").Get<ShortVideoTranslator.Business.TextToSpeechOptions>() ?? new());
builder.Services.AddOpenApi();
builder.Services.AddSingleton<NarrationAssetService>();
builder.Services.AddSingleton<MapPackService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<OwnerIdentityService>();
builder.Services.AddSingleton<AudioTaskWorker>();
if (builder.Configuration.GetValue("Audio:WorkersEnabled", true)) {
    builder.Services.AddHostedService(services => services.GetRequiredService<AudioTaskWorker>());
    builder.Services.AddHostedService<PlatformMaintenanceWorker>();
}

var app = builder.Build();
_ = app.Services.GetRequiredService<JwtTokenService>();

var platform = app.Services.GetRequiredService<PlatformService>();
if (!platform.Store.Read(s => s.Users.Any(u => u.Role == "super_admin")))
{
    var username = builder.Configuration["Admin:BootstrapUsername"] ?? "admin";
    var password = builder.Configuration["Admin:BootstrapPassword"];
    if (string.IsNullOrEmpty(password))
    {
        if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Cần cấu hình Admin__BootstrapPassword khi khởi tạo production.");
        password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var credentialPath = Path.Combine(app.Environment.ContentRootPath, "App_Data", "bootstrap-admin.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(credentialPath)!);
        File.WriteAllText(credentialPath, $"SmartTour local bootstrap\nUsername: {username}\nPassword: {password}\nĐổi mật khẩu ngay sau đăng nhập. File này nằm trong Git ignore.\n");
        app.Logger.LogInformation("Tài khoản Admin local đã tạo. Thông tin đăng nhập: {Path}", credentialPath);
    }
    platform.Bootstrap(username, password);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors("ReactFrontend");
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseAuthentication();
app.Use(async (context, next) => {
    try
    {
        var path = context.Request.Path;
        var protectedMutation = path.StartsWithSegments("/api/auth") || path.StartsWithSegments("/api/admin") || path.StartsWithSegments("/api/owner") || path.StartsWithSegments("/api/media") || path.StartsWithSegments("/api/ai") || path == "/api/localizations/warmup";
        var bearer = context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        var credentialEndpoint = path == "/api/auth/token" || path == "/api/auth/refresh-token";
        if (protectedMutation && !bearer && !credentialEndpoint && (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method)))
        {
            try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { message = "Phiên bảo mật không hợp lệ. Tải lại trang và thử lại." }); return; }
        }
        if (protectedMutation) context.Response.Headers.CacheControl = "no-store";
        await next(context);
    }
    catch (PlatformException exception) { context.Response.StatusCode = exception.StatusCode; await context.Response.WriteAsJsonAsync(new { message = exception.Message }); }
    catch (ArgumentException exception) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { message = exception.Message }); }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
});
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ready", storage = "local-json", version = platform.Store.Read(s => s.Version) }));

app.Run();

public partial class Program;
