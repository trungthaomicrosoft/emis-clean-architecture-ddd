namespace EMIS.EventBus.IntegrationEvents;

/// <summary>
/// Integration Event: Published khi tenant mới được tạo
/// Các services khác (Student, Teacher, Attendance...) sẽ lắng nghe event này
/// để tạo database/schema riêng cho tenant mới
/// </summary>
public class TenantCreatedIntegrationEvent : IntegrationEvent
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public Guid SchoolAdminId { get; set; }
    public string SubscriptionPlan { get; set; } = string.Empty;
    public DateTime? SubscriptionExpiresAt { get; set; }
    public int MaxUsers { get; set; }
    
    // Optional connection string for services that need separate DB per tenant
    public string? ConnectionString { get; set; }

    public TenantCreatedIntegrationEvent(
        Guid tenantId,
        string tenantName,
        string subdomain,
        Guid schoolAdminId,
        string subscriptionPlan,
        DateTime? subscriptionExpiresAt,
        int maxUsers,
        string? connectionString = null)
    {
        TenantId = tenantId;
        TenantName = tenantName;
        Subdomain = subdomain;
        SchoolAdminId = schoolAdminId;
        SubscriptionPlan = subscriptionPlan;
        SubscriptionExpiresAt = subscriptionExpiresAt;
        MaxUsers = maxUsers;
        ConnectionString = connectionString;
    }

    // For deserialization
    public TenantCreatedIntegrationEvent() { }
}
