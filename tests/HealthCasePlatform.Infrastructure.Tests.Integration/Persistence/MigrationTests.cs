using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence;

public sealed class MigrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    [Fact]
    public async Task MigrateAsync_ApplyFromEmptyThenReapply_IsIdempotent()
    {
        await using var first = CreateContext();
        await first.Database.MigrateAsync();

        await using var second = CreateContext();
        await second.Database.MigrateAsync();
    }

    private AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_db.GetConnectionString()).Options);

    public async Task InitializeAsync() => await _db.StartAsync();

    public async Task DisposeAsync() => await _db.DisposeAsync();
}
