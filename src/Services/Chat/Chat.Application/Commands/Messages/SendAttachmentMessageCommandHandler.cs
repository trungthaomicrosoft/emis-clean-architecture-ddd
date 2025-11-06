using AutoMapper;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Domain.Aggregates;
using Chat.Domain.Entities;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using Chat.Domain.ValueObjects;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for sending attachment messages
/// Validates file size, uploads to storage, creates message with attachment
/// </summary>
public class SendAttachmentMessageCommandHandler 
    : IRequestHandler<SendAttachmentMessageCommand, ApiResponse<MessageDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;

    // File size limits (from CHAT_SERVICE_DESIGN.md)
    private const long MaxImageSize = 10 * 1024 * 1024; // 10MB
    private const long MaxAudioSize = 10 * 1024 * 1024; // 10MB
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const long MaxVideoSize = 25 * 1024 * 1024; // 25MB

    public SendAttachmentMessageCommandHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IFileStorageService fileStorageService,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<MessageDto>> Handle(
        SendAttachmentMessageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate conversation exists
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<MessageDto>.ErrorResult("Conversation not found", 404);

            // 2. Check if user can send messages
            if (!conversation.CanSendMessage(request.SenderId))
                return ApiResponse<MessageDto>.ErrorResult(
                    "You do not have permission to send messages in this conversation", 
                    403);

            // 3. Extract file properties
            // Note: File is object to avoid ASP.NET Core dependency in Application layer
            // Cast to dynamic to access properties (IFormFile has OpenReadStream, FileName, ContentType)
            dynamic file = request.File;
            string fileName = file.FileName;
            string contentType = file.ContentType;
            
            // 4. Upload file to storage (MinIO)
            // File validation happens in FileStorageService
            FileUploadResult uploadResult;
            using (var fileStream = file.OpenReadStream())
            {
                uploadResult = await _fileStorageService.UploadFileAsync(
                    fileStream,
                    fileName,
                    contentType,
                    request.TenantId,
                    request.ConversationId,
                    cancellationToken);
            }

            if (!uploadResult.Success)
                return ApiResponse<MessageDto>.ErrorResult(
                    $"File upload failed: {uploadResult.ErrorMessage}", 
                    400);

            // 5. Create Attachment value object
            var attachment = Attachment.Create(
                uploadResult.FileName,
                uploadResult.ContentType,
                uploadResult.FileSize,
                uploadResult.FileUrl,
                uploadResult.ThumbnailUrl);

            // 6. Handle reply context
            ReplyToMessage? replyTo = null;
            if (request.ReplyToMessageId.HasValue)
            {
                var replyToMessage = await _messageRepository.GetByIdAsync(
                    request.ReplyToMessageId.Value,
                    cancellationToken);

                if (replyToMessage != null)
                {
                    replyTo = ReplyToMessage.Create(
                        replyToMessage.MessageId,
                        replyToMessage.Content ?? string.Empty,
                        replyToMessage.SenderName);
                }
            }

            // 7. TODO: Get sender name from UserService
            var senderName = "User"; // Should fetch from UserService

            // 8. Create Message entity with attachment
            var message = Message.CreateWithAttachment(
                request.ConversationId,
                request.SenderId,
                senderName,
                request.MessageType,
                new List<Attachment> { attachment },
                request.Caption, // Caption
                replyTo);

            // Note: Mentions are currently not supported in attachment messages
            // This could be enhanced in the future

            // 9. Save message
            await _messageRepository.AddAsync(message, cancellationToken);

            // 10. Update conversation last message
            conversation.UpdateLastMessage(
                message.MessageId,
                message.Content,
                message.SenderId,
                message.SenderName,
                message.SentAt);

            await _conversationRepository.UpdateAsync(conversation, cancellationToken);

            // 11. Map to DTO and return
            var messageDto = _mapper.Map<MessageDto>(message);
            return ApiResponse<MessageDto>.SuccessResult(messageDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageDto>.ErrorResult(
                $"Failed to send attachment message: {ex.Message}", 
                500);
        }
    }
}
