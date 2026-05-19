using Core.Models.Models;
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
}