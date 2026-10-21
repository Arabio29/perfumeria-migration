using Danelix.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

public sealed class TenancyDbContext : DbContext
{
    internal const string MigrationsHistoryTableName = "__ef_migrations_history_tenancy";

    public TenancyDbContext(DbContextOptions<TenancyDbContext> options)
        : base(options)
    {
    }

    internal DbSet<Tenant> Tenants => Set<Tenant>();

    internal DbSet<Branch> Branches => Set<Branch>();

    internal DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    internal DbSet<BranchMembership> BranchMemberships => Set<BranchMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMembershipConfiguration());
        modelBuilder.ApplyConfiguration(new BranchMembershipConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
