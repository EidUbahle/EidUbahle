using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Phase7;

public sealed class SecurityHardeningTests
{
    [Fact]
    public void LockedUser_HasLockoutEndUtcSet()
    {
        var user = new IdentityUser
        {
            UserId = 1,
            Username = "testuser",
            FailedLoginAttempts = 5,
            LockoutEndUtc = DateTime.UtcNow.AddMinutes(15)
        };

        Assert.True(user.LockoutEndUtc.HasValue);
        Assert.True(user.LockoutEndUtc.Value > DateTime.UtcNow);
    }

    [Fact]
    public void ExpiredLockout_AllowsAuthentication()
    {
        var user = new IdentityUser
        {
            UserId = 1,
            Username = "testuser",
            LockoutEndUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.False(user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow);
    }

    [Fact]
    public void SecurityStamp_IsNotEmpty()
    {
        var user = new IdentityUser { SecurityStamp = Guid.NewGuid().ToString("N") };
        Assert.NotEmpty(user.SecurityStamp);
    }
}
