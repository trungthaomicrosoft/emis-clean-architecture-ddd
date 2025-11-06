using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Entities;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using Chat.Domain.ValueObjects;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for forwarding messages
/// Creates a copy of the message in the destination conversation
/// </summary>
public class ForwardMessageCommandHandler 
    : IRequestHandler<ForwardMessageCommand, ApiResponse<MessageDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMapper _mapper;

    public ForwardMessageCommandHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<MessageDto>> Handle(
        ForwardMessageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get the original message
            var originalMessage = await _messageRepository.GetByIdAsync(
                request.MessageId,
                cancellationToken);

            if (originalMessage == null)
                return ApiResponse<MessageDto>.ErrorResult("Message not found", 404);

            // 2. Check if message is deleted
            if (originalMessage.IsDeleted)
                return ApiResponse<MessageDto>.ErrorResult(
                    "Cannot forward a deleted message", 
                    400);

            // 3. Validate destination conversation
            var toConversation = await _conversationRepository.GetByIdAsync(
                request.ToConversationId,
                cancellationToken);

            if (toConversation == null)
                return ApiResponse<MessageDto>.ErrorResult("Destination conversation not found", 404);

            // 4. Check if user can send to destination
            if (!toConversation.CanSendMessage(request.ForwardedBy))
                return ApiResponse<MessageDto>.ErrorResult(
                    "You do not have permission to send messages in the destination conversation", 
                    403);

            // 5. Check if user is participant in source conversation (can see the message)
            var sourceConversation = await _conversationRepository.GetByIdAsync(
                originalMessage.ConversationId,
                cancellationToken);

            if (sourceConversation == null || !sourceConversation.IsParticipant(request.ForwardedBy))
                return ApiResponse<MessageDto>.ErrorResult(
                    "You do not have access to the original message", 
                    403);

            // 6. Get forwarding user info (TODO: should come from UserService)
            // For now, we'll use a placeholder name
            var forwarderName = "User"; // This should be fetched from UserService

            // 7. Create forwarded message based on original type
            Message forwardedMessage;
            
            if (originalMessage.Type == MessageType.Text)
            {
                forwardedMessage = Message.CreateText(
                    request.ToConversationId,
                    request.ForwardedBy,
                    forwarderName,
                    originalMessage.Content);
            }
            else if (originalMessage.Attachments.Any())
            {
                // Forward with attachments
                forwardedMessage = Message.CreateWithAttachment(
                    request.ToConversationId,
                    request.ForwardedBy,
                    forwarderName,
                    originalMessage.Type,
                    originalMessage.Attachments.ToList(),
                    originalMessage.Content); // Caption
            }
            else
            {
                return ApiResponse<MessageDto>.ErrorResult(
                    "Cannot forward this message type", 
                    400);
            }

            // Note: We could add a "ForwardedFrom" property to track original message
            // but keeping it simple for now

            // 8. Save forwarded message
            await _messageRepository.AddAsync(forwardedMessage, cancellationToken);

            // 9. Update destination conversation last message
            toConversation.UpdateLastMessage(
                forwardedMessage.MessageId,
                forwardedMessage.Content,
                forwardedMessage.SenderId,
                forwardedMessage.SenderName,
                forwardedMessage.SentAt);

            await _conversationRepository.UpdateAsync(toConversation, cancellationToken);

            // 9. Map to DTO and return
            var messageDto = _mapper.Map<MessageDto>(forwardedMessage);
            return ApiResponse<MessageDto>.SuccessResult(messageDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageDto>.ErrorResult(
                $"Failed to forward message: {ex.Message}", 
                500);
        }
    }
}
