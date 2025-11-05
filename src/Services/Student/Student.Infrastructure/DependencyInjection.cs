using EMIS.BuildingBlocks.MultiTenant;
using EMIS.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Student.Domain.Repositories;
using Student.Infrastructure.Persistence;
using Student.Infrastructure.Repositories;

namespace Student.Infrastructure;

/// <summary>
/// Extension methods for setting up infrastructure services in DI container
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("StudentDatabase") 
            ?? throw new InvalidOperationException("Connection string 'StudentDatabase' not found");

        services.AddDbContext<StudentDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySqlOptions =>
                {
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    
                    mySqlOptions.MigrationsAssembly(typeof(StudentDbContext).Assembly.FullName);
                });

            // Enable sensitive data logging in development
            var enableSensitiveLogging = configuration["Logging:EnableSensitiveDataLogging"];
            if (enableSensitiveLogging == "true")
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Add Repositories
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IClassRepository, ClassRepository>();
        
        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddInfrastructureWithTenant(
        this IServiceCollection services,
        IConfiguration configuration,
        ITenantContext tenantContext)
    {
        // Register tenant context
        services.AddSingleton(tenantContext);

        // Add infrastructure services
        return services.AddInfrastructure(configuration);
    }
}
