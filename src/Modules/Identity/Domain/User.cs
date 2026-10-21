using Danelix.SharedKernel;

namespace Danelix.Modules.Identity.Domain;

internal sealed class User
{
    private const int MaxDisplayNameLength = 200;

    private User(Guid id, Email email, string displayName, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        Status = UserStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Email Email { get; }

    public string DisplayName { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static User Create(
        string? email,
        string? displayName,
        DateTimeOffset createdAtUtc,
        Guid? id = null)
    {
        var userId = id ?? Guid.CreateVersion7();
        if (userId == Guid.Empty)
        {
            throw new DomainException("El identificador del usuario es obligatorio.");
        }

        if (createdAtUtc == default)
        {
            throw new DomainException("La fecha de creación del usuario es obligatoria.");
        }

        var normalizedDisplayName = displayName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedDisplayName))
        {
            throw new DomainException("El nombre del usuario es obligatorio.");
        }

        if (normalizedDisplayName.Length > MaxDisplayNameLength)
        {
            throw new DomainException($"El nombre del usuario no puede superar {MaxDisplayNameLength} caracteres.");
        }

        return new User(
            userId,
            Email.Create(email),
            normalizedDisplayName,
            createdAtUtc.ToUniversalTime());
    }

    public void Disable()
    {
        if (Status == UserStatus.Disabled)
        {
            return;
        }

        Status = UserStatus.Disabled;
    }

    public void Activate()
    {
        if (Status == UserStatus.Active)
        {
            return;
        }

        Status = UserStatus.Active;
    }
}
