using Danelix.Modules.Tenancy.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Tenancy;

public class BranchMembershipTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid BranchId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void Create_WithValidData()
    {
        // Act
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Manager, CreatedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(TenantId, membership.TenantId);
        Assert.Equal(BranchId, membership.BranchId);
        Assert.Equal(UserId, membership.UserId);
        Assert.Equal(BranchRole.Manager, membership.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(CreatedAt, membership.CreatedAtUtc);
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(Guid.Empty, BranchId, UserId, BranchRole.Manager, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenBranchIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(TenantId, Guid.Empty, UserId, BranchRole.Manager, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenUserIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(TenantId, BranchId, Guid.Empty, BranchRole.Manager, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenCreatedAtIsDefault()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Manager, default));
    }

    [Fact]
    public void Create_Throws_WhenRoleIsInvalid()
    {
        // Arrange
        var invalidRole = (BranchRole)99;

        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(TenantId, BranchId, UserId, invalidRole, CreatedAt));
    }

    [Fact]
    public void ChangeRole_ChangesRole()
    {
        // Arrange
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);

        // Act
        membership.ChangeRole(BranchRole.Manager);

        // Assert
        Assert.Equal(BranchRole.Manager, membership.Role);
    }

    [Fact]
    public void ChangeRole_Throws_WhenRoleIsInvalid()
    {
        // Arrange
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);
        var invalidRole = (BranchRole)99;

        // Act & Assert
        Assert.Throws<DomainException>(() => membership.ChangeRole(invalidRole));
        Assert.Equal(BranchRole.Staff, membership.Role);
    }

    [Fact]
    public void Suspend_ChangesStatusToSuspended()
    {
        // Arrange
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);

        // Act
        membership.Suspend();

        // Assert
        Assert.Equal(MembershipStatus.Suspended, membership.Status);
    }

    [Fact]
    public void Activate_ChangesStatusToActive()
    {
        // Arrange
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);
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
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);

        // Act
        membership.Revoke();

        // Assert
        Assert.Equal(MembershipStatus.Revoked, membership.Status);
    }

    [Fact]
    public void Activate_Throws_WhenMembershipIsRevoked()
    {
        // Arrange
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);
        membership.Revoke();

        // Act & Assert
        Assert.Throws<DomainException>(membership.Activate);
    }

    [Fact]
    public void Reinvite_ChangesRevokedMembershipToActive()
    {
        // Arrange
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);
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
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, BranchRole.Staff, CreatedAt);

        // Act & Assert
        Assert.Throws<DomainException>(membership.Reinvite);
    }
}
