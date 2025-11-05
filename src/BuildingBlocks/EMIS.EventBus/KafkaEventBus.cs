using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EMIS.EventBus;

public class KafkaEventBus : IKafkaEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly KafkaSettings _settings;

    public KafkaEventBus(
        IOptions<KafkaSettings> settings,
        ILogger<KafkaEventBus> logger)
    {
        _settings = settings.Value;
        _logger = logger;

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
        where TEvent : IIntegrationEvent
    {
        var eventName = @event.GetType().Name;
        var topic = GetTopicName(eventName);

        try
        {
            var message = new Message<string, string>
            {
                Key = @event.Id.ToString(), // Use event Id as partition key
                Value = JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventName) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(@event.OccurredOn.ToString("O")) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Published event {EventName} to Kafka topic {Topic}. Partition: {Partition}, Offset: {Offset}",
                eventName, topic, result.Partition.Value, result.Offset.Value);
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
        // Convention: emis.{service}.{event-name}
        // Example: emis.teacher.teacher-created
        var topicName = eventName.Replace("Event", "")
            .Replace("Integration", "")
            .ToLowerInvariant();
        
        return $"{_settings.TopicPrefix}.{topicName}";
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
}
