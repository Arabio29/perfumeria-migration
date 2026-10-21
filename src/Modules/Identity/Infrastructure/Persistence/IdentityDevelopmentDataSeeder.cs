using Danelix.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Danelix.Modules.Identity.Infrastructure.Persistence;

internal static class IdentityDevelopmentDataSeeder
{
    private static readonly DateTimeOffset SeedCreatedAtUtc = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid MarioId = Guid.Parse("019a2100-0000-7000-8000-000000000101");
    private static readonly Guid CamilaId = Guid.Parse("019a2100-0000-7000-8000-000000000102");
    private static readonly Guid AndresId = Guid.Parse("019a2100-0000-7000-8000-000000000103");
    private static readonly Guid LuciaId = Guid.Parse("019a2100-0000-7000-8000-000000000104");
    private static readonly Guid DiegoId = Guid.Parse("019a2100-0000-7000-8000-000000000105");
    private static readonly Guid ValentinaId = Guid.Parse("019a2100-0000-7000-8000-000000000106");

    public static async Task SeedAsync(IdentityDbContext dbContext, CancellationToken cancellationToken)
    {
        var users = new[]
        {
            User.Create("mario@danelix.local", "Mario (dueño)", SeedCreatedAtUtc, MarioId),
            User.Create("camila@danelix.local", "Camila (gerente)", SeedCreatedAtUtc, CamilaId),
            User.Create("andres@danelix.local", "Andrés (empleado)", SeedCreatedAtUtc, AndresId),
            User.Create("lucia@danelix.local", "Lucía (dueña)", SeedCreatedAtUtc, LuciaId),
            User.Create("diego@danelix.local", "Diego (administrador)", SeedCreatedAtUtc, DiegoId),
            User.Create("valentina@danelix.local", "Valentina (empleada)", SeedCreatedAtUtc, ValentinaId),
        };

        var seedIds = users.Select(user => user.Id).ToArray();
        var existingIds = await dbContext.Users
            .Where(user => seedIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToHashSetAsync(cancellationToken);

        dbContext.Users.AddRange(users.Where(user => !existingIds.Contains(user.Id)));
        await dbContext.SaveChangesAsync(cancellationToken);

        var platformRoles = new[]
        {
            PlatformRoleAssignment.Create(MarioId, PlatformRole.PlatformAdmin, SeedCreatedAtUtc),
            PlatformRoleAssignment.Create(ValentinaId, PlatformRole.SupportOperator, SeedCreatedAtUtc),
        };

        var existingPlatformRoles = (await dbContext.PlatformRoles
                .Where(assignment => seedIds.Contains(assignment.UserId))
                .Select(assignment => new { assignment.UserId, assignment.Role })
                .ToListAsync(cancellationToken))
            .Select(assignment => (assignment.UserId, assignment.Role))
            .ToHashSet();

        dbContext.PlatformRoles.AddRange(
            platformRoles.Where(assignment => !existingPlatformRoles.Contains((assignment.UserId, assignment.Role))));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
