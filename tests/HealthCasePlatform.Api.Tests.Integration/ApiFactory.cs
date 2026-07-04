using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_db.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
