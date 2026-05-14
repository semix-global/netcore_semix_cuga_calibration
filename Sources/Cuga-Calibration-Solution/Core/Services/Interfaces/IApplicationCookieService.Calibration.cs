using Core.Models.Models;

namespace CugaCalibration.Core.Services.Interfaces;

public partial interface IApplicationCookieService
{
    CalibrationCacheBase GetCache(Type type, CancellationToken cancellationToken = default);

    void SetCache(Type type, CalibrationCacheBase item, CancellationToken cancellationToken = default);

    CalibrationDTOBase GetCalibration(Type type, CancellationToken cancellationToken = default);

    void SetCalibration(Type type, CalibrationDTOBase item, CancellationToken cancellationToken = default);

    CalibrationDTOBase[] GetCalibrations(Type type, CancellationToken cancellationToken = default);

    void SetCalibrations(Type type, CalibrationDTOBase[] items, CancellationToken cancellationToken = default);
}