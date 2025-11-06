namespace Chat.Application.DTOs;

/// <summary>
/// DTO for Message
/// </summary>
public class MessageDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<AttachmentDto> Attachments { get; set; } = new();
    public ReplyToMessageDto? ReplyTo { get; set; }
    public List<MentionDto> Mentions { get; set; } = new();
    public List<ReactionDto> Reactions { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsPinned { get; set; }
    public Guid? PinnedBy { get; set; }
    public DateTime? PinnedAt { get; set; }
    public List<ReadReceiptDto> ReadReceipts { get; set; } = new();
}

/// <summary>
/// DTO for Attachment
/// </summary>
public class AttachmentDto
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// DTO for Reply to message
/// </summary>
public class ReplyToMessageDto
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Mention
/// </summary>
public class MentionDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int Length { get; set; }
}

/// <summary>
/// DTO for Reaction
/// </summary>
public class ReactionDto
{
    public string EmojiCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime ReactedAt { get; set; }
}

/// <summary>
/// DTO for Read receipt
/// </summary>
public class ReadReceiptDto
{
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; }
}
