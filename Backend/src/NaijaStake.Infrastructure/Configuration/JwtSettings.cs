namespace NaijaStake.Infrastructure.Configuration;

/// <summary>
/// JWT configuration settings.
/// SecretKey should be stored in environment variables or user secrets, never in appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key for signing JWT tokens. Must be at least 32 characters.
    /// Should be stored in environment variable JWT_SECRET_KEY or user secrets.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer identifier.
    /// </summary>
    public string Issuer { get; set; } = "NaijaStake";

    /// <summary>
    /// Token audience identifier.
    /// </summary>
    public string Audience { get; set; } = "NaijaStakeAPI";

    /// <summary>
    /// Access token expiration time in minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration time in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Validates that the SecretKey is configured and meets minimum requirements.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey is not configured. " +
                "Set it via environment variable JWT_SECRET_KEY or user secrets.");
        }

        if (SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT SecretKey must be at least 32 characters long for security.");
        }
    }
}
