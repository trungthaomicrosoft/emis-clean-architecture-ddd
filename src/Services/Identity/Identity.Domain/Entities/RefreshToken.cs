using EMIS.BuildingBlocks.MultiTenant;

namespace Identity.Domain.Entities;

/// <summary>
/// Entity: Refresh Token để gia hạn JWT
/// Thuộc về User aggregate
/// </summary>
public class RefreshToken : TenantEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    // For EF Core
    private RefreshToken() 
    {
        Token = string.Empty;
    }

    public RefreshToken(Guid tenantId, Guid userId, string token, DateTime expiresAt)
        : base(tenantId)
    {
        UserId = userId;
        Token = token ?? throw new ArgumentNullException(nameof(token));
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
    
    public bool IsActive() => !IsRevoked && !IsExpired();

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }
}
