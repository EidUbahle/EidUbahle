using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Api.Services;
using CentralIdentity.Infrastructure.Data;
using CentralIdentity.Infrastructure.Extensions;
using CentralIdentity.Application.Extensions;

namespace CentralIdentity.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTime, DateTimeService>();

        services.AddApplication();
        services.AddInfrastructure();

        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Central Identity API",
                Version = "v1",
                Description = "Central Identity & Authentication Platform — Phase 1",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "EidUbahle",
                    Email = "admin@eidubahle.com"
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCors", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
                else
                {
                    // No origins configured — restrict to same-origin only (deny cross-origin).
                    // Override Cors:AllowedOrigins in configuration for production deployments.
                    policy
                        .SetIsOriginAllowed(_ => false)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sqlserver", tags: new[] { "db", "sql" });

        return services;
    }
}
