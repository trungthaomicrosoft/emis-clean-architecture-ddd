using EMIS.BuildingBlocks.MultiTenant;
using EMIS.EventBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Student.API.Infrastructure;
using Student.API.Middleware;
using Student.Application;
using Student.Infrastructure;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/student-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Student API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Add Swagger/OpenAPI
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "EMIS Student API",
            Version = "v1",
            Description = "API for Student Management Service",
            Contact = new OpenApiContact
            {
                Name = "EMIS Team",
                Email = "support@emis.com"
            }
        });

        // Add JWT authentication to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        // Add XML comments if available
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000" };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Add JWT Authentication
    var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
        ?? "YourSuperSecretKeyHere_MinimumLengthIs32Characters!";
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "EMIS.IdentityService";
    var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "EMIS.Client";

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
    Log.Information("JWT Authentication configured");

    // Add HttpContextAccessor for tenant resolution
    builder.Services.AddHttpContextAccessor();

    // Add Tenant Context (use Mock for development, Http for production)
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddScoped<ITenantContext, MockTenantContext>();
        Log.Information("Using MockTenantContext for development");
    }
    else
    {
        builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
        Log.Information("Using HttpTenantContext for production");
    }

    // Add Application Layer (MediatR, AutoMapper, FluentValidation)
    builder.Services.AddApplication();
    Log.Information("Application layer registered");

    // Add Infrastructure Layer (EF Core, Repositories, UnitOfWork)
    builder.Services.AddInfrastructure(builder.Configuration);
    Log.Information("Infrastructure layer registered");

    // Add Kafka Event Bus for publishing events
    builder.Services.AddKafkaEventBus(settings =>
    {
        settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
        settings.ClientId = builder.Configuration["KafkaSettings:ClientId"] ?? "student-producer";
        settings.TopicPrefix = builder.Configuration["KafkaSettings:TopicPrefix"] ?? "emis";
    });
    Log.Information("Kafka Event Bus registered");

    // Add Health Checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    
    // Use custom exception handling middleware
    app.UseExceptionHandling();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMIS Student API V1");
            c.RoutePrefix = "swagger";
        });
        
        Log.Information("Swagger UI enabled at: http://localhost:5202/swagger");
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health");

    // Log all registered endpoints
    Log.Information("Application started successfully");
    Log.Information("Available endpoints:");
    Log.Information("  - Swagger UI: /swagger");
    Log.Information("  - Health Check: /health");
    Log.Information("  - Students API: /api/students");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
