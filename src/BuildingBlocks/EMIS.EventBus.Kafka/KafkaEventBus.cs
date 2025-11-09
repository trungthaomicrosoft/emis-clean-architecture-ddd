using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EMIS.EventBus;

/// <summary>
/// Kafka implementation of IEventBus.
/// Implements ordered event processing using Kafka partitions.
/// </summary>
public class KafkaEventBus : IKafkaEventBus, IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly KafkaSettings _settings;
    private readonly KafkaTopicResolver _topicResolver;

    public KafkaEventBus(
        IOptions<KafkaSettings> settings,
        ILogger<KafkaEventBus> logger,
        KafkaTopicResolver topicResolver)
    {
        _settings = settings.Value;
        _logger = logger;
        _topicResolver = topicResolver;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            ClientId = _settings.ClientId,
            Acks = Acks.All, // Wait for all replicas
            EnableIdempotence = true, // Prevent duplicate messages
            MessageTimeoutMs = 30000,
            RequestTimeoutMs = 30000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        
        _logger.LogInformation("Kafka Producer initialized with servers: {Servers}", _settings.BootstrapServers);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IntegrationEvent
    {
        // Determine ordering key
        string orderingKey;
        if (@event is IOrderedEvent orderedEvent)
        {
            orderingKey = orderedEvent.GetOrderingKey();
            _logger.LogDebug(
                "Event {EventType} implements IOrderedEvent, using ordering key: {OrderingKey}",
                @event.GetType().Name, orderingKey);
        }
        else
        {
            orderingKey = @event.Id.ToString();
            _logger.LogDebug(
                "Event {EventType} does not implement IOrderedEvent, using event Id as ordering key",
                @event.GetType().Name);
        }

        await PublishAsync(@event, orderingKey, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string orderingKey, CancellationToken cancellationToken = default) 
        where TEvent : IntegrationEvent
    {
        var eventName = @event.GetType().Name;
        var topic = GetTopicName(eventName);

        try
        {
            var message = new Message<string, string>
            {
                Key = orderingKey, // Use ordering key as Kafka partition key
                Value = JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventName) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(@event.OccurredOn.ToString("O")) },
                    { "ordering-key", System.Text.Encoding.UTF8.GetBytes(orderingKey) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Published event {EventName} to Kafka topic {Topic}. Partition: {Partition}, Offset: {Offset}, OrderingKey: {OrderingKey}",
                eventName, topic, result.Partition.Value, result.Offset.Value, orderingKey);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, 
                "Failed to publish event {EventName} to Kafka topic {Topic}. Error: {Error}",
                eventName, topic, ex.Error.Reason);
            throw;
        }
    }

    private string GetTopicName(string eventName)
    {
        return _topicResolver.GetTopicForEvent(
            eventName,
            _settings.TopicPrefix,
            _settings.DefaultTopicStrategy,
            _settings.EventTopicMappings,
            _settings.ServiceName,
            _settings.ClientId);
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "emis-producer";
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
    /// Auto-detected from ClientId if not set (e.g., "chat-producer" -> "chat")
    /// </summary>
    public string? ServiceName { get; set; }
}
