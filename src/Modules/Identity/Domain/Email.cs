using System.Net.Mail;
using Danelix.SharedKernel;

namespace Danelix.Modules.Identity.Domain;

internal sealed record Email
{
    private const int MaxLength = 320;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("El email es obligatorio.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            throw new DomainException($"El email no puede superar {MaxLength} caracteres.");
        }

        if (!MailAddress.TryCreate(normalized, out var address)
            || address is null
            || !string.Equals(address.Address, normalized, StringComparison.Ordinal)
            || !normalized.Contains('.'))
        {
            throw new DomainException("El formato del email no es válido.");
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
