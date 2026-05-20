using Core.Models.Models.Common.Cookies;
using CugaCalibration.ViewModels.Common.Windows.View;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGenerator.InjectHostDI;
using System.Collections.Concurrent;

namespace CugaCalibration.Core;

public static class ApplicationProvider
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddSingleton(sp =>
        {
            var applicationName = sp.GetRequiredService<string>();
            var sysUserDto = sp.GetRequiredService<SysUserDTO>();

            var applicationCookie = new ApplicationCookie
            {
                ApplicationName = applicationName,
                SysUser = sysUserDto,
                CalibrationMenu = new CalibrationMenu(),
                TitleMenu = new SysMenuDTO()
            };
            return applicationCookie;
        }); // cookie

        services.AddSingleton<ConcurrentDictionary<Type, CalibrationMenuWindowViewModel>>();
        services.AddSingleton<Func<Type, CalibrationMenuWindowViewModel>>(sp => key =>
        {
            var cache = sp.GetRequiredService<ConcurrentDictionary<Type, CalibrationMenuWindowViewModel>>();

            return cache.GetOrAdd(key, _ => ActivatorUtilities.CreateInstance<CalibrationMenuWindowViewModel>(sp));
        });

        services.AddCugaCalibrationSolutionInjectHostDI(hostEnvironment);

        return services;
    }
}