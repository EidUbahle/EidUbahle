using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Api.Services;
using CentralIdentity.Infrastructure.Data;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Extensions;
using CentralIdentity.Application.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CentralIdentity.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTime, DateTimeService>();

        services.AddApplication(configuration);
        services.AddInfrastructure();

        return services;
    }

    /// <summary>
    /// Configures JSON Web Token authentication for tokens issued by this platform's own
    /// authorization_code flow. Signatures are verified using the RSA public key exposed
    /// by <see cref="IJwtKeyProvider"/> (asymmetric RS256 — never a shared/symmetric key).
    /// Audience and session/application checks are enforced during bearer-token validation so
    /// protected endpoints reject tokens for the wrong audience, revoked sessions, or revoked
    /// user/application assignments before controller code executes.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IJwtKeyProvider, IOptions<JwtOptions>>((options, keyProvider, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(keyProvider.GetPublicKey()) { KeyId = keyProvider.KeyId },
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal;
                        if (principal?.Identity is not ClaimsIdentity identity)
                        {
                            context.Fail("Token principal is invalid.");
                            return;
                        }

                        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? principal.FindFirstValue("sub");
                        var clientId = principal.FindFirstValue("client_id");
                        var audience = principal.FindFirstValue("aud");
                        var sessionIdValue = principal.FindFirstValue("session_id");

                        if (!long.TryParse(subject, out var userId) ||
                            string.IsNullOrWhiteSpace(clientId) ||
                            string.IsNullOrWhiteSpace(audience) ||
                            !Guid.TryParse(sessionIdValue, out var sessionId))
                        {
                            context.Fail("Token is missing required claims.");
                            return;
                        }

                        var services = context.HttpContext.RequestServices;
                        var appRepository = services.GetRequiredService<IApplicationRepository>();
                        var userRepository = services.GetRequiredService<IUserRepository>();
                        var userApplicationRepository = services.GetRequiredService<IUserApplicationRepository>();
                        var sessionRepository = services.GetRequiredService<ISessionRepository>();
                        var userRoleRepository = services.GetRequiredService<IUserRoleRepository>();
                        var roleRepository = services.GetRequiredService<IRoleRepository>();

                        var application = await appRepository.GetByClientIdAsync(clientId, context.HttpContext.RequestAborted);
                        if (application is null || !application.IsActive || !string.Equals(application.Audience, audience, StringComparison.Ordinal))
                        {
                            context.Fail("Token audience or application is invalid.");
                            return;
                        }

                        var user = await userRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
                        if (user is null || !user.IsActive)
                        {
                            context.Fail("Token user is invalid.");
                            return;
                        }

                        var assignment = await userApplicationRepository.GetAsync(userId, application.ApplicationId, context.HttpContext.RequestAborted);
                        if (assignment is null || !assignment.IsActive)
                        {
                            context.Fail("User no longer has access to this application.");
                            return;
                        }

                        var session = await sessionRepository.GetByIdAsync(sessionId, context.HttpContext.RequestAborted);
                        if (session is null ||
                            !session.IsActive ||
                            session.RevokedAtUtc.HasValue ||
                            session.ExpiresAtUtc <= DateTime.UtcNow ||
                            session.UserId != userId ||
                            session.ApplicationId != application.ApplicationId ||
                            !string.Equals(session.ClientId, clientId, StringComparison.Ordinal) ||
                            !string.Equals(session.SecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
                        {
                            context.Fail("Token session is invalid.");
                            return;
                        }

                        var activeRoles = await userRoleRepository.GetActiveByUserApplicationAsync(userId, application.ApplicationId, context.HttpContext.RequestAborted);
                        foreach (var userRole in activeRoles)
                        {
                            var role = await roleRepository.GetByIdAsync(userRole.RoleId, context.HttpContext.RequestAborted);
                            if (role is null || !role.IsActive)
                                continue;

                            AddRoleClaim(identity, role.RoleCode);
                            AddRoleClaim(identity, role.RoleName);
                        }
                    }
                };
            });

        return services;
    }

    private static void AddRoleClaim(ClaimsIdentity identity, string role)
    {
        if (string.IsNullOrWhiteSpace(role) ||
            identity.Claims.Any(c => c.Type == identity.RoleClaimType && string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        identity.AddClaim(new Claim(identity.RoleClaimType, role));
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
                Description = "Central Identity & Authentication Platform — user, application and OAuth2/OIDC (authorization_code + PKCE) endpoints",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "EidUbahle",
                    Email = "admin@eidubahle.com"
                }
            });

            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT access token issued by the /connect/token endpoint, sent in the Authorization request header.",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("login", o =>
            {
                o.PermitLimit = 10;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("token", o =>
            {
                o.PermitLimit = 30;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("mfa", o =>
            {
                o.PermitLimit = 5;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
