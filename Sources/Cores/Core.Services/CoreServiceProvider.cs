using Core.Models.Models.Setting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGenerator.InjectHostDI;

namespace Core.Services;

public static class CoreServiceProvider
{
    public static IServiceCollection AddCoreService(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddCoreServicesInjectHostDI(hostEnvironment);

        services.AddSingleton(_ => new CalibrationSetting());

        return services;
    }
}