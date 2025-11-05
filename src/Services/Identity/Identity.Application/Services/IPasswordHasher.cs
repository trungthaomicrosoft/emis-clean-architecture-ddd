namespace Identity.Application.Services;

/// <summary>
/// Service interface: Password hashing & verification
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash password với salt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify password với hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
