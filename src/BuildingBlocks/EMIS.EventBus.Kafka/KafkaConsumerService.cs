using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EMIS.EventBus;

/// <summary>
/// Background service for consuming Kafka messages
/// </summary>
public class KafkaConsumerService : BackgroundService, IEventBusConsumer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaConsumerSettings _settings;
    private IConsumer<string, string>? _consumer;
    private readonly Dictionary<string, Type> _eventHandlers;
    private readonly HashSet<string> _subscribedTopics;
    private readonly KafkaTopicResolver _topicResolver;

    public KafkaConsumerService(
        IServiceProvider serviceProvider,
        IOptions<KafkaConsumerSettings> settings,
        ILogger<KafkaConsumerService> logger,
        KafkaTopicResolver topicResolver)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
        _eventHandlers = new Dictionary<string, Type>();
        _subscribedTopics = new HashSet<string>();
        _topicResolver = topicResolver;
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;
        _eventHandlers[eventName] = typeof(THandler);
        
        // Auto-resolve topic for this event
        var topic = GetTopicForEvent(eventName);
        if (!_subscribedTopics.Contains(topic))
        {
            _subscribedTopics.Add(topic);
        }
        
        _logger.LogInformation(
            "Subscribed to event {EventName} with handler {Handler}. Topic: {Topic}", 
            eventName, typeof(THandler).Name, topic);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            ClientId = _settings.ClientId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Manual commit for reliability
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        
        // Subscribe to topics: use auto-resolved topics from Subscribe<> calls, or fallback to manual config
        var topicsToSubscribe = _subscribedTopics.Any() 
            ? _subscribedTopics.ToList() 
            : _settings.Topics;
        
        if (!topicsToSubscribe.Any())
        {
            _logger.LogWarning("No topics configured for Kafka Consumer. Consumer will not process any messages.");
            return;
        }
        
        _consumer.Subscribe(topicsToSubscribe);
        
        _logger.LogInformation(
            "Kafka Consumer started. Group: {GroupId}, Topics: {Topics}, Strategy: {Strategy}", 
            _settings.GroupId, string.Join(", ", topicsToSubscribe), 
            _subscribedTopics.Any() ? "auto-resolved" : "manual-config");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message == null)
                        continue;

                    await ProcessMessageAsync(consumeResult, stoppingToken);
                    
                    // Commit offset after successful processing
                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing Kafka message");
                }
            }
        }
        finally
        {
            _consumer?.Close();
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        var eventType = System.Text.Encoding.UTF8.GetString(
            result.Message.Headers.FirstOrDefault(h => h.Key == "event-type")?.GetValueBytes() ?? Array.Empty<byte>());

        _logger.LogInformation(
            "Processing message from topic {Topic}, partition {Partition}, offset {Offset}. Event: {EventType}",
            result.Topic, result.Partition.Value, result.Offset.Value, eventType);

        if (!_eventHandlers.TryGetValue(eventType, out var handlerType))
        {
            _logger.LogWarning("No handler registered for event type {EventType}", eventType);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler == null)
            {
                _logger.LogError("Failed to resolve handler {HandlerType}", handlerType.Name);
                return;
            }

            // Deserialize event based on handler's event type
            var eventInterfaceType = handlerType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));
            
            if (eventInterfaceType == null)
                return;

            var eventDataType = eventInterfaceType.GetGenericArguments()[0];
            var @event = JsonSerializer.Deserialize(result.Message.Value, eventDataType, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (@event == null)
                return;

            // Invoke handler
            var handleMethod = handlerType.GetMethod("Handle");
            if (handleMethod != null)
            {
                var task = (Task)handleMethod.Invoke(handler, new[] { @event, cancellationToken })!;
                await task;
                
                _logger.LogInformation("Successfully processed event {EventType}", eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}: {Message}", eventType, ex.Message);
            throw; // Will not commit offset, message will be reprocessed
        }
    }

    private string GetTopicForEvent(string eventName)
    {
        return _topicResolver.GetTopicForEvent(
            eventName,
            _settings.TopicPrefix,
            _settings.DefaultTopicStrategy,
            _settings.EventTopicMappings,
            _settings.ServiceName,
            _settings.ClientId);
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}

public class KafkaConsumerSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "emis-consumer-group";
    public string ClientId { get; set; } = "emis-consumer";
    
    /// <summary>
    /// Topic prefix for all Kafka topics (e.g., "emis")
    /// </summary>
    public string TopicPrefix { get; set; } = "emis";
    
    /// <summary>
    /// Topic routing strategy: "service" (default) or "event"
    /// - "service": Events grouped by service (e.g., emis.chat, emis.student)
    /// - "event": One topic per event type (e.g., emis.messagesent)
    /// </summary>
    public string DefaultTopicStrategy { get; set; } = "service";
    
    /// <summary>
    /// Optional: Override topic name for specific events
    /// Key: Event type name (e.g., "MessageSentEvent")
    /// Value: Topic name (e.g., "emis.chat.messages")
    /// Use this for high-volume events that need separate topics
    /// </summary>
    public Dictionary<string, string> EventTopicMappings { get; set; } = new();
    
    /// <summary>
    /// Service name for service-based topic routing
    /// Auto-detected from ClientId if not set (e.g., "chat-consumer" -> "chat")
    /// </summary>
    public string? ServiceName { get; set; }
    
    /// <summary>
    /// Manual topic list (DEPRECATED: use auto-resolution via Subscribe<TEvent, THandler>)
    /// Only used as fallback when no events are subscribed
    /// </summary>
    public List<string> Topics { get; set; } = new();
}
