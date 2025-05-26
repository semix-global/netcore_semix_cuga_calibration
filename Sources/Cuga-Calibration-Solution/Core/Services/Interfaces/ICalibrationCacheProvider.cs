using Core.Models.Models;
using Local.NoSQL.DB.Providers.Interfaces;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationCacheProvider
{
    Task<bool> TrySaveAsync();

    bool TrySet<T>(T dto, CancellationToken cancellationToken) where T : class, ICacheItem, new();

    bool TrySetArray<T>(T[] dtoList, CancellationToken cancellationToken) where T : class, ICacheItem, new();

    bool TrySetDisable<T>(CancellationToken cancellationToken) where T : CalibrationDtoBase, new();

    bool TrySetArrayDisable<T>(CancellationToken cancellationToken) where T : CalibrationDtoBase, new();

    bool TrySetIsRequiredSelfCheck<T>(bool isRequiredSelfCheck, CancellationToken cancellationToken) where T : CalibrationDtoBase, new();

    bool TrySetArrayIsRequiredSelfCheck<T>(bool isRequiredSelfCheck, CancellationToken cancellationToken) where T : CalibrationDtoBase, new();
}