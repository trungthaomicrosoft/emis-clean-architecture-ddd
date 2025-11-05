using EMIS.SharedKernel;

namespace Identity.Domain.Events;

public class PasswordChangedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public string PhoneNumber { get; }

    public PasswordChangedEvent(Guid userId, Guid tenantId, string phoneNumber)
    {
        UserId = userId;
        TenantId = tenantId;
        PhoneNumber = phoneNumber;
    }
}
