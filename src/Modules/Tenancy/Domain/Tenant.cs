using Danelix.SharedKernel;

namespace Danelix.Modules.Tenancy.Domain;

internal sealed class Tenant
{
    private const int MaxNameLength = 200;

    private Tenant(Guid id, string name, TenantSlug slug, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Status = TenantStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public TenantSlug Slug { get; private set; }

    public TenantStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static Tenant Create(
        string? name,
        string? slug,
        DateTimeOffset createdAtUtc,
        Guid? id = null)
    {
        var tenantId = id ?? Guid.CreateVersion7();
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("El identificador del tenant es obligatorio.");
        }

        if (createdAtUtc == default)
        {
            throw new DomainException("La fecha de creación del tenant es obligatoria.");
        }

        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainException("El nombre del tenant es obligatorio.");
        }

        if (normalizedName.Length > MaxNameLength)
        {
            throw new DomainException($"El nombre del tenant no puede superar {MaxNameLength} caracteres.");
        }

        return new Tenant(
            tenantId,
            normalizedName,
            TenantSlug.Create(slug),
            createdAtUtc.ToUniversalTime());
    }

    public void Suspend()
    {
        if (Status == TenantStatus.Closed)
        {
            throw new DomainException("Un tenant cerrado no se puede suspender.");
        }

        Status = TenantStatus.Suspended;
    }

    public void Activate()
    {
        if (Status == TenantStatus.Closed)
        {
            throw new DomainException("Un tenant cerrado no se puede reactivar.");
        }

        Status = TenantStatus.Active;
    }

    public void Close()
    {
        Status = TenantStatus.Closed;
    }
}
