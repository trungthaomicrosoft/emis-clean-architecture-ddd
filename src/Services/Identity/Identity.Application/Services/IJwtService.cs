using Identity.Domain.Aggregates;

namespace Identity.Application.Services;

/// <summary>
/// Service interface: JWT token generation & validation
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT access token
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generate refresh token (random string)
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate access token and extract user id
    /// </summary>
    Guid? ValidateAccessToken(string token);
}
