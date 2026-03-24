namespace WeatherApp.Api.Dtos
{
    public class WeatherResponseDto
    {
        public string City { get; set; } = default!;
        public CurrentWeatherDto Current { get; set; } = default!;
        public List<HourlyWeatherDto> Hourly { get; set; } = [];
        public List<DailyForecastDto> Forecast { get; set; } = [];
    }

    public class CurrentWeatherDto
    {
        public DateTime LastUpdated { get; set; }
        public decimal TempC { get; set; }
        public decimal FeelsLikeC { get; set; }
        public int Humidity { get; set; }
        public decimal WindKph { get; set; }
        public string ConditionText { get; set; } = default!;
        public string ConditionIcon { get; set; } = default!;
    }

    public class HourlyWeatherDto
    {
        public DateTime Time { get; set; }
        public decimal TempC { get; set; }
        public string ConditionText { get; set; } = default!;
        public string ConditionIcon { get; set; } = default!;
        public int ChanceOfRain { get; set; }
    }

    public class DailyForecastDto
    {
        public DateTime Date { get; set; }
        public decimal MaxTempC { get; set; }
        public decimal MinTempC { get; set; }
        public string ConditionText { get; set; } = default!;
        public string ConditionIcon { get; set; } = default!;
    }
}
