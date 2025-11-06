using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing a summary of the last message in a conversation
/// Used for display in conversation lists
/// </summary>
public class MessageSummary : ValueObject
{
    public Guid MessageId { get; private set; }
    public string Content { get; private set; }
    public Guid SenderId { get; private set; }
    public string SenderName { get; private set; }
    public DateTime SentAt { get; private set; }

    private MessageSummary() 
    {
        Content = string.Empty;
        SenderName = string.Empty;
    }

    private MessageSummary(
        Guid messageId,
        string content,
        Guid senderId,
        string senderName,
        DateTime sentAt)
    {
        MessageId = messageId;
        Content = content;
        SenderId = senderId;
        SenderName = senderName;
        SentAt = sentAt;
    }

    public static MessageSummary Create(
        Guid messageId,
        string content,
        Guid senderId,
        string senderName,
        DateTime sentAt)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("MessageId cannot be empty", nameof(messageId));
        if (senderId == Guid.Empty)
            throw new ArgumentException("SenderId cannot be empty", nameof(senderId));
        if (string.IsNullOrWhiteSpace(senderName))
            throw new ArgumentException("SenderName cannot be empty", nameof(senderName));

        // Truncate content for summary (max 100 chars)
        var truncatedContent = content?.Length > 100 
            ? content.Substring(0, 100) + "..." 
            : content ?? string.Empty;

        return new MessageSummary(messageId, truncatedContent, senderId, senderName, sentAt);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MessageId;
        yield return Content;
        yield return SenderId;
        yield return SenderName;
        yield return SentAt;
    }
}
