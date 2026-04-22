using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mediconnet_Backend.Tests.Services;

public class PermissionServiceTests
{
    private static (ApplicationDbContext ctx, PermissionService sut) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PermissionService(ctx, NullLogger<PermissionService>.Instance, cache);
        return (ctx, sut);
    }

    // ==================== HasRoleAsync ====================

    [Fact]
    public async Task HasRoleAsync_MatchingRole_ReturnsTrue()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 1, Role = "medecin" });
        await ctx.SaveChangesAsync();

        (await sut.HasRoleAsync(1, "medecin")).Should().BeTrue();
    }

    [Fact]
    public async Task HasRoleAsync_DifferentRole_ReturnsFalse()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 1, Role = "patient" });
        await ctx.SaveChangesAsync();

        (await sut.HasRoleAsync(1, "medecin")).Should().BeFalse();
    }

    [Fact]
    public async Task HasRoleAsync_UnknownUser_ReturnsFalse()
    {
        var (_, sut) = CreateSut();

        (await sut.HasRoleAsync(999, "medecin")).Should().BeFalse();
    }

    // ==================== GetUserRoleAsync ====================

    [Fact]
    public async Task GetUserRoleAsync_KnownUser_ReturnsRole()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 5, Role = "infirmier" });
        await ctx.SaveChangesAsync();

        (await sut.GetUserRoleAsync(5)).Should().Be("infirmier");
    }

    [Fact]
    public async Task GetUserRoleAsync_UnknownUser_ReturnsUnknown()
    {
        var (_, sut) = CreateSut();

        (await sut.GetUserRoleAsync(999)).Should().Be("unknown");
    }

    // ==================== HasPermissionAsync ====================

    [Fact]
    public async Task HasPermissionAsync_Admin_AlwaysReturnsTrue()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 1, Role = "administrateur" });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionAsync(1, "anything.at.all")).Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UnknownUser_ReturnsFalse()
    {
        var (_, sut) = CreateSut();

        (await sut.HasPermissionAsync(999, "patients.view")).Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_RoleHasPermission_ReturnsTrue()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 10, Role = "medecin" });
        var perm = new Permission { IdPermission = 1, Code = "patients.view", Actif = true };
        ctx.Permissions.Add(perm);
        ctx.RolePermissions.Add(new RolePermission
        {
            IdRolePermission = 1,
            Role = "medecin",
            IdPermission = 1,
            Actif = true,
            Permission = perm
        });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionAsync(10, "patients.view")).Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UserSpecificRevoke_OverridesRole()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 11, Role = "medecin" });
        var perm = new Permission { IdPermission = 1, Code = "patients.edit", Actif = true };
        ctx.Permissions.Add(perm);
        ctx.RolePermissions.Add(new RolePermission
        {
            IdRolePermission = 1, Role = "medecin", IdPermission = 1, Actif = true, Permission = perm
        });
        ctx.UserPermissions.Add(new UserPermission
        {
            IdUserPermission = 1, IdUser = 11, IdPermission = 1, Granted = false, Permission = perm
        });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionAsync(11, "patients.edit")).Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_UserSpecificGrant_OverridesNoRole()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 12, Role = "patient" });
        var perm = new Permission { IdPermission = 2, Code = "special.action", Actif = true };
        ctx.Permissions.Add(perm);
        ctx.UserPermissions.Add(new UserPermission
        {
            IdUserPermission = 2, IdUser = 12, IdPermission = 2, Granted = true, Permission = perm
        });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionAsync(12, "special.action")).Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_NoRolePermission_ReturnsFalse()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 13, Role = "patient" });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionAsync(13, "patients.delete")).Should().BeFalse();
    }

    // ==================== HasPermissionByRoleAsync ====================

    [Fact]
    public async Task HasPermissionByRoleAsync_MatchingActivePermission_ReturnsTrue()
    {
        var (ctx, sut) = CreateSut();
        var perm = new Permission { IdPermission = 1, Code = "reports.view", Actif = true };
        ctx.Permissions.Add(perm);
        ctx.RolePermissions.Add(new RolePermission
        {
            IdRolePermission = 1, Role = "medecin", IdPermission = 1, Actif = true, Permission = perm
        });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionByRoleAsync("medecin", "reports.view")).Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionByRoleAsync_InactiveRolePermission_ReturnsFalse()
    {
        var (ctx, sut) = CreateSut();
        var perm = new Permission { IdPermission = 1, Code = "reports.view", Actif = true };
        ctx.Permissions.Add(perm);
        ctx.RolePermissions.Add(new RolePermission
        {
            IdRolePermission = 1, Role = "medecin", IdPermission = 1, Actif = false, Permission = perm
        });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionByRoleAsync("medecin", "reports.view")).Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByRoleAsync_InactivePermission_ReturnsFalse()
    {
        var (ctx, sut) = CreateSut();
        var perm = new Permission { IdPermission = 1, Code = "reports.view", Actif = false };
        ctx.Permissions.Add(perm);
        ctx.RolePermissions.Add(new RolePermission
        {
            IdRolePermission = 1, Role = "medecin", IdPermission = 1, Actif = true, Permission = perm
        });
        await ctx.SaveChangesAsync();

        (await sut.HasPermissionByRoleAsync("medecin", "reports.view")).Should().BeFalse();
    }

    // ==================== GetUserPermissionsAsync ====================

    [Fact]
    public async Task GetUserPermissionsAsync_Admin_ReturnsAllActivePermissions()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 1, Role = "administrateur" });
        ctx.Permissions.AddRange(
            new Permission { IdPermission = 1, Code = "a.b", Actif = true },
            new Permission { IdPermission = 2, Code = "c.d", Actif = true },
            new Permission { IdPermission = 3, Code = "e.f", Actif = false }
        );
        await ctx.SaveChangesAsync();

        var perms = await sut.GetUserPermissionsAsync(1);

        perms.Should().BeEquivalentTo("a.b", "c.d");
    }

    [Fact]
    public async Task GetUserPermissionsAsync_UnknownUser_ReturnsEmpty()
    {
        var (_, sut) = CreateSut();

        (await sut.GetUserPermissionsAsync(999)).Should().BeEmpty();
    }

    // ==================== HasAnyPermissionAsync / HasAllPermissionsAsync ====================

    [Fact]
    public async Task HasAnyPermissionAsync_AtLeastOneGranted_ReturnsTrue()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 20, Role = "administrateur" });
        await ctx.SaveChangesAsync();

        (await sut.HasAnyPermissionAsync(20, "a", "b", "c")).Should().BeTrue();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_NoneGranted_ReturnsFalse()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 21, Role = "patient" });
        await ctx.SaveChangesAsync();

        (await sut.HasAnyPermissionAsync(21, "a", "b", "c")).Should().BeFalse();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_Admin_ReturnsTrue()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 22, Role = "administrateur" });
        await ctx.SaveChangesAsync();

        (await sut.HasAllPermissionsAsync(22, "a", "b")).Should().BeTrue();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_MissingOne_ReturnsFalse()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 23, Role = "patient" });
        await ctx.SaveChangesAsync();

        (await sut.HasAllPermissionsAsync(23, "a", "b")).Should().BeFalse();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_EmptyCodes_ReturnsTrue()
    {
        var (ctx, sut) = CreateSut();
        ctx.Utilisateurs.Add(new Utilisateur { IdUser = 24, Role = "patient" });
        await ctx.SaveChangesAsync();

        (await sut.HasAllPermissionsAsync(24)).Should().BeTrue();
    }
}
