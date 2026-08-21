using CentralIdentity.Infrastructure.Security;
using Xunit;

namespace CentralIdentity.UnitTests.Phase2;

public class ClientSecretHasherTests
{
    private readonly HmacClientSecretHasher _hasher = new();

    [Fact]
    public void HashSecret_ProducesNonPlaintextHash()
    {
        var hash = _hasher.HashSecret("cs_super_secret_value");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.DoesNotContain("cs_super_secret_value", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifySecret_ReturnsTrue_ForCorrectSecret()
    {
        var hash = _hasher.HashSecret("cs_super_secret_value");

        Assert.True(_hasher.VerifySecret("cs_super_secret_value", hash));
    }

    [Fact]
    public void VerifySecret_ReturnsFalse_ForIncorrectSecret()
    {
        var hash = _hasher.HashSecret("cs_super_secret_value");

        Assert.False(_hasher.VerifySecret("wrong_secret", hash));
    }

    [Fact]
    public void VerifySecret_ReturnsFalse_ForMalformedHash()
    {
        Assert.False(_hasher.VerifySecret("cs_super_secret_value", "garbage"));
    }
}
