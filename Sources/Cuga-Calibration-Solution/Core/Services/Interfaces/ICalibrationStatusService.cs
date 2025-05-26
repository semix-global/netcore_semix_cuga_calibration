using Core.Models.Models;
using Local.NoSQL.DB.Providers.Interfaces;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationStatusService
{
    #region ADS

    bool GetAdsCalibrationIsOKStatus();

    #endregion ADS

    #region microscope

    bool EnableDependMicroscopePixelSizeCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    #endregion microscope

    #region chuck

    bool EnableDependGantryCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependGlobalScaleErrorCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependChuckCenterCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependPrealignerCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependBrightStageMapCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependDarkStageMapCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    #endregion chuck

    #region laser

    bool EnableDependLaserAodDelayCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependLaserPrescanChirpAodAlignmentCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependLaserXYAstigmatismCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependIlluminationProfileCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependLaserXTCCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependLaserPixelSizeCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    bool EnableDependLaserLineCentricityCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage);

    #endregion laser

    #region Common

    /// <summary>
    /// 改写数据库单一结果校准状态
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="isOk"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    bool EnableCalibration<T>(bool isOk, CancellationToken cancellationToken, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new();

    /// <summary>
    /// 改写数据库集合结果校准状态
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="isOk"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    bool EnableCalibrationItems<T>(bool isOk, CancellationToken cancellationToken, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new();

    bool GetCalibrationDtoIsOKStatus<T>(out T calibrationDto, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new();

    bool GetCalibrationDtoItemsIsOKStatus<T>(out T[] calibrationDtoItems, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new();

    #endregion Common
}