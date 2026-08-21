using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CentralIdentity.Api.Controllers;

[ApiController]
[Route("api/mfa")]
[Authorize]
[EnableRateLimiting("mfa")]
public sealed class MfaController : ApiControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly IMfaRepository _mfaRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditLog;
    private readonly JwtOptions _jwtOptions;

    public MfaController(
        IMfaService mfaService,
        IMfaRepository mfaRepo,
        IUserRepository userRepo,
        IAuditLogRepository auditLog,
        IOptions<JwtOptions> jwtOptions)
    {
        _mfaService = mfaService;
        _mfaRepo = mfaRepo;
        _userRepo = userRepo;
        _auditLog = auditLog;
        _jwtOptions = jwtOptions.Value;
    }

    private long GetUserId() => long.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? "0");

    [HttpPost("setup")]
    public async Task<IActionResult> Setup(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Unauthorized();
        }

        var (secret, qrUri) = _mfaService.GenerateTotpSetup(user.Email, _jwtOptions.Issuer);
        var encrypted = _mfaService.EncryptSecret(secret);

        await _mfaRepo.CreateOrUpdateAsync(new IdentityMfaMethod
        {
            UserId = userId,
            MethodType = "TOTP",
            SecretEncrypted = encrypted,
            IsEnabled = false,
            IsVerified = false,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        return Ok(new { qrUri, message = "Scan the QR code and verify with POST /api/mfa/verify" });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] MfaVerifyRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var method = await _mfaRepo.GetByUserAndTypeAsync(userId, "TOTP", ct);
        if (method is null)
        {
            return BadRequest("MFA not set up.");
        }

        var secret = _mfaService.DecryptSecret(method.SecretEncrypted);
        if (!_mfaService.VerifyTotp(secret, request.Code))
        {
            await _auditLog.LogAsync(new IdentityAuditLog
            {
                UserId = userId,
                EventType = "MfaFailed",
                Severity = "Warning",
                Description = "TOTP verification failed during setup/verify",
                CreatedAtUtc = DateTime.UtcNow
            }, ct);

            return BadRequest("Invalid code.");
        }

        method.IsVerified = true;
        await _mfaRepo.CreateOrUpdateAsync(method, ct);
        return Ok(new { verified = true });
    }

    [HttpPost("enable")]
    public async Task<IActionResult> Enable(CancellationToken ct)
    {
        var userId = GetUserId();
        var method = await _mfaRepo.GetByUserAndTypeAsync(userId, "TOTP", ct);
        if (method is null || !method.IsVerified)
        {
            return BadRequest("TOTP must be set up and verified first.");
        }

        method.IsEnabled = true;
        method.EnabledAtUtc = DateTime.UtcNow;
        await _mfaRepo.CreateOrUpdateAsync(method, ct);

        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            user.TwoFactorEnabled = true;
            await _userRepo.UpdateAsync(user, ct);
        }

        await _auditLog.LogAsync(new IdentityAuditLog
        {
            UserId = userId,
            EventType = "MfaEnabled",
            Severity = "Information",
            Description = "TOTP MFA enabled",
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        return Ok(new { enabled = true });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] MfaVerifyRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var method = await _mfaRepo.GetByUserAndTypeAsync(userId, "TOTP", ct);
        if (method is null || !method.IsEnabled)
        {
            return BadRequest("MFA is not enabled.");
        }

        var secret = _mfaService.DecryptSecret(method.SecretEncrypted);
        if (!_mfaService.VerifyTotp(secret, request.Code))
        {
            return BadRequest("Invalid code.");
        }

        method.IsEnabled = false;
        method.DisabledAtUtc = DateTime.UtcNow;
        await _mfaRepo.CreateOrUpdateAsync(method, ct);

        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            user.TwoFactorEnabled = false;
            await _userRepo.UpdateAsync(user, ct);
        }

        await _auditLog.LogAsync(new IdentityAuditLog
        {
            UserId = userId,
            EventType = "MfaDisabled",
            Severity = "Warning",
            Description = "TOTP MFA disabled",
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        return Ok(new { disabled = true });
    }

    [HttpPost("recovery-codes/regenerate")]
    public async Task<IActionResult> RegenerateRecoveryCodes(CancellationToken ct)
    {
        var userId = GetUserId();
        var codes = _mfaService.GenerateRecoveryCodes(8);
        var hashed = codes.Select(c => new IdentityRecoveryCode
        {
            UserId = userId,
            CodeHash = _mfaService.HashRecoveryCode(c),
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        await _mfaRepo.SaveRecoveryCodesAsync(userId, hashed, ct);
        return Ok(new { recoveryCodes = codes });
    }

    [HttpPost("challenge")]
    public IActionResult CreateChallenge()
        => Ok(new { message = "Submit your TOTP code to /api/mfa/verify" });
}

public sealed class MfaVerifyRequest
{
    public string Code { get; set; } = string.Empty;
}
