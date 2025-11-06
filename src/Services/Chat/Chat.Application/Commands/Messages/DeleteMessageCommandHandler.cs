using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for deleting message
/// </summary>
public class DeleteMessageCommandHandler 
    : IRequestHandler<DeleteMessageCommand, ApiResponse<bool>>
{
    private readonly IMessageRepository _messageRepository;

    public DeleteMessageCommandHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteMessageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken);

            if (message == null)
                return ApiResponse<bool>.ErrorResult("Message not found", 404);

            // Delete method contains all business rule validations
            message.Delete(request.DeleterUserId);

            await _messageRepository.UpdateAsync(message, cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (BusinessRuleValidationException ex)
        {
            return ApiResponse<bool>.ErrorResult(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult($"Failed to delete message: {ex.Message}", 500);
        }
    }
}
