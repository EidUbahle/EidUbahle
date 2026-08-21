using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Common;
using CentralIdentity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Application.Services;

public interface IApplicationService
{
    Task<Result<ApplicationRegistrationResult>> RegisterApplicationAsync(RegisterApplicationCommand command, CancellationToken ct = default);
    Task<Result<IdentityApplication>> GetApplicationAsync(long applicationId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IdentityApplication>>> GetApplicationsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result> UpdateApplicationAsync(UpdateApplicationCommand command, CancellationToken ct = default);
    Task<Result> EnableApplicationAsync(long applicationId, CancellationToken ct = default);
    Task<Result> DisableApplicationAsync(long applicationId, CancellationToken ct = default);
}

public sealed record RegisterApplicationCommand(
    string ApplicationCode,
    string ApplicationName,
    string? Description,
    string ClientType,
    string Audience,
    string? AllowedRedirectUris,
    string? AllowedOrigins);

public sealed record UpdateApplicationCommand(
    long ApplicationId,
    string? ApplicationName,
    string? Description,
    string? AllowedRedirectUris,
    string? AllowedOrigins);

public sealed record ApplicationRegistrationResult(
    long ApplicationId,
    string ApplicationCode,
    string ClientId,
    string? PlaintextClientSecret,
    string ClientType,
    string Audience);

public sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _appRepo;
    private readonly IClientSecretHasher _secretHasher;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(IApplicationRepository appRepo, IClientSecretHasher secretHasher, ILogger<ApplicationService> logger)
    {
        _appRepo = appRepo;
        _secretHasher = secretHasher;
        _logger = logger;
    }

    public async Task<Result<ApplicationRegistrationResult>> RegisterApplicationAsync(RegisterApplicationCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ApplicationCode)) return Result.Failure<ApplicationRegistrationResult>("ApplicationCode is required.");
        if (string.IsNullOrWhiteSpace(command.ApplicationName)) return Result.Failure<ApplicationRegistrationResult>("ApplicationName is required.");
        if (string.IsNullOrWhiteSpace(command.Audience)) return Result.Failure<ApplicationRegistrationResult>("Audience is required.");

        var clientType = command.ClientType?.Trim() ?? "Confidential";
        if (clientType != "Confidential" && clientType != "Public")
            return Result.Failure<ApplicationRegistrationResult>("ClientType must be 'Confidential' or 'Public'.");

        var code = command.ApplicationCode.Trim().ToUpperInvariant();
        if (await _appRepo.ExistsByCodeAsync(code, ct))
            return Result.Failure<ApplicationRegistrationResult>($"ApplicationCode '{code}' already exists.");

        var clientId = GenerateClientId();
        while (await _appRepo.ExistsByClientIdAsync(clientId, ct))
            clientId = GenerateClientId();

        string? plainSecret = null;
        string? secretHash = null;
        if (clientType == "Confidential")
        {
            plainSecret = GeneratePlainClientSecret();
            secretHash = _secretHasher.HashSecret(plainSecret);
        }

        var app = new IdentityApplication
        {
            ApplicationCode = code,
            ApplicationName = command.ApplicationName.Trim(),
            Description = command.Description?.Trim(),
            ClientId = clientId,
            ClientSecretHash = secretHash,
            ClientType = clientType,
            Audience = command.Audience.Trim(),
            AllowedRedirectUris = command.AllowedRedirectUris?.Trim(),
            AllowedOrigins = command.AllowedOrigins?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var id = await _appRepo.CreateAsync(app, ct);
        _logger.LogInformation("Application registered: {ApplicationCode} (ID={ApplicationId})", code, id);

        return Result.Success(new ApplicationRegistrationResult(id, code, clientId, plainSecret, clientType, app.Audience));
    }

    public async Task<Result<IdentityApplication>> GetApplicationAsync(long applicationId, CancellationToken ct = default)
    {
        var app = await _appRepo.GetByIdAsync(applicationId, ct);
        if (app is null) return Result.Failure<IdentityApplication>($"Application {applicationId} not found.");
        return Result.Success(app);
    }

    public async Task<Result<IReadOnlyList<IdentityApplication>>> GetApplicationsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        var apps = await _appRepo.GetAllAsync(page, pageSize, ct);
        return Result.Success(apps);
    }

    public async Task<Result> UpdateApplicationAsync(UpdateApplicationCommand command, CancellationToken ct = default)
    {
        var app = await _appRepo.GetByIdAsync(command.ApplicationId, ct);
        if (app is null) return Result.Failure($"Application {command.ApplicationId} not found.");
        if (command.ApplicationName is not null) app.ApplicationName = command.ApplicationName.Trim();
        if (command.Description is not null) app.Description = command.Description.Trim();
        if (command.AllowedRedirectUris is not null) app.AllowedRedirectUris = command.AllowedRedirectUris.Trim();
        if (command.AllowedOrigins is not null) app.AllowedOrigins = command.AllowedOrigins.Trim();
        app.UpdatedAtUtc = DateTime.UtcNow;
        await _appRepo.UpdateAsync(app, ct);
        return Result.Success();
    }

    public async Task<Result> EnableApplicationAsync(long applicationId, CancellationToken ct = default)
    {
        var app = await _appRepo.GetByIdAsync(applicationId, ct);
        if (app is null) return Result.Failure($"Application {applicationId} not found.");
        app.IsActive = true;
        app.UpdatedAtUtc = DateTime.UtcNow;
        await _appRepo.UpdateAsync(app, ct);
        return Result.Success();
    }

    public async Task<Result> DisableApplicationAsync(long applicationId, CancellationToken ct = default)
    {
        var app = await _appRepo.GetByIdAsync(applicationId, ct);
        if (app is null) return Result.Failure($"Application {applicationId} not found.");
        app.IsActive = false;
        app.UpdatedAtUtc = DateTime.UtcNow;
        await _appRepo.UpdateAsync(app, ct);
        return Result.Success();
    }

    private static string GenerateClientId() =>
        "ci_" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private static string GeneratePlainClientSecret() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(40))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
