using Danelix.SharedKernel;

namespace Danelix.Modules.Tenancy.Domain;

internal sealed class BranchMembership
{
    private BranchMembership(
        Guid id,
        Guid tenantId,
        Guid branchId,
        Guid userId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid BranchId { get; }

    public Guid UserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static BranchMembership Create(
        Guid tenantId,
        Guid branchId,
        Guid userId,
        DateTimeOffset createdAtUtc,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("El tenant de la membresía de sucursal es obligatorio.");
        }

        if (branchId == Guid.Empty)
        {
            throw new DomainException("La sucursal de la membresía es obligatoria.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("El usuario de la membresía de sucursal es obligatorio.");
        }

        var membershipId = id ?? Guid.CreateVersion7();
        if (membershipId == Guid.Empty)
        {
            throw new DomainException("El identificador de la membresía de sucursal es obligatorio.");
        }

        if (createdAtUtc == default)
        {
            throw new DomainException("La fecha de creación de la membresía de sucursal es obligatoria.");
        }

        return new BranchMembership(
            membershipId,
            tenantId,
            branchId,
            userId,
            createdAtUtc.ToUniversalTime());
    }
}
