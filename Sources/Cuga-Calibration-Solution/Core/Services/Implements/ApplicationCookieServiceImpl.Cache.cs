using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Net.Utilities.Models;
using System.Runtime.CompilerServices;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
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
}