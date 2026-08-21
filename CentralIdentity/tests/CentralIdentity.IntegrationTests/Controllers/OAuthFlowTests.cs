using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CentralIdentity.IntegrationTests.Controllers;

/// <summary>
/// End-to-end exercise of the OAuth2 authorization_code + PKCE flow across the real
/// /connect/authorize, /connect/token and /connect/userinfo endpoints (against in-memory
/// fake repositories instead of a live SQL Server).
/// </summary>
public class OAuthFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OAuthFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => services.ReplaceRepositoriesWithFakes());
        });
    }

    private async Task<(IdentityUser User, IdentityApplication App, string ClientSecret)> SeedAsync()
    {
        var userRepo = _factory.Services.GetRequiredService<IUserRepository>();
        var appRepo = _factory.Services.GetRequiredService<IApplicationRepository>();
        var userAppRepo = _factory.Services.GetRequiredService<IUserApplicationRepository>();
        var secretHasher = _factory.Services.GetRequiredService<IClientSecretHasher>();

        var userId = await userRepo.CreateAsync(new IdentityUser
        {
            Username = "flow-test-user",
            Email = "flow-test@example.com",
            PasswordHash = "hash",
            FirstName = "Flow",
            LastName = "Tester",
            IsActive = true,
            SecurityStamp = "stamp"
        });

        const string clientSecret = "plaintext-test-secret-value";
        var appId = await appRepo.CreateAsync(new IdentityApplication
        {
            ApplicationCode = "FLOWTEST",
            ApplicationName = "Flow Test App",
            ClientId = "ci_flowtest",
            ClientSecretHash = secretHasher.HashSecret(clientSecret),
            ClientType = "Confidential",
            Audience = "https://flowtest.example.com",
            AllowedRedirectUris = "https://client.example.com/callback",
            IsActive = true
        });

        await userAppRepo.AssignAsync(new IdentityUserApplication
        {
            UserId = userId,
            ApplicationId = appId,
            IsActive = true,
            SecurityStamp = "stamp"
        });

        var user = await userRepo.GetByIdAsync(userId);
        var app = await appRepo.GetByIdAsync(appId);
        return (user!, app!, clientSecret);
    }

    private static string ComputeS256Challenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    [Fact]
    public async Task FullAuthorizationCodeFlow_WithPkce_IssuesAccessToken_UsableAgainstUserinfo()
    {
        var (user, app, clientSecret) = await SeedAsync();
        var codeVerifier = "a-valid-code-verifier-with-enough-entropy-1234567890abcdef";
        var codeChallenge = ComputeS256Challenge(codeVerifier);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Step 1: authorize
        var authorizeUrl = "/connect/authorize" +
            $"?response_type=code&client_id={app.ClientId}&redirect_uri=https://client.example.com/callback" +
            $"&scope=profile%20email&state=xyz&code_challenge={codeChallenge}&code_challenge_method=S256&user_id={user.UserId}";
        var authorizeResponse = await client.GetAsync(authorizeUrl);

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        var location = authorizeResponse.Headers.Location!.ToString();
        Assert.StartsWith("https://client.example.com/callback", location);
        Assert.Contains("state=xyz", location);

        var code = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(location).Query)["code"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(code));

        // Step 2: token exchange
        var tokenForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code!,
            ["redirect_uri"] = "https://client.example.com/callback",
            ["client_id"] = app.ClientId,
            ["client_secret"] = clientSecret,
            ["code_verifier"] = codeVerifier
        });
        var tokenResponse = await client.PostAsync("/connect/token", tokenForm);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var accessToken = tokenBody.GetProperty("access_token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal("Bearer", tokenBody.GetProperty("token_type").GetString());
        Assert.True(tokenBody.GetProperty("expires_in").GetInt32() > 0);

        // Step 3: use the access token against userinfo
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.OK, userInfoResponse.StatusCode);
        var userInfoBody = await userInfoResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(user.UserId.ToString(), userInfoBody.GetProperty("sub").GetString());
        Assert.Equal(user.Email, userInfoBody.GetProperty("email").GetString());
    }

    [Fact]
    public async Task AuthorizationCode_CannotBeReplayed_SecondTokenExchangeFails()
    {
        var (user, app, clientSecret) = await SeedAsync();
        var codeVerifier = "a-valid-code-verifier-with-enough-entropy-1234567890abcdef";
        var codeChallenge = ComputeS256Challenge(codeVerifier);
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeUrl = "/connect/authorize" +
            $"?response_type=code&client_id={app.ClientId}&redirect_uri=https://client.example.com/callback" +
            $"&code_challenge={codeChallenge}&code_challenge_method=S256&user_id={user.UserId}";
        var authorizeResponse = await client.GetAsync(authorizeUrl);
        var location = authorizeResponse.Headers.Location!.ToString();
        var code = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(location).Query)["code"].ToString();

        var tokenForm = () => new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code!,
            ["redirect_uri"] = "https://client.example.com/callback",
            ["client_id"] = app.ClientId,
            ["client_secret"] = clientSecret,
            ["code_verifier"] = codeVerifier
        });

        var first = await client.PostAsync("/connect/token", tokenForm());
        var second = await client.PostAsync("/connect/token", tokenForm());

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task TokenExchange_WithWrongCodeVerifier_ReturnsBadRequest()
    {
        var (user, app, clientSecret) = await SeedAsync();
        var codeChallenge = ComputeS256Challenge("correct-verifier-value-1234567890");
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeUrl = "/connect/authorize" +
            $"?response_type=code&client_id={app.ClientId}&redirect_uri=https://client.example.com/callback" +
            $"&code_challenge={codeChallenge}&code_challenge_method=S256&user_id={user.UserId}";
        var authorizeResponse = await client.GetAsync(authorizeUrl);
        var location = authorizeResponse.Headers.Location!.ToString();
        var code = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(location).Query)["code"].ToString();

        var tokenForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code!,
            ["redirect_uri"] = "https://client.example.com/callback",
            ["client_id"] = app.ClientId,
            ["client_secret"] = clientSecret,
            ["code_verifier"] = "wrong-verifier-value"
        });
        var tokenResponse = await client.PostAsync("/connect/token", tokenForm);

        Assert.Equal(HttpStatusCode.BadRequest, tokenResponse.StatusCode);
    }

    [Fact]
    public async Task TokenExchange_WithIncorrectNonEmptyClientSecret_ReturnsUnauthorized()
    {
        var (user, app, _) = await SeedAsync();
        var codeChallenge = ComputeS256Challenge("correct-verifier-value-1234567890");
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeUrl = "/connect/authorize" +
            $"?response_type=code&client_id={app.ClientId}&redirect_uri=https://client.example.com/callback" +
            $"&code_challenge={codeChallenge}&code_challenge_method=S256&user_id={user.UserId}";
        var authorizeResponse = await client.GetAsync(authorizeUrl);
        var location = authorizeResponse.Headers.Location!.ToString();
        var code = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(location).Query)["code"].ToString();

        // Uses a well-formed, non-empty secret that simply does not match the registered hash —
        // this must be rejected even though it passes the "is the field non-empty" check.
        var tokenForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code!,
            ["redirect_uri"] = "https://client.example.com/callback",
            ["client_id"] = app.ClientId,
            ["client_secret"] = "this-is-definitely-not-the-right-secret",
            ["code_verifier"] = "correct-verifier-value-1234567890"
        });
        var tokenResponse = await client.PostAsync("/connect/token", tokenForm);

        Assert.Equal(HttpStatusCode.Unauthorized, tokenResponse.StatusCode);
    }

    [Fact]
    public async Task TokenExchange_WithMissingClientSecret_ReturnsUnauthorized()
    {
        var (user, app, _) = await SeedAsync();
        var codeChallenge = ComputeS256Challenge("correct-verifier-value-1234567890");
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeUrl = "/connect/authorize" +
            $"?response_type=code&client_id={app.ClientId}&redirect_uri=https://client.example.com/callback" +
            $"&code_challenge={codeChallenge}&code_challenge_method=S256&user_id={user.UserId}";
        var authorizeResponse = await client.GetAsync(authorizeUrl);
        var location = authorizeResponse.Headers.Location!.ToString();
        var code = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(location).Query)["code"].ToString();

        var tokenForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code!,
            ["redirect_uri"] = "https://client.example.com/callback",
            ["client_id"] = app.ClientId,
            ["client_secret"] = string.Empty,
            ["code_verifier"] = "correct-verifier-value-1234567890"
        });
        var tokenResponse2 = await client.PostAsync("/connect/token", tokenForm);

        Assert.Equal(HttpStatusCode.Unauthorized, tokenResponse2.StatusCode);
    }

    [Fact]
    public async Task Authorize_RejectsUnregisteredRedirectUri()
    {
        var (user, app, _) = await SeedAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authorizeUrl = "/connect/authorize" +
            $"?response_type=code&client_id={app.ClientId}&redirect_uri=https://evil.example.com/callback" +
            $"&code_challenge=abc&code_challenge_method=S256&user_id={user.UserId}";
        var response = await client.GetAsync(authorizeUrl);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authorize_RevokedUserApplicationAccess_IsDenied()
    {
        var (user, app, _) = await SeedAsync();
        var userAppRepo = _factory.Services.GetRequiredService<IUserApplicationRepository>();
        var assignment = await userAppRepo.GetAsync(user.UserId, app.ApplicationId);
        assignment!.IsActive = false;
        await userAppRepo.UpdateAsync(assignment);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var authorizeUrl = "/connect/authorize" +
            $"?response_type=code&client_id={app.ClientId}&redirect_uri=https://client.example.com/callback" +
            $"&code_challenge=abc&code_challenge_method=S256&user_id={user.UserId}";
        var response = await client.GetAsync(authorizeUrl);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
