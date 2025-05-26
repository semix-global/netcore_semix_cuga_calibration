using CugaCalibration.Core.Models;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGenerator.InjectHostDI;

namespace CugaCalibration.Core;

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
        services.AddCugaCalibrationSolutionInjectHostDI(hostEnvironment);

        return services;
    }
}