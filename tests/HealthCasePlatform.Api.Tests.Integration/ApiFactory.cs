using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_db.GetConnectionString()));

            services.RemoveAll<IConnectionFactory>();
            services.AddSingleton<IConnectionFactory>(_ =>
                new ConnectionFactory { Uri = new Uri(_rabbitMq.GetConnectionString()) });

            services.AddAuthentication("Fake")
                .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Fake", _ => { });
        });
    }

    public HttpClient CreateClientWithRoles(params string[] roles)
    {
        var client = CreateClient();
        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(FakeAuthHandler.RolesHeader, string.Join(',', roles));
        }

        return client;
    }

    public HttpClient CreateClientAs(string userId, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(FakeAuthHandler.UserHeader, userId);
        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(FakeAuthHandler.RolesHeader, string.Join(',', roles));
        }

        return client;
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        await _rabbitMq.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _rabbitMq.DisposeAsync();
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
