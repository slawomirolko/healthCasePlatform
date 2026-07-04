using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HealthCasePlatform.Infrastructure.Persistence;

public sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = AppContext.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var settings = configuration
            .GetSection(DatabaseSettings.SectionName)
            .Get<DatabaseSettings>() ?? new DatabaseSettings();

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException(
                $"{DatabaseSettings.SectionName}:{nameof(DatabaseSettings.ConnectionString)} is not configured. " +
                "Provide it in appsettings.json or via the Database__ConnectionString environment variable.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(settings.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
