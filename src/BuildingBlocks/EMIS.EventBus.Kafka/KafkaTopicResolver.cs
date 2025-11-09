using Microsoft.Extensions.Logging;

namespace EMIS.EventBus;

/// <summary>
/// Resolves Kafka topic names based on event type and configuration
/// Shared logic between KafkaEventBus (Publisher) and KafkaConsumerService (Consumer)
/// </summary>
public class KafkaTopicResolver
{
    private readonly ILogger<KafkaTopicResolver> _logger;

    public KafkaTopicResolver(ILogger<KafkaTopicResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get topic name for a specific event using the configured strategy
    /// </summary>
    public string GetTopicForEvent(
        string eventName, 
        string topicPrefix,
        string defaultTopicStrategy,
        Dictionary<string, string> eventTopicMappings,
        string? serviceName,
        string clientId)
    {
        // Step 1: Check if there's an explicit mapping for this event
        if (eventTopicMappings.TryGetValue(eventName, out var mappedTopic))
        {
            _logger.LogDebug("Using mapped topic {Topic} for event {EventName}", mappedTopic, eventName);
            return mappedTopic;
        }

        // Step 2: Use default strategy
        if (defaultTopicStrategy == "service")
        {
            return GetServiceBasedTopic(topicPrefix, serviceName, clientId);
        }
        else
        {
            return GetEventBasedTopic(topicPrefix, eventName);
        }
    }

    /// <summary>
    /// Get service-based topic name (e.g., emis.chat, emis.student)
    /// </summary>
    private string GetServiceBasedTopic(string topicPrefix, string? serviceName, string clientId)
    {
        // Get service name from parameter or auto-detect from ClientId
        var resolvedServiceName = serviceName;
        
        if (string.IsNullOrEmpty(resolvedServiceName))
        {
            // Auto-detect: "chat-producer" -> "chat", "chat-consumer" -> "chat"
            resolvedServiceName = clientId
                .Replace("-producer", "")
                .Replace("-consumer", "")
                .Split('-')
                .FirstOrDefault() ?? "default";
        }

        var topic = $"{topicPrefix}.{resolvedServiceName.ToLowerInvariant()}";
        _logger.LogDebug("Using service-based topic: {Topic}", topic);
        return topic;
    }

    /// <summary>
    /// Get event-based topic name (e.g., emis.messagesent, emis.studentcreated)
    /// </summary>
    private string GetEventBasedTopic(string topicPrefix, string eventName)
    {
        // Convention: emis.{event-name}
        // Example: MessageSentEvent -> emis.messagesent
        var topicName = eventName
            .Replace("Event", "")
            .Replace("Integration", "")
            .ToLowerInvariant();
        
        var topic = $"{topicPrefix}.{topicName}";
        _logger.LogDebug("Using event-based topic: {Topic}", topic);
        return topic;
    }
}
