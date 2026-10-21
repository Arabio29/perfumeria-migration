using Danelix.SharedKernel;

namespace Danelix.Modules.Tenancy.Domain;

internal sealed record TenantSlug
{
    private const int MinLength = 3;
    private const int MaxLength = 50;

    private TenantSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TenantSlug Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("El slug del tenant es obligatorio.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length is < MinLength or > MaxLength)
        {
            throw new DomainException($"El slug debe tener entre {MinLength} y {MaxLength} caracteres.");
        }

        if (normalized[0] == '-' || normalized[^1] == '-')
        {
            throw new DomainException("El slug no puede empezar ni terminar con guion.");
        }

        if (normalized.Contains("--", StringComparison.Ordinal))
        {
            throw new DomainException("El slug no puede contener guiones consecutivos.");
        }

        foreach (var character in normalized)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-')
            {
                throw new DomainException("El slug solo puede contener letras, números y guiones.");
            }
        }

        return new TenantSlug(normalized);
    }

    public override string ToString() => Value;
}
