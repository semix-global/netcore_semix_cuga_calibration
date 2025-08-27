using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.Chuck.AutoFocus;
using Core.Models.Models.Chuck.BrightFieldStageMap;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.DarkFieldStageMap;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.FocusShift;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
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
using Core.Wcf.Models;
using Local.NoSQL.DB.Providers.Interfaces;
using MoreLinq;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Extensions;

public static class CoreWcfModelsExtension
{
    #region Initialize

    private static Func<List<MicroscopeLensInformation>> _getMicroscopeLensInformationListFunc;

    // 初始化方法
    public static void Initialize(Func<List<MicroscopeLensInformation>> getMicroscopeLensInformationListFunc)
    {
        _getMicroscopeLensInformationListFunc = getMicroscopeLensInformationListFunc;
    }

    // 内部使用的获取方法
    private static List<MicroscopeLensInformation> GetMicroscopeLensInformationList()
    {
        if (_getMicroscopeLensInformationListFunc == null)
        {
            throw new InvalidOperationException("MicroscopeLensInformationList resolver has not been initialized");
        }

        return _getMicroscopeLensInformationListFunc();
    }

    public static (bool isChanged, List<MicroscopeLensInformation> lensInfos) IsLensChanged()
    {
        var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();

        var calibrationSetting = cacheProvider.GetOrDefault<CalibrationSetting>();
        var lensInformationList = GetMicroscopeLensInformationList();

        var lensChanged = calibrationSetting.MicroscopeLensInformationItems.All(t => lensInformationList.Contains(t)) == false
                          || calibrationSetting.MicroscopeLensInformationItems.Count != lensInformationList.Count;

        return (lensChanged, lensInformationList);
    }

    #endregion Initialize

    #region CalibrationBase

    public static bool GetIsOkStatus(this List<CalibrationBase> calibrationItems)
    {
        return calibrationItems.Count != 0 && calibrationItems.All(t => t.IsOk);
    }

    public static void SetIsOkStatus(this CalibrationBase calibrationItem, bool isOk)
    {
        calibrationItem.IsCalibrated = isOk;
        calibrationItem.IsVerified = isOk;
    }

    public static void SetIsOkStatus(this List<CalibrationBase> calibrationItems, bool isOk)
    {
        calibrationItems.ForEach(t =>
        {
            t.IsCalibrated = isOk;
            t.IsVerified = isOk;
        });
    }

    #endregion CalibrationBase

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
        var (_, lensInformationList) = IsLensChanged();

        var isOk = result.Length == lensInformationList.Count
                   && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Microscope Focus is Empty";

        return isOk;
    }

    public static bool IsOk(this MicroscopeCalChipDto result, out string errorMessage)
    {
        errorMessage = string.Empty;
        var (isLensChanged, lensInformationList) = IsLensChanged();

        var isOk = result.IsOk;
        if (isOk == false)
            errorMessage = "Microscope Cal Chip is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.MicroscopeLensInformation = lensInformationList[0];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this MicroscopePixelSizeItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (_, lensInformationList) = IsLensChanged();

        var isOk = result.Length == lensInformationList.Count
                   && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Microscope Pixel Size is Empty";

        return isOk;
    }

    public static bool IsOk(this MicroscopeCentricityItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (_, lensInformationList) = IsLensChanged();

        var isOk = result.Length == lensInformationList.Count
                   && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Microscope Centricity is Empty";

        return isOk;
    }

    #endregion Microscope

    #region Chuck

    public static bool IsOk(this ChuckGantryDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Gantry is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.LowMicroscopeLensInformation = lensInfos[0];
            result.HighMicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckCenterObjDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Center is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.LowMicroscopeLensInformation = lensInfos[0];
            result.HighMicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckPrealignerObjDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Prealigner is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.LowMicroscopeLensInformation = lensInfos[0];
            result.HighMicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckAutoFocusDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Auto Focus is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.MicroscopeLensInformation = lensInfos[0];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckGlobalScaleErrorDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Global Scale is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.LowMicroscopeLensInformation = lensInfos[0];
            result.HighMicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckRotateScaleErrorDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Rotate Scale is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.LowMicroscopeLensInformation = lensInfos[0];
            result.HighMicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckBrightFieldStageMapDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Bright Field Stage Map is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.MicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckDarkFieldStageMapDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Dark Field Stage Map is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.MicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this ChuckStageMapDto result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.IsOk;

        if (isOk == false)
            errorMessage = "Chuck Stage Map is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.HighMicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2];
            cacheProvider.Set(result, CancellationToken.None);
        }

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

    public static bool IsOk(this LaserAttenuatorObjDto[] result, out string errorMessage)
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

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length * 14 && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Laser Illumination Profile is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.ForEach(t => t.MicroscopeLensInformation = lensInfos[0]);
            cacheProvider.SetArray(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this LaserLineCentricityItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.SingleOrDefault(t => t is { PmtId: 8, OpticsMagTypeEnum: OpticsMagTypeEnum.High, StageSpeedEnum: StageSpeedEnum.Low })?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Line Centricity is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.ForEach(t => t.MicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2]);
            cacheProvider.SetArray(result, CancellationToken.None);
        }

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

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.SingleOrDefault(t => t.PmtId == 8 && t.OpticsMagTypeEnum == OpticsMagTypeEnum.High)?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser Pixel Size is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.ForEach(t => t.MicroscopeLensInformation = lensInfos.Count <= 2
                ? lensInfos[^1]
                : lensInfos[2]);
            cacheProvider.SetArray(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this LaserXPixelSizeItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.SingleOrDefault(t => t.PmtId == 8 && t is { OpticsMagTypeEnum: OpticsMagTypeEnum.High, XStageSpeedEnum: StageSpeedEnum.Low })?.IsOk == true;

        if (isOk == false)
            errorMessage = "Laser X Pixel Size is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.ForEach(t => t.MicroscopeLensInformation = lensInfos[0]);
            cacheProvider.SetArray(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this LaserXTCCalibrationItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();

        var isOk = result.SingleOrDefault(t => t.PmtId == 8 && t.OpticsMagTypeEnum == OpticsMagTypeEnum.High)?.IsOk == true;
        if (isOk == false) errorMessage = "Laser XTC is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.ForEach(t => t.MicroscopeLensInformation = lensInfos[0]);
            cacheProvider.SetArray(result, CancellationToken.None);
        }

        return isOk;
    }

    public static bool IsOk(this LaserXYAstigmatismCalibrationItemDto[] result, out string errorMessage)
    {
        errorMessage = string.Empty;

        var (isLensChanged, lensInfos) = IsLensChanged();
        var isOk = result.Length == EnumHelper.Enums<OpticsMagTypeEnum>().Length && result.All(t => t.IsOk);

        if (isOk == false)
            errorMessage = "Laser XY Astigmatism is Empty";

        if (isLensChanged && isOk)
        {
            var cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
            result.ForEach(t => t.MicroscopeLensInformation = lensInfos[0]);
            cacheProvider.SetArray(result, CancellationToken.None);
        }

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

    #endregion Laser
}