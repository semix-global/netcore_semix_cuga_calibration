using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Microsoft.Extensions.Logging;
using Net.Utilities.Models;
using System.Runtime.CompilerServices;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
    public object GetOrDefault(Type type, bool isRecipe, CancellationToken cancellationToken = default) => InvokeGetCache(() =>
    {
        var cacheEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.CacheType == type);
        if (cacheEntry is not null)
        {
            Guard.IsTrue(isRecipe, nameof(isRecipe), "Cache is must be Recipe Provider");

            return GetCache(type, cancellationToken);
        }

        var dtoEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.DTOType == type);
        if (dtoEntry is not null)
        {
            Guard.IsFalse(isRecipe, nameof(isRecipe), "Cache is must be Cache Provider");
            Guard.IsFalse(dtoEntry.IsArray, nameof(dtoEntry.IsArray), "Get is not Array, so entry must be no array");

            return GetCalibration(type, cancellationToken);
        }

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
    });

    public object[] GetArrayOrDefault(Type type, bool isRecipe, CancellationToken cancellationToken = default) => InvokeGetCache(() =>
    {
        var cacheEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.CacheType == type);
        if (cacheEntry is not null) return ThrowHelper.ThrowArgumentException<object[]>("Cache is not support get array");

        var dtoEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.DTOType == type);
        if (dtoEntry is not null)
        {
            Guard.IsFalse(isRecipe, nameof(isRecipe), "Cache is must be Cache Provider");
            Guard.IsTrue(dtoEntry.IsArray, nameof(dtoEntry.IsArray), "Get is Array, so entry must be array");

            return Unsafe.As<object[]>(GetCalibrations(type, cancellationToken));
        }

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
    });

    public void Set(Type type, object cache, bool isRecipe, CancellationToken cancellationToken = default) => InvokeGetCache(() =>
    {
        var cacheEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.CacheType == type);
        if (cacheEntry is not null)
        {
            Guard.IsTrue(isRecipe, nameof(isRecipe), "Cache is must be Recipe Provider");

            SetCache(type, Unsafe.As<CalibrationCacheBase>(cache), cancellationToken);
        }

        var dtoEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.DTOType == type);
        if (dtoEntry is not null)
        {
            Guard.IsFalse(isRecipe, nameof(isRecipe), "Cache is must be Cache Provider");
            Guard.IsFalse(dtoEntry.IsArray, nameof(dtoEntry.IsArray), "Get is not Array, so entry must be no array");

            SetCalibration(type, Unsafe.As<CalibrationDTOBase>(cache), cancellationToken);
        }

        if (isRecipe) recipeCacheProvider.Set(type, cache, cancellationToken);
        else cacheProvider.Set(type, cache, cancellationToken);

        return Unit.Default;
    });

    public void SetArray(Type type, object[] caches, bool isRecipe, CancellationToken cancellationToken = default) => InvokeGetCache(() =>
    {
        var cacheEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.CacheType == type);
        if (cacheEntry is not null) ThrowHelper.ThrowArgumentException("Cache is not support get array");

        var dtoEntry = ApplicationCookie.CalibrationViewModelEntries.Values.SingleOrDefault(t => t.DTOType == type);
        if (dtoEntry is not null)
        {
            Guard.IsFalse(isRecipe, nameof(isRecipe), "Cache is must be Cache Provider");
            Guard.IsTrue(dtoEntry.IsArray, nameof(dtoEntry.IsArray), "Set is Array, so entry must be array");

            SetCalibrations(type, Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase[]>(caches), cancellationToken);
        }

        if (isRecipe) recipeCacheProvider.SetArray(type, caches, cancellationToken);
        else cacheProvider.SetArray(type, caches, cancellationToken);

        return Unit.Default;
    });

    private T InvokeGetCache<T>(Func<T> func)
    {
        try
        {
            logger.LogTrace("Start Get Cache...");

            return func();
        }
        finally
        {
            logger.LogTrace("Stop Get Cache...");
        }
    }
}