using WeatherApp.Api.Dtos;

namespace WeatherApp.Api.Services
{
    public interface IWeatherService
    {
        Task<WeatherResponseDto> GetWeatherAsync(CancellationToken cancellationToken = default);
    }
}
