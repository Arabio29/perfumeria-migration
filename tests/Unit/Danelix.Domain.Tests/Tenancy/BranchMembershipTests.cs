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
        var membership = BranchMembership.Create(TenantId, BranchId, UserId, CreatedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(TenantId, membership.TenantId);
        Assert.Equal(BranchId, membership.BranchId);
        Assert.Equal(UserId, membership.UserId);
        Assert.Equal(CreatedAt, membership.CreatedAtUtc);
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(Guid.Empty, BranchId, UserId, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenBranchIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(TenantId, Guid.Empty, UserId, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenUserIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(TenantId, BranchId, Guid.Empty, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenCreatedAtIsDefault()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => BranchMembership.Create(TenantId, BranchId, UserId, default));
    }
}
