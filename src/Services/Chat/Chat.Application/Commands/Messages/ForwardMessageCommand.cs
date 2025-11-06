using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to forward a message to another conversation
/// </summary>
public class ForwardMessageCommand : IRequest<ApiResponse<MessageDto>>
{
    public Guid MessageId { get; set; } // Message to forward
    public Guid ToConversationId { get; set; } // Destination conversation
    public Guid ForwardedBy { get; set; } // User forwarding the message
    public Guid TenantId { get; set; }
}
