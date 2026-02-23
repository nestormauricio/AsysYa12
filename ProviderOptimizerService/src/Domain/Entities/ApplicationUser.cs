namespace ProviderOptimizerService.Domain.Entities;

public enum UserRole { User = 1, Admin = 2 }

/// <summary>Application user entity for authentication.</summary>
public class ApplicationUser : BaseEntity
{
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ApplicationUser() { }

    public static ApplicationUser Create(string username, string email, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username required.", nameof(username));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash required.", nameof(passwordHash));

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }
}
