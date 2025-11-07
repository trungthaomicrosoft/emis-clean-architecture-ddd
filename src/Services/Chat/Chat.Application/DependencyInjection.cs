using Chat.Application.Events;
using Chat.Application.Events.Handlers;
using Chat.Application.IntegrationEvents.Handlers;
using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Chat.Application;

/// <summary>
/// Application layer dependency injection configuration
/// Registers MediatR, AutoMapper, FluentValidation, and Integration Event Handlers
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register AutoMapper
        services.AddAutoMapper(assembly);

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register Integration Event Handlers
        services.AddIntegrationEventHandlers();

        return services;
    }

    /// <summary>
    /// Register integration event handlers for Kafka events
    /// </summary>
    private static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services)
    {
        // Student Service events
        services.AddTransient<IIntegrationEventHandler<StudentCreatedIntegrationEvent>, 
            StudentCreatedIntegrationEventHandler>();

        // Chat Service internal events (message processing)
        services.AddTransient<IIntegrationEventHandler<MessageSentEvent>,
            MessageSentEventHandler>();

        // Future handlers can be added here:
        // services.AddTransient<IIntegrationEventHandler<ParentEnrolledEvent>, ParentEnrolledEventHandler>();
        // services.AddTransient<IIntegrationEventHandler<TeacherAssignedEvent>, TeacherAssignedEventHandler>();

        return services;
    }
}
