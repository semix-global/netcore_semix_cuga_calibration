using Core.Models.Models.Setting;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGenerator.InjectHostDI;

namespace Core.Services;

public static class CoreServiceProvider
{
    public static IServiceCollection AddCoreService(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddCoreServicesInjectHostDI(hostEnvironment);

        services.AddSingleton(sp =>
        {
            var cacheProvider = sp.GetRequiredService<ICacheProvider>();

            return cacheProvider.Get<CalibrationSetting>() ?? new CalibrationSetting();
        });

        return services;
    }
}