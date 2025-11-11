using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EMIS.EventBus;

/// <summary>
/// Extension methods for configuring Kafka EventBus
/// </summary>
public static class KafkaEventBusExtensions
{
    #region New Configuration-Based Methods (Recommended)
    
    /// <summary>
    /// Add Kafka EventBus producer to DI container (reads from IConfiguration)
    /// Recommended way: automatically binds settings from appsettings.json
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="sectionName">Configuration section name (default: "Kafka")</param>
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Kafka")
    {
        // Bind shared settings from root section
        var sharedSettings = configuration.GetSection(sectionName).Get<KafkaBaseSettings>() 
            ?? new KafkaBaseSettings();
        
        // Configure producer settings (merge shared + producer-specific)
        services.Configure<KafkaProducerSettings>(settings =>
        {
            // Copy shared settings
            settings.BootstrapServers = sharedSettings.BootstrapServers;
            settings.TopicPrefix = sharedSettings.TopicPrefix;
            settings.ServiceName = sharedSettings.ServiceName;
            settings.DefaultTopicStrategy = sharedSettings.DefaultTopicStrategy;
            settings.EventTopicMappings = sharedSettings.EventTopicMappings;
            
            // Bind producer-specific settings (will override shared if exists in config)
            var producerSection = configuration.GetSection($"{sectionName}:Producer");
            if (producerSection.Exists())
            {
                producerSection.Bind(settings);
            }
        });
        
        services.AddSingleton<KafkaTopicResolver>();
        services.AddSingleton<IKafkaEventBus, KafkaEventBus>();
        
        return services;
    }

    /// <summary>
    /// Add Kafka Consumer as background service (reads from IConfiguration)
    /// Recommended way: automatically binds settings from appsettings.json
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configureSubscriptions">Configure event subscriptions</param>
    /// <param name="sectionName">Configuration section name (default: "Kafka")</param>
    public static IServiceCollection AddKafkaConsumer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IEventBusConsumer> configureSubscriptions,
        string sectionName = "Kafka")
    {
        // Bind shared settings from root section
        var sharedSettings = configuration.GetSection(sectionName).Get<KafkaBaseSettings>() 
            ?? new KafkaBaseSettings();
        
        // Configure consumer settings (merge shared + consumer-specific)
        services.Configure<KafkaConsumerSettings>(settings =>
        {
            // Copy shared settings
            settings.BootstrapServers = sharedSettings.BootstrapServers;
            settings.TopicPrefix = sharedSettings.TopicPrefix;
            settings.ServiceName = sharedSettings.ServiceName;
            settings.DefaultTopicStrategy = sharedSettings.DefaultTopicStrategy;
            settings.EventTopicMappings = sharedSettings.EventTopicMappings;
            
            // Bind consumer-specific settings (will override shared if exists in config)
            var consumerSection = configuration.GetSection($"{sectionName}:Consumer");
            if (consumerSection.Exists())
            {
                consumerSection.Bind(settings);
            }
        });
        
        services.AddSingleton<KafkaTopicResolver>();
        services.AddSingleton<KafkaConsumerService>();
        services.AddSingleton<IEventBusConsumer>(provider => 
            provider.GetRequiredService<KafkaConsumerService>());
        services.AddHostedService(provider => 
            provider.GetRequiredService<KafkaConsumerService>());

        // Configure subscriptions
        using var serviceProvider = services.BuildServiceProvider();
        var consumerService = serviceProvider.GetRequiredService<IEventBusConsumer>();
        configureSubscriptions(consumerService);

        return services;
    }
    
    #endregion

    #region Legacy Action-Based Methods (Backward Compatibility)
    
    /// <summary>
    /// Add Kafka EventBus producer to DI container (legacy action-based configuration)
    /// For backward compatibility. Use AddKafkaEventBus(IConfiguration) for new code.
    /// </summary>
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        Action<KafkaProducerSettings> configureSettings)
    {
        services.Configure(configureSettings);
        services.AddSingleton<KafkaTopicResolver>();
        services.AddSingleton<IKafkaEventBus, KafkaEventBus>();
        
        return services;
    }

    /// <summary>
    /// Add Kafka Consumer as background service (legacy action-based configuration)
    /// For backward compatibility. Use AddKafkaConsumer(IConfiguration, Action) for new code.
    /// </summary>
    public static IServiceCollection AddKafkaConsumer(
        this IServiceCollection services,
        Action<KafkaConsumerSettings> configureSettings,
        Action<IEventBusConsumer> configureSubscriptions)
    {
        services.Configure(configureSettings);
        services.AddSingleton<KafkaTopicResolver>();
        services.AddSingleton<KafkaConsumerService>();
        
        // Register as IEventBusConsumer abstraction
        services.AddSingleton<IEventBusConsumer>(provider => 
            provider.GetRequiredService<KafkaConsumerService>());
        
        services.AddHostedService(provider => 
            provider.GetRequiredService<KafkaConsumerService>());

        // Configure subscriptions
        using var serviceProvider = services.BuildServiceProvider();
        var consumerService = serviceProvider.GetRequiredService<IEventBusConsumer>();
        configureSubscriptions(consumerService);

        return services;
    }
    
    #endregion

    /// <summary>
    /// Subscribe to an integration event
    /// </summary>
    public static IEventBusConsumer Subscribe<TEvent, THandler>(
        this IEventBusConsumer consumer)
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        consumer.Subscribe<TEvent, THandler>();
        return consumer;
    }
}
