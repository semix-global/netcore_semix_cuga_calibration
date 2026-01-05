using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.Chuck.AlignmentDegreeOffset;
using Core.Models.Models.Chuck.AutoFocus;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.IlluminationProfile;
using Core.Models.Models.CIB.LightMatching;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.DOEAngle;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.LineOrientationOffset;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Optics.INC;
using Core.Models.Models.Optics.Relay;
using Core.Models.Models.Setting;
using Local.NoSQL.DB.Providers.Interfaces;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Extensions;

public static class CoreWcfModelsExtension
{
    #region Ads

    public static bool IsOk(this AdsPressureGainsDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;
        if (isOk == false) errorMessage = "Ads Pressure Gain is Empty";

        return isOk;
    }

    public static bool IsOk(this AdsXGainsItemDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;
        if (isOk == false) errorMessage = "Ads X Gain is Empty";

        return isOk;
    }

    public static bool IsOk(this AdsYGainsItemDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;
        if (isOk == false) errorMessage = "Ads Y Gain is Empty";

        return isOk;
    }

    #endregion Ads

    #region Microscope

    public static bool IsOk(this MicroscopeFocusItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var lensInformationList = HostApplication.GetRequiredService<ApplicationCookie>().MicroscopeLensInformations;

        var lensChanged = result.All(t => lensInformationList.Contains(t.LensInformation)) == false
                          || result.Length != lensInformationList.Count;

        var isOk = lensChanged == false
                   && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Microscope Focus is Empty";

        if (lensChanged)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result = [.. result.Where(t => lensInformationList.Contains(t.LensInformation))];
            cacheProvider.SetArray(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this MicroscopeCalChipDto result, out string errorMessage)
    {
        errorMessage = string.Empty;
        var lensInformationList = HostApplication.GetRequiredService<ApplicationCookie>().MicroscopeLensInformations;

        var lensChanged = lensInformationList.Contains(result.MicroscopeLensInformation) == false;

        var isOk = result.IsOk && lensChanged == false;
        if (isOk == false)
            errorMessage = "Microscope Cal Chip is Empty";

        if (lensChanged)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            cacheProvider.Set(new MicroscopeCalChipDto(), CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this MicroscopePixelSizeItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var lensInformationList = HostApplication.GetRequiredService<ApplicationCookie>().MicroscopeLensInformations;

        var lensChanged = result.All(t => lensInformationList.Contains(t.LensInformation)) == false
                          || result.Length != lensInformationList.Count;

        var isOk = lensChanged == false
                   && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Microscope Pixel Size is Empty";

        if (lensChanged)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result = [.. result.Where(t => lensInformationList.Contains(t.LensInformation))];
            cacheProvider.SetArray(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this MicroscopeCentricityItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var lensInformationList = HostApplication.GetRequiredService<ApplicationCookie>().MicroscopeLensInformations;

        var lensChanged = result.All(t => lensInformationList.Contains(t.LensInformation)) == false
                          || result.Length != lensInformationList.Count;

        var isOk = lensChanged == false
                   && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Microscope Centricity is Empty";

        if (lensChanged)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result = [.. result.Where(t => lensInformationList.Contains(t.LensInformation))];
            cacheProvider.SetArray(result, CancellationToken.None);
        }

        return isOk;
    }

    #endregion Microscope

    #region Chuck

    public static bool IsOk(this ChuckGantryDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Gantry is Empty";

        return isOk;
    }

    public static bool IsOk(this ChuckCenterAndThetaItemDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Center is Empty";

        return isOk;
    }

    public static bool IsOk(this ChuckPrealignerDTO result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Prealigner is Empty";

        return isOk;
    }

    public static bool IsOk(this ChuckAutoFocusDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Auto Focus is Empty";

        return isOk;
    }

    public static bool IsOk(this ChuckGlobalScaleErrorDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Global Scale is Empty";

        return isOk;
    }

    public static bool IsOk(this ChuckAlignmentDegreeOffsetItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var isOk = result.SingleOrDefault(t => t.ProductivityInformation == applicationCookie.OILowProductivityInformation)?.IsOk == true;

        if (isOk == false)
            errorMessage = "Chuck Alignment Degree Offset is Empty";

        return isOk;
    }

    public static bool IsOk(this ChuckStageMapDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Stage Map is Empty";

        return isOk;
    }

    #endregion Chuck

    #region Laser

    public static bool IsOk(this LaserAutoFocusDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;
        if (isOk == false) errorMessage = "Laser Auto Focus is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserBeamStabilizerObjDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;
        if (isOk == false) errorMessage = "Laser Beam Stabilizer is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserIlluminationProfileItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length * 14 && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Laser Illumination Profile is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserLineCentricityItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var isOk = result.SingleOrDefault(t => t.OpticsIlluminationMode == CalibrationConstantsHelper.MainOpticsIlluminationModeEnum
                                               && t.PmtId == CalibrationConstantsHelper.MainPmtId
                                               && t.ProductivityInformation == applicationCookie.OILowProductivityInformation)?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Line Centricity is Empty";

        return isOk;
    }

    public static bool IsOk(this LineOrientationOffsetItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var isOk = result.SingleOrDefault(t => t.PmtId == CalibrationConstantsHelper.MainPmtId
                                               && t.ProductivityInformation == applicationCookie.NILowProductivityInformation)?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Line Orientation Offset is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserPixelSizeItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var isOk = result.SingleOrDefault(t => t.OpticsIlluminationMode == CalibrationConstantsHelper.MainOpticsIlluminationModeEnum
                                               && t.PmtId == CalibrationConstantsHelper.MainPmtId
                                               && t.ProductivityInformation == applicationCookie.OILowProductivityInformation)?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Pixel Size is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserXTCCalibrationItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var isOk = result.SingleOrDefault(t => t.PmtId == CalibrationConstantsHelper.MainChannelId
                                               && t.ProductivityInformation == applicationCookie.OILowProductivityInformation)?.IsOk == true;
        if (isOk == false) errorMessage = "Laser XTC is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserXYAstigmatismCalibrationItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var isOk = result.Length == applicationCookie.NIProductivityInformations.Count && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Laser XY Astigmatism is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserDOEAngleDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;
        if (isOk == false) errorMessage = "Laser DOE Angle is Empty";

        return isOk;
    }

    #endregion Laser

    public static bool IsOk(this CalibrationSetting result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var lensInformationList = HostApplication.GetRequiredService<ApplicationCookie>().MicroscopeLensInformations;

        var lensChanged = lensInformationList.Contains(result.SettingCommonParam.LowMicroscopeLensInformation) == false
                          || lensInformationList.Contains(result.SettingCommonParam.HighMicroscopeLensInformation) == false;

        if (lensChanged)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.SettingCommonParam.LowMicroscopeLensInformation = result.SettingCommonParam.HighMicroscopeLensInformation = lensInformationList[0];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return lensChanged;
    }

    #region NEW

    public static bool IsOk(this CIBMMDDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.CIBInformations.Contains(t.CIBInformation) && t.IsOk);
        var isOk = isOkCount == applicationCookie.CIBInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB MMD is Empty";

        return isOk;
    }

