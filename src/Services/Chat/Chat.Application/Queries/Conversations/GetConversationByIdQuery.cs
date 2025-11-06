using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Query to get a specific conversation by ID
/// </summary>
public class GetConversationByIdQuery : IRequest<ApiResponse<ConversationDetailDto>>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; } // For authorization check
}
