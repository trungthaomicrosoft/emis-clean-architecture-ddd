using Chat.Application.Interfaces;
using Chat.Domain.Repositories;
using Chat.Infrastructure.Configuration;
using Chat.Infrastructure.Persistence;
using Chat.Infrastructure.Repositories;
using Chat.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Chat.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// Registers repositories, services, and external integrations
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration settings
        services.Configure<MongoDbSettings>(configuration.GetSection(nameof(MongoDbSettings)));
        services.Configure<RedisSettings>(configuration.GetSection(nameof(RedisSettings)));
        services.Configure<MinIOSettings>(configuration.GetSection(nameof(MinIOSettings)));

        // Register MongoDB
        services.AddMongoDB(configuration);

        // Register Redis
        services.AddRedisCache(configuration);

        // Register MinIO
        services.AddMinIO(configuration);

        // Register repositories
        services.AddRepositories();

        // Register services
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddMongoDB(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mongoSettings = configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
        
        if (mongoSettings == null)
        {
            throw new InvalidOperationException("MongoDbSettings configuration is missing");
        }

        // Register MongoDB client
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = MongoClientSettings.FromConnectionString(mongoSettings.ConnectionString);
            
            // Configure connection pooling
            settings.MaxConnectionPoolSize = 100;
            settings.MinConnectionPoolSize = 10;
            settings.WaitQueueTimeout = TimeSpan.FromSeconds(30);
            
            // Configure server selection timeout
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(10);
            
            return new MongoClient(settings);
        });

        // Register database
        services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoSettings.DatabaseName);
        });

        // Register ChatDbContext
        services.AddScoped<ChatDbContext>();

        return services;
    }

    private static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(nameof(RedisSettings)).Get<RedisSettings>();
        
        if (redisSettings == null)
        {
            throw new InvalidOperationException("RedisSettings configuration is missing");
        }

        // Register Redis connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            
            // Configure connection options
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.AbortOnConnectFail = false;
            options.ReconnectRetryPolicy = new ExponentialRetry(5000);
            
            return ConnectionMultiplexer.Connect(options);
        });

        // Register IDistributedCache implementation
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisSettings.ConnectionString;
            options.InstanceName = redisSettings.InstanceName;
        });

        return services;
    }

    private static IServiceCollection AddMinIO(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var minioSettings = configuration.GetSection(nameof(MinIOSettings)).Get<MinIOSettings>();
        
        if (minioSettings == null)
        {
            throw new InvalidOperationException("MinIOSettings configuration is missing");
        }

        // Register MinIO client
        services.AddSingleton<IMinioClient>(sp =>
        {
            var client = new MinioClient()
                .WithEndpoint(minioSettings.Endpoint)
                .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey);

            if (minioSettings.UseSSL)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register repositories with scoped lifetime
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IFileStorageService, MinIOFileStorageService>();

        return services;
    }

    /// <summary>
    /// Ensures MongoDB indexes are created on application startup
    /// </summary>
    public static async Task EnsureMongoDbIndexesAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await context.EnsureIndexesAsync();
    }
}
