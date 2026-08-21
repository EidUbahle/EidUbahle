using System.Security.Cryptography;
using System.Text;
using CentralIdentity.Application.Services;
using Xunit;

namespace CentralIdentity.UnitTests.Phase3;

public class PkceTests
{
    [Fact]
    public void ComputeS256Challenge_MatchesManualSha256Base64Url()
    {
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var actual = AuthorizationCodeService.ComputeS256Challenge(verifier);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void VerifyPkce_ReturnsTrue_ForMatchingVerifierAndChallenge()
    {
        const string verifier = "a-valid-code-verifier-with-enough-entropy-1234567890";
        var challenge = AuthorizationCodeService.ComputeS256Challenge(verifier);

        Assert.True(AuthorizationCodeService.VerifyPkce(verifier, challenge));
    }

    [Fact]
    public void VerifyPkce_ReturnsFalse_ForMismatchedVerifier()
    {
        var challenge = AuthorizationCodeService.ComputeS256Challenge("correct-verifier");

        Assert.False(AuthorizationCodeService.VerifyPkce("wrong-verifier", challenge));
    }

    [Fact]
    public void ComputeS256Challenge_ProducesUrlSafeOutput_WithNoPaddingOrUnsafeChars()
    {
        var challenge = AuthorizationCodeService.ComputeS256Challenge("some-verifier-value-1234567890abcdef");

        Assert.DoesNotContain('+', challenge);
        Assert.DoesNotContain('/', challenge);
        Assert.DoesNotContain('=', challenge);
    }
}
