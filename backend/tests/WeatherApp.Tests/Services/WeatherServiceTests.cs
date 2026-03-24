using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using WeatherApp.Api.Data;
using WeatherApp.Api.Services;

namespace WeatherApp.Tests.Services
{
    public class WeatherServiceTests
    {
        [Fact]
        public async Task GetWeatherAsync_ShouldReturnMappedResponse_AndWriteSuccessLog()
        {
            // Arrange
            var json = """
            {
                "location": {
                "name": "Moscow",
                "localtime_epoch": 1764061200
                },
                "current": {
                "last_updated": "2025-11-25 15:00",
                "temp_c": 5.0,
                "feelslike_c": 2.0,
                "humidity": 80,
                "wind_kph": 12.0,
                "condition": {
                    "text": "Cloudy",
                    "icon": "//cdn.weatherapi.com/weather/64x64/day/119.png"
                }
                },
                "forecast": {
                "forecastday": [
                    {
                    "date": "2025-11-25T00:00:00",
                    "day": {
                        "maxtemp_c": 6.0,
                        "mintemp_c": 1.0,
                        "condition": {
                        "text": "Cloudy",
                        "icon": "//cdn.weatherapi.com/weather/64x64/day/119.png"
                        }
                    },
                    "hour": [
                        {
                        "time_epoch": 1764057600,
                        "temp_c": 4.0,
                        "chance_of_rain": 10,
                        "condition": {
                            "text": "Cloudy",
                            "icon": "//cdn.weatherapi.com/weather/64x64/day/119.png"
                        }
                        },
                        {
                        "time_epoch": 1764061200,
                        "temp_c": 5.0,
                        "chance_of_rain": 20,
                        "condition": {
                            "text": "Cloudy",
                            "icon": "//cdn.weatherapi.com/weather/64x64/day/119.png"
                        }
                        },
                        {
                        "time_epoch": 1764064800,
                        "temp_c": 6.0,
                        "chance_of_rain": 30,
                        "condition": {
                            "text": "Sunny",
                            "icon": "//cdn.weatherapi.com/weather/64x64/day/113.png"
                        }
                        }
                    ]
                    },
                    {
                    "date": "2025-11-26T00:00:00",
                    "day": {
                        "maxtemp_c": 7.0,
                        "mintemp_c": 2.0,
                        "condition": {
                        "text": "Sunny",
                        "icon": "//cdn.weatherapi.com/weather/64x64/day/113.png"
                        }
                    },
                    "hour": [
                        {
                        "time_epoch": 1764144000,
                        "temp_c": 3.0,
                        "chance_of_rain": 5,
                        "condition": {
                            "text": "Clear",
                            "icon": "//cdn.weatherapi.com/weather/64x64/night/113.png"
                        }
                        },
                        {
                        "time_epoch": 1764147600,
                        "temp_c": 4.0,
                        "chance_of_rain": 0,
                        "condition": {
                            "text": "Sunny",
                            "icon": "//cdn.weatherapi.com/weather/64x64/day/113.png"
                        }
                        }
                    ]
                    },
                    {
                    "date": "2025-11-27T00:00:00",
                    "day": {
                        "maxtemp_c": 8.0,
                        "mintemp_c": 3.0,
                        "condition": {
                        "text": "Rain",
                        "icon": "//cdn.weatherapi.com/weather/64x64/day/296.png"
                        }
                    },
                    "hour": []
                    }
                ]
                }
            }
            """;

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.weatherapi.com/v1/")
            };

            var configuration = BuildConfiguration();

            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var dbContext = new AppDbContext(dbOptions);

            var service = new WeatherService(httpClient, configuration, dbContext);

            // Act
            var result = await service.GetWeatherAsync();

            // Assert
            result.City.Should().Be("Moscow");
            result.Current.TempC.Should().Be(5.0m);
            result.Current.ConditionIcon.Should().Be("https://cdn.weatherapi.com/weather/64x64/day/119.png");

            result.Forecast.Should().HaveCount(3);
            result.Hourly.Should().NotBeEmpty();
            result.Hourly.Should().OnlyContain(x => x.Time.Date == new DateTime(2025, 11, 25) || x.Time.Date == new DateTime(2025, 11, 26));

            var logs = dbContext.WeatherRequestLogs.ToList();
            logs.Should().HaveCount(1);
            logs[0].IsSuccess.Should().BeTrue();
            logs[0].City.Should().Be("Moscow");
        }

        [Fact]
        public async Task GetWeatherAsync_ShouldWriteErrorLog_WhenHttpRequestFails()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("boom")
                });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.weatherapi.com/v1/")
            };

            var configuration = BuildConfiguration();

            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var dbContext = new AppDbContext(dbOptions);

            var service = new WeatherService(httpClient, configuration, dbContext);

            // Act
            var act = async () => await service.GetWeatherAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>();

            var logs = dbContext.WeatherRequestLogs.ToList();
            logs.Should().HaveCount(1);
            logs[0].IsSuccess.Should().BeFalse();
            logs[0].City.Should().Be("Moscow");
            logs[0].ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        private static IConfiguration BuildConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["WeatherApi:ApiKey"] = "test-key",
                ["WeatherApi:Latitude"] = "55.7558",
                ["WeatherApi:Longitude"] = "37.6176"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }
    }
}
