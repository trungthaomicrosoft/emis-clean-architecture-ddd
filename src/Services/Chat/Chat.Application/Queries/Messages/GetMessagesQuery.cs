using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Query to get messages in a conversation with cursor-based pagination
/// More efficient than skip/take for large datasets
/// </summary>
public class GetMessagesQuery : IRequest<ApiResponse<MessagesResultDto>>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; } // For authorization
    public DateTime? BeforeTimestamp { get; set; } // Cursor for pagination
    public int PageSize { get; set; } = 50; // Default 50 messages per page
}

/// <summary>
/// DTO for messages result with pagination info
/// </summary>
public class MessagesResultDto
{
    public List<MessageDto> Messages { get; set; } = new();
    public bool HasMore { get; set; }
    public DateTime? OldestMessageTimestamp { get; set; } // For next page cursor
}
