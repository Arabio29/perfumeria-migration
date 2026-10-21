using Danelix.Modules.Identity.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Identity;

public class EmailTests
{
    [Fact]
    public void Create_TrimsAndLowercases()
    {
        // Act
        var email = Email.Create("  Owner@Aurora.Test  ");

        // Assert
        Assert.Equal("owner@aurora.test", email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenEmpty(string? value)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Email.Create(value));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("owner@")]
    [InlineData("@aurora.test")]
    [InlineData("owner@aurora")]
    [InlineData("owner aurora@test.com")]
    public void Create_Throws_WhenFormatIsInvalid(string value)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Email.Create(value));
    }

    [Fact]
    public void Create_Throws_WhenTooLong()
    {
        // Arrange
        var localPart = new string('a', 315);
        var value = $"{localPart}@a.test";

        // Act & Assert
        Assert.Throws<DomainException>(() => Email.Create(value));
    }
}
