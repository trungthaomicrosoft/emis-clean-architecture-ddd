using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a participant is removed from a conversation
/// </summary>
public class ParticipantRemovedEvent : DomainEvent
{
    public Guid ConversationId { get; }
    public Guid UserId { get; }

    public ParticipantRemovedEvent(Guid conversationId, Guid userId)
    {
        ConversationId = conversationId;
        UserId = userId;
    }
}
