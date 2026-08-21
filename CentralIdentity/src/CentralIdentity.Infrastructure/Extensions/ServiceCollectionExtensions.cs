using CentralIdentity.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CentralIdentity.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<SqlServerHealthCheck>();

        return services;
    }
}
