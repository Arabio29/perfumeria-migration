using System.Data;
using Danelix.Modules.Tenancy.Application;
using Danelix.Modules.Tenancy.Domain;
using Danelix.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

internal sealed class EfTenantMembershipAdministration(TenancyDbContext dbContext)
    : ITenantMembershipAdministration
{
    public async Task ChangeAsync(
        Guid tenantId,
        Guid membershipId,
        TenantRole requestedRole,
        MembershipStatus requestedStatus,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("El tenant es obligatorio.");
        }

        if (membershipId == Guid.Empty)
        {
            throw new DomainException("La membership es obligatoria.");
        }

        if (!Enum.IsDefined(requestedRole))
        {
            throw new DomainException("El rol de la membership no es válido.");
        }

        if (!Enum.IsDefined(requestedStatus))
        {
            throw new DomainException("El estado de la membership no es válido.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var lockedTenants = await dbContext.Tenants
            .FromSqlInterpolated($"SELECT * FROM tenants WHERE id = {tenantId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (lockedTenants.Count == 0)
        {
            throw new DomainException("El tenant no existe.");
        }

        var membership = await dbContext.TenantMemberships.SingleOrDefaultAsync(
            candidate => candidate.TenantId == tenantId && candidate.Id == membershipId,
            cancellationToken);

        if (membership is null)
        {
            throw new DomainException("La membership no existe en el tenant indicado.");
        }

        var otherActiveOwnerCount = await dbContext.TenantMemberships.CountAsync(
            candidate => candidate.TenantId == tenantId
                && candidate.Id != membershipId
                && candidate.Role == TenantRole.Owner
                && candidate.Status == MembershipStatus.Active,
            cancellationToken);

        TenantOwnerInvariant.EnsureOwnerRemains(
            membership,
            requestedRole,
            requestedStatus,
            otherActiveOwnerCount);

        switch (requestedStatus)
        {
            case MembershipStatus.Active when membership.Status == MembershipStatus.Revoked:
                membership.Reinvite();
                break;
            case MembershipStatus.Active:
                membership.Activate();
                break;
            case MembershipStatus.Suspended:
                membership.Suspend();
                break;
            case MembershipStatus.Revoked:
                membership.Revoke();
                break;
            default:
                throw new DomainException("El estado de la membership no es válido.");
        }

        membership.ChangeRole(requestedRole);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
