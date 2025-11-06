using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a new message is sent
/// </summary>
public class MessageSentEvent : DomainEvent
{
    public Guid MessageId { get; }
    public Guid ConversationId { get; }
    public Guid SenderId { get; }
    public string SenderName { get; }
    public string Content { get; }
    public string MessageType { get; }
    public List<Guid> MentionedUserIds { get; }
    public List<Guid> RecipientUserIds { get; }

    public MessageSentEvent(
        Guid messageId,
        Guid conversationId,
        Guid senderId,
        string senderName,
        string content,
        string messageType,
        List<Guid> mentionedUserIds,
        List<Guid> recipientUserIds)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        SenderId = senderId;
        SenderName = senderName;
        Content = content;
        MessageType = messageType;
        MentionedUserIds = mentionedUserIds;
        RecipientUserIds = recipientUserIds;
    }
}
