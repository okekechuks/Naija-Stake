namespace NaijaStake.API.DTOs;

/// <summary>
/// DTO for user registration request.
/// </summary>
public class RegisterUserDto
{
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
}

/// <summary>
/// DTO for user login request.
/// </summary>
public class LoginUserDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

/// <summary>
/// DTO for user response (no sensitive data).
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// DTO for authentication response (includes token).
/// </summary>
public class AuthResponse
{
    public UserDto User { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAt { get; set; }
}
