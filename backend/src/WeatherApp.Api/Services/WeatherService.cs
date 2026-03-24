using System.Net;
using System.Text.Json;
using WeatherApp.Api.Data;
using WeatherApp.Api.Dtos;
using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;

        public WeatherService(HttpClient httpClient, IConfiguration configuration, AppDbContext dbContext)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<WeatherResponseDto> GetWeatherAsync(CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["WeatherApi:ApiKey"];
            var lat = _configuration["WeatherApi:Latitude"];
            var lon = _configuration["WeatherApi:Longitude"];

            var url = $"https://api.weatherapi.com/v1/forecast.json?key={apiKey}&q={lat},{lon}&days=3";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Version = HttpVersion.Version11;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var location = root.GetProperty("location");
                var current = root.GetProperty("current");
                var forecastDays = root.GetProperty("forecast").GetProperty("forecastday");

                var nowLocalEpoch = location.GetProperty("localtime_epoch").GetInt64();
                var now = DateTimeOffset.FromUnixTimeSeconds(nowLocalEpoch).DateTime;

                var hourlyItems = new List<HourlyWeatherDto>();
                var dailyItems = new List<DailyForecastDto>();

                foreach (var day in forecastDays.EnumerateArray())
                {
                    var date = DateTime.Parse(day.GetProperty("date").GetString()!);
                    var dayInfo = day.GetProperty("day");

                    dailyItems.Add(new DailyForecastDto
                    {
                        Date = date,
                        MaxTempC = dayInfo.GetProperty("maxtemp_c").GetDecimal(),
                        MinTempC = dayInfo.GetProperty("mintemp_c").GetDecimal(),
                        ConditionText = dayInfo.GetProperty("condition").GetProperty("text").GetString()?.Trim() ?? "",
                        ConditionIcon = NormalizeIcon(dayInfo.GetProperty("condition").GetProperty("icon").GetString())
                    });

                    foreach (var hour in day.GetProperty("hour").EnumerateArray())
                    {
                        var hourEpoch = hour.GetProperty("time_epoch").GetInt64();
                        var hourDateTime = DateTimeOffset.FromUnixTimeSeconds(hourEpoch).DateTime;

                        var isTodayRemainingHour = hourDateTime.Date == now.Date && hourDateTime.Hour >= now.Hour;
                        var isTomorrow = hourDateTime.Date == now.Date.AddDays(1);

                        if (isTodayRemainingHour || isTomorrow)
                        {
                            hourlyItems.Add(new HourlyWeatherDto
                            {
                                Time = hourDateTime,
                                TempC = hour.GetProperty("temp_c").GetDecimal(),
                                ConditionText = hour.GetProperty("condition").GetProperty("text").GetString()?.Trim() ?? "",
                                ConditionIcon = NormalizeIcon(hour.GetProperty("condition").GetProperty("icon").GetString()),
                                ChanceOfRain = hour.TryGetProperty("chance_of_rain", out var rain)
                                    ? rain.GetInt32()
                                    : 0
                            });
                        }
                    }
                }

                var city = location.GetProperty("name").GetString() ?? "Unknown";

                var result = new WeatherResponseDto
                {
                    City = city,
                    Current = new CurrentWeatherDto
                    {
                        LastUpdated = DateTime.Parse(current.GetProperty("last_updated").GetString()!),
                        TempC = current.GetProperty("temp_c").GetDecimal(),
                        FeelsLikeC = current.GetProperty("feelslike_c").GetDecimal(),
                        Humidity = current.GetProperty("humidity").GetInt32(),
                        WindKph = current.GetProperty("wind_kph").GetDecimal(),
                        ConditionText = current.GetProperty("condition").GetProperty("text").GetString()?.Trim() ?? "",
                        ConditionIcon = NormalizeIcon(current.GetProperty("condition").GetProperty("icon").GetString())
                    },
                    Hourly = hourlyItems.OrderBy(x => x.Time).ToList(),
                    Forecast = dailyItems.OrderBy(x => x.Date).ToList()
                };

                _dbContext.WeatherRequestLogs.Add(new WeatherRequestLog
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    IsSuccess = true,
                    City = city
                });

                await _dbContext.SaveChangesAsync(cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _dbContext.WeatherRequestLogs.Add(new WeatherRequestLog
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    IsSuccess = false,
                    City = "Moscow",
                    ErrorMessage = ex.Message
                });

                await _dbContext.SaveChangesAsync(cancellationToken);

                throw;
            }
        }

        private static string NormalizeIcon(string? icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
                return string.Empty;

            if (icon.StartsWith("//"))
                return $"https:{icon}";

            if (icon.StartsWith("/"))
                return $"https://{icon.TrimStart('/')}";

            return icon;
        }
    }
}
