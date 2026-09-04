using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGenerator.InjectHostDI;

namespace Net.Utilities.Calibration;

public static class NetUtilitiesCalibrationServiceProvider
{
    public static IServiceCollection AddNetUtilitiesCalibrationService(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddNetUtilitiesCalibrationInjectHostDI(hostEnvironment);

        return services;
    }
}