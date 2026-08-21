using CentralIdentity.Infrastructure.Security;
using Xunit;

namespace CentralIdentity.UnitTests.Phase2;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesNonPlaintextHash()
    {
        var hash = _hasher.HashPassword("Sup3rSecret!123");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.DoesNotContain("Sup3rSecret!123", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePassword_DueToRandomSalt()
    {
        var hash1 = _hasher.HashPassword("Sup3rSecret!123");
        var hash2 = _hasher.HashPassword("Sup3rSecret!123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.HashPassword("Sup3rSecret!123");

        Assert.True(_hasher.VerifyPassword("Sup3rSecret!123", hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForIncorrectPassword()
    {
        var hash = _hasher.HashPassword("Sup3rSecret!123");

        Assert.False(_hasher.VerifyPassword("WrongPassword", hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForMalformedHash()
    {
        Assert.False(_hasher.VerifyPassword("Sup3rSecret!123", "not-a-valid-hash"));
    }
}
