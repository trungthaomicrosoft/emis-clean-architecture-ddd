using Chat.Application.Interfaces;
using Chat.Domain.Entities;
using Chat.Domain.Repositories;
using Chat.Domain.ValueObjects;
using EMIS.EventBus;
using Microsoft.Extensions.Logging;

namespace Chat.Application.Events.Handlers;

/// <summary>
/// Handler for MessageSentEvent - Processes messages asynchronously
/// Optimized with debounced conversation updates for high throughput
/// </summary>
public class MessageSentEventHandler : IIntegrationEventHandler<MessageSentEvent>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MessageSentEventHandler> _logger;

    // Debounce configuration - Only update conversation if no new message in X seconds
    private static readonly Dictionary<Guid, Timer> _updateTimers = new();
    private static readonly Dictionary<Guid, MessageSentEvent> _pendingUpdates = new();
    private static readonly object _timerLock = new();
    private const int DEBOUNCE_DELAY_MS = 2000; // 2 seconds

    public MessageSentEventHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        ICacheService cacheService,
        ILogger<MessageSentEventHandler> logger)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(MessageSentEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // 1. Handle reply message lookup (if needed)
            ReplyToMessage? replyTo = null;
            if (@event.ReplyToMessageId.HasValue)
            {
                var replyToMessage = await _messageRepository.GetByIdAsync(
                    @event.ReplyToMessageId.Value,
                    cancellationToken);

                if (replyToMessage != null && !replyToMessage.IsDeleted)
                {
                    replyTo = ReplyToMessage.Create(
                        replyToMessage.MessageId,
                        replyToMessage.Content,
                        replyToMessage.SenderName);
                }
            }

            // 2. Convert mentions
            var mentions = @event.Mentions
                .Select(m => Mention.Create(m.UserId, m.UserName, m.StartIndex, m.Length))
                .ToList();

            // 3. Create message entity
            var message = Message.CreateText(
                @event.ConversationId,
                @event.SenderId,
                @event.SenderName,
                @event.Content,
                replyTo,
                mentions);

            // Override message ID and timestamp from event (for idempotency)
            typeof(Message).GetProperty(nameof(Message.MessageId))!
                .SetValue(message, @event.MessageId);
            typeof(Message).GetProperty(nameof(Message.SentAt))!
                .SetValue(message, @event.SentAt);

            // 4. Save message to database (always fast)
            await _messageRepository.AddAsync(message, cancellationToken);

            // 5. Smart conversation update with debouncing
            await ScheduleConversationUpdateAsync(@event, cancellationToken);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "Successfully processed message {MessageId} in {ElapsedMs}ms",
                @event.MessageId, elapsedMs);

            // 6. Invalidate conversation cache (so next read gets fresh data)
            var cacheKey = $"conversation:{@event.ConversationId}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            // 7. TODO: Trigger SignalR broadcast and push notifications
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process MessageSentEvent for message {MessageId}. Will retry.",
                @event.MessageId);
            
            // Kafka will retry this message automatically
            throw;
        }
    }

    /// <summary>
    /// Schedule conversation update with debouncing
    /// - First message in conversation: Update immediately
    /// - Subsequent messages: Debounce (wait 2 seconds for burst to complete)
    /// </summary>
    private async Task ScheduleConversationUpdateAsync(
        MessageSentEvent @event,
        CancellationToken cancellationToken)
    {
        bool isFirstMessage = false;

        lock (_timerLock)
        {
            // Check if this is the first message (no pending updates)
            isFirstMessage = !_pendingUpdates.ContainsKey(@event.ConversationId);

            // Store latest event
            _pendingUpdates[@event.ConversationId] = @event;

            // Cancel existing timer if any
            if (_updateTimers.TryGetValue(@event.ConversationId, out var existingTimer))
            {
                existingTimer.Dispose();
            }

            // Create new debounce timer
            var timer = new Timer(
                async _ => await ProcessConversationUpdateAsync(@event.ConversationId, cancellationToken),
                null,
                DEBOUNCE_DELAY_MS,
                Timeout.Infinite);

            _updateTimers[@event.ConversationId] = timer;
        }

        // If first message, update immediately (so conversation appears in list)
        if (isFirstMessage)
        {
            _logger.LogInformation(
                "First message in conversation {ConversationId}, updating immediately",
                @event.ConversationId);

            await UpdateConversationLastMessageAsync(@event, cancellationToken);
        }
        else
        {
            _logger.LogDebug(
                "Debouncing conversation update for {ConversationId}",
                @event.ConversationId);
        }
    }

    /// <summary>
    /// Process debounced conversation update
    /// Called after debounce delay expires (no new messages)
    /// </summary>
    private async Task ProcessConversationUpdateAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        MessageSentEvent? eventToProcess = null;

        lock (_timerLock)
        {
            if (_pendingUpdates.TryGetValue(conversationId, out var evt))
            {
                eventToProcess = evt;
                _pendingUpdates.Remove(conversationId);
            }

            if (_updateTimers.TryGetValue(conversationId, out var timer))
            {
                timer.Dispose();
                _updateTimers.Remove(conversationId);
            }
        }

        if (eventToProcess != null)
        {
            _logger.LogInformation(
                "Processing debounced conversation update for {ConversationId}",
                conversationId);

            await UpdateConversationLastMessageAsync(eventToProcess, cancellationToken);
        }
    }

    /// <summary>
    /// Update conversation LastMessage in database
    /// </summary>
    private async Task UpdateConversationLastMessageAsync(
        MessageSentEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _conversationRepository.GetByIdAsync(
                @event.ConversationId,
                cancellationToken);

            if (conversation != null)
            {
                // Only update if this message is newer than current last message
                // (Handles out-of-order processing)
                if (conversation.LastMessage == null || 
                    @event.SentAt >= conversation.LastMessage.SentAt)
                {
                    conversation.UpdateLastMessage(
                        @event.MessageId,
                        @event.Content,
                        @event.SenderId,
                        @event.SenderName,
                        @event.SentAt);

                    await _conversationRepository.UpdateAsync(conversation, cancellationToken);

                    _logger.LogInformation(
                        "Updated LastMessage for conversation {ConversationId}",
                        @event.ConversationId);
                }
                else
                {
                    _logger.LogDebug(
                        "Skipping LastMessage update for {ConversationId} - message is older",
                        @event.ConversationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to update conversation {ConversationId} LastMessage",
                @event.ConversationId);
            // Don't throw - conversation update is not critical
        }
    }
}
