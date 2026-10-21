using Danelix.SharedKernel;

namespace Danelix.Modules.Tenancy.Domain;

internal static class TenantOwnerInvariant
{
    public static void EnsureOwnerRemains(
        TenantMembership membership,
        TenantRole requestedRole,
        MembershipStatus requestedStatus,
        int otherActiveOwnerCount)
    {
        ArgumentNullException.ThrowIfNull(membership);

        if (!Enum.IsDefined(requestedRole))
        {
            throw new DomainException("El rol de la membresía no es válido.");
        }

        if (!Enum.IsDefined(requestedStatus))
        {
            throw new DomainException("El estado de la membresía no es válido.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(otherActiveOwnerCount);

        var removesActiveOwner = membership.Role == TenantRole.Owner
            && membership.Status == MembershipStatus.Active
            && (requestedRole != TenantRole.Owner || requestedStatus != MembershipStatus.Active);

        if (removesActiveOwner && otherActiveOwnerCount == 0)
        {
            throw new DomainException("El tenant debe conservar al menos un TenantOwner activo.");
        }
    }
}
