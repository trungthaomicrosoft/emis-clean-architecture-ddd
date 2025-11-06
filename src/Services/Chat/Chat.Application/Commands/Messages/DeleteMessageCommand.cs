using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to delete a message (soft delete)
/// </summary>
public class DeleteMessageCommand : IRequest<ApiResponse<bool>>
{
    public Guid MessageId { get; set; }
    public Guid DeleterUserId { get; set; }
}
