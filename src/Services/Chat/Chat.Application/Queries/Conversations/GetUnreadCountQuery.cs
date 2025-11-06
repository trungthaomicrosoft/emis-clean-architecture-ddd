using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Query to get total unread conversations count for a user
/// </summary>
public class GetUnreadCountQuery : IRequest<ApiResponse<int>>
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
}
