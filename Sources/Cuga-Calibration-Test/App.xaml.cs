using Core.Models.Helper;
using Core.Recipe.Services;
using Core.Services;
using Core.Utilities;
using CugaCalibration.Core;
using CugaCalibrationTest.Views;
using Local.SQL.Cache.Providers;
using Local.SQL.DB.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Net.Utilities.Models;
using Net.Utilities.ScottPlot.WPF;
using Net.Utilities.WPF.MVVM;
using NLog.Extensions.Hosting;
using NLog.Extensions.Logging;
using SourceGenerator.AssemblyMetadata;
using SourceGenerator.InjectHostDI;
using System.Globalization;
using System.Windows;

namespace CugaCalibrationTest;

public sealed partial class App
{
    [STAThread]
    private static void Main(string[] args)
    {
        var app = new App();

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services
                    .Configure<ApplicationSetting>(context.Configuration.GetSection(BaseApplicationSetting.AppSetting))
                    .AddMvvmService(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, CugaCalibrationTestAssemblyMetadata.Version, app, context.HostingEnvironment)
                    .AddSqlDbContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.SqlDbDataSource, context.HostingEnvironment)
                    .AddCacheContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.NosqlDbDataSource, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddRecipeService(context.HostingEnvironment)
                    .AddKeyedCacheContext(CalibrationConstantsHelper.RecipeDbKey, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddScottPlotServices()
                    .AddCoreService(context.HostingEnvironment)
                    .AddApplication(context.HostingEnvironment)
                    .AddCugaCalibrationTestInjectHostDI(context.HostingEnvironment);
            })
            .UseNLog(new NLogProviderOptions { ReplaceLoggerFactory = true })
#if RELEASE
            .UseEnvironment(Environments.Production)
#endif
            .Build()
            .ConfigureHostApplication();

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        app.InitializeComponent();
        app.MainWindow = HostApplication.GetRequiredService<MainWindow>();
        app.MainWindow.Visibility = Visibility.Visible;

        // ReSharper disable AccessToDisposedClosure

        app.Startup += async (_, _) => { await host.StartAsync().ConfigureAwait(false); };
        app.Exit += async (_, _) => { await host.StopAsync().ConfigureAwait(false); };

        // ReSharper restore AccessToDisposedClosure

        app.Run();
    }

    public App()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
    }
}