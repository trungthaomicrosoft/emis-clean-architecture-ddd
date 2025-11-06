using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a message is edited
/// </summary>
public class MessageEditedEvent : DomainEvent
{
    public Guid MessageId { get; }
    public Guid ConversationId { get; }
    public string OldContent { get; }
    public string NewContent { get; }
    public DateTime EditedAt { get; }

    public MessageEditedEvent(
        Guid messageId,
        Guid conversationId,
        string oldContent,
        string newContent,
        DateTime editedAt)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        OldContent = oldContent;
        NewContent = newContent;
        EditedAt = editedAt;
    }
}
