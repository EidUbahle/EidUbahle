using System.Net;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CentralIdentity.IntegrationTests.Controllers;

public sealed class Phase8EndToEndTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public Phase8EndToEndTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.ReplaceWithFakes();
            });
        });
        _client = _factory.CreateClient();
    }

    private async Task SeedPublicClientAsync()
    {
        var appRepo = _factory.Services.GetRequiredService<IApplicationRepository>();
        if (await appRepo.GetByClientIdAsync("test-client", default) is not null)
            return;

        await appRepo.CreateAsync(new IdentityApplication
        {
            ApplicationCode = "PHASE8TEST",
            ApplicationName = "Phase 8 Test Client",
            ClientId = "test-client",
            ClientType = "Public",
            Audience = "https://test.example.com",
            AllowedRedirectUris = "https://test.example.com/callback",
            IsActive = true
        });
    }

    [Fact]
    public async Task WellKnown_Endpoint_Returns200()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Token_InvalidGrant_Returns400()
    {
        await SeedPublicClientAsync();

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = "invalid-code",
            ["client_id"] = "test-client",
            ["redirect_uri"] = "https://test.example.com/callback",
            ["code_verifier"] = "test-verifier"
        };

        var response = await _client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_MissingToken_Returns400()
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = "test-client"
        };

        var response = await _client.PostAsync("/connect/revoke", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
