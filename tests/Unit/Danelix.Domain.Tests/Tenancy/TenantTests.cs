using Danelix.Modules.Tenancy.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Tenancy;

public class TenantTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_StartsActive()
    {
        // Act
        var tenant = Tenant.Create("  Perfumería Aurora  ", "  Aurora  ", CreatedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Perfumería Aurora", tenant.Name);
        Assert.Equal("aurora", tenant.Slug.Value);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal(CreatedAt, tenant.CreatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsEmpty(string? name)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Tenant.Create(name, "aurora", CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Tenant.Create("Aurora", "aurora", CreatedAt, Guid.Empty));
    }

    [Fact]
    public void Suspend_ChangesStatusToSuspended()
    {
        // Arrange
        var tenant = Tenant.Create("Aurora", "aurora", CreatedAt);

        // Act
        tenant.Suspend();

        // Assert
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
    }

    [Fact]
    public void Activate_ChangesStatusToActive()
    {
        // Arrange
        var tenant = Tenant.Create("Aurora", "aurora", CreatedAt);
        tenant.Suspend();

        // Act
        tenant.Activate();

        // Assert
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    [Fact]
    public void Close_ChangesStatusToClosed()
    {
        // Arrange
        var tenant = Tenant.Create("Aurora", "aurora", CreatedAt);

        // Act
        tenant.Close();

        // Assert
        Assert.Equal(TenantStatus.Closed, tenant.Status);
    }

    [Fact]
    public void Activate_Throws_WhenClosed()
    {
        // Arrange
        var tenant = Tenant.Create("Aurora", "aurora", CreatedAt);
        tenant.Close();

        // Act & Assert
        Assert.Throws<DomainException>(tenant.Activate);
    }
}
