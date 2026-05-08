using CommunityToolkit.Diagnostics;
using Core.Models.Extensions;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System.Reflection;
using CugaCalibration.ViewModels;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationStatusService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationStatusServiceImpl(ICacheProvider cacheProvider) : ICalibrationStatusService
{
    private const int TimeoutSecond = 60;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public T GetCache<T>(CancellationToken cancellationToken) where T : CalibrationCacheBase, new() => Invoke( () => 
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == typeof(T));

        if (Guard.IsAssignableToTypeAndReturn<CalibrationCacheBase?>(entry.Cookie.Cache) is not null) return (T)entry.Cookie.Cache;

        if (cacheProvider.TryGetOrDefault(entry.CacheType, out var cache) == false)
        {
            cache = Activator.CreateInstance(entry.CacheType);

            cacheProvider.Set(entry.CacheType, cache, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), cache);

        return (T)cache;
    }, cancellationToken);

    public T GetCalibration<T>(CancellationToken cancellationToken) where T : CalibrationDTOBase, new()=> Invoke( () => 
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == typeof(T) && e.IsArray == false);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase?>(entry.Cookie.Calibration) is not null) return (T)entry.Cookie.Calibration;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefault(entry.DTOType, out var calibration) == false)
        {
            calibration = Activator.CreateInstance(entry.DTOType);

            cacheProvider.Set(entry.DTOType, calibration, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), calibration);

        return (T)calibration;
    }, cancellationToken);

    public T[] GetCalibrations<T>(CancellationToken cancellationToken) where T : CalibrationDTOBase, new()=> Invoke( () => 
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == typeof(T) && e.IsArray);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase[]?>(entry.Cookie.Calibrations) is not null) return (T[])entry.Cookie.Calibrations;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefaultArray(entry.DTOType, out var calibrations) == false)
            cacheProvider.SetArray(entry.DTOType, calibrations, cancellationToken);

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        return (T[])calibrations;
    }, cancellationToken);

    public void SetCache<T>(T value,CancellationToken cancellationToken) where T : CalibrationCacheBase, new()=> Invoke( () => 
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == typeof(T));

        cacheProvider.Set(value, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), value);
        
        return Unit.Default;
    }, cancellationToken);

    public void SetCalibration<T>(T value,CancellationToken cancellationToken) where T : CalibrationDTOBase, new()=> Invoke( () => 
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == typeof(T) && e.IsArray == false);

        cacheProvider.Set(value, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), value);
        
        return Unit.Default;
    }, cancellationToken);

    public void SetCalibrations<T>(T[] value,CancellationToken cancellationToken) where T : CalibrationDTOBase, new()=> Invoke( () => 
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == typeof(T) && e.IsArray);

        cacheProvider.SetArray(typeof(T), value.Cast<object>().ToArray(), cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), value);

        return Unit.Default;
    }, cancellationToken);

    public bool GetCalibrationDtoIsOKStatus<T>(out T calibrationDto, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new()
    {
        try
        {
            calibrationDto = cacheProvider.GetOrDefault<T>();
            var type = calibrationDto.GetType();
            errorMessage = nameof(type.Name);

            var extensionType = typeof(CoreWcfModelsExtension);
            // 获取方法信息
            var methodInfo = extensionType.GetMethods()
                .FirstOrDefault(m => m.Name == nameof(CoreWcfModelsExtension.IsOk) && m.GetParameters()[0].ParameterType == type);

            if (methodInfo is null)
            {
                errorMessage = "IsOk Extension Method not found!";
                return false;
            }

            // 调用方法
            var parameters = new object[] { calibrationDto, null! };
            var result = methodInfo.Invoke(null, parameters);
            errorMessage = parameters[1].ToString();
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            calibrationDto = null!;
            return false;
        }
    }

    public bool GetCalibrationDtoItemsIsOKStatus<T>(out T[] calibrationDtoItems, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new()
    {
        try
        {
            calibrationDtoItems = cacheProvider.GetOrDefaultArray<T>();
            var type = calibrationDtoItems.GetType();
            errorMessage = nameof(type.Name);

            var extensionType = typeof(CoreWcfModelsExtension);

            // 获取方法信息
            var methodInfo = extensionType.GetMethods()
                .FirstOrDefault(m => m.Name == nameof(CoreWcfModelsExtension.IsOk) && m.GetParameters()[0].ParameterType == type);

            if (methodInfo is null)
            {
                errorMessage = "IsOk Extension Method not found!";
                return false;
            }

            // 调用方法
            var parameters = new object[] { calibrationDtoItems, null! };
            var result = methodInfo.Invoke(null, parameters);
            errorMessage = parameters[1].ToString();
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            calibrationDtoItems = null!;
            errorMessage = ex.Message;
            return false;
        }
    }

    private T Invoke<T>(Func<T> func, CancellationToken cancellationToken)
    {
        var isRelease = false;
        try
        {
            isRelease = _semaphore.Wait(TimeSpan.FromSeconds(TimeoutSecond), cancellationToken);

            return isRelease ? func() : ThrowHelper.ThrowTimeoutException<T>();
        }
        finally
        {
            if (isRelease) _semaphore.Release();
        }
    }
}