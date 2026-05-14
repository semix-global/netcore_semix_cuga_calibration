using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.ViewModels;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
    private const int TimeoutSecond = 60;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CalibrationCacheBase GetCache(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == type);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationCacheBase?>(entry.Cookie.Cache) is not null) return ObjectHelper.InvokeMethod<CalibrationCacheBase>(entry.Cookie.Cache, nameof(ICloneable<>.Clone));

        if (cacheProvider.TryGetOrDefault(entry.CacheType, out var cache) == false)
        {
            cache = Activator.CreateInstance(entry.CacheType);

            cacheProvider.Set(entry.CacheType, cache, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), cache);

        return ObjectHelper.InvokeMethod<CalibrationCacheBase>(cache, nameof(ICloneable<>.Clone));
    }, cancellationToken);

    public void SetCache(Type type, CalibrationCacheBase item, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == type);

        cacheProvider.Set(item, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), item);

        return Unit.Default;
    }, cancellationToken);


    public CalibrationDTOBase GetCalibration(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray == false);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase?>(entry.Cookie.Calibration) is not null) return entry.Cookie.Calibration;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefault(entry.DTOType, out var calibration) == false)
        {
            calibration = Activator.CreateInstance(entry.DTOType);

            cacheProvider.Set(entry.DTOType, calibration, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), calibration);

        return (CalibrationDTOBase)calibration;
    }, cancellationToken);

    public void SetCalibration(Type type, CalibrationDTOBase item, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray == false);

        cacheProvider.Set(type, item, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), item);

        return Unit.Default;
    }, cancellationToken);

    public CalibrationDTOBase[] GetCalibrations(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase[]?>(entry.Cookie.Calibrations) is not null) return entry.Cookie.Calibrations;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefaultArray(entry.DTOType, out var calibrations) == false)
            cacheProvider.SetArray(entry.DTOType, calibrations, cancellationToken);

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        return (CalibrationDTOBase[])calibrations;
    }, cancellationToken);

    public void SetCalibrations(Type type, CalibrationDTOBase[] items, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        cacheProvider.SetArray(type, items.Cast<object>().ToArray(), cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), items);

        return Unit.Default;
    }, cancellationToken);

    private T Invoke<T>(Func<T> func, CancellationToken cancellationToken)
    {
        var isRelease = false;
        try
        {
            logger.LogTrace("Semaphore waiting for {FuncName} with timeout of {TimeoutSecond} seconds...", func.Method.Name, TimeoutSecond);

            isRelease = _semaphore.Wait(TimeSpan.FromSeconds(TimeoutSecond), cancellationToken);

            return isRelease ? func() : ThrowHelper.ThrowTimeoutException<T>();
        }
        finally
        {
            if (isRelease) _semaphore.Release();

            logger.LogTrace("Semaphore released for {FuncName} with timeout of {TimeoutSecond} seconds...", func.Method.Name, TimeoutSecond);
        }
    }
}