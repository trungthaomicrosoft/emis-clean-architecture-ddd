using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a message is unpinned
/// </summary>
public class MessageUnpinnedEvent : DomainEvent
{
    public Guid MessageId { get; }
    public Guid ConversationId { get; }
    public Guid UnpinnedBy { get; }

    public MessageUnpinnedEvent(Guid messageId, Guid conversationId, Guid unpinnedBy)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        UnpinnedBy = unpinnedBy;
    }
}
