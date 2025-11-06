using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to add a participant to a conversation
/// Only admins can add participants
/// </summary>
public class AddParticipantCommand : IRequest<ApiResponse<bool>>
{
    public Guid ConversationId { get; set; }
    public Guid UserIdToAdd { get; set; } // User to add
    public string UserName { get; set; } = string.Empty; // Name of user to add
    public Guid RequestedBy { get; set; } // User requesting the add (must be admin)
}
