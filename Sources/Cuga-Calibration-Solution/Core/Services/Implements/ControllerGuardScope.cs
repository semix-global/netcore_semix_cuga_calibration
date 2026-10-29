using Core.Models.Exceptions;
using CugaCalibration.Core.Services.Interfaces;

namespace CugaCalibration.Core.Services.Implements;

/// <summary>
/// 控制器下发防呆的多参数执行器
/// </summary>
public static class ControllerGuardScope
{
    /// <summary>
    /// 按注册顺序取快照并执行, 恢复时逆序且各自独立
    /// </summary>
    /// <param name="guards">控制器防呆对象</param>
    /// <param name="func">业务</param>
    /// <param name="mode">重置模式</param>
    /// <param name="recordCache">是否执行前重新记录控制器初始值（记录回滚点），参数效果（true：异常时回滚到执行器新记录的初始值，false：异常时回滚到历史记录的初始值）</param>
    /// <returns></returns>
    public static async Task<bool> RunAsync(
        IControllerGuard[] guards,
        Func<IControllerGuardContext, Task<bool>> func,
        ControllerGuardMode mode,
        bool recordCache = true)
    {
        if (recordCache)
        {
            foreach (var guard in guards) guard.RecordControllerInitialCache();
        }

        var context = new ControllerGuardContext();
        var result = false;
        Exception? original = null;
        try
        {
            result = await func.Invoke(context).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            original = ex;
            throw;
        }
        finally
        {
            var shouldRestore = mode == ControllerGuardMode.AlwaysRestore || original != null || result == false;
            Exception? restoreException = null;
            if (shouldRestore) restoreException = TryRestoreAll(guards, isIgnoreCleanStatus: false);

            context.RunDeferred();

            if (restoreException != null && original is null)
                throw new CugaException("Failed to restore the controller", restoreException);
        }
    }

    /// <summary>
    /// 重置非 Clean状态下的控制器
    /// </summary>
    /// <param name="guards"></param>
    public static void RestoreControllers(params IControllerGuard[] guards)
    {
        var restoreException = TryRestoreAll(guards, isIgnoreCleanStatus: true);
        if (restoreException != null)
            throw new CugaException("Failed to restore the controller parameter", restoreException);
    }

    /// <summary>
    /// 重置控制器集合
    /// </summary>
    /// <param name="guards"></param>
    /// <param name="isIgnoreCleanStatus">true：只重置非clean状态下的控制器，false：显式重置</param>
    /// <returns></returns>
    private static Exception? TryRestoreAll(IReadOnlyList<IControllerGuard> guards, bool isIgnoreCleanStatus)
    {
        List<Exception> exceptions = [];
        for (var i = guards.Count - 1; i >= 0; i--)
        {
            var guard = guards[i];
            if (isIgnoreCleanStatus && guard.State == ControllerGuardState.Clean) continue;

            try
            {
                guard.RestoreController();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count == 0) return null;

        if (exceptions.Count == 1) return exceptions[0];

        return new AggregateException(exceptions);
    }

    private sealed class ControllerGuardContext : IControllerGuardContext
    {
        private readonly List<Action> _deferred = [];

        public void Defer(Action action)
        {
            _deferred.Add(action);
        }

        public void RunDeferred()
        {
            foreach (var action in _deferred) action.Invoke();
        }
    }
}