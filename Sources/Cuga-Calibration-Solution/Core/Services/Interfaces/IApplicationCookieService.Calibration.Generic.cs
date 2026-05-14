using Core.Models.Models;

namespace CugaCalibration.Core.Services.Interfaces;

public partial interface IApplicationCookieService
{
    T GetCache<T>(CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new();

    void SetCache<T>(T item, CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new();

    T GetCalibration<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();

    T[] GetCalibrations<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();

    void SetCalibration<T>(T item, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();

    void SetCalibrations<T>(T[] items, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new();
}