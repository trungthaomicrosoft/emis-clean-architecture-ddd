using EMIS.SharedKernel;

namespace Identity.Domain.Events;

/// <summary>
/// Domain Event: Tenant mới được tạo
/// </summary>
public class TenantCreatedEvent : DomainEvent
{
    public Guid TenantId { get; }
    public string TenantName { get; }
    public string Subdomain { get; }
    public Guid SchoolAdminId { get; }
    public string SubscriptionPlan { get; }
    public DateTime? SubscriptionExpiresAt { get; }

    public TenantCreatedEvent(
        Guid tenantId,
        string tenantName,
        string subdomain,
        Guid schoolAdminId,
        string subscriptionPlan,
        DateTime? subscriptionExpiresAt)
    {
        TenantId = tenantId;
        TenantName = tenantName;
        Subdomain = subdomain;
        SchoolAdminId = schoolAdminId;
        SubscriptionPlan = subscriptionPlan;
        SubscriptionExpiresAt = subscriptionExpiresAt;
    }
}
