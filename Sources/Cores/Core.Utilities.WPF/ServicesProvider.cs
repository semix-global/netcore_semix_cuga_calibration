using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using ScottPlot;

namespace Core.Utilities.WPF;

public static class ServicesProvider
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IPlotControl>(sp =>
        {
            PlotControl? plotControl = null;
            sp.GetRequiredService<ISynchronizationContextProvider>().Send(_ => plotControl = new PlotControl(), null);

            return GuardUtils.IsNotNullAndReturn(plotControl);
        });

        return services;
    }
}