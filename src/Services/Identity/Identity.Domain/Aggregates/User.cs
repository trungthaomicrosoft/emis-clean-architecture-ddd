using EMIS.BuildingBlocks.MultiTenant;
using EMIS.SharedKernel;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Aggregates;

/// <summary>
/// Aggregate Root: User
/// Business logic: Authentication, authorization, first-time password setup
/// </summary>
public class User : TenantEntity, IAggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = new();

    // Identity properties
    public PhoneNumber PhoneNumber { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    
    // Profile properties
    public string FullName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    
    // Reference to entity in other services (Teacher, Parent)
    public Guid? EntityId { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? PasswordSetAt { get; private set; }

    // Navigation
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // For EF Core
    private User() { }

    /// <summary>
    /// Constructor: Tạo user mới (chưa có password - PendingActivation)
    /// Dành cho Teacher và Parent được admin thêm vào
    /// </summary>
    public User(
        Guid tenantId,
        PhoneNumber phoneNumber,
        string fullName,
        UserRole role,
        Guid? entityId = null,
        string? email = null)
        : base(tenantId)
    {
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        FullName = ValidateFullName(fullName);
        Role = role;
        Email = email;
        EntityId = entityId;
        Status = UserStatus.PendingActivation;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserCreatedEvent(Id, TenantId, phoneNumber.Value, role));
    }

    /// <summary>
    /// Constructor: Tạo School Admin với password ngay
    /// </summary>
    public User(
        Guid tenantId,
        PhoneNumber phoneNumber,
        string fullName,
        string passwordHash,
        string? email = null)
        : base(tenantId)
    {
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        FullName = ValidateFullName(fullName);
        Role = UserRole.SchoolAdmin;
        Email = email;
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        Status = UserStatus.Active;
        CreatedAt = DateTime.UtcNow;
        PasswordSetAt = DateTime.UtcNow;

        AddDomainEvent(new UserCreatedEvent(Id, TenantId, phoneNumber.Value, UserRole.SchoolAdmin));
    }

    /// <summary>
    /// Business Logic: Thiết lập mật khẩu lần đầu
    /// </summary>
    public void SetPasswordFirstTime(string passwordHash)
    {
        if (Status != UserStatus.PendingActivation)
            throw new InvalidOperationException($"Cannot set password for user with status {Status}");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        PasswordSetAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PasswordSetEvent(Id, TenantId, PhoneNumber.Value));
    }

    /// <summary>
    /// Business Logic: Đổi mật khẩu
    /// </summary>
    public void ChangePassword(string newPasswordHash)
    {
        if (Status != UserStatus.Active)
            throw new InvalidOperationException($"Cannot change password for user with status {Status}");

        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("New password hash cannot be empty", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        PasswordSetAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PasswordChangedEvent(Id, TenantId, PhoneNumber.Value));
    }

    /// <summary>
    /// Business Logic: Đăng nhập thành công
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserLoggedInEvent(Id, TenantId, PhoneNumber.Value, Role));
    }

    /// <summary>
    /// Business Logic: Tạo refresh token mới
    /// </summary>
    public RefreshToken GenerateRefreshToken(string token, int expiryDays = 7)
    {
        var refreshToken = new RefreshToken(
            TenantId,
            Id,
            token,
            DateTime.UtcNow.AddDays(expiryDays));

        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    /// <summary>
    /// Business Logic: Thu hồi tất cả refresh token
    /// </summary>
    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive()))
        {
            token.Revoke();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Suspend account
    /// </summary>
    public void Suspend()
    {
        if (Status == UserStatus.Inactive)
            throw new InvalidOperationException("Cannot suspend inactive user");

        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
        RevokeAllRefreshTokens();

        AddDomainEvent(new UserStatusChangedEvent(Id, TenantId, UserStatus.Suspended));
    }

    /// <summary>
    /// Business Logic: Activate account
    /// </summary>
    public void Activate()
    {
        if (Status == UserStatus.PendingActivation && string.IsNullOrEmpty(PasswordHash))
            throw new InvalidOperationException("Cannot activate user without password");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserStatusChangedEvent(Id, TenantId, UserStatus.Active));
    }

    /// <summary>
    /// Business Logic: Cập nhật thông tin profile
    /// </summary>
    public void UpdateProfile(string? fullName = null, string? email = null)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
            FullName = ValidateFullName(fullName);

        if (email != null)
            Email = email;

        UpdatedAt = DateTime.UtcNow;
    }

    private static string ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        if (fullName.Length < 2 || fullName.Length > 255)
            throw new ArgumentException("Full name must be between 2 and 255 characters", nameof(fullName));

        return fullName.Trim();
    }
}
