using Danelix.Modules.Identity.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Identity;

public class UserTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_StartsActive()
    {
        // Act
        var user = User.Create("Owner@Aurora.Test", "  Ana  ", CreatedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("owner@aurora.test", user.Email.Value);
        Assert.Equal("Ana", user.DisplayName);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(CreatedAt, user.CreatedAtUtc);
    }

    [Fact]
    public void Create_NormalizesLocalDateToUtc()
    {
        // Arrange
        var localDate = new DateTimeOffset(2026, 10, 21, 8, 0, 0, TimeSpan.FromHours(-5));

        // Act
        var user = User.Create("owner@aurora.test", "Ana", localDate);

        // Assert
        Assert.Equal(TimeSpan.Zero, user.CreatedAtUtc.Offset);
        Assert.Equal(localDate.ToUniversalTime(), user.CreatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenDisplayNameIsEmpty(string? displayName)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => User.Create("owner@aurora.test", displayName, CreatedAt));
    }

    [Fact]
    public void Create_Throws_WhenIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => User.Create("owner@aurora.test", "Ana", CreatedAt, Guid.Empty));
    }

    [Fact]
    public void Create_Throws_WhenCreatedAtIsDefault()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => User.Create("owner@aurora.test", "Ana", default));
    }

    [Fact]
    public void Disable_ChangesStatusToDisabled()
    {
        // Arrange
        var user = User.Create("owner@aurora.test", "Ana", CreatedAt);

        // Act
        user.Disable();

        // Assert
        Assert.Equal(UserStatus.Disabled, user.Status);
    }

    [Fact]
    public void Activate_ChangesStatusToActive()
    {
        // Arrange
        var user = User.Create("owner@aurora.test", "Ana", CreatedAt);
        user.Disable();

        // Act
        user.Activate();

        // Assert
        Assert.Equal(UserStatus.Active, user.Status);
    }
}
