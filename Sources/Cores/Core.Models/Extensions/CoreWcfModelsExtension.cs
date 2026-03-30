using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.AutoFocus.CalChipFocusOffset;
using Core.Models.Models.AutoFocus.GlobalFocusOffset;
using Core.Models.Models.Chuck.AlignmentDegreeOffset;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.IlluminationProfile;
using Core.Models.Models.CIB.LightMatching;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.CIB.LineOrientationOffset;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.XTC;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Optics.GlobalFieldTilt;
using Core.Models.Models.Optics.INC;
using Core.Models.Models.Optics.Relay;
using Core.Models.Models.Setting;
using Local.SQL.Cache.Providers.Interfaces;
using Net.Utilities.WPF.MVVM;
using DarkAutoFocusDTO = Core.Models.Models.AutoFocus.DarkAutoFocus.DarkAutoFocusDTO;

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

    public static bool IsOk(this MicroscopeCalChipDTO result, out string errorMessage)
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
            cacheProvider.Set(new MicroscopeCalChipDTO(), CancellationToken.None);
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

    public static bool IsOk(this DarkAutoFocusDTO result, out string errorMessage)
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

    public static bool IsOk(this GlobalFieldTiltDTO result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;
        if (isOk == false) errorMessage = "Global Field Tilt is Empty";

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

    public static bool IsOk(this CIBYPixelSizeDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.CIBInformationPMTIds.Count * applicationCookie.OpticsMagTypeProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB Y Pixel Size is Empty";

        return isOk;
    }

    public static bool IsOk(this CIBLineCentricityDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.CIBInformationPMTIds.Count * applicationCookie.OpticsMagTypeProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB  Line Centricity is Empty";

        return isOk;
    }

    public static bool IsOk(this CIBLineOrientationOffsetDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.ProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB Line Orientation Offset is Empty";

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

    public static bool IsOk(this CIBXTCDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.OpticsMagTypeProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "CIB XTC is Empty";

        return isOk;
    }


    public static bool IsOk(this AODUniformityDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                                          && applicationCookie.LaserLightInformations.Contains(t.LaserLightInformation)
                                          && t.IsOk);
        var isOk = isOkCount == applicationCookie.OpticsMagTypeProductivityInformations.Count * applicationCookie.LaserLightInformations.Count;

        errorMessage = isOk ? string.Empty : "AOD Uniformity is Empty";

        return isOk;
    }

    #endregion

    #region Auto Focus

    public static bool IsOk(this AutoFocusGlobalFocusOffsetDTO[] result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOkCount = result.Count(t => applicationCookie.ProductivityInformations.Contains(t.ProductivityInformation) && t.IsOk);
        var isOk = isOkCount == applicationCookie.ProductivityInformations.Count;

        errorMessage = isOk ? string.Empty : "Global Focus Offset is Empty";

        return isOk;
    }

    public static bool IsOk(this AutoFocusCalChipFocusOffsetDTO result, out string errorMessage)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        var isOk = applicationCookie.ProductivityInformations.Contains(result.ProductivityInformation) && result.IsOk;

        errorMessage = isOk ? string.Empty : "Cal Chip Focus Offset is Empty";

        return isOk;
    }

    #endregion
}