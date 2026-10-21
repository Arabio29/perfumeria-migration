using Danelix.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

internal static class TenancyDevelopmentDataSeeder
{
    private static readonly DateTimeOffset SeedCreatedAtUtc = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid MarioId = Guid.Parse("019a2100-0000-7000-8000-000000000101");
    private static readonly Guid CamilaId = Guid.Parse("019a2100-0000-7000-8000-000000000102");
    private static readonly Guid AndresId = Guid.Parse("019a2100-0000-7000-8000-000000000103");
    private static readonly Guid LuciaId = Guid.Parse("019a2100-0000-7000-8000-000000000104");
    private static readonly Guid DiegoId = Guid.Parse("019a2100-0000-7000-8000-000000000105");
    private static readonly Guid ValentinaId = Guid.Parse("019a2100-0000-7000-8000-000000000106");

    private static readonly Guid AuroraTenantId = Guid.Parse("019a2100-0000-7000-8000-000000000201");
    private static readonly Guid BrisaTenantId = Guid.Parse("019a2100-0000-7000-8000-000000000202");

    private static readonly Guid AuroraCentralBranchId = Guid.Parse("019a2100-0000-7000-8000-000000000301");
    private static readonly Guid AuroraNorthBranchId = Guid.Parse("019a2100-0000-7000-8000-000000000302");
    private static readonly Guid BrisaCentralBranchId = Guid.Parse("019a2100-0000-7000-8000-000000000303");

    private static readonly Guid MarioMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000401");
    private static readonly Guid CamilaMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000402");
    private static readonly Guid AndresMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000403");
    private static readonly Guid LuciaMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000404");
    private static readonly Guid DiegoMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000405");
    private static readonly Guid ValentinaMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000406");

    private static readonly Guid CamilaCentralBranchMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000501");
    private static readonly Guid CamilaNorthBranchMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000502");
    private static readonly Guid AndresBranchMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000503");
    private static readonly Guid ValentinaBranchMembershipId = Guid.Parse("019a2100-0000-7000-8000-000000000504");

    public static async Task SeedAsync(TenancyDbContext dbContext, CancellationToken cancellationToken)
    {
        var tenants = new[]
        {
            Tenant.Create("Perfumería Aurora", "aurora", SeedCreatedAtUtc, AuroraTenantId),
            Tenant.Create("Perfumería Brisa", "brisa", SeedCreatedAtUtc, BrisaTenantId),
        };

        var branches = new[]
        {
            Branch.Create(AuroraTenantId, "Sucursal Centro", "CENTRAL", SeedCreatedAtUtc, AuroraCentralBranchId),
            Branch.Create(AuroraTenantId, "Sucursal Norte", "NORTH", SeedCreatedAtUtc, AuroraNorthBranchId),
            Branch.Create(BrisaTenantId, "Sucursal Principal", "MAIN", SeedCreatedAtUtc, BrisaCentralBranchId),
        };

        var tenantMemberships = new[]
        {
            TenantMembership.Create(AuroraTenantId, MarioId, TenantRole.Owner, SeedCreatedAtUtc, MarioMembershipId),
            TenantMembership.Create(AuroraTenantId, CamilaId, TenantRole.Member, SeedCreatedAtUtc, CamilaMembershipId),
            TenantMembership.Create(AuroraTenantId, AndresId, TenantRole.Member, SeedCreatedAtUtc, AndresMembershipId),
            TenantMembership.Create(BrisaTenantId, LuciaId, TenantRole.Owner, SeedCreatedAtUtc, LuciaMembershipId),
            TenantMembership.Create(BrisaTenantId, DiegoId, TenantRole.Admin, SeedCreatedAtUtc, DiegoMembershipId),
            TenantMembership.Create(BrisaTenantId, ValentinaId, TenantRole.Member, SeedCreatedAtUtc, ValentinaMembershipId),
        };

        var branchMemberships = new[]
        {
            BranchMembership.Create(
                AuroraTenantId,
                AuroraCentralBranchId,
                CamilaId,
                BranchRole.Manager,
                SeedCreatedAtUtc,
                CamilaCentralBranchMembershipId),
            BranchMembership.Create(
                AuroraTenantId,
                AuroraNorthBranchId,
                CamilaId,
                BranchRole.Manager,
                SeedCreatedAtUtc,
                CamilaNorthBranchMembershipId),
            BranchMembership.Create(
                AuroraTenantId,
                AuroraCentralBranchId,
                AndresId,
                BranchRole.Staff,
                SeedCreatedAtUtc,
                AndresBranchMembershipId),
            BranchMembership.Create(
                BrisaTenantId,
                BrisaCentralBranchId,
                ValentinaId,
                BranchRole.Staff,
                SeedCreatedAtUtc,
                ValentinaBranchMembershipId),
        };

        var tenantIds = tenants.Select(tenant => tenant.Id).ToArray();
        var existingTenantIds = await dbContext.Tenants
            .Where(tenant => tenantIds.Contains(tenant.Id))
            .Select(tenant => tenant.Id)
            .ToHashSetAsync(cancellationToken);
        dbContext.Tenants.AddRange(tenants.Where(tenant => !existingTenantIds.Contains(tenant.Id)));

        var branchIds = branches.Select(branch => branch.Id).ToArray();
        var existingBranchIds = await dbContext.Branches
            .Where(branch => branchIds.Contains(branch.Id))
            .Select(branch => branch.Id)
            .ToHashSetAsync(cancellationToken);
        dbContext.Branches.AddRange(branches.Where(branch => !existingBranchIds.Contains(branch.Id)));

        var tenantMembershipIds = tenantMemberships.Select(membership => membership.Id).ToArray();
        var existingTenantMembershipIds = await dbContext.TenantMemberships
            .Where(membership => tenantMembershipIds.Contains(membership.Id))
            .Select(membership => membership.Id)
            .ToHashSetAsync(cancellationToken);
        dbContext.TenantMemberships.AddRange(
            tenantMemberships.Where(membership => !existingTenantMembershipIds.Contains(membership.Id)));

        var branchMembershipIds = branchMemberships.Select(membership => membership.Id).ToArray();
        var existingBranchMembershipIds = await dbContext.BranchMemberships
            .Where(membership => branchMembershipIds.Contains(membership.Id))
            .Select(membership => membership.Id)
            .ToHashSetAsync(cancellationToken);
        dbContext.BranchMemberships.AddRange(
            branchMemberships.Where(membership => !existingBranchMembershipIds.Contains(membership.Id)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
