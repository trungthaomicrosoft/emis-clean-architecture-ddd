using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Entities;
using Chat.Domain.Events;
using Chat.Domain.Repositories;
using Chat.Domain.ValueObjects;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for sending text message
/// </summary>
public class SendTextMessageCommandHandler 
    : IRequestHandler<SendTextMessageCommand, ApiResponse<MessageDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public SendTextMessageCommandHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<MessageDto>> Handle(
        SendTextMessageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get conversation and validate access
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<MessageDto>.ErrorResult("Conversation not found", 404);

            if (!conversation.IsParticipant(request.SenderId))
                return ApiResponse<MessageDto>.ErrorResult("User is not a participant in this conversation", 403);

            if (!conversation.CanSendMessage(request.SenderId))
                return ApiResponse<MessageDto>.ErrorResult("User does not have permission to send messages", 403);

            // 2. Handle reply (if applicable)
            ReplyToMessage? replyTo = null;
            if (request.ReplyToMessageId.HasValue)
            {
                var replyToMessage = await _messageRepository.GetByIdAsync(
                    request.ReplyToMessageId.Value,
                    cancellationToken);

                if (replyToMessage != null && !replyToMessage.IsDeleted)
                {
                    replyTo = ReplyToMessage.Create(
                        replyToMessage.MessageId,
                        replyToMessage.Content,
                        replyToMessage.SenderName);
                }
            }

            // 3. Handle mentions
            var mentions = request.Mentions
                .Select(m => Mention.Create(m.UserId, m.UserName, m.StartIndex, m.Length))
                .ToList();

            // 4. Create message entity
            var message = Message.CreateText(
                request.ConversationId,
                request.SenderId,
                request.SenderName,
                request.Content,
                replyTo,
                mentions);

            // 5. Save message
            await _messageRepository.AddAsync(message, cancellationToken);

            // 6. Update conversation last message
            conversation.UpdateLastMessage(
                message.MessageId,
                message.Content,
                message.SenderId,
                message.SenderName,
                message.SentAt);

            await _conversationRepository.UpdateAsync(conversation, cancellationToken);

            // 7. Publish domain event (for SignalR broadcasting and notifications)
            var mentionedUserIds = mentions.Select(m => m.UserId).ToList();
            var recipientUserIds = conversation.Participants
                .Where(p => p.UserId != request.SenderId)
                .Select(p => p.UserId)
                .ToList();

            // Domain events will be dispatched by infrastructure layer

            // 8. Return DTO
            var messageDto = _mapper.Map<MessageDto>(message);
            return ApiResponse<MessageDto>.SuccessResult(messageDto);
        }
        catch (BusinessRuleValidationException ex)
        {
            return ApiResponse<MessageDto>.ErrorResult(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageDto>.ErrorResult($"Failed to send message: {ex.Message}", 500);
        }
    }
}
