using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Entities;

/// <summary>
/// Core User entity representing a platform user/player.
/// Follows DDD principles with aggregate root pattern.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    
    // Status tracking
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Related aggregates
    public Wallet? Wallet { get; set; }
    public ICollection<Stake> Stakes { get; private set; } = new List<Stake>();

    // Navigation property for transactions (auditing)
    public ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    private User() { }

    /// <summary>
    /// Factory method to create a new user. Use only during registration.
    /// </summary>
    public static User Create(string email, string phoneNumber, string passwordHash, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        
        if (!email.Contains("@"))
            throw new ArgumentException("Invalid email format.", nameof(email));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("First and last name are required.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PhoneNumber = phoneNumber,
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        return user;
    }

    /// <summary>
    /// Records a successful login. Updates LastLoginAt timestamp.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account. Prevents future logins.
    /// </summary>
    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a deactivated account.
    /// </summary>
    public void Reactivate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public string GetFullName() => $"{FirstName} {LastName}";
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}
