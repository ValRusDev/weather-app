namespace WeatherApp.Api.Models
{
    public class WeatherRequestLog
    {
        public long Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string City { get; set; } = "Moscow";
    }
}
