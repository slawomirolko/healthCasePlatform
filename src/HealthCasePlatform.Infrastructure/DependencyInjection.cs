using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Infrastructure.Persistence;
using HealthCasePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthCasePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var settings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseSqlServer(settings.ConnectionString);
        });

        services.AddScoped<ICaseRepository, CaseRepository>();

        return services;
    }
}
