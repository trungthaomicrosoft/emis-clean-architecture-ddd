using Chat.Application.DTOs;
using Chat.Domain.Enums;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Query to search messages in a conversation
/// Phase 1: MongoDB text search
/// Phase 2: Can be migrated to Elasticsearch for advanced features
/// </summary>
public class SearchMessagesQuery : IRequest<ApiResponse<PagedResult<MessageDto>>>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; } // For authorization
    public string SearchTerm { get; set; } = string.Empty;
    
    // Filters
    public MessageType? FilterByType { get; set; } // Filter by image, video, file, etc.
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
