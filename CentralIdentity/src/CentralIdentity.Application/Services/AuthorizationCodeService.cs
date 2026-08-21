using System.Security.Cryptography;
using System.Text;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Common;
using CentralIdentity.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CentralIdentity.Application.Services;

public interface IAuthorizationCodeService
{
    Task<Result<string>> CreateAuthorizationCodeAsync(CreateAuthorizationCodeCommand command, CancellationToken ct = default);
    Task<Result<AuthorizationCode>> ValidateAndConsumeAsync(ValidateAuthorizationCodeCommand command, CancellationToken ct = default);
}

public sealed record CreateAuthorizationCodeCommand(
    long UserId,
    long ApplicationId,
    string ClientId,
    string RedirectUri,
    string Scope,
    string? CodeChallenge,
    string? CodeChallengeMethod);

public sealed record ValidateAuthorizationCodeCommand(
    string Code,
    string ClientId,
    string RedirectUri,
    string? CodeVerifier);

/// <summary>
/// Issues and validates short-lived, single-use OAuth2 authorization codes (RFC 6749 §4.1 / PKCE RFC 7636).
/// Codes themselves are high-entropy random values; only a SHA-256 digest is ever persisted so a
/// database read alone cannot be replayed as a valid code.
/// </summary>
public sealed class AuthorizationCodeService : IAuthorizationCodeService
{
    private const int CodeByteLength = 32;

    private readonly IAuthorizationCodeRepository _repo;
    private readonly OAuthOptions _options;
    private readonly ILogger<AuthorizationCodeService> _logger;

    public AuthorizationCodeService(
        IAuthorizationCodeRepository repo,
        IOptions<OAuthOptions> options,
        ILogger<AuthorizationCodeService> logger)
    {
        _repo = repo;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<string>> CreateAuthorizationCodeAsync(CreateAuthorizationCodeCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.RedirectUri))
            return Result.Failure<string>("RedirectUri is required.");

        if (_options.RequirePkce && string.IsNullOrWhiteSpace(command.CodeChallenge))
            return Result.Failure<string>("code_challenge is required (PKCE is mandatory).");

        if (!string.IsNullOrWhiteSpace(command.CodeChallenge))
        {
            var method = command.CodeChallengeMethod ?? "S256";
            if (method != "S256")
                return Result.Failure<string>("Only the S256 code_challenge_method is supported.");
        }

        var plainCode = GeneratePlainCode();
        var codeHash = HashCode(plainCode);

        var entity = new AuthorizationCode
        {
            CodeHash = codeHash,
            UserId = command.UserId,
            ApplicationId = command.ApplicationId,
            RedirectUri = command.RedirectUri,
            ClientId = command.ClientId,
            Scope = command.Scope,
            CodeChallenge = command.CodeChallenge,
            CodeChallengeMethod = command.CodeChallenge is null ? null : (command.CodeChallengeMethod ?? "S256"),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AuthorizationCodeLifetimeMinutes)
        };

        await _repo.StoreAsync(entity, ct);
        _logger.LogInformation("Authorization code issued for User {UserId} / Application {ApplicationId}", command.UserId, command.ApplicationId);
        return Result.Success(plainCode);
    }

    public async Task<Result<AuthorizationCode>> ValidateAndConsumeAsync(ValidateAuthorizationCodeCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            return Result.Failure<AuthorizationCode>("Authorization code is required.");

        var codeHash = HashCode(command.Code);
        var stored = await _repo.GetByHashAsync(codeHash, ct);
        if (stored is null)
            return Result.Failure<AuthorizationCode>("Invalid authorization code.");

        if (stored.IsUsed)
            return Result.Failure<AuthorizationCode>("Authorization code has already been used.");

        if (stored.ExpiresAtUtc <= DateTime.UtcNow)
            return Result.Failure<AuthorizationCode>("Authorization code has expired.");

        if (!string.Equals(stored.ClientId, command.ClientId, StringComparison.Ordinal))
            return Result.Failure<AuthorizationCode>("client_id does not match the authorization request.");

        if (!string.Equals(stored.RedirectUri, command.RedirectUri, StringComparison.Ordinal))
            return Result.Failure<AuthorizationCode>("redirect_uri does not match the authorization request.");

        if (!string.IsNullOrWhiteSpace(stored.CodeChallenge))
        {
            if (string.IsNullOrWhiteSpace(command.CodeVerifier))
                return Result.Failure<AuthorizationCode>("code_verifier is required.");

            if (!VerifyPkce(command.CodeVerifier, stored.CodeChallenge))
                return Result.Failure<AuthorizationCode>("code_verifier does not match code_challenge.");
        }
        else if (_options.RequirePkce)
        {
            return Result.Failure<AuthorizationCode>("PKCE is required but no code_challenge was recorded for this code.");
        }

        await _repo.MarkAsUsedAsync(codeHash, ct);
        return Result.Success(stored);
    }

    /// <summary>
    /// Validates a PKCE S256 code_verifier against the stored code_challenge:
    /// code_challenge == BASE64URL(SHA256(code_verifier)).
    /// </summary>
    public static bool VerifyPkce(string codeVerifier, string codeChallenge)
    {
        var computed = ComputeS256Challenge(codeVerifier);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(codeChallenge));
    }

    public static string ComputeS256Challenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string GeneratePlainCode() => Base64UrlEncode(RandomNumberGenerator.GetBytes(CodeByteLength));

    private static string HashCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))).ToLowerInvariant();

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
