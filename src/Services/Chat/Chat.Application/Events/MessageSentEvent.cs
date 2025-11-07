using EMIS.EventBus;

namespace Chat.Application.Events;

/// <summary>
/// Event published when a text message is sent
/// Will be processed asynchronously by background worker
/// </summary>
public class MessageSentEvent : IntegrationEvent
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    
    // Reply info
    public Guid? ReplyToMessageId { get; set; }
    public string? ReplyToContent { get; set; }
    public string? ReplyToSenderName { get; set; }
    
    // Mentions
    public List<MentionData> Mentions { get; set; } = new();
    
    // Recipients for real-time broadcast
    public List<Guid> RecipientUserIds { get; set; } = new();

    public MessageSentEvent() { }

    public MessageSentEvent(
        Guid messageId,
        Guid conversationId,
        Guid tenantId,
        Guid senderId,
        string senderName,
        string content,
        DateTime sentAt,
        List<Guid> recipientUserIds)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        TenantId = tenantId;
        SenderId = senderId;
        SenderName = senderName;
        Content = content;
        SentAt = sentAt;
        RecipientUserIds = recipientUserIds;
    }
}

/// <summary>
/// Mention data for serialization
/// </summary>
public class MentionData
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int Length { get; set; }

    public MentionData() { }

    public MentionData(Guid userId, string userName, int startIndex, int length)
    {
        UserId = userId;
        UserName = userName;
        StartIndex = startIndex;
        Length = length;
    }
}
