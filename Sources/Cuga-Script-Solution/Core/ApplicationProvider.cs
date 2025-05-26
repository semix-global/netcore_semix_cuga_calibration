using CugaCalibration.Core.Models;
using CugaCalibration.ViewModels.Common;
using CugaScript.Core.Models;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SourceGenerator.InjectHostDI;

namespace CugaScript.Core;

public static class ApplicationProvider
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddSingleton(sp =>
        {
            var applicationName = sp.GetRequiredService<string>();
            var sysUserDto = sp.GetRequiredService<SysUserDto>();
            return new ApplicationCookie
            {
                ApplicationName = applicationName,
                SysUser = sysUserDto,
                CalibrationMenu = new CalibrationMenu(),
                TitleMenu = new SysMenuDto()
            };
        }); // cookie
        services.AddSingleton(sp =>
        {
            var adsViewModel = sp.GetRequiredService<AdsViewModel>();
            var afViewModel = sp.GetRequiredService<AfViewModel>();
            var laserViewModel = sp.GetRequiredService<LaserViewModel>();
            var microscopeViewModel = sp.GetRequiredService<MicroscopeViewModel>();
            var reviewViewModel = sp.GetRequiredService<ReviewViewModel>();
            var stageViewModel = sp.GetRequiredService<StageViewModel>();
            var logger = sp.GetRequiredService<ILogger<ScriptParameter>>();
            return new ScriptParameter
            {
                AdsViewModel = adsViewModel,
                AfViewModel = afViewModel,
                LaserViewModel = laserViewModel,
                MicroscopeViewModel = microscopeViewModel,
                ReviewViewModel = reviewViewModel,
                StageViewModel = stageViewModel,
                Logger = logger
            };
        }); // script parameter
        services.AddCugaScriptSolutionInjectHostDI(hostEnvironment);

        return services;
    }
}