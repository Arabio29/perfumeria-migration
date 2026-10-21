using Danelix.Modules.Tenancy.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Tenancy;

public class TenantMembershipTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void Create_WithValidData_StartsActive()
    {
        // Act
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Owner, CreatedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(TenantId, membership.TenantId);
        Assert.Equal(UserId, membership.UserId);
        Assert.Equal(TenantRole.Owner, membership.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(CreatedAt, membership.CreatedAtUtc);
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => TenantMembership.Create(Guid.Empty, UserId, TenantRole.Owner, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenUserIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => TenantMembership.Create(TenantId, Guid.Empty, TenantRole.Owner, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenRoleIsInvalid()
    {
        // Arrange
        var invalidRole = (TenantRole)99;

        // Act & Assert
        Assert.Throws<DomainException>(
            () => TenantMembership.Create(TenantId, UserId, invalidRole, CreatedAt));
    }

    [Fact]
    public void ChangeRole_ChangesRole()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);

        // Act
        membership.ChangeRole(TenantRole.Admin);

        // Assert
        Assert.Equal(TenantRole.Admin, membership.Role);
    }

    [Fact]
    public void ChangeRole_Throws_WhenRoleIsInvalid()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);
        var invalidRole = (TenantRole)99;

        // Act & Assert
        Assert.Throws<DomainException>(() => membership.ChangeRole(invalidRole));
        Assert.Equal(TenantRole.Member, membership.Role);
    }

    [Fact]
    public void Suspend_ChangesStatusToSuspended()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);

        // Act
        membership.Suspend();

        // Assert
        Assert.Equal(MembershipStatus.Suspended, membership.Status);
    }

    [Fact]
    public void Activate_ChangesStatusToActive()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);
        membership.Suspend();

        // Act
        membership.Activate();

        // Assert
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    [Fact]
    public void Revoke_ChangesStatusToRevoked()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);

        // Act
        membership.Revoke();

        // Assert
        Assert.Equal(MembershipStatus.Revoked, membership.Status);
    }

    [Fact]
    public void Activate_Throws_WhenRevoked()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);
        membership.Revoke();

        // Act & Assert
        Assert.Throws<DomainException>(membership.Activate);
    }

    [Fact]
    public void Reinvite_ChangesRevokedMembershipToActive()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);
        membership.Revoke();

        // Act
        membership.Reinvite();

        // Assert
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    [Fact]
    public void Reinvite_Throws_WhenMembershipIsNotRevoked()
    {
        // Arrange
        var membership = TenantMembership.Create(TenantId, UserId, TenantRole.Member, CreatedAt);

        // Act & Assert
        Assert.Throws<DomainException>(membership.Reinvite);
    }
}
