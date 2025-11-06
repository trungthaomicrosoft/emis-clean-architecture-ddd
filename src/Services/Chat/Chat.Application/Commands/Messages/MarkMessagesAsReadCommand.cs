using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to mark messages as read in a conversation
/// </summary>
public class MarkMessagesAsReadCommand : IRequest<ApiResponse<bool>>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public List<Guid> MessageIds { get; set; } = new();
}
