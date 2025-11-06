using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a message is pinned
/// </summary>
public class MessagePinnedEvent : DomainEvent
{
    public Guid MessageId { get; }
    public Guid ConversationId { get; }
    public string Content { get; }
    public Guid PinnedBy { get; }
    public List<Guid> ParticipantUserIds { get; }

    public MessagePinnedEvent(
        Guid messageId,
        Guid conversationId,
        string content,
        Guid pinnedBy,
        List<Guid> participantUserIds)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        Content = content;
        PinnedBy = pinnedBy;
        ParticipantUserIds = participantUserIds;
    }
}
