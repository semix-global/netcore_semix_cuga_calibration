using Core.Models.Exceptions;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CugaCalibration.Core.Services.Implements;

/// <summary>
/// 控制器参数防呆. 由 ViewModel 直接构造, 状态属于调用方
/// </summary>
/// <typeparam name="T">参数值类型</typeparam>
public sealed class ControllerGuard<T> : IControllerGuard<T>
{
    /// <summary>
    /// 读控制器值方法
    /// </summary>
    private readonly Func<T> _readFunc;

    /// <summary>
    /// 写控制器值方法
    /// </summary>
    private readonly Action<T> _writeFunc;

    private readonly ILogger _logger;
    private readonly Action<string> _notify;

    /// <summary>
    /// 控制器初始值
    /// </summary>
    private T _cacheValue = default!;

    /// <summary>
    /// 控制器写入值
    /// </summary>
    private T _writeValue = default!;

    /// <summary>
    /// 是否已记录控制器初始值缓存
    /// </summary>
    private bool _isCacheRecorded;

    /// <summary>
    /// 是否已经记录写入值
    /// </summary>
    private bool _hasWriteValue;

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="name">参数名</param>
    /// <param name="readFunc">从控制器读取当前值</param>
    /// <param name="writeFunc">向控制器写入值, 调用方应自带回读校验</param>
    /// <param name="logger">日志</param>
    /// <param name="notify">重置失败时的错误信息</param>
    public ControllerGuard(string name, Func<T> readFunc, Action<T> writeFunc, ILogger logger, Action<string> notify)
    {
        Name = name;
        _readFunc = readFunc;
        _writeFunc = writeFunc;
        _logger = logger;
        _notify = notify;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ControllerGuardState State { get; private set; } = ControllerGuardState.Clean;

    /// <inheritdoc />
    public T CacheValue => _cacheValue;

    /// <inheritdoc />
    public void RecordControllerInitialCache()
    {
        RestoreControllerIfNotClean();
        _cacheValue = _readFunc.Invoke();
        _isCacheRecorded = true;
        _logger.LogInformation("{Name} Record Controller Initial Cache: {@Cache}", Name, _cacheValue);
    }

    /// <inheritdoc />
    public void RestoreController()
    {
        if (_isCacheRecorded == false) throw new CugaException($"{Name} Controller's initial cache is empty");

        try
        {
            _writeFunc.Invoke(_cacheValue);
            State = ControllerGuardState.Clean;
        }
        catch (Exception ex)
        {
            State = ControllerGuardState.Dirty;
            _logger.LogError(ex, "{Name} restore controller failed", Name);
            try
            {
                _notify.Invoke($"Failed to restore controller {Name}!");
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "{Name} restore notify failed", Name);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public void RestoreControllerIfNotClean()
    {
        if (State == ControllerGuardState.Clean) return;

        RestoreController();
    }

    /// <inheritdoc />
    public void WriteController(T writeValue)
    {
        RecordControllerInitialCache();
        _writeValue = writeValue;
        _hasWriteValue = true;
        try
        {
            _writeFunc.Invoke(writeValue);
            State = ControllerGuardState.Covered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Name} write controller failed", Name);
            Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public void WriteController()
    {
        if (_hasWriteValue == false) throw new CugaException($"{Name} Controller's historical write values is empty");

        if (State == ControllerGuardState.Covered) return;

        RestoreControllerIfNotClean();
        try
        {
            _writeFunc.Invoke(_writeValue);
            State = ControllerGuardState.Covered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Name} write controller failed", Name);
            Rollback();
            throw;
        }
    }

    private void Rollback()
    {
        try
        {
            RestoreController();
        }
        catch (Exception restoreEx)
        {
            _logger.LogError(restoreEx, "{Name} restore after write failed", Name);
        }
    }
}