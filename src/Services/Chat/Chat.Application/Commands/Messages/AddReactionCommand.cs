using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to add emoji reaction to a message
/// </summary>
public class AddReactionCommand : IRequest<ApiResponse<bool>>
{
    public Guid MessageId { get; set; }
    public string EmojiCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}
