using Danelix.SharedKernel;

namespace Danelix.Modules.Tenancy.Domain;

internal sealed class Branch
{
    private const int MaxNameLength = 200;
    private const int MinCodeLength = 2;
    private const int MaxCodeLength = 20;

    private Branch(Guid id, Guid tenantId, string name, string code, DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Code = code;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static Branch Create(
        Guid tenantId,
        string? name,
        string? code,
        DateTimeOffset createdAtUtc,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("El tenant de la sucursal es obligatorio.");
        }

        var branchId = id ?? Guid.CreateVersion7();
        if (branchId == Guid.Empty)
        {
            throw new DomainException("El identificador de la sucursal es obligatorio.");
        }

        if (createdAtUtc == default)
        {
            throw new DomainException("La fecha de creación de la sucursal es obligatoria.");
        }

        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainException("El nombre de la sucursal es obligatorio.");
        }

        if (normalizedName.Length > MaxNameLength)
        {
            throw new DomainException($"El nombre de la sucursal no puede superar {MaxNameLength} caracteres.");
        }

        var normalizedCode = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new DomainException("El código de la sucursal es obligatorio.");
        }

        if (normalizedCode.Length is < MinCodeLength or > MaxCodeLength)
        {
            throw new DomainException($"El código de la sucursal debe tener entre {MinCodeLength} y {MaxCodeLength} caracteres.");
        }

        foreach (var character in normalizedCode)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-')
            {
                throw new DomainException("El código de la sucursal solo puede contener letras, números y guiones.");
            }
        }

        return new Branch(
            branchId,
            tenantId,
            normalizedName,
            normalizedCode,
            createdAtUtc.ToUniversalTime());
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
