using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface ITokenService
{
    Task<(string accessToken, string refreshToken, IdentitySession session)> IssueTokensAsync(
        IdentityUser user, IdentityApplication application, IEnumerable<string> scopes,
        string? ipAddress, string? userAgent, CancellationToken ct);

    Task<(string accessToken, string refreshToken)> RefreshAsync(
        string refreshToken, string clientId, string? ipAddress, string? userAgent, CancellationToken ct);

    Task RevokeRefreshTokenAsync(string refreshToken, string clientId, CancellationToken ct);
}
