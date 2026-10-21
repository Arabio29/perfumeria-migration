using Danelix.Modules.Identity.Domain;
using Danelix.SharedKernel;

namespace Danelix.Domain.Tests.Identity;

public class PlatformRoleAssignmentTests
{
    private static readonly Guid UserId = Guid.Parse("019a2100-0000-7000-8000-000000000101");
    private static readonly DateTimeOffset GrantedAt = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_AssignsRole()
    {
        // Act
        var assignment = PlatformRoleAssignment.Create(UserId, PlatformRole.PlatformAdmin, GrantedAt);

        // Assert
        Assert.Equal(UserId, assignment.UserId);
        Assert.Equal(PlatformRole.PlatformAdmin, assignment.Role);
        Assert.Equal(GrantedAt, assignment.GrantedAtUtc);
    }

    [Fact]
    public void Create_NormalizesLocalDateToUtc()
    {
        // Arrange
        var localDate = new DateTimeOffset(2026, 10, 21, 8, 0, 0, TimeSpan.FromHours(-5));

        // Act
        var assignment = PlatformRoleAssignment.Create(UserId, PlatformRole.SupportOperator, localDate);

        // Assert
        Assert.Equal(TimeSpan.Zero, assignment.GrantedAtUtc.Offset);
        Assert.Equal(localDate.ToUniversalTime(), assignment.GrantedAtUtc);
    }

    [Fact]
    public void Create_Throws_WhenUserIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => PlatformRoleAssignment.Create(Guid.Empty, PlatformRole.PlatformAdmin, GrantedAt));
    }

    [Fact]
    public void Create_Throws_WhenRoleIsInvalid()
    {
        // Arrange
        var invalidRole = (PlatformRole)99;

        // Act & Assert
        Assert.Throws<DomainException>(
            () => PlatformRoleAssignment.Create(UserId, invalidRole, GrantedAt));
    }

    [Fact]
    public void Create_Throws_WhenGrantedAtIsDefault()
    {
        // Act & Assert
        Assert.Throws<DomainException>(
            () => PlatformRoleAssignment.Create(UserId, PlatformRole.PlatformAdmin, default));
    }
}
