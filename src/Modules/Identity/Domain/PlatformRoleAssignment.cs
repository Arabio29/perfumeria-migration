using Danelix.SharedKernel;

namespace Danelix.Modules.Identity.Domain;

/// <summary>
/// Rol global de plataforma. No pertenece a un tenant ni sustituye una membership de negocio.
/// </summary>
internal sealed class PlatformRoleAssignment
{
    private PlatformRoleAssignment(Guid userId, PlatformRole role, DateTimeOffset grantedAtUtc)
    {
        UserId = userId;
        Role = role;
        GrantedAtUtc = grantedAtUtc;
    }

    public Guid UserId { get; }

    public PlatformRole Role { get; }

    public DateTimeOffset GrantedAtUtc { get; }

    public static PlatformRoleAssignment Create(
        Guid userId,
        PlatformRole role,
        DateTimeOffset grantedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("El usuario del rol de plataforma es obligatorio.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException("El rol de plataforma no es válido.");
        }

        if (grantedAtUtc == default)
        {
            throw new DomainException("La fecha de asignación del rol de plataforma es obligatoria.");
        }

        return new PlatformRoleAssignment(userId, role, grantedAtUtc.ToUniversalTime());
    }
}
