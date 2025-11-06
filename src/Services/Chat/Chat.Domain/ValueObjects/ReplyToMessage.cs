using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing a reply/quote to another message
/// </summary>
public class ReplyToMessage : ValueObject
{
    public Guid MessageId { get; private set; }
    public string Content { get; private set; }
    public string SenderName { get; private set; }

    private ReplyToMessage() 
    {
        Content = string.Empty;
        SenderName = string.Empty;
    }

    private ReplyToMessage(Guid messageId, string content, string senderName)
    {
        MessageId = messageId;
        Content = content;
        SenderName = senderName;
    }

    public static ReplyToMessage Create(Guid messageId, string content, string senderName)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("MessageId cannot be empty", nameof(messageId));
        if (string.IsNullOrWhiteSpace(senderName))
            throw new ArgumentException("SenderName cannot be empty", nameof(senderName));

        // Truncate content for display (max 100 chars)
        var truncatedContent = content?.Length > 100 
            ? content.Substring(0, 100) + "..." 
            : content ?? string.Empty;

        return new ReplyToMessage(messageId, truncatedContent, senderName);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MessageId;
        yield return Content;
        yield return SenderName;
    }
}
