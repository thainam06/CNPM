using System.Text.Json;
using ShortVideoTranslator.Business;
namespace ShortVideoTranslator.Api.Services;

public record WeatherContext(string Provider, double TemperatureC, string Description, DateTimeOffset ObservedAt);
public sealed class WeatherContextService(IConfiguration config, IHttpClientFactory clients, TimeProvider clock)
{
    public bool Configured => !string.IsNullOrWhiteSpace(config["Weather:ApiKey"]);
    private readonly object gate = new();
    private readonly Dictionary<string, (WeatherContext Value, DateTimeOffset Expires)> cache = [];
    public async Task<WeatherContext?> ReadAsync(Place place, CancellationToken token)
    {
        if (!Configured || !place.LocationVerified || place.Latitude == null || place.Longitude == null) return null;
        var coordinate = FormattableString.Invariant($"lat={place.Latitude.Value}&lon={place.Longitude.Value}");
        lock (gate) { if (cache.TryGetValue(coordinate, out var saved) && saved.Expires > clock.GetUtcNow()) return saved.Value; }
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token); budget.CancelAfter(TimeSpan.FromSeconds(5));
        try {
            var url = $"https://api.openweathermap.org/data/2.5/weather?{coordinate}&units=metric&lang=vi&appid={Uri.EscapeDataString(config["Weather:ApiKey"]!)}";
            using var response = await clients.CreateClient("WeatherContext").GetAsync(url, budget.Token);
            if (!response.IsSuccessStatusCode) return null;
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(budget.Token));
            var root = body.RootElement;
            var time = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("dt").GetInt64());
            var temperature = root.GetProperty("main").GetProperty("temp").GetDouble();
            var description = root.GetProperty("weather")[0].GetProperty("description").GetString();
            if (time < clock.GetUtcNow().AddHours(-1) || time > clock.GetUtcNow().AddMinutes(5) || !double.IsFinite(temperature) || temperature is < -90 or > 65 || string.IsNullOrWhiteSpace(description) || description.Length > 200) return null;
            var value = new WeatherContext("OpenWeather", temperature, description, time);
            var expiry = clock.GetUtcNow().AddMinutes(10); if (time.AddHours(1) < expiry) expiry = time.AddHours(1);
            lock (gate) { if (cache.Count >= 100) cache.Clear(); cache[coordinate] = (value, expiry); }
            return value;
        } catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException or ArgumentOutOfRangeException or FormatException or OperationCanceledException) {
            token.ThrowIfCancellationRequested(); return null;
        }
    }
}
