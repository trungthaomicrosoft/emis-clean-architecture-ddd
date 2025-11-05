using EMIS.SharedKernel;
using Identity.Domain.Enums;

namespace Identity.Domain.Events;

public class UserLoggedInEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public string PhoneNumber { get; }
    public UserRole Role { get; }

    public UserLoggedInEvent(Guid userId, Guid tenantId, string phoneNumber, UserRole role)
    {
        UserId = userId;
        TenantId = tenantId;
        PhoneNumber = phoneNumber;
        Role = role;
    }
}
