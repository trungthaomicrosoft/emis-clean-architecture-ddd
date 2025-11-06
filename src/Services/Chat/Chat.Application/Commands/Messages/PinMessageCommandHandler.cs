using Chat.Domain.Events;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for pinning message
/// </summary>
public class PinMessageCommandHandler 
    : IRequestHandler<PinMessageCommand, ApiResponse<bool>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public PinMessageCommandHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<ApiResponse<bool>> Handle(
        PinMessageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken);

            if (message == null)
                return ApiResponse<bool>.ErrorResult("Message not found", 404);

            // Validate user is admin
            var conversation = await _conversationRepository.GetByIdAsync(
                message.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<bool>.ErrorResult("Conversation not found", 404);

            if (!conversation.IsAdmin(request.PinnedByUserId))
                return ApiResponse<bool>.ErrorResult("Only admins can pin messages", 403);

            // Pin message (business rules validated in domain method)
            message.Pin(request.PinnedByUserId);

            await _messageRepository.UpdateAsync(message, cancellationToken);

            // Raise domain event for notification
            var participantUserIds = conversation.Participants.Select(p => p.UserId).ToList();
            message.AddDomainEvent(new MessagePinnedEvent(
                message.MessageId,
                message.ConversationId,
                message.Content,
                request.PinnedByUserId,
                participantUserIds));

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (BusinessRuleValidationException ex)
        {
            return ApiResponse<bool>.ErrorResult(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult($"Failed to pin message: {ex.Message}", 500);
        }
    }
}
