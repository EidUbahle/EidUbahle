using Microsoft.Extensions.DependencyInjection;

namespace CentralIdentity.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application-layer services here as they are added in future phases.
        return services;
    }
}
