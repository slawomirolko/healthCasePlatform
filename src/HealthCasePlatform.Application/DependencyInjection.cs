using HealthCasePlatform.Application.Cases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthCasePlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICaseService, CaseService>();
        return services;
    }
}
