using Core.Models.Models;

namespace CugaCalibration.Core.Services.Interfaces;

public partial interface IApplicationCookieService
{
    CalibrationCacheBase GetCache(Type type, CancellationToken cancellationToken = default);

    void SetCache(Type type, CalibrationCacheBase cache, CancellationToken cancellationToken = default);

    CalibrationDTOBase GetCalibration(Type type, CancellationToken cancellationToken = default);

    void SetCalibration(Type type, CalibrationDTOBase calibration, CancellationToken cancellationToken = default);

    CalibrationDTOBase[] GetCalibrations(Type type, CancellationToken cancellationToken = default);

    void SetCalibrations(Type type, CalibrationDTOBase[] calibrations, CancellationToken cancellationToken = default);
}