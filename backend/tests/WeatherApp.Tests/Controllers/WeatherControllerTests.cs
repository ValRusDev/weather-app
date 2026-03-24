using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Api.Controllers;
using WeatherApp.Api.Dtos;
using WeatherApp.Api.Services;

namespace WeatherApp.Tests.Controllers
{
    public class WeatherControllerTests
    {
        [Fact]
        public async Task Get_ShouldReturnOk_WhenServiceReturnsData()
        {
            // Arrange
            var weatherServiceMock = new Mock<IWeatherService>();
            var loggerMock = new Mock<ILogger<WeatherController>>();

            var response = new WeatherResponseDto
            {
                City = "Moscow",
                Current = new CurrentWeatherDto
                {
                    LastUpdated = DateTime.UtcNow,
                    TempC = 5,
                    FeelsLikeC = 2,
                    Humidity = 80,
                    WindKph = 12,
                    ConditionText = "Cloudy",
                    ConditionIcon = "https://example.com/icon.png"
                },
                Hourly = [],
                Forecast = []
            };

            weatherServiceMock
                .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var controller = new WeatherController(weatherServiceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.Get(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var value = okResult.Value.Should().BeOfType<WeatherResponseDto>().Subject;
            value.City.Should().Be("Moscow");
        }

        [Fact]
        public async Task Get_ShouldReturn500_WhenServiceThrows()
        {
            // Arrange
            var weatherServiceMock = new Mock<IWeatherService>();
            var loggerMock = new Mock<ILogger<WeatherController>>();

            weatherServiceMock
                .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("External API error"));

            var controller = new WeatherController(weatherServiceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.Get(CancellationToken.None);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }
    }
}
