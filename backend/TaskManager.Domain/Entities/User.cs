using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string name, string email, string passwordHash, UserRole role, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        Email = NormalizeEmail(email),
        PasswordHash = passwordHash,
        Role = role,
        CreatedAt = now
    };

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
