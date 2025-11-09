using System.Text;
using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using EMIS.SharedKernel;
using FluentValidation;
using Identity.Application.EventHandlers;
using Identity.Application.Services;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/identity-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Identity Service API", 
        Version = "v1",
        Description = "Authentication & Authorization Service for EMIS"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=emis_identity;User=root;Password=EMISPassword123!;";

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
    ?? "your-256-bit-secret-key-here-at-least-32-characters-long-for-production-use";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "https://emis-api.com";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "https://emis-app.com";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Identity.Application.Commands.LoginCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(Identity.Application.Validators.LoginCommandValidator).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(Identity.Application.Mappings.UserProfile).Assembly);

// Repositories & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Event Handlers
builder.Services.AddScoped<TeacherCreatedEventHandler>();

// Kafka Consumer
builder.Services.AddKafkaConsumer(
    settings =>
    {
        settings.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        settings.GroupId = "identity-service-consumer";
        settings.ClientId = "identity-service";
        settings.TopicPrefix = builder.Configuration["Kafka:TopicPrefix"] ?? "emis";
        settings.ServiceName = builder.Configuration["Kafka:ServiceName"] ?? "teacher";
        settings.DefaultTopicStrategy = builder.Configuration["Kafka:DefaultTopicStrategy"] ?? "service";
    },
    consumer =>
    {
        consumer.Subscribe<TeacherCreatedIntegrationEvent, TeacherCreatedEventHandler>();
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
        Log.Information("Identity database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the Identity database");
    }
}

Log.Information("Identity Service is starting...");

app.Run();

Log.CloseAndFlush();
