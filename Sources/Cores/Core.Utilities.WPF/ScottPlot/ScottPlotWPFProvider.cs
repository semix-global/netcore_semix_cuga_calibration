using Core.Utilities.WPF.ScottPlot.WPF;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;

namespace Core.Utilities.WPF.ScottPlot;

public static class ScottPlotWPFProvider
{
    public static IServiceCollection AddScottPlotServices(this IServiceCollection services)
    {
        services.AddTransient<IScatterPlotControl>(sp =>
        {
            ScatterPlotControl? scatterPlotControl = null;
            sp.GetRequiredService<ISynchronizationContextProvider>().Send(_ => scatterPlotControl = new ScatterPlotControl(), null);

            return GuardUtils.IsNotNullAndReturn(scatterPlotControl);
        });

        return services;
    }
}