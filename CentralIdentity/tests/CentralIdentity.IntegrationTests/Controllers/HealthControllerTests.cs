using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace CentralIdentity.IntegrationTests.Controllers;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the SQL Server health check with an always-healthy one so
                // integration tests do not require a live database.
                services.RemoveAll<IHealthCheck>();
                services.AddHealthChecks();
            });
        });
    }

    [Fact]
    public async Task GET_api_health_returns_ok_or_unavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
            $"Unexpected status code: {response.StatusCode}");
    }

    [Fact]
    public async Task GET_api_health_returns_json_content_type()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
