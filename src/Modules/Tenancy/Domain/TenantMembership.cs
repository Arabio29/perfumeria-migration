using Danelix.SharedKernel;

namespace Danelix.Modules.Tenancy.Domain;

internal sealed class TenantMembership
{
    private TenantMembership(
        Guid id,
        Guid tenantId,
        Guid userId,
        TenantRole role,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Role = role;
        Status = MembershipStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid UserId { get; }

    public TenantRole Role { get; private set; }

    public MembershipStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public void ChangeRole(TenantRole newRole)
    {
        if (!Enum.IsDefined(newRole))
        {
            throw new DomainException("El rol de la membresía no es válido.");
        }

        Role = newRole;
    }

    public static TenantMembership Create(
        Guid tenantId,
        Guid userId,
        TenantRole role,
        DateTimeOffset createdAtUtc,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("El tenant de la membresía es obligatorio.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("El usuario de la membresía es obligatorio.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException("El rol de la membresía no es válido.");
        }

        var membershipId = id ?? Guid.CreateVersion7();
        if (membershipId == Guid.Empty)
        {
            throw new DomainException("El identificador de la membresía es obligatorio.");
        }

        if (createdAtUtc == default)
        {
            throw new DomainException("La fecha de creación de la membresía es obligatoria.");
        }

        return new TenantMembership(
            membershipId,
            tenantId,
            userId,
            role,
            createdAtUtc.ToUniversalTime());
    }

    public void Suspend()
    {
        if (Status == MembershipStatus.Revoked)
        {
            throw new DomainException("Una membresía revocada no se puede suspender.");
        }

        Status = MembershipStatus.Suspended;
    }

    public void Activate()
    {
        if (Status == MembershipStatus.Revoked)
        {
            throw new DomainException("Una membresía revocada no se puede reactivar.");
        }

        Status = MembershipStatus.Active;
    }

    public void Reinvite()
    {
        if (Status != MembershipStatus.Revoked)
        {
            throw new DomainException("Solo se puede re-invitar una membresía revocada.");
        }

        Status = MembershipStatus.Active;
    }

    public void Revoke()
    {
        if (Status == MembershipStatus.Revoked)
        {
            return;
        }

        Status = MembershipStatus.Revoked;
    }
}
