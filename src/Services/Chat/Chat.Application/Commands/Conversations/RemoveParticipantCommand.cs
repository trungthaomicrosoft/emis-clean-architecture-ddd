using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to remove a participant from a conversation
/// Only admins can remove participants
/// </summary>
public class RemoveParticipantCommand : IRequest<ApiResponse<bool>>
{
    public Guid ConversationId { get; set; }
    public Guid UserIdToRemove { get; set; } // User to remove
    public Guid RequestedBy { get; set; } // User requesting the removal (must be admin)
}
