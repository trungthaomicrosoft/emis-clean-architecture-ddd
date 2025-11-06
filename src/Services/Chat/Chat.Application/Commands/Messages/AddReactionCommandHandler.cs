using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for adding reaction to message
/// </summary>
public class AddReactionCommandHandler 
    : IRequestHandler<AddReactionCommand, ApiResponse<bool>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public AddReactionCommandHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<ApiResponse<bool>> Handle(
        AddReactionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken);

            if (message == null)
                return ApiResponse<bool>.ErrorResult("Message not found", 404);

            // Validate user is participant
            var conversation = await _conversationRepository.GetByIdAsync(
                message.ConversationId,
                cancellationToken);

            if (conversation == null || !conversation.IsParticipant(request.UserId))
                return ApiResponse<bool>.ErrorResult("User is not a participant in this conversation", 403);

            // Add reaction (business rules validated in domain method)
            message.AddReaction(request.EmojiCode, request.UserId, request.UserName);

            await _messageRepository.UpdateAsync(message, cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (BusinessRuleValidationException ex)
        {
            return ApiResponse<bool>.ErrorResult(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult($"Failed to add reaction: {ex.Message}", 500);
        }
    }
}
