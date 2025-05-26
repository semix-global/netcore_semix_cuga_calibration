using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.Enum;
using System.Reflection;

namespace Net.Utilities.Extensions;

public static class IOCServiceCollectionExtension
{
    /// <summary>
    /// 注册引用程序域中所有有AppService标记的类的服务
    /// </summary>
    public static IServiceCollection AddAppService(this IServiceCollection services, Assembly assembly, IHostEnvironment hostEnvironment)
    {
        foreach (var type in assembly.GetTypes())
        {
            var serviceAttribute = type.GetCustomAttribute<IOCAppServiceAttribute>();
            if (serviceAttribute is null) continue;

            var serviceType = serviceAttribute.ServiceType;

            if (serviceType is null && serviceAttribute.IsGetInterfaceServiceType) serviceType = type.GetInterfaces().FirstOrDefault(); // 情况1 适用于依赖抽象编程，注意这里只获取第一个
            serviceType ??= type; // 情况2 不常见特殊情况下才会指定ServiceType，写起来麻烦

            var iocEnvironmentEnums = EnumHelper.GetFlagsEnums(serviceAttribute.IOCEnvironmentEnum);
            // 如果包含当前环境注入
            switch (hostEnvironment.EnvironmentName)
            {
                case { } s when s == Environments.Development:
                    if (iocEnvironmentEnums.Contains(IOCEnvironmentEnum.Development) == false) continue;

                    Inject(serviceAttribute, serviceType, type);
                    break;

                case { } s when s == Environments.Staging:
                    if (iocEnvironmentEnums.Contains(IOCEnvironmentEnum.Staging) == false) continue;

                    Inject(serviceAttribute, serviceType, type);
                    break;

                case { } s when s == Environments.Production:
                    if (iocEnvironmentEnums.Contains(IOCEnvironmentEnum.Production) == false) continue;

                    Inject(serviceAttribute, serviceType, type);
                    break;
            }
        }

        return services;

        void Inject(IOCAppServiceAttribute serviceAttribute, Type serviceType, Type type)
        {
            switch (serviceAttribute.IOCLifetimeEnum)
            {
                case IOCLifeTimeEnum.Singleton:
                    services.AddSingleton(serviceType, type);
                    break;

                case IOCLifeTimeEnum.Scoped:
                    services.AddScoped(serviceType, type);
                    break;

                case IOCLifeTimeEnum.Transient:
                    services.AddTransient(serviceType, type);
                    break;

                default:
                    services.AddSingleton(serviceType, type);
                    break;
            }
        }
    }
}