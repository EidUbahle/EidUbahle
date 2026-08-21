using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CentralIdentity.IntegrationTests.Controllers;

public class WellKnownEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WellKnownEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GET_openid_configuration_returns_200_with_expected_endpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(body.TryGetProperty("issuer", out _));
        Assert.True(body.TryGetProperty("authorization_endpoint", out var authEndpoint));
        Assert.True(body.TryGetProperty("token_endpoint", out var tokenEndpoint));
        Assert.True(body.TryGetProperty("userinfo_endpoint", out var userinfoEndpoint));
        Assert.True(body.TryGetProperty("jwks_uri", out var jwksUri));
        Assert.EndsWith("/connect/authorize", authEndpoint.GetString());
        Assert.EndsWith("/connect/token", tokenEndpoint.GetString());
        Assert.EndsWith("/connect/userinfo", userinfoEndpoint.GetString());
        Assert.EndsWith("/.well-known/jwks.json", jwksUri.GetString());
    }

    [Fact]
    public async Task GET_jwks_json_returns_200_with_rsa_public_key_only()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/.well-known/jwks.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var keys = body.GetProperty("keys");
        Assert.True(keys.GetArrayLength() >= 1);

        var key = keys[0];
        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("sig", key.GetProperty("use").GetString());
        Assert.Equal("RS256", key.GetProperty("alg").GetString());
        Assert.True(key.TryGetProperty("n", out _));
        Assert.True(key.TryGetProperty("e", out _));

        // Must never expose private key material (PKCS#1/PKCS#8 "d", "p", "q" etc. fields).
        Assert.False(key.TryGetProperty("d", out _));
        Assert.False(key.TryGetProperty("p", out _));
        Assert.False(key.TryGetProperty("q", out _));
    }
}
