using CommunityToolkit.Diagnostics;
using Core.Models;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Recipe.Services;
using Core.Services;
using Core.Utilities.WPF.Tray.Model;
using Core.Utilities.WPF.Tray.Service.Implements;
using Core.Utilities.WPF.Tray.Service.Interfaces;
using Core.Utilities.WPF.Tray.UI;
using CugaCalibration.Core;
using CugaCalibration.ViewModels;
using CugaCalibration.Views;
using Local.SQL.Cache.Providers;
using Local.SQL.DB.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Net.Utilities.Models;
using Net.Utilities.SourceGenerators.Calibration;
using Net.Utilities.WPF.MVVM;
using NLog;
using NLog.Extensions.Hosting;
using NLog.Extensions.Logging;
using Python.Runtime;
using SourceGenerator.AssemblyMetadata;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CugaCalibration;

public sealed partial class App
{
    private static readonly Logger Logger = LogManager.Setup().GetCurrentClassLogger();
    private static readonly ITrayService TrayService = new TrayService();

    [STAThread]
    private static void Main(string[] args)
    {
        var mutex = new Mutex(true, typeof(App).Namespace, out var create);
        if (create == false)
        {
            MessageBox.Show("The program is already running", typeof(App).Namespace, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var pythonDllFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\Python314\python314.dll");
            var pythonHome = Path.GetDirectoryName(pythonDllFilePath);
            Guard.IsTrue(File.Exists(pythonDllFilePath));
            Guard.IsTrue(Directory.Exists(pythonHome));

            Runtime.PythonDLL = pythonDllFilePath;
            PythonEngine.PythonHome = pythonHome;

            PythonEngine.Initialize();
            _ = PythonEngine.BeginAllowThreads();

            var app = new App();

#pragma warning disable IDE0079
#pragma warning disable IDISP004

            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services
                        .Configure<ApplicationSetting>(context.Configuration.GetSection(BaseApplicationSetting.AppSetting))
                        .AddMvvmService(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, CugaCalibrationSolutionAssemblyMetadata.Version, app, context.HostingEnvironment)
                        .AddSqlDbContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.SqlDbDataSource, context.HostingEnvironment)
                        .AddCacheContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.NosqlDbDataSource, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                        .AddRecipeService(context.HostingEnvironment)
                        .AddKeyedCacheContext(CalibrationConstantsHelper.RecipeDbKey, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                        .AddCoreService(context.HostingEnvironment)
                        .AddApplication(context.HostingEnvironment);
                })
                .UseNLog(new NLogProviderOptions { ReplaceLoggerFactory = true })
#if RELEASE
                .UseEnvironment(Environments.Production)
#endif
#if SIMULATOR
                .UseEnvironment(Environments.Development)
#endif
                .Build()
                .ConfigureHostApplication();

#pragma warning restore IDISP004
#pragma warning restore IDE0079

            app.InitializeComponent();
            app.MainWindow = HostApplication.GetRequiredService<MainWindow>();
            app.MainWindow.Visibility = Visibility.Visible;

            // 创建托盘图标实例并持有引用，避免被 GC 回收
            var trayViewModel = HostApplication.GetRequiredService<TrayViewModel>();
            var applicationCookies = HostApplication.GetRequiredService<ApplicationCookie>();
            var icon = (BitmapImage)app.FindResource("AppIcon");

            TrayService.Initialize(
                new TrayOptions
                {
                    ToolTip = $"{applicationCookies.ApplicationName}",
                    Icon = icon,
                    DataContext = trayViewModel,
                    ContextMenu = new TrayMenu
                    {
                        DataContext = trayViewModel
                    }
                });

            // todo: 等后续ScottPlot改造好移动到static中
            CugaCalibrationSolutionCalibrationViewModelEntriesCollector.Init();

#pragma warning disable IDE0079
#pragma warning disable VSTHRD101

            // ReSharper disable AccessToDisposedClosure

            app.Startup += async (_, _) =>
            {
                TaskScheduler.UnobservedTaskException += TaskSchedulerOmUnobservedTaskException; // Task线程内未捕获异常处理事件
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException; // 非UI线程未捕获异常处理事件
                app.DispatcherUnhandledException += AppOnDispatcherUnhandledException; // UI线程未捕获异常处理事件

                await host.StartAsync().ConfigureAwait(false);
            };
            app.Exit += async (_, _) =>
            {
                app.DispatcherUnhandledException -= AppOnDispatcherUnhandledException; // UI线程未捕获异常处理事件
                TaskScheduler.UnobservedTaskException -= TaskSchedulerOmUnobservedTaskException; // Task线程内未捕获异常处理事件
                AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException; // 非UI线程未捕获异常处理事件

                TrayService.Dispose();

                await host.StopAsync().ConfigureAwait(false);
            };

            // ReSharper restore AccessToDisposedClosure

#pragma warning restore VSTHRD101
#pragma warning restore IDE0079

            app.Run();
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Stopped program because of exception");
            MessageBox.Show($"Stopped program because of exception\r\n{ex.Message}{Environment.NewLine}{ex.StackTrace}",
                typeof(App).Namespace, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LogManager.Shutdown();
            PythonEngine.Shutdown();
            mutex.Dispose();
        }
    }

    public App()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
        // Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
        // Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
    }

    #region 全局异常捕获

    private static void AppOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // 处理线程异常
        Current.Dispatcher.Invoke(() => MessageBox.Show($"System Information: Exception not caught\r\n{e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}",
            typeof(App).Namespace, MessageBoxButton.OK, MessageBoxImage.Error), null);
        Logger.Fatal(e.Exception, "System Information: Exception not caught");
        e.Handled = true; // 继续运行程序
    }

    private static void TaskSchedulerOmUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // 处理线程异常
        Current.Dispatcher.Invoke(() => MessageBox.Show($"System Information: Exception not caught\r\n{e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}",
            typeof(App).Namespace, MessageBoxButton.OK, MessageBoxImage.Error), null);
        Logger.Fatal(e.Exception, "System Information: Exception not caught");
    }

    private static void CurrentDomainOnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        // 处理应用程序域中的异常
        if (e.ExceptionObject is Exception exception)
        {
            Current.Dispatcher.Invoke(() => MessageBox.Show($"System Information: Exception not caught\r\n{exception.Message}{Environment.NewLine}{exception.StackTrace}",
                typeof(App).Namespace, MessageBoxButton.OK, MessageBoxImage.Error), null);
            Logger.Fatal(exception, "System Information: Exception not caught");
            return;
        }

        // 发生了未处理的非托管异常
        Current.Dispatcher.Invoke(() => MessageBox.Show($"System Information: Exception not caught\r\nAn unhandled unmanaged exception occurred {e.ExceptionObject}",
            typeof(App).Namespace, MessageBoxButton.OK, MessageBoxImage.Error), null);
        Logger.Fatal($"System Information: Exception not caught(An unhandled unmanaged exception occurred){e.ExceptionObject}");
    }

    #endregion 全局异常捕获
}