using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Helper.IOC.Providers;
using System.Reflection;

namespace Net.Utilities.WPF.MVVM;

public static class HostApplication
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public static ISynchronizationContextProvider ContextProvider { get; private set; } = null!;

    public static IHost ConfigureHostApplication(this IHost host)
    {
        ServiceProvider = host.Services;
        ContextProvider = ServiceProvider.GetRequiredService<ISynchronizationContextProvider>();

        return host;
    }

    public static T GetRequiredService<T>() where T : class
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public static object GetRequiredService(Type type)
    {
        return ServiceProvider.GetRequiredService(type);
    }

    public static T? GetRequiredService<T>(string className) where T : class
    {
        return GetRequiredService(className) as T;
    }

    public static object? GetRequiredService(string className)
    {
        if (string.IsNullOrWhiteSpace(className)) return null;

        var site = ServiceProvider.GetType().GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(ServiceProvider);
        return site?.GetType().GetField("_descriptors", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(site) is not ServiceDescriptor[] descriptors
            ? null
            : descriptors.Where(t => t.ServiceType.FullName == className).Select(s => GetRequiredService(s.ServiceType)).FirstOrDefault();
    }
}