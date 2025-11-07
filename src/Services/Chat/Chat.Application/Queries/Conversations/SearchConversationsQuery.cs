using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Query to search conversations by name
/// </summary>
public class SearchConversationsQuery : IRequest<ApiResponse<List<ConversationDto>>>
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
