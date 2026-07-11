using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Infrastructure.Persistence;
using HealthCasePlatform.Infrastructure.Persistence.AuditStores;
using HealthCasePlatform.Infrastructure.Persistence.Mongo;
using HealthCasePlatform.Infrastructure.Persistence.NotificationStores;
using HealthCasePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HealthCasePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<AuditSettings>(configuration.GetSection(AuditSettings.SectionName));
        services.Configure<MongoAuditSettings>(configuration.GetSection(MongoAuditSettings.SectionName));

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var settings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseSqlServer(settings.ConnectionString);
        });

        services.AddScoped<ICaseRepository, CaseRepository>();

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
            services.AddScoped<INotificationWriter, SqlNotificationWriter>();
        }

        return services;
    }
}
