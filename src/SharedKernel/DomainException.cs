namespace Danelix.SharedKernel;

/// <summary>
/// Representa una violación de una regla de negocio del dominio.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
