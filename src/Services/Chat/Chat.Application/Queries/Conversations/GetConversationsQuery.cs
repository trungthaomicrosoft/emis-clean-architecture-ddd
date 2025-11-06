using Chat.Application.DTOs;
using Chat.Domain.Enums;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Query to get all conversations for a user with pagination and filters
/// </summary>
public class GetConversationsQuery : IRequest<ApiResponse<PagedResult<ConversationDto>>>
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public ConversationType? FilterByType { get; set; }
    public bool IncludeArchived { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
