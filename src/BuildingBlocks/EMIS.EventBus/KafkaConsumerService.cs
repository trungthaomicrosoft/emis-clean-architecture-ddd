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
public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaConsumerSettings _settings;
    private IConsumer<string, string>? _consumer;
    private readonly Dictionary<string, Type> _eventHandlers;

    public KafkaConsumerService(
        IServiceProvider serviceProvider,
        IOptions<KafkaConsumerSettings> settings,
        ILogger<KafkaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
        _eventHandlers = new Dictionary<string, Type>();
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;
        _eventHandlers[eventName] = typeof(THandler);
        _logger.LogInformation("Subscribed to event {EventName} with handler {Handler}", 
            eventName, typeof(THandler).Name);
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
        
        // Subscribe to topics
        var topics = _settings.Topics;
        _consumer.Subscribe(topics);
        
        _logger.LogInformation("Kafka Consumer started. Group: {GroupId}, Topics: {Topics}", 
            _settings.GroupId, string.Join(", ", topics));

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
    public List<string> Topics { get; set; } = new();
}
