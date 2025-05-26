using Net.Utilities.Enums;

// ReSharper disable InconsistentNaming

namespace Net.Utilities.Attributes;

/// <summary>
/// 标记服务
/// 如何使用？
/// 1、如果服务是本身 直接在类上使用[AppService]
/// 2、如果服务是接口 在类上使用 [AppService(ServiceType = typeof(实现接口))]
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IOCAppServiceAttribute : Attribute
{
    /// <summary>
    /// 服务声明周期
    /// 不给默认值的话注册的是AddSingleton
    /// </summary>
    public IOCLifeTimeEnum IOCLifetimeEnum { get; set; } = IOCLifeTimeEnum.Singleton;

    /// <summary>
    /// 什么环境下注册
    /// </summary>
    public IOCEnvironmentEnum IOCEnvironmentEnum { get; set; } = IOCEnvironmentEnum.Development | IOCEnvironmentEnum.Staging | IOCEnvironmentEnum.Production;

    /// <summary>
    /// 指定服务类型
    /// </summary>
    public Type? ServiceType { get; set; }

    /// <summary>
    /// 是否可以从第一个接口获取服务类型
    /// </summary>
    public bool IsGetInterfaceServiceType { get; set; }
}