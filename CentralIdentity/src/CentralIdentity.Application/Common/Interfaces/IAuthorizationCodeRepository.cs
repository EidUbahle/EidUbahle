using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IAuthorizationCodeRepository
{
    Task StoreAsync(AuthorizationCode code, CancellationToken ct = default);
    Task<AuthorizationCode?> GetByHashAsync(string codeHash, CancellationToken ct = default);
    Task MarkAsUsedAsync(string codeHash, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}
