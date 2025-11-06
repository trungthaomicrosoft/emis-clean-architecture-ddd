using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to edit a message
/// Business Rule: Can only edit within 15 minutes
/// </summary>
public class EditMessageCommand : IRequest<ApiResponse<bool>>
{
    public Guid MessageId { get; set; }
    public Guid EditorUserId { get; set; }
    public string NewContent { get; set; } = string.Empty;
}
