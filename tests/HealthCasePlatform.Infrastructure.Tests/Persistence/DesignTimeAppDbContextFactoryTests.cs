using HealthCasePlatform.Infrastructure.Persistence;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Persistence;

public sealed class DesignTimeAppDbContextFactoryTests
{
    private const string ConnectionStringEnvVar = "Database__ConnectionString";

    [Fact]
    public void CreateDbContext_WhenConnectionStringConfigured_ReturnsSqlServerContext()
    {
        Environment.SetEnvironmentVariable(ConnectionStringEnvVar, "Server=localhost;Database=Test;Trusted_Connection=True;TrustServerCertificate=True");

        try
        {
            var factory = new DesignTimeAppDbContextFactory();
            using var context = factory.CreateDbContext(Array.Empty<string>());

            context.Database.ProviderName.ShouldBe("Microsoft.EntityFrameworkCore.SqlServer");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, null);
        }
    }
}
