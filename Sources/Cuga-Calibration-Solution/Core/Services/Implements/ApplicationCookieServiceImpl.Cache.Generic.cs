using Local.SQL.Cache.Providers.Bases;
using System.Runtime.CompilerServices;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
    public T GetOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => (T)GetOrDefault(typeof(T), isRecipe, cancellationToken);

    public T[] GetArrayOrDefault<T>(bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => (T[])GetArrayOrDefault(typeof(T), isRecipe, cancellationToken);

    public void Set<T>(T cache, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => Set(typeof(T), cache, isRecipe, cancellationToken);

    public void SetArray<T>(T[] caches, bool isRecipe, CancellationToken cancellationToken = default) where T : ObservableCacheBase => SetArray(typeof(T), Unsafe.As<object[]>(caches), isRecipe, cancellationToken);
}