using Core.Models.Helper;
using Core.Services;
using Core.Utilities;
using CugaCalibration.Core;
using CugaCalibrationTest.Views;
using Local.NoSQL.DB.Providers;
using Local.SQL.DB.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;
using NLog.Extensions.Hosting;
using NLog.Extensions.Logging;
using SourceGenerator.InjectHostDI;
using System.Globalization;
using System.Windows;
using Net.Utilities.ScottPlot.WPF;
using SourceGenerator.AssemblyMetadata;

namespace CugaCalibrationTest;

public sealed partial class App
{
    [STAThread]
    private static void Main(string[] args)
    {
        var app = new App();
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services
                    .Configure<ApplicationSetting>(context.Configuration.GetSection(BaseApplicationSetting.AppSetting))
                    .AddMvvmService(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, CugaCalibrationTestAssemblyMetadata.Version, app, context.HostingEnvironment)
                    .AddSqlDbContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.SqlDbDataSource, context.HostingEnvironment)
                    .AddNoSQLDBContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.NosqlDbDataSource, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddKeyedNoSQLDBContext(CalibrationConstantsHelper.RecipeDbKey, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
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

        app.InitializeComponent();
        app.MainWindow = HostApplication.GetRequiredService<MainWindow>();
        app.MainWindow.Visibility = Visibility.Visible;
        app.Startup += async (_, _) => { await host.StartAsync().ConfigureAwait(false); };
        app.Exit += async (_, _) => { await host.StopAsync().ConfigureAwait(false); };
        app.Run();
    }

    public App()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
    }
}