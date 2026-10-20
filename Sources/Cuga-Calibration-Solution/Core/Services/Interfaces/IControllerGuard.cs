namespace CugaCalibration.Core.Services.Interfaces;

/// <summary>
/// 控制器重置的工作模式
/// </summary>
public enum ControllerGuardMode
{
    /// <summary>
    /// 失败或异常时恢复初始值
    /// </summary>
    RestoreOnFailure,

    /// <summary>
    /// 无论成功或失败都恢复初始值
    /// </summary>
    AlwaysRestore
}

/// <summary>
/// 控制器数据状态
/// </summary>
public enum ControllerGuardState
{
    /// <summary>
    /// 控制器与初始值一致
    /// </summary>
    Clean,

    /// <summary>
    /// 下发失败，控制器中遗留上一轮的脏数据
    /// </summary>
    Dirty,

    /// <summary>
    /// 下发成功，控制器已被下发值覆盖
    /// </summary>
    Covered
}

/// <summary>
/// 控制器下发防呆
/// </summary>
public interface IControllerGuard
{
    /// <summary>
    /// 参数名, 用于日志和恢复失败提示
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    ControllerGuardState State { get; }

    /// <summary>
    /// 记录控制器初始值，非 Clean 时需要先下发初始值, 再回读并记录
    /// </summary>
    void RecordControllerInitialCache();

    /// <summary>
    /// 将初始值写回控制器. 成功后状态为 Clean, 失败后状态为 Dirty
    /// </summary>
    void RestoreController();

    /// <summary>
    /// 非 Clean 时重置
    /// </summary>
    void RestoreControllerIfNotClean();
}

/// <summary>
/// 带值类型的控制器参数防呆
/// </summary>
/// <typeparam name="T">参数值类型</typeparam>
public interface IControllerGuard<T> : IControllerGuard
{
    /// <summary>
    /// 控制器初始值
    /// </summary>
    T CacheValue { get; }

    /// <summary>
    /// 下发指定值并记录初始值缓存，状态置为 Covered. 下发失败时立即重置
    /// </summary>
    /// <param name="writeValue">下发值</param>
    void WriteController(T writeValue);

    /// <summary>
    /// 下发历史缓存的指定值, 不更新初始值缓存. Covered状态下不下发
    /// </summary>
    void WriteController();
}

/// <summary>
/// 防呆执行上下文, 用于把弹窗推迟到恢复之后
/// </summary>
public interface IControllerGuardContext
{
    /// <summary>
    /// 注册在全部结束后才执行的动作
    /// </summary>
    /// <param name="action">推迟执行的动作</param>
    void Defer(Action action);
}