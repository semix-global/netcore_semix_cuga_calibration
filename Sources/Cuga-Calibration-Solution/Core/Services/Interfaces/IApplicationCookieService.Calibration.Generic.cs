using Core.Models.Models;
using Local.SQL.Cache.Providers.Bases;

namespace CugaCalibration.Core.Services.Interfaces;

public partial interface IApplicationCookieService
{
    T GetCache<T>(CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new();

    void SetCache<T>(T cache, CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new();

    T GetCalibration<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();

    T[] GetCalibrations<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();

    void SetCalibration<T>(T calibration, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();

    void SetCalibrations<T>(T[] calibrations, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();

    T GetOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;

    T[] GetArrayOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;

    void Set<T>(T cache, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;

    void SetArray<T>(T[] caches, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;
}