using Danelix.Modules.Tenancy.Domain;

namespace Danelix.Modules.Tenancy.Application;

/// <summary>
/// Cambia rol/estado de una membership manteniendo la invariante de propietario.
/// La autorización del actor debe verificarse antes de invocar esta operación.
/// </summary>
internal interface ITenantMembershipAdministration
{
    Task ChangeAsync(
        Guid tenantId,
        Guid membershipId,
        TenantRole requestedRole,
        MembershipStatus requestedStatus,
        CancellationToken cancellationToken = default);
}
