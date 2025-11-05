using System.Reflection;
using EMIS.EventBus;
using EMIS.SharedKernel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Teacher.Domain.Repositories;
using Teacher.Infrastructure.Persistence;
using Teacher.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/teacher-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Teacher Service API", Version = "v1" });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=emis_teacher;User=root;Password=EMISPassword123!;";

builder.Services.AddDbContext<TeacherDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Teacher.Application.Commands.CreateTeacherCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(Teacher.Application.Validators.CreateTeacherCommandValidator).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(Teacher.Application.Mappings.TeacherProfile).Assembly);

// Repositories
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Kafka EventBus
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
    settings.ClientId = "teacher-service-producer";
    settings.TopicPrefix = "emis.teacher";
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
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Teacher Service API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Auto migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TeacherDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
    }
}

Log.Information("Teacher Service is starting...");

app.Run();

Log.CloseAndFlush();
