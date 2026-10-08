using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Core.Algorithm.Implements;

[IOCAppService(ServiceType = typeof(IAlgorithmEngines), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development | IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class AlgorithmEngines(IServiceProvider serviceProvider) : IAlgorithmEngines
{
    public ITemplateMatchEngine TemplateMatch(AlgorithmEngineTypeEnum engine) => serviceProvider.GetRequiredKeyedService<ITemplateMatchEngine>(engine);
}

