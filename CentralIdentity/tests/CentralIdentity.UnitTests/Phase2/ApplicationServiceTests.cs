using CentralIdentity.Application.Services;
using CentralIdentity.Infrastructure.Security;
using CentralIdentity.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CentralIdentity.UnitTests.Phase2;

public class ApplicationServiceTests
{
    private static ApplicationService CreateService(out FakeApplicationRepository repo)
    {
        repo = new FakeApplicationRepository();
        return new ApplicationService(repo, new HmacClientSecretHasher(), NullLogger<ApplicationService>.Instance);
    }

    [Fact]
    public async Task RegisterApplicationAsync_GeneratesClientId_WithExpectedPrefixAndUniqueness()
    {
        var service = CreateService(out _);
        var command = new RegisterApplicationCommand("HOSPITAL", "Hospital App", null, "Confidential", "https://hospital.example.com", null, null);

        var result1 = await service.RegisterApplicationAsync(command with { ApplicationCode = "HOSPITAL" });
        var result2 = await service.RegisterApplicationAsync(command with { ApplicationCode = "UNIVERSITY" });

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.StartsWith("ci_", result1.Value.ClientId);
        Assert.StartsWith("ci_", result2.Value.ClientId);
        Assert.NotEqual(result1.Value.ClientId, result2.Value.ClientId);
    }

    [Fact]
    public async Task RegisterApplicationAsync_ConfidentialClient_ReturnsPlaintextSecretOnceOnly()
    {
        var service = CreateService(out _);
        var command = new RegisterApplicationCommand("HOSPITAL", "Hospital App", null, "Confidential", "https://hospital.example.com", null, null);

        var result = await service.RegisterApplicationAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.PlaintextClientSecret));
    }

    [Fact]
    public async Task RegisterApplicationAsync_PublicClient_HasNoClientSecret()
    {
        var service = CreateService(out _);
        var command = new RegisterApplicationCommand("SPA", "Single Page App", null, "Public", "https://spa.example.com", null, null);

        var result = await service.RegisterApplicationAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.PlaintextClientSecret);
    }

    [Fact]
    public async Task RegisterApplicationAsync_RejectsDuplicateApplicationCode()
    {
        var service = CreateService(out _);
        var command = new RegisterApplicationCommand("HOSPITAL", "Hospital App", null, "Confidential", "https://hospital.example.com", null, null);
        await service.RegisterApplicationAsync(command);

        var duplicate = await service.RegisterApplicationAsync(command with { ApplicationName = "Hospital App 2" });

        Assert.False(duplicate.IsSuccess);
        Assert.Contains("already exists", duplicate.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Weird")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RegisterApplicationAsync_RejectsInvalidClientType(string clientType)
    {
        var service = CreateService(out _);
        var command = new RegisterApplicationCommand("HOSPITAL", "Hospital App", null, clientType, "https://hospital.example.com", null, null);

        var result = await service.RegisterApplicationAsync(command);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterApplicationAsync_NormalizesApplicationCodeToUpperInvariant()
    {
        var service = CreateService(out var repo);
        var command = new RegisterApplicationCommand("hospital", "Hospital App", null, "Confidential", "https://hospital.example.com", null, null);

        var result = await service.RegisterApplicationAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("HOSPITAL", result.Value.ApplicationCode);
    }
}
