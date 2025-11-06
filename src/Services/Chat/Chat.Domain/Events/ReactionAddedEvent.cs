using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a reaction is added to a message
/// </summary>
public class ReactionAddedEvent : DomainEvent
{
    public Guid MessageId { get; }
    public Guid ConversationId { get; }
    public string EmojiCode { get; }
    public Guid UserId { get; }
    public string UserName { get; }

    public ReactionAddedEvent(
        Guid messageId,
        Guid conversationId,
        string emojiCode,
        Guid userId,
        string userName)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        EmojiCode = emojiCode;
        UserId = userId;
        UserName = userName;
    }
}
