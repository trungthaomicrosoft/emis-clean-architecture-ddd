using Chat.API.Hubs;
using Chat.Application;
using Chat.Application.Events;
using Chat.Infrastructure;
using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting Chat Service API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() 
        { 
            Title = "EMIS Chat Service API", 
            Version = "v1",
            Description = "Real-time chat messaging service for EMIS platform"
        });

        // Add JWT authentication to Swagger
        options.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your token"
        });

        options.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new()
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Add JWT Authentication
    var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
        ?? throw new InvalidOperationException("JWT Secret not configured");
    
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // Configure JWT authentication for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // Add CORS
    var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:3000" };
    
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ChatCorsPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for SignalR
        });
    });

    // Add SignalR with Redis backplane
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(builder.Configuration["RedisSettings:ConnectionString"] 
            ?? "localhost:6379", options =>
        {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("emis_chat");
        });

    // Add Application and Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddIntegrationServices(builder.Configuration);

    // Add Kafka Consumer for consuming integration events
    builder.Services.AddKafkaConsumer(
        settings =>
        {
            settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
            settings.GroupId = builder.Configuration["KafkaSettings:GroupId"] ?? "emis-chat-service";
            settings.ClientId = builder.Configuration["KafkaSettings:ClientId"] ?? "chat-consumer";
            settings.Topics = builder.Configuration.GetSection("KafkaSettings:Topics").Get<List<string>>() 
                ?? new List<string> { "emis.student.created", "emis.message.sent" };
        },
        consumer =>
        {
            // Subscribe to StudentCreatedIntegrationEvent
            consumer.Subscribe<StudentCreatedIntegrationEvent, 
                Chat.Application.IntegrationEvents.Handlers.StudentCreatedIntegrationEventHandler>();
            
            // Subscribe to MessageSentEvent (internal chat events)
            consumer.Subscribe<MessageSentEvent,
                Chat.Application.Events.Handlers.MessageSentEventHandler>();
        });

    // Add Kafka Producer for publishing events
    builder.Services.AddKafkaEventBus(settings =>
    {
        settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
        settings.ClientId = builder.Configuration["KafkaSettings:ProducerClientId"] ?? "chat-producer";
        settings.TopicPrefix = builder.Configuration["KafkaSettings:TopicPrefix"] ?? "emis";
    });

    // Add Health Checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Ensure MongoDB indexes are created
    await app.Services.EnsureMongoDbIndexesAsync();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat Service API v1"));
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors("ChatCorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    
    // Map SignalR Hub
    app.MapHub<ChatHub>("/hubs/chat");

    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Chat Service API failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
