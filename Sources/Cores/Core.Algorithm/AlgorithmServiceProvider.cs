using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Implements.Halcon;
using Core.Algorithm.Implements.Mock.AlgoCSharp;
using Core.Algorithm.Implements.Mock.Halcon;
using Core.Algorithm.Interfaces.BestFocus;
using Core.Algorithm.Interfaces.PixelSize;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Interfaces.YPixelSize;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Calibration;
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
            services.AddKeyedSingleton<IBestFocusEngine, HalconBestFocusEngineMock>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<IBestFocusEngine, AlgoCvBestFocusEngineMock>(AlgorithmEngineTypeEnum.AlgoCSharp);
            services.AddKeyedSingleton<IYPixelSizeEngine, HalconYPixelSizeEngineMock>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<IYPixelSizeEngine, AlgoCvYPixelSizeEngineMock>(AlgorithmEngineTypeEnum.AlgoCSharp);
            services.AddKeyedSingleton<IPixelSizeEngine, HalconPixelSizeEngineMock>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<IPixelSizeEngine, AlgoCvPixelSizeEngineMock>(AlgorithmEngineTypeEnum.AlgoCSharp);
        }
        else
        {
            services.AddKeyedSingleton<ITemplateMatchEngine, HalconTemplateMatchEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<ITemplateMatchEngine, AlgoCvTemplateMatchEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
            services.AddKeyedSingleton<IBestFocusEngine, HalconBestFocusEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<IBestFocusEngine, AlgoCvBestFocusEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
            services.AddKeyedSingleton<IYPixelSizeEngine, HalconYPixelSizeEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<IYPixelSizeEngine, AlgoCvYPixelSizeEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
            services.AddKeyedSingleton<IPixelSizeEngine, HalconPixelSizeEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
            services.AddKeyedSingleton<IPixelSizeEngine, AlgoCvPixelSizeEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
        }

        return services;
    }
}