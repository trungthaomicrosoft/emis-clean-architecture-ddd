using Chat.Application;
using Chat.Application.Events;
using Chat.Infrastructure;
using EMIS.EventBus;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting Chat Service Worker");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog();

    // Add Application and Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddIntegrationServices(builder.Configuration);

    // Add Kafka Consumer for internal Chat events
    builder.Services.AddKafkaConsumer(
        settings =>
        {
            settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
            settings.GroupId = builder.Configuration["KafkaSettings:GroupId"] ?? "emis-chat-worker";
            settings.ClientId = builder.Configuration["KafkaSettings:ClientId"] ?? "chat-worker-consumer";
            settings.Topics = builder.Configuration.GetSection("KafkaSettings:Topics").Get<List<string>>() 
                ?? new List<string> { "emis.message.sent" };
        },
        consumer =>
        {
            // Subscribe to MessageSentEvent (internal chat events)
            consumer.Subscribe<MessageSentEvent,
                Chat.Application.Events.Handlers.MessageSentEventHandler>();
        });

    // Add Health Checks
    builder.Services.AddHealthChecks();

    var host = builder.Build();

    // Ensure MongoDB indexes are created
    await host.Services.EnsureMongoDbIndexesAsync();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Chat Service Worker failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
