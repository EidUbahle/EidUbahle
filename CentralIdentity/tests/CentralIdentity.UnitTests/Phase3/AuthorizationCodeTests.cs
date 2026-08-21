using CentralIdentity.Application.Options;
using CentralIdentity.Application.Services;
using CentralIdentity.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CentralIdentity.UnitTests.Phase3;

public class AuthorizationCodeTests
{
    private static AuthorizationCodeService CreateService(
        out FakeAuthorizationCodeRepository repo,
        bool requirePkce = true,
        int lifetimeMinutes = 5)
    {
        repo = new FakeAuthorizationCodeRepository();
        var options = Options.Create(new OAuthOptions
        {
            RequirePkce = requirePkce,
            AuthorizationCodeLifetimeMinutes = lifetimeMinutes
        });
        return new AuthorizationCodeService(repo, options, NullLogger<AuthorizationCodeService>.Instance);
    }

    private static CreateAuthorizationCodeCommand ValidCreateCommand(string? challenge = "challenge-value") =>
        new(UserId: 1, ApplicationId: 2, ClientId: "ci_test", RedirectUri: "https://app.example.com/callback",
            Scope: "profile", CodeChallenge: challenge, CodeChallengeMethod: challenge is null ? null : "S256");

    [Fact]
    public async Task CreateAuthorizationCodeAsync_StoresOnlyHash_NotPlaintextCode()
    {
        var service = CreateService(out var repo);

        var result = await service.CreateAuthorizationCodeAsync(ValidCreateCommand());

        Assert.True(result.IsSuccess);
        var plainCode = result.Value;

        // The plaintext code itself must never be discoverable as a stored CodeHash value.
        var lookupByPlainText = await repo.GetByHashAsync(plainCode);
        Assert.Null(lookupByPlainText);
    }

    [Fact]
    public async Task CreateAuthorizationCodeAsync_RequiresPkce_WhenConfigured()
    {
        var service = CreateService(out _, requirePkce: true);

        var result = await service.CreateAuthorizationCodeAsync(ValidCreateCommand(challenge: null));

        Assert.False(result.IsSuccess);
        Assert.Contains("code_challenge", result.Error);
    }

    [Fact]
    public async Task CreateAuthorizationCodeAsync_RejectsUnsupportedChallengeMethod()
    {
        var service = CreateService(out _);
        var command = ValidCreateCommand() with { CodeChallengeMethod = "plain" };

        var result = await service.CreateAuthorizationCodeAsync(command);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_Succeeds_ForValidCodeWithMatchingPkce()
    {
        var service = CreateService(out _);
        var verifier = "a-valid-code-verifier-with-enough-entropy-1234567890";
        var challenge = AuthorizationCodeService.ComputeS256Challenge(verifier);
        var created = await service.CreateAuthorizationCodeAsync(ValidCreateCommand(challenge));

        var result = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            created.Value, "ci_test", "https://app.example.com/callback", verifier));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.UserId);
        Assert.Equal(2, result.Value.ApplicationId);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_FailsOnSecondUse_EnforcingSingleUse()
    {
        var service = CreateService(out _);
        var verifier = "a-valid-code-verifier-with-enough-entropy-1234567890";
        var challenge = AuthorizationCodeService.ComputeS256Challenge(verifier);
        var created = await service.CreateAuthorizationCodeAsync(ValidCreateCommand(challenge));

        var first = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            created.Value, "ci_test", "https://app.example.com/callback", verifier));
        var second = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            created.Value, "ci_test", "https://app.example.com/callback", verifier));

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Contains("already been used", second.Error);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_FailsWhenExpired()
    {
        var service = CreateService(out var repo, lifetimeMinutes: 5);
        var verifier = "a-valid-code-verifier-with-enough-entropy-1234567890";
        var challenge = AuthorizationCodeService.ComputeS256Challenge(verifier);
        var created = await service.CreateAuthorizationCodeAsync(ValidCreateCommand(challenge));

        // Force expiry by rewinding the stored code's ExpiresAtUtc.
        var codeHash = await GetStoredHashAsync(repo, created.Value);
        var stored = await repo.GetByHashAsync(codeHash);
        stored!.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        var result = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            created.Value, "ci_test", "https://app.example.com/callback", verifier));

        Assert.False(result.IsSuccess);
        Assert.Contains("expired", result.Error);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_FailsWhenClientIdMismatch()
    {
        var service = CreateService(out _);
        var verifier = "a-valid-code-verifier-with-enough-entropy-1234567890";
        var challenge = AuthorizationCodeService.ComputeS256Challenge(verifier);
        var created = await service.CreateAuthorizationCodeAsync(ValidCreateCommand(challenge));

        var result = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            created.Value, "ci_other_client", "https://app.example.com/callback", verifier));

        Assert.False(result.IsSuccess);
        Assert.Contains("client_id", result.Error);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_FailsWhenRedirectUriMismatch()
    {
        var service = CreateService(out _);
        var verifier = "a-valid-code-verifier-with-enough-entropy-1234567890";
        var challenge = AuthorizationCodeService.ComputeS256Challenge(verifier);
        var created = await service.CreateAuthorizationCodeAsync(ValidCreateCommand(challenge));

        var result = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            created.Value, "ci_test", "https://evil.example.com/callback", verifier));

        Assert.False(result.IsSuccess);
        Assert.Contains("redirect_uri", result.Error);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_FailsWhenCodeVerifierDoesNotMatchChallenge()
    {
        var service = CreateService(out _);
        var verifier = "a-valid-code-verifier-with-enough-entropy-1234567890";
        var challenge = AuthorizationCodeService.ComputeS256Challenge(verifier);
        var created = await service.CreateAuthorizationCodeAsync(ValidCreateCommand(challenge));

        var result = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            created.Value, "ci_test", "https://app.example.com/callback", "wrong-verifier"));

        Assert.False(result.IsSuccess);
        Assert.Contains("code_verifier", result.Error);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_FailsForUnknownCode()
    {
        var service = CreateService(out _);

        var result = await service.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            "not-a-real-code", "ci_test", "https://app.example.com/callback", null));

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid authorization code", result.Error);
    }

    private static async Task<string> GetStoredHashAsync(FakeAuthorizationCodeRepository repo, string plainCode)
    {
        // Recompute the same SHA-256 hex digest AuthorizationCodeService uses internally, so the
        // test can locate the stored record without needing reflection into private helpers.
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plainCode));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        var found = await repo.GetByHashAsync(hex);
        Assert.NotNull(found);
        return hex;
    }
}
