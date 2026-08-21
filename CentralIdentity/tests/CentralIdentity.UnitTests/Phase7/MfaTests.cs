using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Security;
using CentralIdentity.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace CentralIdentity.UnitTests.Phase7;

public sealed class MfaTests
{
    private static TotpMfaService CreateService()
    {
        var jwtOpts = Options.Create(new JwtOptions { Issuer = "https://identity.test" });
        var secOpts = Options.Create(new SecurityOptions { MfaEncryptionKey = string.Empty });
        return new TotpMfaService(jwtOpts, secOpts);
    }

    [Fact]
    public void GenerateTotpSetup_ReturnsSecretAndUri()
    {
        var svc = CreateService();
        var (secret, qrUri) = svc.GenerateTotpSetup("test@example.com", "TestIssuer");
        Assert.NotEmpty(secret);
        Assert.Contains("otpauth://totp/", qrUri);
        Assert.Contains(secret, qrUri);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip()
    {
        var svc = CreateService();
        const string original = "JBSWY3DPEHPK3PXP";
        var encrypted = svc.EncryptSecret(original);
        Assert.NotEqual(original, encrypted);
        var decrypted = svc.DecryptSecret(encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void GenerateRecoveryCodes_Returns8Codes()
    {
        var svc = CreateService();
        var codes = svc.GenerateRecoveryCodes(8);
        Assert.Equal(8, codes.Count);
        Assert.All(codes, c => Assert.NotEmpty(c));
        Assert.Equal(8, codes.Distinct().Count());
    }

    [Fact]
    public void HashRecoveryCode_VerifiesCorrectly()
    {
        var svc = CreateService();
        const string code = "abc123def456";
        var hash = svc.HashRecoveryCode(code);
        Assert.True(svc.VerifyRecoveryCode(code, hash));
        Assert.False(svc.VerifyRecoveryCode("wrongcode", hash));
    }

    [Fact]
    public void InvalidTotpCode_ReturnsFalse()
    {
        var svc = CreateService();
        Assert.False(svc.VerifyTotp("JBSWY3DPEHPK3PXP", "000000"));
        Assert.False(svc.VerifyTotp("JBSWY3DPEHPK3PXP", "abc"));
    }

    [Fact]
    public async Task MfaRepository_Fake_StoresAndRetrieves()
    {
        var repo = new FakeMfaRepository();
        var method = new IdentityMfaMethod
        {
            UserId = 1,
            MethodType = "TOTP",
            SecretEncrypted = "encrypted",
            IsEnabled = false,
            IsVerified = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repo.CreateOrUpdateAsync(method, default);
        var fetched = await repo.GetByUserAndTypeAsync(1, "TOTP", default);
        Assert.NotNull(fetched);
        Assert.Equal("encrypted", fetched!.SecretEncrypted);
    }

    [Fact]
    public async Task RecoveryCodes_SaveAndUse()
    {
        var svc = CreateService();
        var repo = new FakeMfaRepository();
        var codes = svc.GenerateRecoveryCodes(8);
        var hashed = codes.Select(c => new IdentityRecoveryCode
        {
            UserId = 1,
            CodeHash = svc.HashRecoveryCode(c),
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        await repo.SaveRecoveryCodesAsync(1, hashed, default);

        var active = await repo.GetActiveRecoveryCodesAsync(1, default);
        Assert.Equal(8, active.Count);
        Assert.True(svc.VerifyRecoveryCode(codes[0], active[0].CodeHash));
    }

    [Fact]
    public void SensitiveData_NotInTotpSecret()
    {
        var svc = CreateService();
        var (_, qrUri) = svc.GenerateTotpSetup("user@example.com", "TestIssuer");
        Assert.DoesNotContain("password", qrUri, StringComparison.OrdinalIgnoreCase);
    }
}
