using Chat.Domain.Events;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for marking messages as read
/// </summary>
public class MarkMessagesAsReadCommandHandler 
    : IRequestHandler<MarkMessagesAsReadCommand, ApiResponse<bool>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public MarkMessagesAsReadCommandHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<ApiResponse<bool>> Handle(
        MarkMessagesAsReadCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<bool>.ErrorResult("Conversation not found", 404);

            if (!conversation.IsParticipant(request.UserId))
                return ApiResponse<bool>.ErrorResult("User is not a participant", 403);

            var readAt = DateTime.UtcNow;

            // Mark each message as read
            foreach (var messageId in request.MessageIds)
            {
                var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
                if (message != null && message.ConversationId == request.ConversationId)
                {
                    message.MarkAsRead(request.UserId);
                    await _messageRepository.UpdateAsync(message, cancellationToken);
                }
            }

            // Update conversation participant's lastReadAt and reset unread count
            conversation.MarkMessagesAsRead(request.UserId, readAt);
            await _conversationRepository.UpdateAsync(conversation, cancellationToken);

            // Publish domain event for read receipts
            conversation.AddDomainEvent(new MessagesReadEvent(
                request.ConversationId,
                request.UserId,
                request.MessageIds,
                readAt));

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult($"Failed to mark messages as read: {ex.Message}", 500);
        }
    }
}
