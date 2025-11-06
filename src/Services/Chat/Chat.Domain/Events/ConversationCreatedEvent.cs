using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a new conversation is created
/// </summary>
public class ConversationCreatedEvent : DomainEvent
{
    public Guid ConversationId { get; }
    public Guid TenantId { get; }
    public string ConversationType { get; }
    public List<Guid> ParticipantUserIds { get; }

    public ConversationCreatedEvent(
        Guid conversationId,
        Guid tenantId,
        string conversationType,
        List<Guid> participantUserIds)
    {
        ConversationId = conversationId;
        TenantId = tenantId;
        ConversationType = conversationType;
        ParticipantUserIds = participantUserIds;
    }
}
