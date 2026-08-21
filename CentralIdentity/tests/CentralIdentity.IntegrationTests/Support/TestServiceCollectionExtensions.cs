using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.IntegrationTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CentralIdentity.IntegrationTests.Support;

/// <summary>
/// Swaps the real ADO.NET-backed repositories for in-memory fakes so Phase 2/3 integration
/// tests can exercise full controller + DI + auth-middleware pipelines without a live SQL Server.
/// Fakes are registered as singletons so seeded data persists for the lifetime of the
/// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/> instance.
/// </summary>
public static class TestServiceCollectionExtensions
{
    public static void ReplaceWithFakes(this IServiceCollection services) =>
        services.ReplaceRepositoriesWithFakes();

    public static void ReplaceRepositoriesWithFakes(this IServiceCollection services)
    {
        services.RemoveAll<IUserRepository>();
        services.AddSingleton<IUserRepository, FakeUserRepository>();

        services.RemoveAll<IApplicationRepository>();
        services.AddSingleton<IApplicationRepository, FakeApplicationRepository>();

        services.RemoveAll<IUserApplicationRepository>();
        services.AddSingleton<IUserApplicationRepository, FakeUserApplicationRepository>();

        services.RemoveAll<IAuthorizationCodeRepository>();
        services.AddSingleton<IAuthorizationCodeRepository, FakeAuthorizationCodeRepository>();

        services.RemoveAll<IRefreshTokenRepository>();
        services.AddSingleton<IRefreshTokenRepository, FakeRefreshTokenRepository>();

        services.RemoveAll<ISessionRepository>();
        services.AddSingleton<ISessionRepository, FakeSessionRepository>();

        services.RemoveAll<IAuditLogRepository>();
        services.AddSingleton<IAuditLogRepository, FakeAuditLogRepository>();

        services.RemoveAll<IMfaRepository>();
        services.AddSingleton<IMfaRepository, FakeMfaRepository>();

        services.RemoveAll<IRoleRepository>();
        services.AddSingleton<IRoleRepository, FakeRoleRepository>();

        services.RemoveAll<IPermissionRepository>();
        services.AddSingleton<IPermissionRepository, FakePermissionRepository>();

        services.RemoveAll<IUserRoleRepository>();
        services.AddSingleton<IUserRoleRepository, FakeUserRoleRepository>();
    }
}
