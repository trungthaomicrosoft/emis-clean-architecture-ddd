using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a message is deleted (soft delete)
/// </summary>
public class MessageDeletedEvent : DomainEvent
{
    public Guid MessageId { get; }
    public Guid ConversationId { get; }
    public Guid DeletedBy { get; }
    public DateTime DeletedAt { get; }

    public MessageDeletedEvent(
        Guid messageId,
        Guid conversationId,
        Guid deletedBy,
        DateTime deletedAt)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        DeletedBy = deletedBy;
        DeletedAt = deletedAt;
    }
}
