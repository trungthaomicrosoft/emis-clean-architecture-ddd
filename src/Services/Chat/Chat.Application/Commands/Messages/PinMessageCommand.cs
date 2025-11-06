using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to pin a message
/// </summary>
public class PinMessageCommand : IRequest<ApiResponse<bool>>
{
    public Guid MessageId { get; set; }
    public Guid PinnedByUserId { get; set; }
}
