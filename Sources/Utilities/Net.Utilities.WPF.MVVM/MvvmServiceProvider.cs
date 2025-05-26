using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Helper.IOC.Providers.Impl;
using Net.Utilities.Models;
using SourceGenerator.InjectHostDI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Net.Utilities.WPF.MVVM;

public static class MvvmServiceProvider
{
    public static IServiceCollection AddMvvmService(this IServiceCollection services, IHostEnvironment hostEnvironment, Application application, string version)
    {
        services.AddSingleton(_ => new Frame { NavigationUIVisibility /*不显示导航UI*/ = NavigationUIVisibility.Hidden });
        services.AddNetUtilitiesWPFMVVMInjectHostDI(hostEnvironment);

        services.AddSingleton<IMessenger, WeakReferenceMessenger>(_ => WeakReferenceMessenger.Default); // 注册消息中心
        services.AddSingleton<ISynchronizationContextProvider>(_ => new SynchronizationContextProvider(new DispatcherSynchronizationContext(application.Dispatcher))); // 注册主线程调度器的同步上下文

        services.AddSingleton(sp =>
        {
            var appSettingOptions = sp.GetRequiredService<IOptions<ApplicationSetting>>();
            return $"{appSettingOptions.Value.AppName} V{version}";
        });

        return services;
    }
}