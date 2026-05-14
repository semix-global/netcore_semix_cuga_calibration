using System.Runtime.CompilerServices;
using Core.Models.Models;

namespace CugaCalibration.Core.Services.Implements;

public sealed partial class ApplicationCookieServiceImpl
{
    public T GetCache<T>(CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new() => (T)GetCache(typeof(T), cancellationToken);

    public void SetCache<T>(T item, CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new() => SetCache(typeof(T), item, cancellationToken);

    public T GetCalibration<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => (T)GetCalibration(typeof(T), cancellationToken);

    public void SetCalibration<T>(T item, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => SetCalibration(typeof(T), item, cancellationToken);

    public T[] GetCalibrations<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => (T[])GetCalibrations(typeof(T), cancellationToken);

    public void SetCalibrations<T>(T[] items, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => SetCalibrations(typeof(T), Unsafe.As<CalibrationDTOBase[]>(items), cancellationToken);
}