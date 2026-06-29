using Local.SQL.Cache.Providers.Bases;

namespace CugaCalibration.Core.Services.Interfaces;

public partial interface IApplicationCookieService
{
    T GetOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;

    T[] GetArrayOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;

    void Set<T>(T cache, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;

    void SetArray<T>(T[] caches, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase;
}