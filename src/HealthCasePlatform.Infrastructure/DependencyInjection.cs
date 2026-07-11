using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Cases.Messaging;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Outbox;
using HealthCasePlatform.Infrastructure.Persistence;
using HealthCasePlatform.Infrastructure.Persistence.AuditStores;
using HealthCasePlatform.Infrastructure.Persistence.Mongo;
using HealthCasePlatform.Infrastructure.Persistence.NotificationStores;
using HealthCasePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace HealthCasePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<AuditSettings>(configuration.GetSection(AuditSettings.SectionName));
        services.Configure<MongoAuditSettings>(configuration.GetSection(MongoAuditSettings.SectionName));

        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        services.AddOptions<RabbitMqSettings>().ValidateOnStart();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var settings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseSqlServer(settings.ConnectionString);
        });

        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<INotificationWriter, SqlNotificationWriter>();

        var audit = configuration.GetSection(AuditSettings.SectionName).Get<AuditSettings>() ?? new AuditSettings();

        if (audit.Provider == "MongoDb")
        {
            var mongo = configuration.GetSection(MongoAuditSettings.SectionName).Get<MongoAuditSettings>() ?? new MongoAuditSettings();
            AuditEntryClassMap.Register();
            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongo.ConnectionString));
            services.AddScoped<IAuditLogWriter>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                var db = client.GetDatabase(mongo.Database);
                return new MongoAuditLogWriter(db, mongo.CollectionName);
            });
        }
        else
        {
            services.AddScoped<IAuditLogWriter, SqlAuditLogWriter>();
        }

        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
            return new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost
            };
        });

        services.AddSingleton<RabbitMqCaseSubmittedPublisher>();
        services.AddScoped<ICaseEventOutbox, SqlCaseEventOutbox>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<CaseSubmittedConsumer>();

        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", failureStatus: HealthStatus.Degraded);

        return services;
    }
}
