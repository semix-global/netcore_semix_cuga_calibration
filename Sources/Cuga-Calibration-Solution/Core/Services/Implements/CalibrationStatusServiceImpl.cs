using Core.Models.Extensions;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationStatusService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationStatusServiceImpl(
    CalibrationSetting calibrationSetting,
    ICalibrationCacheProvider calibrationCacheProviderService,
    ICacheProvider cacheProvider) : ICalibrationStatusService
{
    #region Ads

    public bool GetAdsCalibrationIsOKStatus()
    {
        if (GetCalibrationDtoIsOKStatus<AdsPressureGainsDto>(out _, out _) == false) return false;
        if (GetCalibrationDtoItemsIsOKStatus<AdsXGainsItemDto>(out _, out _) == false) return false;
        if (GetCalibrationDtoItemsIsOKStatus<AdsYGainsItemDto>(out _, out _) == false) return false;
        return true;
    }

    #endregion Ads

    #region Microscope

    public bool EnableDependMicroscopePixelSizeCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibrationItems<MicroscopeCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibration<ChuckGantryDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibration<ChuckCenterAndThetaItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibration<ChuckPrealignerObjDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibration<ChuckGlobalScaleErrorDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibration<ChuckStageMapDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    #endregion Microscope

    #region Chuck

    public bool EnableDependGantryCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibration<ChuckGlobalScaleErrorDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibration<ChuckStageMapDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserOpticalPowerMeterDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependGlobalScaleErrorCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibration<ChuckStageMapDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserOpticalPowerMeterDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependChuckCenterCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibration<ChuckPrealignerObjDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibration<ChuckStageMapDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependPrealignerCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibration<ChuckStageMapDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependBrightStageMapCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserOpticalPowerMeterDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependDarkStageMapCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibrationItems<LaserOpticalPowerMeterDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    #endregion Chuck

    #region Laser

    public bool EnableDependLaserAodDelayCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibrationItems<AODAlignmentDTO>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserXYAstigmatismCalibrationItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserIlluminationProfileItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserXTCCalibrationItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependLaserPrescanChirpAodAlignmentCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibrationItems<LaserXYAstigmatismCalibrationItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserIlluminationProfileItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserXTCCalibrationItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        return true;
    }

    public bool EnableDependLaserXYAstigmatismCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibrationItems<LaserIlluminationProfileItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserXTCCalibrationItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependIlluminationProfileCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        errorMessage = string.Empty;

        return true;
    }

    public bool EnableDependLaserPmtAgcDelayCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        errorMessage = string.Empty;

        return true;
    }

    public bool EnableDependLaserXTCCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        errorMessage = string.Empty;

        return true;
    }

    public bool EnableDependLaserPixelSizeCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibration<ChuckStageMapDto>(isOk, cancellationToken, out errorMessage) == false) return false;
        if (EnableCalibrationItems<LaserLineCentricityItemDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependLaserLineCentricityCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        if (EnableCalibration<ChuckStageMapDto>(isOk, cancellationToken, out errorMessage) == false) return false;

        return true;
    }

    public bool EnableDependLaserFocusShiftCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        errorMessage = string.Empty;
        return true;
    }

    public bool EnableDependLaserRtfcCalibrations(bool isOk, CancellationToken cancellationToken, out string errorMessage)
    {
        errorMessage = string.Empty;
        return true;
    }

    #endregion Laser

    #region Common

    #region Dependency

    public bool EnableCalibration<T>(bool isOk, CancellationToken cancellationToken, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new()
    {
        errorMessage = string.Empty;

#if DEBUG || SIMULATOR|| RELEASE
        if (calibrationSetting.SettingCommonParam.DependencyEnable == false) return true;
#endif

        if (calibrationCacheProviderService.TrySetDisable<T>(cancellationToken)) return true;

        errorMessage = typeof(T).Name;
        return false;
    }

    public bool EnableCalibrationItems<T>(bool isOk, CancellationToken cancellationToken, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new()
    {
        errorMessage = string.Empty;

#if DEBUG || SIMULATOR || RELEASE
        if (calibrationSetting.SettingCommonParam.DependencyEnable == false) return true;
#endif
        if (calibrationCacheProviderService.TrySetArrayDisable<T>(cancellationToken)) return true;

        errorMessage = typeof(T).Name;
        return false;
    }

    #endregion Dependency

    #region Prerequisites

    public bool GetCalibrationDtoIsOKStatus<T>(out T calibrationDto, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new()
    {
        try
        {
            calibrationDto = cacheProvider.GetOrDefault<T>();
            var type = calibrationDto.GetType();
            errorMessage = nameof(type.Name);

#if DEBUG || SIMULATOR || RELEASE
            if (calibrationSetting.SettingCommonParam.DependencyEnable == false) return true;
#endif

            var extensionType = typeof(CoreWcfModelsExtension);
            // 获取方法信息
            var methodInfo = extensionType.GetMethods()
                .FirstOrDefault(m => m.Name == nameof(CoreWcfModelsExtension.IsOk) && m.GetParameters()[0].ParameterType == type);

            if (methodInfo is null)
            {
                errorMessage = "IsOk Extension Method not found!";
                return false;
            }

            // 调用方法
            var parameters = new object[] { calibrationDto, null! };
            var result = methodInfo.Invoke(null, parameters);
            errorMessage = parameters[1].ToString();
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            calibrationDto = null!;
            return false;
        }
    }

    public bool GetCalibrationDtoItemsIsOKStatus<T>(out T[] calibrationDtoItems, out string errorMessage) where T : CalibrationDtoBase, ICacheItem, new()
    {
        try
        {
            calibrationDtoItems = cacheProvider.GetOrDefaultArray<T>();
            var type = calibrationDtoItems.GetType();
            errorMessage = nameof(type.Name);

#if DEBUG || SIMULATOR || RELEASE
            if (calibrationSetting.SettingCommonParam.DependencyEnable == false) return true;
#endif

            var extensionType = typeof(CoreWcfModelsExtension);

            // 获取方法信息
            var methodInfo = extensionType.GetMethods()
                .FirstOrDefault(m => m.Name == nameof(CoreWcfModelsExtension.IsOk) && m.GetParameters()[0].ParameterType == type);

            if (methodInfo is null)
            {
                errorMessage = "IsOk Extension Method not found!";
                return false;
            }

            // 调用方法
            var parameters = new object[] { calibrationDtoItems, null! };
            var result = methodInfo.Invoke(null, parameters);
            errorMessage = parameters[1].ToString();
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            calibrationDtoItems = null!;
            errorMessage = ex.Message;
            return false;
        }
    }

    #endregion Prerequisites

    #endregion Common
}