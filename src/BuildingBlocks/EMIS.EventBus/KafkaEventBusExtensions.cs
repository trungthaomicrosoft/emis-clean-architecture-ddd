using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EMIS.EventBus;

/// <summary>
/// Extension methods for configuring Kafka EventBus
/// </summary>
public static class KafkaEventBusExtensions
{
    /// <summary>
    /// Add Kafka EventBus producer to DI container
    /// </summary>
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        Action<KafkaSettings> configureSettings)
    {
        services.Configure(configureSettings);
        services.AddSingleton<IKafkaEventBus, KafkaEventBus>();
        
        return services;
    }

    /// <summary>
    /// Add Kafka Consumer as background service
    /// </summary>
    public static IServiceCollection AddKafkaConsumer(
        this IServiceCollection services,
        Action<KafkaConsumerSettings> configureSettings,
        Action<KafkaConsumerService> configureSubscriptions)
    {
        services.Configure(configureSettings);
        services.AddSingleton<KafkaConsumerService>();
        services.AddHostedService(provider => provider.GetRequiredService<KafkaConsumerService>());

        // Configure subscriptions
        using var serviceProvider = services.BuildServiceProvider();
        var consumerService = serviceProvider.GetRequiredService<KafkaConsumerService>();
        configureSubscriptions(consumerService);

        return services;
    }

    /// <summary>
    /// Subscribe to an integration event
    /// </summary>
    public static KafkaConsumerService Subscribe<TEvent, THandler>(
        this KafkaConsumerService consumer)
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        consumer.Subscribe<TEvent, THandler>();
        return consumer;
    }
}