    public static bool IsOk(this CIBLightMatchingDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Where(t => applicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                                          && applicationCookie.OpticsApodizationModeEnums.Contains(t.OpticsApodizationModeEnum)
                                          && applicationCookie.OpticsPolarizationModeEnums.Contains(t.OpticsPolarizationModeEnum)
                                          && applicationCookie.CollectorPolarizationModeEnums.Contains(t.CollectorPolarizationModeEnum)
                                          && t.IsOk)
            .SelectMany(t => t.Items)
            .Count(t => applicationCookie.CIBInformations.Contains(t.CIBInformation));

        var isOk = isOkCount == applicationCookie.ProductivityInformations.Count
            * applicationCookie.OpticsApodizationModeEnums.Count * applicationCookie.OpticsPolarizationModeEnums.Count * applicationCookie.CollectorPolarizationModeEnums.Count
            * applicationCookie.CIBInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB Light Matching is Empty";

        return isOk;
    }

    public static bool IsOk(this OpticsRelayDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationModeEnum) && t.IsOk);
        var isOk = isOkCount == applicationCookie.OpticsIlluminationModeEnums.Count;

        errorMessage = isOk ? string.Empty : "Optics Relay is Empty";

        return isOk;
    }

    public static bool IsOk(this CIBXPixelSizeDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.ProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB X Pixel Size is Empty";

        return isOk;
    }

    public static bool IsOk(this AODAlignmentDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.OpticsMagTypeProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "AOD Alignment is Empty";

        return isOk;
    }

    public static bool IsOk(this AODDelayDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.OpticsMagTypeProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "AOD Delay is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserOpticalPowerMeterDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.OpticsMagTypeProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "Laser Optical Power Meter is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserAttenuatorDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.OpticsMagTypeProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "Laser Attenuator is Empty";

        return isOk;
    }

    public static bool IsOk(this CIBIlluminationProfileDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Where(t => applicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                                          && applicationCookie.OpticsApodizationModeEnums.Contains(t.OpticsApodizationModeEnum)
                                          && applicationCookie.OpticsPolarizationModeEnums.Contains(t.OpticsPolarizationModeEnum)
                                          && applicationCookie.CollectorPolarizationModeEnums.Contains(t.CollectorPolarizationModeEnum)
                                          && t.IsOk)
            .SelectMany(t => t.Items)
            .Count(t => applicationCookie.CIBInformations.Contains(t.CIBInformation));

        var isOk = isOkCount == applicationCookie.ProductivityInformations.Count
            * applicationCookie.OpticsApodizationModeEnums.Count * applicationCookie.OpticsPolarizationModeEnums.Count * applicationCookie.CollectorPolarizationModeEnums.Count
            * applicationCookie.CIBInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB Light Matching is Empty";

        return isOk;
    }

    public static bool IsOk(this OpticsINCDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.ProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "Optics INC is Empty";

        return isOk;
    }

    #endregion
}