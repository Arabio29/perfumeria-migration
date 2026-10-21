using Danelix.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Danelix.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    internal const string MigrationsHistoryTableName = "__ef_migrations_history_identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    internal DbSet<User> Users => Set<User>();

    internal DbSet<PlatformRoleAssignment> PlatformRoles => Set<PlatformRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PlatformRoleAssignmentConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
