using EMIS.SharedKernel;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Aggregates;

/// <summary>
/// Aggregate Root: Tenant (Trường học)
/// Business Rules:
/// - Subdomain phải unique trong toàn hệ thống
/// - Trial plan tự động hết hạn sau 30 ngày
/// - Suspended tenant không thể login
/// - MaxUsers dựa vào subscription plan
/// </summary>
public class Tenant : Entity, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public Subdomain Subdomain { get; private set; } = null!;
    public TenantStatus Status { get; private set; }
    public SubscriptionPlan SubscriptionPlan { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }
    public int MaxUsers { get; private set; }
    public string? ConnectionString { get; private set; }
    
    // Metadata
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Address { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // For EF Core
    private Tenant() { }

    /// <summary>
    /// Constructor: Tạo tenant mới với Trial plan (30 ngày)
    /// </summary>
    public Tenant(
        string name,
        Subdomain subdomain,
        string contactEmail,
        string contactPhone)
    {
        Name = ValidateName(name);
        Subdomain = subdomain ?? throw new ArgumentNullException(nameof(subdomain));
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        
        // Default to Trial plan for new registrations
        Status = TenantStatus.Trial;
        SubscriptionPlan = SubscriptionPlan.Trial;
        SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days trial
        MaxUsers = GetMaxUsersByPlan(SubscriptionPlan.Trial);
        
        CreatedAt = DateTime.UtcNow;

        // Domain event will be added after admin is created
    }

    /// <summary>
    /// Business Logic: Nâng cấp subscription plan
    /// </summary>
    public void UpgradePlan(SubscriptionPlan newPlan, int durationMonths = 12)
    {
        if (newPlan <= SubscriptionPlan)
            throw new InvalidOperationException($"Cannot downgrade from {SubscriptionPlan} to {newPlan}");

        SubscriptionPlan = newPlan;
        MaxUsers = GetMaxUsersByPlan(newPlan);
        SubscriptionExpiresAt = DateTime.UtcNow.AddMonths(durationMonths);
        
        // Activate if was trial
        if (Status == TenantStatus.Trial)
            Status = TenantStatus.Active;
        
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Gia hạn subscription
    /// </summary>
    public void RenewSubscription(int durationMonths = 12)
    {
        if (Status == TenantStatus.Inactive)
            throw new InvalidOperationException("Cannot renew inactive tenant");

        var currentExpiry = SubscriptionExpiresAt ?? DateTime.UtcNow;
        var newExpiry = currentExpiry > DateTime.UtcNow 
            ? currentExpiry.AddMonths(durationMonths)
            : DateTime.UtcNow.AddMonths(durationMonths);

        SubscriptionExpiresAt = newExpiry;
        
        if (Status == TenantStatus.Suspended)
            Status = TenantStatus.Active;
        
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Tạm ngưng tenant (hết hạn hoặc vi phạm)
    /// </summary>
    public void Suspend(string reason)
    {
        if (Status == TenantStatus.Inactive)
            throw new InvalidOperationException("Cannot suspend inactive tenant");

        Status = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
        
        // TODO: Log suspension reason
    }

    /// <summary>
    /// Business Logic: Kích hoạt lại tenant
    /// </summary>
    public void Activate()
    {
        if (Status == TenantStatus.Inactive)
            throw new InvalidOperationException("Cannot activate inactive tenant. Create new tenant instead.");

        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Vô hiệu hóa tenant vĩnh viễn
    /// </summary>
    public void Deactivate()
    {
        Status = TenantStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Cập nhật thông tin liên hệ
    /// </summary>
    public void UpdateContactInfo(
        string? name = null,
        string? contactEmail = null,
        string? contactPhone = null,
        string? address = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = ValidateName(name);

        if (contactEmail != null)
            ContactEmail = contactEmail;

        if (contactPhone != null)
            ContactPhone = contactPhone;

        if (address != null)
            Address = address;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Set connection string (cho database per tenant)
    /// </summary>
    public void SetConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

        ConnectionString = connectionString;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Kiểm tra tenant có hết hạn không
    /// </summary>
    public bool IsExpired()
    {
        return SubscriptionExpiresAt.HasValue && SubscriptionExpiresAt.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Business Logic: Publish domain event khi tenant được tạo thành công (có admin)
    /// </summary>
    public void PublishTenantCreatedEvent(Guid schoolAdminId)
    {
        AddDomainEvent(new TenantCreatedEvent(
            Id,
            Name,
            Subdomain.Value,
            schoolAdminId,
            SubscriptionPlan.ToString(),
            SubscriptionExpiresAt));
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty", nameof(name));

        if (name.Length < 3 || name.Length > 255)
            throw new ArgumentException("Tenant name must be between 3 and 255 characters", nameof(name));

        return name.Trim();
    }

    private static int GetMaxUsersByPlan(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Trial => 50,
            SubscriptionPlan.Basic => 100,
            SubscriptionPlan.Standard => 500,
            SubscriptionPlan.Professional => 2000,
            SubscriptionPlan.Enterprise => int.MaxValue,
            _ => 50
        };
    }
}
