using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Student.Application;

/// <summary>
/// Extension methods for setting up application services in DI container
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Add AutoMapper
        services.AddAutoMapper(assembly);

        // Add FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
