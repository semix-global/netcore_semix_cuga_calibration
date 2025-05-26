namespace Net.Utilities.Helper.IOC.Providers;

public interface ISynchronizationContextProvider
{
    /// <summary>
    /// 主线程上下文同步执行方法
    /// </summary>
    /// <param name="d">执行方法</param>
    void Send(Action d);

    /// <summary>
    /// 主线程上下文异步执行方法
    /// </summary>
    /// <param name="d">执行方法</param>
    void Post(Action d);

    /// <summary>
    /// 主线程上下文同步执行方法
    /// </summary>
    /// <param name="d">执行方法</param>
    /// <param name="state">方法参数</param>
    void Send(Action<object?> d, object? state);

    /// <summary>
    /// 主线程上下文异步执行方法
    /// </summary>
    /// <param name="d">执行方法</param>
    /// <param name="state">方法参数</param>
    void Post(Action<object?> d, object? state);
}