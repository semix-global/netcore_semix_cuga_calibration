using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Implements.Halcon;
using Core.Algorithm.Implements.Mock.AlgoCSharp;
using Core.Algorithm.Implements.Mock.Halcon;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGenerator.InjectHostDI;

namespace Core.Algorithm;

public static class AlgorithmServiceProvider
{
    public static IServiceCollection AddCoreAlgorithm(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddCoreAlgorithmInjectHostDI(hostEnvironment);

        if (hostEnvironment.IsDevelopment())
        {
            services.AddKeyedSingleton<ITemplateMatchEngine, HalconTemplateMatchEngineMock>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<ITemplateMatchEngine, AlgoCvTemplateMatchEngineMock>(AlgorithmEngineTypeEnum.AlgoCSharp);
        }
        else
        {
            services.AddKeyedSingleton<ITemplateMatchEngine, HalconTemplateMatchEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<ITemplateMatchEngine, AlgoCvTemplateMatchEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
        }

        return services;
    }
}
