using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Infrastructure.Data;
using CentralIdentity.Infrastructure.Repositories;
using CentralIdentity.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CentralIdentity.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<SqlServerHealthCheck>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IUserApplicationRepository, UserApplicationRepository>();
        services.AddScoped<IAuthorizationCodeRepository, AuthorizationCodeRepository>();

        // Security
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IClientSecretHasher, HmacClientSecretHasher>();
        services.AddSingleton<IJwtKeyProvider, RsaJwtKeyProvider>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

        return services;
    }
}
