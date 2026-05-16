using System.Reflection;
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

        if (ReferenceEquals(entry.Cookie.Cache, ObjectHelper.GetFieldValue(type, null, nameof(CalibrationCacheBase<>.Default), flags: ObjectHelper.AllBindingFlags | BindingFlags.FlattenHierarchy)) == false) return ObjectHelper.InvokeMethod<CalibrationCacheBase>(entry.Cookie.Cache, nameof(ICloneable<>.Clone), null);

        if (reciCacheProvider.TryGetOrDefault(entry.CacheType, out var cache) == false)
        {
            cache = Guard.IsNotNullAndReturn(Activator.CreateInstance(entry.CacheType));

            reciCacheProvider.Set(entry.CacheType, cache, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), cache);

        return ObjectHelper.InvokeMethod<CalibrationCacheBase>(cache, nameof(ICloneable<>.Clone), null);
    }, cancellationToken);

    public void SetCache(Type type, CalibrationCacheBase item, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == type);

        reciCacheProvider.Set(item, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), item);

        return Unit.Default;
    }, cancellationToken);


    public CalibrationDTOBase GetCalibration(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray == false);

        if (ReferenceEquals(entry.Cookie.Calibration, ObjectHelper.GetFieldValue(type, null, nameof(CalibrationDTOBase<>.Default), flags: ObjectHelper.AllBindingFlags | BindingFlags.FlattenHierarchy)) == false) return entry.Cookie.Calibration;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefault(entry.DTOType, out var calibration) == false)
        {
            calibration = Guard.IsNotNullAndReturn(Activator.CreateInstance(entry.DTOType));

            cacheProvider.Set(entry.DTOType, calibration, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), calibration);

        calibrationViewModel.UpdateEntryStatus(cancellationToken);

        return (CalibrationDTOBase)calibration;
    }, cancellationToken);

    public void SetCalibration(Type type, CalibrationDTOBase item, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray == false);

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        cacheProvider.Set(type, item, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), item);

        calibrationViewModel.UpdateEntryStatus(cancellationToken);

        return Unit.Default;
    }, cancellationToken);

    public CalibrationDTOBase[] GetCalibrations(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        if (ReferenceEquals(entry.Cookie.Calibrations, ObjectHelper.GetFieldValue(type, null, nameof(CalibrationDTOBase<>.Defaults), flags: ObjectHelper.AllBindingFlags | BindingFlags.FlattenHierarchy)) == false) return entry.Cookie.Calibrations;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefaultArray(entry.DTOType, out var calibrations) == false)
            cacheProvider.SetArray(entry.DTOType, calibrations, cancellationToken);

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        calibrationViewModel.UpdateEntryStatus(cancellationToken);

        return (CalibrationDTOBase[])calibrations;
    }, cancellationToken);

    public void SetCalibrations(Type type, CalibrationDTOBase[] items, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        cacheProvider.SetArray(type, items.Cast<object>().ToArray(), cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), items);

        calibrationViewModel.UpdateEntryStatus(cancellationToken);

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