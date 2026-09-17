using System.Windows;
using Core.Models;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Recipe.Services;
using Core.Services;
using CugaCalibration.Core;
using CugaCalibration.ViewModels.Common;
using Local.SQL.Cache.Providers;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Calibration;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;
using SourceGenerator.AssemblyMetadata;

namespace CugaCalibrationUnitTest;

public sealed class HostFixture : IDisposable
{
    private static readonly Application Application = new();

    public readonly IHost Host;

    public HostFixture()
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP004

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((context, services) =>
            {
                services
                    .Configure<ApplicationSetting>(context.Configuration.GetSection(BaseApplicationSetting.AppSetting))
                    .AddMvvmService(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, CugaCalibrationUnitTestAssemblyMetadata.Version, Application, context.HostingEnvironment)
                    .AddSqlDbContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.SqlDbDataSource, context.HostingEnvironment)
                    .AddCacheContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.NosqlDbDataSource, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddNetUtilitiesCalibrationService(context.HostingEnvironment)
                    .AddRecipeService(context.HostingEnvironment)
                    .AddKeyedCacheContext(CalibrationConstantsHelper.RecipeDbKey, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddCoreService(context.HostingEnvironment)
                    .AddApplication(context.HostingEnvironment);
            })
            .UseEnvironment(Environments.Development)
            .Build()
            .ConfigureHostApplication();

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        var microscopeLensInformations = HostApplication.GetRequiredService<MicroscopeViewModel>().GetMicroscopeLensInformations();
        var laserLightInformations = HostApplication.GetRequiredService<LaserViewModel>().GetLaserLightInformations();
        var productivityInformations = HostApplication.GetRequiredService<OpticsViewModel>().GetProductivityInformations();
        var cibInformations = HostApplication.GetRequiredService<CIBViewModel>().GetCIBInformations();

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        applicationCookie.MicroscopeLensInformations = [.. microscopeLensInformations.Select(t => t.Clone())];
        applicationCookie.LaserLightInformations = [.. laserLightInformations.Select(t => t.Clone())];
        applicationCookie.ProductivityInformations = [.. productivityInformations.Select(t => t.Clone())];
        applicationCookie.CIBInformations = [.. cibInformations.Select(t => t.Clone())];
    }

    public void Dispose()
    {
        HostApplication.GetRequiredService<IFreeSql>().Dispose();
        HostApplication.GetRequiredService<ICacheProvider>().Dispose();
        HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey).Dispose();
        Host.Dispose();
    }
}