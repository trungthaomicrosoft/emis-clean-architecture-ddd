using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Query to get pinned messages in a conversation
/// </summary>
public class GetPinnedMessagesQuery : IRequest<ApiResponse<List<MessageDto>>>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; } // For authorization
}
