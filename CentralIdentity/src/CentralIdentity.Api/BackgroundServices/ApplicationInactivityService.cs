using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CentralIdentity.Api.BackgroundServices;

/// <summary>
/// Background service that periodically revokes per-application access for users
/// who have been inactive for longer than SecurityOptions.ApplicationInactivityDays.
/// Operates per User+Application pair — revoking one application NEVER affects others.
/// </summary>
public sealed class ApplicationInactivityService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApplicationInactivityService> _logger;
    private readonly SecurityOptions _options;

    public ApplicationInactivityService(
        IServiceScopeFactory scopeFactory,
        ILogger<ApplicationInactivityService> logger,
        IOptions<SecurityOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ApplicationInactivityService started. Interval: {Interval} min, Threshold: {Days} days, BatchSize: {BatchSize}",
            _options.InactivityJobIntervalMinutes,
            _options.ApplicationInactivityDays,
            _options.InactivityBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApplicationInactivityService");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.InactivityJobIntervalMinutes), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var threshold = DateTime.UtcNow.AddDays(-_options.ApplicationInactivityDays);
        var totalRevoked = 0;

        while (true)
        {
            using var scope = _scopeFactory.CreateScope();
            var userAppRepo = scope.ServiceProvider.GetRequiredService<IUserApplicationRepository>();
            var refreshTokenRepo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
            var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            var batch = await userAppRepo.GetInactiveByThresholdAsync(threshold, _options.InactivityBatchSize, ct);
            if (batch.Count == 0)
                break;

            foreach (var ua in batch)
            {
                try
                {
                    await userAppRepo.RevokeForInactivityAsync(ua.UserId, ua.ApplicationId, ct);
                    await refreshTokenRepo.RevokeByUserApplicationAsync(ua.UserId, ua.ApplicationId, "InactivityRevocation", ct);
                    await sessionRepo.RevokeByUserApplicationAsync(ua.UserId, ua.ApplicationId, "InactivityRevocation", ct);

                    await auditRepo.LogAsync(new CentralIdentity.Domain.Entities.IdentityAuditLog
                    {
                        UserId = ua.UserId,
                        ApplicationId = ua.ApplicationId,
                        EventType = "UserApplicationInactivityRevoked",
                        Severity = "Warning",
                        Description = $"User {ua.UserId} application {ua.ApplicationId} revoked due to inactivity (threshold: {_options.ApplicationInactivityDays} days)",
                        CreatedAtUtc = DateTime.UtcNow
                    }, ct);

                    totalRevoked++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to revoke inactive UserApplication UserId={UserId} ApplicationId={ApplicationId}",
                        ua.UserId,
                        ua.ApplicationId);
                }
            }

            _logger.LogInformation("Inactivity batch processed: {BatchCount} records revoked.", batch.Count);

            if (batch.Count < _options.InactivityBatchSize)
                break;
        }

        if (totalRevoked > 0)
            _logger.LogInformation("ApplicationInactivityService: total {Count} UserApplications revoked.", totalRevoked);
    }
}
