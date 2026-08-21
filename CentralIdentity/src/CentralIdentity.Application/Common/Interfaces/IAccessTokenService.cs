using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IAccessTokenService
{
    string CreateAccessToken(IdentityUser user, IdentityApplication application, IEnumerable<string> scopes);
}
