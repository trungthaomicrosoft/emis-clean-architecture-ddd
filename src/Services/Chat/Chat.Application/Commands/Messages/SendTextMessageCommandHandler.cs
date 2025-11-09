using AutoMapper;
using Chat.Application.DTOs;
using Chat.Application.Events;
using Chat.Application.Interfaces;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using EMIS.EventBus;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Handler for sending text message - Optimized for high throughput
/// Uses Kafka event-driven pattern instead of direct database writes
/// </summary>
public class SendTextMessageCommandHandler 
    : IRequestHandler<SendTextMessageCommand, ApiResponse<MessageDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly IMapper _mapper;
    private readonly ILogger<SendTextMessageCommandHandler> _logger;

    public SendTextMessageCommandHandler(
        IConversationRepository conversationRepository,
        ICacheService cacheService,
        IEventBus eventBus,
        IMapper mapper,
        ILogger<SendTextMessageCommandHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<MessageDto>> Handle(
        SendTextMessageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // 1. Get conversation from cache (fast path) or database (slow path)
            var conversation = await GetConversationWithCacheAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
            {
                _logger.LogWarning("Conversation {ConversationId} not found", request.ConversationId);
                return ApiResponse<MessageDto>.ErrorResult("Conversation not found", 404);
            }

            // 2. Validate participant access (using cached data)
            if (!conversation.IsParticipant(request.SenderId))
            {
                _logger.LogWarning(
                    "User {UserId} is not a participant in conversation {ConversationId}",
                    request.SenderId, request.ConversationId);
                return ApiResponse<MessageDto>.ErrorResult(
                    "User is not a participant in this conversation", 403);
            }

            // 3. Validate send permission
            if (!conversation.CanSendMessage(request.SenderId))
            {
                _logger.LogWarning(
                    "User {UserId} does not have permission to send messages in conversation {ConversationId}",
                    request.SenderId, request.ConversationId);
                return ApiResponse<MessageDto>.ErrorResult(
                    "User does not have permission to send messages", 403);
            }

            // 4. Generate message ID (local, fast)
            var messageId = Guid.NewGuid();
            var sentAt = DateTime.UtcNow;

            // 5. Get recipient list for real-time broadcast
            var recipientUserIds = conversation.Participants
                .Where(p => p.UserId != request.SenderId)
                .Select(p => p.UserId)
                .ToList();

            // 6. Create event for async processing
            var messageSentEvent = new MessageSentEvent(
                messageId,
                request.ConversationId,
                conversation.TenantId,
                request.SenderId,
                request.SenderName,
                request.Content,
                sentAt,
                recipientUserIds);

            // Add reply info if applicable
            if (request.ReplyToMessageId.HasValue)
            {
                messageSentEvent.ReplyToMessageId = request.ReplyToMessageId;
                // Note: Reply content will be fetched by background worker
            }

            // Add mentions
            if (request.Mentions.Any())
            {
                messageSentEvent.Mentions = request.Mentions
                    .Select(m => new MentionData(m.UserId, m.UserName, m.StartIndex, m.Length))
                    .ToList();
            }

            // 7. Publish to Kafka (async, non-blocking)
            await _eventBus.PublishAsync(messageSentEvent, cancellationToken);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "Message {MessageId} queued for processing in {ElapsedMs}ms. Conversation: {ConversationId}",
                messageId, elapsedMs, request.ConversationId);

            // 8. Return optimistic response (message will be persisted asynchronously)
            var messageDto = new MessageDto
            {
                MessageId = messageId,
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                SenderName = request.SenderName,
                Content = request.Content,
                SentAt = sentAt,
                Status = "Sending", // Will be updated to "Sent" after processing
                Mentions = request.Mentions.Select(m => new MentionDto
                {
                    UserId = m.UserId,
                    UserName = m.UserName,
                    StartIndex = m.StartIndex,
                    Length = m.Length
                }).ToList()
            };

            return ApiResponse<MessageDto>.SuccessResult(messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to queue message for conversation {ConversationId}",
                request.ConversationId);
            return ApiResponse<MessageDto>.ErrorResult(
                $"Failed to send message: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get conversation with cache-aside pattern
    /// Cache TTL: 5 minutes
    /// </summary>
    private async Task<Domain.Aggregates.Conversation?> GetConversationWithCacheAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"conversation:{conversationId}";

        // Try cache first
        var cachedConversation = await _cacheService.GetAsync<Domain.Aggregates.Conversation>(
            cacheKey,
            cancellationToken);

        if (cachedConversation != null)
        {
            _logger.LogDebug("Cache hit for conversation {ConversationId}", conversationId);
            return cachedConversation;
        }

        // Cache miss - fetch from database
        _logger.LogDebug("Cache miss for conversation {ConversationId}, fetching from database", conversationId);
        var conversation = await _conversationRepository.GetByIdAsync(
            conversationId,
            cancellationToken);

        if (conversation != null)
        {
            // Store in cache for 5 minutes
            await _cacheService.SetAsync(
                cacheKey,
                conversation,
                TimeSpan.FromMinutes(5),
                cancellationToken);
        }

        return conversation;
    }
}
