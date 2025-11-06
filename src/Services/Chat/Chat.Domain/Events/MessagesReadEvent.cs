using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when messages are marked as read by a user
/// </summary>
public class MessagesReadEvent : DomainEvent
{
    public Guid ConversationId { get; }
    public Guid UserId { get; }
    public List<Guid> MessageIds { get; }
    public DateTime ReadAt { get; }

    public MessagesReadEvent(
        Guid conversationId,
        Guid userId,
        List<Guid> messageIds,
        DateTime readAt)
    {
        ConversationId = conversationId;
        UserId = userId;
        MessageIds = messageIds;
        ReadAt = readAt;
    }
}
