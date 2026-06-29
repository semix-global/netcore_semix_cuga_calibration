using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.ViewModels;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
    private const int TimeoutSecond = 60;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CalibrationCacheBase GetCache(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == type);

        if (ReferenceEquals(entry.Cookie.Cache, ObjectHelper.GetFieldValue(type, null, nameof(CalibrationCacheBase<>.Default), flags: ObjectHelper.AllBindingFlags | BindingFlags.FlattenHierarchy)) == false) return ObjectHelper.InvokeMethod<CalibrationCacheBase>(entry.Cookie.Cache, nameof(ICloneable<>.Clone), null);

        if (recipeCacheProvider.TryGetOrDefault(type, out var cache) == false)
        {
            cache = Guard.IsNotNullAndReturn(Activator.CreateInstance(type));

            recipeCacheProvider.Set(type, cache, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), cache);

        return ObjectHelper.InvokeMethod<CalibrationCacheBase>(cache, nameof(ICloneable<>.Clone), null);
    }, cancellationToken);

    public void SetCache(Type type, CalibrationCacheBase cache, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == type);

        recipeCacheProvider.Set(type, cache, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), cache);

        return Unit.Default;
    }, cancellationToken);

    public CalibrationDTOBase GetCalibration(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray == false);

        if (ReferenceEquals(entry.Cookie.Calibration, ObjectHelper.GetFieldValue(type, null, nameof(CalibrationDTOBase<>.Default), flags: ObjectHelper.AllBindingFlags | BindingFlags.FlattenHierarchy)) == false) return entry.Cookie.Calibration;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefault(type, out var calibration) == false)
        {
            calibration = Guard.IsNotNullAndReturn(Activator.CreateInstance(type));

            cacheProvider.Set(type, calibration, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), calibration);

        calibrationViewModel.UpdateEntryStatus(entry.Cookie.Calibration, cancellationToken);

        return (CalibrationDTOBase)calibration;
    }, cancellationToken);

    public void SetCalibration(Type type, CalibrationDTOBase calibration, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray == false);

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        cacheProvider.Set(type, calibration, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), calibration);

        calibrationViewModel.UpdateEntryStatus(entry.Cookie.Calibration, cancellationToken);

        return Unit.Default;
    }, cancellationToken);

    public CalibrationDTOBase[] GetCalibrations(Type type, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        if (ReferenceEquals(entry.Cookie.Calibrations, ObjectHelper.GetFieldValue(type, null, nameof(CalibrationDTOBase<>.Defaults), flags: ObjectHelper.AllBindingFlags | BindingFlags.FlattenHierarchy)) == false) return entry.Cookie.Calibrations;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefaultArray(type, out var calibrations) == false)
            cacheProvider.SetArray(type, (object[])Array.CreateInstance(type, 0), cancellationToken);

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        calibrationViewModel.UpdateEntryStatus(entry.Cookie.Calibrations, cancellationToken);

        return (CalibrationDTOBase[])calibrations;
    }, cancellationToken);

    public void SetCalibrations(Type type, CalibrationDTOBase[] calibrations, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        cacheProvider.SetArray(type, Unsafe.As<object[]>(calibrations), cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        calibrationViewModel.UpdateEntryStatus(entry.Cookie.Calibrations, cancellationToken);

        return Unit.Default;
    }, cancellationToken);

    public object GetOrDefault(Type type, bool isRecipe, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => (t.DTOType == type || t.CacheType == type) && t.IsArray == false);

        if (entry is not null) return isRecipe ? GetCache(type, cancellationToken) : GetCalibration(type, cancellationToken);

        if (isRecipe)
        {
            if (recipeCacheProvider.TryGetOrDefault(type, out var cache) == false)
            {
                cache = Guard.IsNotNullAndReturn(Activator.CreateInstance(type));

                recipeCacheProvider.Set(type, cache, cancellationToken);
            }

            return cache;
        }
        else
        {
            if (cacheProvider.TryGetOrDefault(type, out var cache) == false)
            {
                cache = Guard.IsNotNullAndReturn(Activator.CreateInstance(type));

                cacheProvider.Set(type, cache, cancellationToken);
            }

            return cache;
        }
    }, cancellationToken);

    public object[] GetArrayOrDefault(Type type, bool isRecipe, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => (t.DTOType == type || t.CacheType == type) && t.IsArray);

        if (entry is not null)
            return type == entry.DTOType
                ? Unsafe.As<object[]>(GetCalibrations(type, cancellationToken))
                : throw new ArgumentException("Cache is not support get array!", nameof(type));

        if (isRecipe)
        {
            if (recipeCacheProvider.TryGetOrDefaultArray(type, out var caches) == false)
                recipeCacheProvider.SetArray(type, (object[])Array.CreateInstance(type, 0), cancellationToken);

            return caches;
        }
        else
        {
            if (cacheProvider.TryGetOrDefaultArray(type, out var caches) == false)
                cacheProvider.SetArray(type, (object[])Array.CreateInstance(type, 0), cancellationToken);

            return caches;
        }
    }, cancellationToken);

    public void Set(Type type, object cache, bool isRecipe, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(e => (e.DTOType == type || e.CacheType == type) && e.IsArray == false);
        if (entry is not null)
        {
            if (type == entry.CacheType)
            {
                if (cache is not CalibrationCacheBase recipeCache) throw new ArgumentException("Cache must be CalibrationCacheBase", nameof(cache));
                SetCache(type, recipeCache, cancellationToken);
            }
            else
            {
                if (cache is not CalibrationDTOBase calibration) throw new ArgumentException("Cache must be CalibrationDTOBase", nameof(cache));
                SetCalibration(type, calibration, cancellationToken);
            }
        }
        else
        {
            if (isRecipe) recipeCacheProvider.Set(type, cache, cancellationToken);
            else cacheProvider.Set(type, cache, cancellationToken);
        }

        return Unit.Default;
    }, cancellationToken);

    public void SetArray(Type type, object[] caches, bool isRecipe, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(e => (e.DTOType == type || e.CacheType == type) && e.IsArray);
        if (entry is not null)
        {
            if (type == entry.CacheType) throw new ArgumentException("Cache is not support get array!", nameof(type));

            if (caches is not CalibrationDTOBase[] calibrations) throw new ArgumentException("Cache must be CalibrationDTOBase", nameof(caches));
            SetCalibrations(type, calibrations, cancellationToken);
        }
        else
        {
            if (isRecipe) recipeCacheProvider.SetArray(type, caches, cancellationToken);
            else cacheProvider.SetArray(type, caches, cancellationToken);
        }

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