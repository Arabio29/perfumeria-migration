using System.Net;
namespace Danelix.Api.Tests;

public class HealthEndpointTests : IClassFixture<DanelixApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(DanelixApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsNotFound()
    {
        // Act — la plantilla fue eliminada en el incremento anterior
        var response = await _client.GetAsync("/WeatherForecast");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
