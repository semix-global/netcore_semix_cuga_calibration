using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.Chuck.AutoFocus;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.DOEAngle;
using Core.Models.Models.Laser.FocusShift;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.LineOrientationOffset;
using Core.Models.Models.Laser.OpticalPower;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PmtAgcDelay;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.Rtfc;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Local.NoSQL.DB.Providers.Interfaces;
using MoreLinq;
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

    public static bool IsOk(this ChuckCenterObjDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Center is Empty";

        return isOk;
    }

    public static bool IsOk(this ChuckPrealignerObjDto result, out string errorMessage)
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

    public static bool IsOk(this ChuckRotateScaleErrorDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Rotate Scale is Empty";

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

    public static bool IsOk(this LaserAodDelayItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);
        if (isOk == false) errorMessage = "Laser Aod Delay is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserAttenuatorDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);
        if (isOk == false) errorMessage = "Laser Attenuator is Empty";

        return isOk;
    }

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

        var isOk = result.SingleOrDefault(t => t is { PmtId: 8, OpticsMagTypeEnum: OpticsMagTypeEnum.High, StageSpeedEnum: StageSpeedEnum.Low })?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Line Centricity is Empty";

        return isOk;
    }

    public static bool IsOk(this LineOrientationOffsetItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.SingleOrDefault(t => t is { PmtId: 8, OpticsMagTypeEnum: OpticsMagTypeEnum.High, StageSpeedEnum: StageSpeedEnum.Low })?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Line Orientation Offset is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserOpticalPowerDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);
        if (isOk == false) errorMessage = "Laser Optical Power is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserPixelSizeItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.SingleOrDefault(t => t.PmtId == 8 && t.OpticsMagTypeEnum == OpticsMagTypeEnum.High)?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Pixel Size is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserXPixelSizeItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.SingleOrDefault(t => t.PmtId == 8 && t is { OpticsMagTypeEnum: OpticsMagTypeEnum.High, XStageSpeedEnum: StageSpeedEnum.Low })?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser X Pixel Size is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserXTCCalibrationItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.SingleOrDefault(t => t.PmtId == 8 && t.OpticsMagTypeEnum == OpticsMagTypeEnum.High)?.IsOk == true;
        if (isOk == false) errorMessage = "Laser XTC is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserXYAstigmatismCalibrationItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Laser XY Astigmatism is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserPrescanChirpAodAlignmentDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);
        if (isOk == false) errorMessage = "Laser Prescan Chirp Aod Alignment is Empty";

        return isOk;
    }

    public static bool IsOk(this FocusShiftDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        //var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);
        var isOk = result.SingleOrDefault(t => t.OpticsMagTypeEnum == OpticsMagTypeEnum.High)?.IsOk == true;
        if (isOk == false) errorMessage = "Laser Focus Shift is Empty";

        return isOk;
    }

    public static bool IsOk(this RtfcDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        //var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);
        var isOk = result.SingleOrDefault(t => t.OpticsMagTypeEnum == OpticsMagTypeEnum.High)?.IsOk == true;
        if (isOk == false) errorMessage = "Laser Rtfc is Empty";

        return isOk;
    }

    public static bool IsOk(this LaserPmtAgcDelayItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var isOk = result.SingleOrDefault(t => t.PmtId == 8 && t.OpticsMagTypeEnum == OpticsMagTypeEnum.High)?.IsOk == true;
        if (isOk == false) errorMessage = "Laser XTC is Empty";

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
}