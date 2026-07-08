using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence;

public sealed class DbFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_db.GetConnectionString())
            .Options);

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();
}
