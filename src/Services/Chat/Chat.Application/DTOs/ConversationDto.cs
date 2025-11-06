namespace Chat.Application.DTOs;

/// <summary>
/// DTO for Conversation list view
/// </summary>
public class ConversationDto
{
    public Guid ConversationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ConversationMetadataDto? Metadata { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
    public MessageSummaryDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for Conversation detail view
/// </summary>
public class ConversationDetailDto : ConversationDto
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// DTO for Conversation metadata
/// </summary>
public class ConversationMetadataDto
{
    public Guid? StudentId { get; set; }
    public string? StudentName { get; set; }
    public Guid? ClassId { get; set; }
    public string? ClassName { get; set; }
}

/// <summary>
/// DTO for Participant
/// </summary>
public class ParticipantDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
    public int UnreadCount { get; set; }
}

/// <summary>
/// DTO for Message summary (in conversation list)
/// </summary>
public class MessageSummaryDto
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
