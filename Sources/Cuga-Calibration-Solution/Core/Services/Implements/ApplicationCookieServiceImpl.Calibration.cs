using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.ViewModels;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.WPF.MVVM;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
    public CalibrationCacheBase GetCache(Type type, CancellationToken cancellationToken)
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
    }

    public void SetCache(Type type, CalibrationCacheBase cache, CancellationToken cancellationToken)
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == type);

        recipeCacheProvider.Set(type, cache, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), cache);
    }

    public CalibrationDTOBase GetCalibration(Type type, CancellationToken cancellationToken)
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
    }

    public void SetCalibration(Type type, CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray == false);

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        cacheProvider.Set(type, calibration, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), calibration);

        calibrationViewModel.UpdateEntryStatus(entry.Cookie.Calibration, cancellationToken);
    }

    public CalibrationDTOBase[] GetCalibrations(Type type, CancellationToken cancellationToken)
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        if (ReferenceEquals(entry.Cookie.Calibrations, ObjectHelper.GetFieldValue(type, null, nameof(CalibrationDTOBase<>.Defaults), flags: ObjectHelper.AllBindingFlags | BindingFlags.FlattenHierarchy)) == false) return entry.Cookie.Calibrations;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefaultArray(type, out var calibrations) == false)
            cacheProvider.SetArray(type, (object[])Array.CreateInstance(type, 0), cancellationToken);

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        calibrationViewModel.UpdateEntryStatus(entry.Cookie.Calibrations, cancellationToken);

        return (CalibrationDTOBase[])calibrations;
    }

    public void SetCalibrations(Type type, CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == type && e.IsArray);

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        cacheProvider.SetArray(type, Unsafe.As<object[]>(calibrations), cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        calibrationViewModel.UpdateEntryStatus(entry.Cookie.Calibrations, cancellationToken);
    }
}