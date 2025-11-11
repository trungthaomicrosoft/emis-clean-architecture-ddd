namespace EMIS.EventBus;

/// <summary>
/// Base configuration shared between Kafka Producer and Consumer
/// Contains common settings like BootstrapServers, TopicPrefix, etc.
/// </summary>
public class KafkaBaseSettings
{
    /// <summary>
    /// Kafka broker addresses (comma-separated list)
    /// Example: "localhost:9092" or "broker1:9092,broker2:9092"
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";
    
    /// <summary>
    /// Topic prefix for all Kafka topics (e.g., "emis")
    /// Used for topic naming: {TopicPrefix}.{ServiceName} or {TopicPrefix}.{EventName}
    /// </summary>
    public string TopicPrefix { get; set; } = "emis";
    
    /// <summary>
    /// Service name for service-based topic routing
    /// Auto-detected from ClientId if not set (e.g., "chat-producer" -> "chat")
    /// Used when DefaultTopicStrategy = "service"
    /// </summary>
    public string? ServiceName { get; set; }
    
    /// <summary>
    /// Topic routing strategy: "service" (default) or "event"
    /// - "service": Events grouped by service (e.g., emis.chat, emis.student)
    /// - "event": One topic per event type (e.g., emis.messagesent, emis.studentcreated)
    /// </summary>
    public string DefaultTopicStrategy { get; set; } = "service";
    
    /// <summary>
    /// Optional: Override topic name for specific events
    /// Key: Event type name (e.g., "MessageSentEvent", "StudentCreatedIntegrationEvent")
    /// Value: Topic name (e.g., "emis.chat.messages", "emis.student.created")
    /// Use this for high-volume events that need dedicated topics
    /// </summary>
    public Dictionary<string, string> EventTopicMappings { get; set; } = new();
}

/// <summary>
/// Kafka Producer-specific configuration
/// Inherits shared settings and adds producer-specific options
/// </summary>
public class KafkaProducerSettings : KafkaBaseSettings
{
    /// <summary>
    /// Unique identifier for the producer client
    /// Used for Kafka metrics and monitoring
    /// </summary>
    public string ClientId { get; set; } = "emis-producer";
    
    /// <summary>
    /// Acknowledgment level: "All", "One", "None"
    /// - "All": Wait for all in-sync replicas (safest, slowest)
    /// - "One": Wait for leader only (balanced)
    /// - "None": Fire and forget (fastest, least safe)
    /// </summary>
    public string Acks { get; set; } = "All";
    
    /// <summary>
    /// Enable idempotent producer (prevents duplicate messages)
    /// Recommended: true for data consistency
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;
    
    /// <summary>
    /// Message timeout in milliseconds
    /// Maximum time to wait for message delivery
    /// </summary>
    public int MessageTimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// Request timeout in milliseconds
    /// Maximum time to wait for broker response
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;
}

/// <summary>
/// Kafka Consumer-specific configuration
/// Inherits shared settings and adds consumer-specific options
/// </summary>
public class KafkaConsumerSettings : KafkaBaseSettings
{
    /// <summary>
    /// Consumer group ID for offset management and load balancing
    /// Consumers with the same GroupId share message consumption
    /// </summary>
    public string GroupId { get; set; } = "emis-consumer-group";
    
    /// <summary>
    /// Unique identifier for the consumer client
    /// Used for Kafka metrics and monitoring
    /// </summary>
    public string ClientId { get; set; } = "emis-consumer";
    
    /// <summary>
    /// Where to start consuming when no offset exists: "Earliest", "Latest"
    /// - "Earliest": Start from the beginning of the topic
    /// - "Latest": Start from the end (only new messages)
    /// </summary>
    public string AutoOffsetReset { get; set; } = "Earliest";
    
    /// <summary>
    /// Enable automatic offset commit
    /// Recommended: false (manual commit for better reliability)
    /// </summary>
    public bool EnableAutoCommit { get; set; } = false;
    
    /// <summary>
    /// Enable automatic offset store
    /// Recommended: false when EnableAutoCommit is false
    /// </summary>
    public bool EnableAutoOffsetStore { get; set; } = false;
    
    /// <summary>
    /// Maximum time to wait for messages before timing out (milliseconds)
    /// Default: 100ms
    /// </summary>
    public int ConsumerTimeoutMs { get; set; } = 100;
    
    /// <summary>
    /// Manual topic list (DEPRECATED: use auto-resolution via Subscribe<TEvent, THandler>)
    /// Only used as fallback when no events are subscribed
    /// </summary>
    [Obsolete("Use Subscribe<TEvent, THandler> for automatic topic resolution")]
    public List<string> Topics { get; set; } = new();
}
