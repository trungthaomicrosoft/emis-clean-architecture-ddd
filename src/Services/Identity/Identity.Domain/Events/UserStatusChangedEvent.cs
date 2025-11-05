using EMIS.SharedKernel;
using Identity.Domain.Enums;

namespace Identity.Domain.Events;

public class UserStatusChangedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public UserStatus NewStatus { get; }

    public UserStatusChangedEvent(Guid userId, Guid tenantId, UserStatus newStatus)
    {
        UserId = userId;
        TenantId = tenantId;
        NewStatus = newStatus;
    }
}
