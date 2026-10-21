using Danelix.Modules.Tenancy.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Tenancy;

public class BranchTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.CreateVersion7();

    [Fact]
    public void Create_WithValidData_StartsActive()
    {
        // Act
        var branch = Branch.Create(TenantId, "  Centro  ", "  br-01  ", CreatedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, branch.Id);
        Assert.Equal(TenantId, branch.TenantId);
        Assert.Equal("Centro", branch.Name);
        Assert.Equal("BR-01", branch.Code);
        Assert.True(branch.IsActive);
        Assert.Equal(CreatedAt, branch.CreatedAtUtc);
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Branch.Create(Guid.Empty, "Centro", "BR-01", CreatedAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsEmpty(string? name)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Branch.Create(TenantId, name, "BR-01", CreatedAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("CENTRO PRINCIPAL")]
    [InlineData("CENTRO_1")]
    public void Create_Throws_WhenCodeIsInvalid(string? code)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Branch.Create(TenantId, "Centro", code, CreatedAt));
    }

    [Fact]
    public void Deactivate_ChangesStatusToInactive()
    {
        // Arrange
        var branch = Branch.Create(TenantId, "Centro", "BR-01", CreatedAt);

        // Act
        branch.Deactivate();

        // Assert
        Assert.False(branch.IsActive);
    }

    [Fact]
    public void Activate_ChangesStatusToActive()
    {
        // Arrange
        var branch = Branch.Create(TenantId, "Centro", "BR-01", CreatedAt);
        branch.Deactivate();

        // Act
        branch.Activate();

        // Assert
        Assert.True(branch.IsActive);
    }
}
