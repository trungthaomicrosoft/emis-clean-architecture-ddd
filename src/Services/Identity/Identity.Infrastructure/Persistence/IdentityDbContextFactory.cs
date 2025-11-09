using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for IdentityDbContext
/// Used by EF Core tools for migrations when database is not available
/// </summary>
public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        
        // Use a temporary connection string for design-time
        var connectionString = "Server=localhost;Port=3306;Database=emis_identity;User=root;Password=EMISPassword123!;";
        
        // Use specific MySQL version to avoid connection during migration
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 26)));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
