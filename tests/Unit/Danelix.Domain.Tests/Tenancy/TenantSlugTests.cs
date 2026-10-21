using Danelix.Modules.Tenancy.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Tenancy;

public class TenantSlugTests
{
    [Fact]
    public void Create_TrimsAndLowercases()
    {
        // Act
        var slug = TenantSlug.Create("  Aurora-Perfumes  ");

        // Assert
        Assert.Equal("aurora-perfumes", slug.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    public void Create_Throws_WhenTooShortOrEmpty(string? value)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => TenantSlug.Create(value));
    }

    [Fact]
    public void Create_Throws_WhenTooLong()
    {
        // Arrange
        var value = new string('a', 51);

        // Act & Assert
        Assert.Throws<DomainException>(() => TenantSlug.Create(value));
    }

    [Theory]
    [InlineData("aurora_1")]
    [InlineData("aurora 1")]
    [InlineData("aurora/1")]
    [InlineData("aurora.1")]
    [InlineData("áurora")]
    public void Create_Throws_WhenContainsInvalidCharacters(string value)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => TenantSlug.Create(value));
    }

    [Theory]
    [InlineData("-aurora")]
    [InlineData("aurora-")]
    [InlineData("aurora--perfumes")]
    public void Create_Throws_WhenHyphenPlacementIsInvalid(string value)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => TenantSlug.Create(value));
    }
}
