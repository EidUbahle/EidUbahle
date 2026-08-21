using CentralIdentity.Application.Services;
using CentralIdentity.Infrastructure.Security;
using CentralIdentity.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CentralIdentity.UnitTests.Phase2;

public class UserServiceTests
{
    private static UserService CreateService(out FakeUserRepository repo)
    {
        repo = new FakeUserRepository();
        return new UserService(repo, new Pbkdf2PasswordHasher(), NullLogger<UserService>.Instance);
    }

    private static CreateUserCommand ValidCommand() => new(
        "jdoe", "jdoe@example.com", "555-0100", "Sup3rSecret!123", "Jane", "Doe");

    [Fact]
    public async Task CreateUserAsync_StoresHashedPassword_NeverPlaintext()
    {
        var service = CreateService(out var repo);

        var result = await service.CreateUserAsync(ValidCommand());

        Assert.True(result.IsSuccess);
        var stored = await repo.GetByIdAsync(result.Value);
        Assert.NotNull(stored);
        Assert.DoesNotContain("Sup3rSecret!123", stored!.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateUserAsync_RejectsDuplicateEmail()
    {
        var service = CreateService(out _);
        await service.CreateUserAsync(ValidCommand());

        var duplicate = await service.CreateUserAsync(ValidCommand() with { Username = "jdoe2" });

        Assert.False(duplicate.IsSuccess);
        Assert.Contains("Email", duplicate.Error);
    }

    [Fact]
    public async Task CreateUserAsync_RejectsDuplicateUsername()
    {
        var service = CreateService(out _);
        await service.CreateUserAsync(ValidCommand());

        var duplicate = await service.CreateUserAsync(ValidCommand() with { Email = "other@example.com" });

        Assert.False(duplicate.IsSuccess);
        Assert.Contains("Username", duplicate.Error);
    }

    [Theory]
    [InlineData("", "jdoe@example.com", "Sup3rSecret!123", "Jane", "Doe")]
    [InlineData("jdoe", "", "Sup3rSecret!123", "Jane", "Doe")]
    [InlineData("jdoe", "jdoe@example.com", "", "Jane", "Doe")]
    [InlineData("jdoe", "jdoe@example.com", "Sup3rSecret!123", "", "Doe")]
    [InlineData("jdoe", "jdoe@example.com", "Sup3rSecret!123", "Jane", "")]
    public async Task CreateUserAsync_RejectsMissingRequiredFields(
        string username, string email, string password, string firstName, string lastName)
    {
        var service = CreateService(out _);
        var command = new CreateUserCommand(username, email, null, password, firstName, lastName);

        var result = await service.CreateUserAsync(command);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DisableUserAsync_ThenEnableUserAsync_TogglesIsActive()
    {
        var service = CreateService(out var repo);
        var created = await service.CreateUserAsync(ValidCommand());

        await service.DisableUserAsync(created.Value);
        var disabled = await repo.GetByIdAsync(created.Value);
        Assert.False(disabled!.IsActive);

        await service.EnableUserAsync(created.Value);
        var enabled = await repo.GetByIdAsync(created.Value);
        Assert.True(enabled!.IsActive);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsFailure_WhenUserNotFound()
    {
        var service = CreateService(out _);

        var result = await service.GetUserAsync(999);

        Assert.False(result.IsSuccess);
    }
}
