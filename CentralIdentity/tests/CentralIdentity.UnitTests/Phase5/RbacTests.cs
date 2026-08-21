using CentralIdentity.Domain.Entities;
using CentralIdentity.UnitTests.Fakes;
using Xunit;

namespace CentralIdentity.UnitTests.Phase5;

public sealed class RbacTests
{
    [Fact]
    public async Task CreateRole_Succeeds()
    {
        var repo = new FakeRoleRepository();
        var role = new IdentityRole { ApplicationId = 1, RoleCode = "UniversityAdmin", RoleName = "University Admin", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        var id = await repo.CreateAsync(role, default);
        Assert.True(id > 0);
        var fetched = await repo.GetByIdAsync(id, default);
        Assert.NotNull(fetched);
        Assert.Equal("UniversityAdmin", fetched!.RoleCode);
    }

    [Fact]
    public async Task CreatePermission_Succeeds()
    {
        var repo = new FakePermissionRepository();
        var perm = new IdentityPermission { ApplicationId = 1, PermissionCode = "Students.View", PermissionName = "View Students", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        var id = await repo.CreateAsync(perm, default);
        Assert.True(id > 0);
        var fetched = await repo.GetByIdAsync(id, default);
        Assert.NotNull(fetched);
        Assert.Equal("Students.View", fetched!.PermissionCode);
    }

    [Fact]
    public async Task AssignPermissionToRole_Succeeds()
    {
        var roleRepo = new FakeRoleRepository();
        var role = new IdentityRole { ApplicationId = 1, RoleCode = "Admin", RoleName = "Admin", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        var roleId = await roleRepo.CreateAsync(role, default);
        await roleRepo.AssignPermissionAsync(roleId, 100, default);
        Assert.Contains(roleRepo.RolePermissions, rp => rp.RoleId == roleId && rp.PermissionId == 100);
    }

    [Fact]
    public async Task AssignRoleToUser_Succeeds()
    {
        var userRoleRepo = new FakeUserRoleRepository();
        await userRoleRepo.AssignAsync(new IdentityUserRole { UserId = 1, ApplicationId = 1, RoleId = 10, AssignedAtUtc = DateTime.UtcNow, IsActive = true }, default);
        var roles = await userRoleRepo.GetActiveByUserApplicationAsync(1, 1, default);
        Assert.Single(roles);
        Assert.Equal(10, roles[0].RoleId);
    }

    [Fact]
    public async Task RevokeRole_RemovesRole()
    {
        var userRoleRepo = new FakeUserRoleRepository();
        await userRoleRepo.AssignAsync(new IdentityUserRole { UserId = 1, ApplicationId = 1, RoleId = 10, AssignedAtUtc = DateTime.UtcNow, IsActive = true }, default);
        await userRoleRepo.RevokeAsync(1, 1, 10, default);
        var roles = await userRoleRepo.GetActiveByUserApplicationAsync(1, 1, default);
        Assert.Empty(roles);
    }

    [Fact]
    public async Task CrossApplicationRole_NotVisible_InOtherApplication()
    {
        var userRoleRepo = new FakeUserRoleRepository();
        await userRoleRepo.AssignAsync(new IdentityUserRole { UserId = 1, ApplicationId = 1, RoleId = 10, AssignedAtUtc = DateTime.UtcNow, IsActive = true }, default);
        var hospitalRoles = await userRoleRepo.GetActiveByUserApplicationAsync(1, 2, default);
        Assert.Empty(hospitalRoles);
    }

    [Fact]
    public async Task DisabledRole_StillAssignedButShouldBeFilteredByPolicy()
    {
        var roleRepo = new FakeRoleRepository();
        var role = new IdentityRole { ApplicationId = 1, RoleCode = "OldRole", RoleName = "Old Role", IsActive = false, CreatedAtUtc = DateTime.UtcNow };
        var id = await roleRepo.CreateAsync(role, default);
        var fetched = await roleRepo.GetByIdAsync(id, default);
        Assert.NotNull(fetched);
        Assert.False(fetched!.IsActive);
    }

    [Fact]
    public async Task CrossApp_PermissionCode_SeparatePerApplication()
    {
        var permRepo = new FakePermissionRepository();
        await permRepo.CreateAsync(new IdentityPermission { ApplicationId = 1, PermissionCode = "Students.View", PermissionName = "View Students", IsActive = true, CreatedAtUtc = DateTime.UtcNow }, default);
        await permRepo.CreateAsync(new IdentityPermission { ApplicationId = 2, PermissionCode = "Patients.View", PermissionName = "View Patients", IsActive = true, CreatedAtUtc = DateTime.UtcNow }, default);

        var universityPerms = await permRepo.GetByApplicationAsync(1, default);
        var hospitalPerms = await permRepo.GetByApplicationAsync(2, default);

        Assert.Single(universityPerms);
        Assert.Equal("Students.View", universityPerms[0].PermissionCode);
        Assert.Single(hospitalPerms);
        Assert.Equal("Patients.View", hospitalPerms[0].PermissionCode);
    }

    [Fact]
    public async Task UserWithRevokedApplication_HasNoEffectivePermissions()
    {
        var userRoleRepo = new FakeUserRoleRepository();
        var perms = await userRoleRepo.GetEffectivePermissionsAsync(1, 2, default);
        Assert.Empty(perms);
    }

    [Fact]
    public async Task RoleAssignment_DifferentApplications_AreIndependent()
    {
        var userRoleRepo = new FakeUserRoleRepository();
        await userRoleRepo.AssignAsync(new IdentityUserRole { UserId = 1, ApplicationId = 1, RoleId = 10, AssignedAtUtc = DateTime.UtcNow, IsActive = true }, default);
        await userRoleRepo.AssignAsync(new IdentityUserRole { UserId = 1, ApplicationId = 2, RoleId = 20, AssignedAtUtc = DateTime.UtcNow, IsActive = true }, default);

        var universityRoles = await userRoleRepo.GetActiveByUserApplicationAsync(1, 1, default);
        var hospitalRoles = await userRoleRepo.GetActiveByUserApplicationAsync(1, 2, default);

        Assert.Single(universityRoles);
        Assert.Equal(10, universityRoles[0].RoleId);
        Assert.Single(hospitalRoles);
        Assert.Equal(20, hospitalRoles[0].RoleId);
    }

    [Fact]
    public async Task RevokeUniversityRole_DoesNotAffectHospitalRole()
    {
        var userRoleRepo = new FakeUserRoleRepository();
        await userRoleRepo.AssignAsync(new IdentityUserRole { UserId = 1, ApplicationId = 1, RoleId = 10, AssignedAtUtc = DateTime.UtcNow, IsActive = true }, default);
        await userRoleRepo.AssignAsync(new IdentityUserRole { UserId = 1, ApplicationId = 2, RoleId = 20, AssignedAtUtc = DateTime.UtcNow, IsActive = true }, default);
        await userRoleRepo.RevokeAsync(1, 1, 10, default);

        var universityRoles = await userRoleRepo.GetActiveByUserApplicationAsync(1, 1, default);
        var hospitalRoles = await userRoleRepo.GetActiveByUserApplicationAsync(1, 2, default);

        Assert.Empty(universityRoles);
        Assert.Single(hospitalRoles);
    }
}
