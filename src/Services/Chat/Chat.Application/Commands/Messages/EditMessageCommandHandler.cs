using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for editing message
/// </summary>
public class EditMessageCommandHandler 
    : IRequestHandler<EditMessageCommand, ApiResponse<bool>>
{
    private readonly IMessageRepository _messageRepository;

    public EditMessageCommandHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<ApiResponse<bool>> Handle(
        EditMessageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken);

            if (message == null)
                return ApiResponse<bool>.ErrorResult("Message not found", 404);

            // Edit method contains all business rule validations
            message.Edit(request.NewContent, request.EditorUserId);

            await _messageRepository.UpdateAsync(message, cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (BusinessRuleValidationException ex)
        {
            return ApiResponse<bool>.ErrorResult(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult($"Failed to edit message: {ex.Message}", 500);
        }
    }
}
