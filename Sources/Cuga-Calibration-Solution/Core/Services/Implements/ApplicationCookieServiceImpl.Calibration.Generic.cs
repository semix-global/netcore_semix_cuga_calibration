using Core.Models.Models;
using Local.SQL.Cache.Providers.Bases;
using System.Runtime.CompilerServices;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
    public T GetCache<T>(CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new() => (T)GetCache(typeof(T), cancellationToken);

    public void SetCache<T>(T cache, CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new() => SetCache(typeof(T), cache, cancellationToken);

    public T GetCalibration<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => (T)GetCalibration(typeof(T), cancellationToken);

    public void SetCalibration<T>(T calibration, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => SetCalibration(typeof(T), calibration, cancellationToken);

    public T[] GetCalibrations<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => (T[])GetCalibrations(typeof(T), cancellationToken);

    public void SetCalibrations<T>(T[] calibrations, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => SetCalibrations(typeof(T), Unsafe.As<CalibrationDTOBase[]>(calibrations), cancellationToken);

    public T GetOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => (T)GetOrDefault(typeof(T), isRecipe, cancellationToken);

    public T[] GetArrayOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => (T[])GetArrayOrDefault(typeof(T), isRecipe, cancellationToken);

    public void Set<T>(T cache, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => Set(typeof(T), cache, isRecipe, cancellationToken);

    public void SetArray<T>(T[] caches, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => SetArray(typeof(T), Unsafe.As<object[]>(caches), isRecipe, cancellationToken);
}