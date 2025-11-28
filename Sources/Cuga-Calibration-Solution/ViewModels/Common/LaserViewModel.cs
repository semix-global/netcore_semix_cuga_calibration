using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(LaserViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class LaserViewModel(
    ICalibrationLaserService calibrationLaserService,
    ICalibrationConfigService calibrationConfigService,
    ILogger<LaserViewModel> logger,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    ICacheProvider cacheProvider) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationLaserService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public (Point PD1Point, Point PD2Point) GetLaserBeamPoint()
    {
        var ret = calibrationLaserService.GetLaserBeamPoint();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        return (ret.Anything.PD1Point, ret.Anything.PD2Point);
    }

    public (Point PD1Point, Point PD2Point) GetLaserBeamOriginPoint()
    {
        var ret = calibrationLaserService.GetLaserBeamOriginPoint();

        return ret.IsSuccess ? (ret.Anything.PD1Point, ret.Anything.PD2Point) : throw new CugaException(ret.ErrorMsg);
    }

    public void AdjustBeamStabilizer(bool isEnable)
    {
        var ret = calibrationLaserService.AdjustBeamStabilizer(isEnable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetOpticalMeasurePower()
    {
        var ret = calibrationLaserService.GetOpticalMeasurePower();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetOpticalMeasurePower(ProductivityInformation productivityInformation, double flatnessTime)
    {
        var ret = calibrationLaserService.GetOpticalMeasurePower(productivityInformation, flatnessTime);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<LaserLightInformation> GetLaserLightInformations()
    {
        var ret = calibrationLaserService.GetLaserLightInformations();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public LaserLightInformation LevelToLaserLightInformation(double level)
    {
        var ret = calibrationLaserService.LevelToLaserLightInformation(level);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public LaserLightInformation CoefficientToLaserLightInformation(double coefficient)
    {
        var ret = calibrationLaserService.CoefficientToLaserLightInformation(coefficient);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<ProductivityInformation> GetProductivityInformations()
    {
        var ret = calibrationLaserService.GetProductivityInformations();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    [Obsolete]
    public void ToggleOpticsMagType(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var ret = calibrationLaserService.ToggleOpticsMagType(opticsMagTypeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleOpticsMagType(ProductivityInformation productivityInformation)
    {
        var ret = calibrationLaserService.ToggleOpticsMagType(productivityInformation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum opticsAodWorkingModeEnum)
    {
        var ret = calibrationLaserService.ToggleOpticsAODWorkingMode(opticsAodWorkingModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        var ret = calibrationLaserService.ToggleOpticsPolarization(opticsPolarizationTypeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    [Obsolete]
    public void SetAODDelayValue(OpticsMagTypeEnum opticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        var ret = calibrationLaserService.SetAODDelayValue(opticsMagTypeEnum, prescanAodDelay, chirpAodDelay);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAodDelay, double chirpAodDelay)
    {
        var ret = calibrationLaserService.SetAODDelayValue(productivityInformation, prescanAodDelay, chirpAodDelay);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    [Obsolete]
    public void SetPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum opticsMagTypeEnum, double coefficient)
    {
        var ret = calibrationLaserService.SetDefaultPrescanAODWaveProfileByCoefficient(opticsMagTypeEnum, coefficient);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetPrescanAODWaveProfileByCoefficient(ProductivityInformation productivityInformation, double coefficient)
    {
        var ret = calibrationLaserService.SetDefaultPrescanAODWaveProfileByCoefficient(productivityInformation, coefficient);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetPrescanAODWaveProfiles(IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfiles)
    {
        var ret = calibrationLaserService.SetPrescanAODWaveProfiles(prescanAODWaveProfiles);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    [Obsolete]
    public void SetChirpAODWaveProfile(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var ret = calibrationLaserService.SetDefaultChirpAODWaveProfile(opticsMagTypeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChirpAODWaveProfile(ProductivityInformation productivityInformation)
    {
        var ret = calibrationLaserService.SetDefaultChirpAODWaveProfile(productivityInformation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChirpAODWaveProfiles(IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfiles)
    {
        var ret = calibrationLaserService.SetChirpAODWaveProfiles(chirpAODWaveProfiles);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<PrescanAODWaveformProfile> GeneratePrescanAodWaves(GeneratePrescanAODWaveformParam generatePrescanAODWaveformParam)
    {
        var ret = calibrationLaserService.GeneratePrescanAodWaves(generatePrescanAODWaveformParam);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<ChirpAODWaveformProfile> GenerateChirpAodWaves(GenerateChirpAODWaveformParam generateChirpAODWaveformParam)
    {
        var ret = calibrationLaserService.GenerateChirpAodWaves(generateChirpAODWaveformParam);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleCIBControlModeAndProfileType(CIBConfiguration cIbConfiguration, int pmtId = CalibrationConstantsHelper.MainPmtId, int channelId = CalibrationConstantsHelper.MainChannelId)
    {
        var ret = calibrationLaserService.ToggleCIBControlTypeAndProfileType(cIbConfiguration, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableAutoGainControl(bool enable, int pmtId = Constants.NegInt32Value, int channelId = Constants.NegInt32Value)
    {
        var ret = calibrationLaserService.ToggleEnableAutoGainControl(enable, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleProfileMode(CIBProfileModeEnum cibProfileModeEnum, int pmtId = Constants.NegInt32Value, int channelId = Constants.NegInt32Value)
    {
        var ret = calibrationLaserService.ToggleProfileMode(cibProfileModeEnum, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableMarkMode(bool enable, int pmtId = Constants.NegInt32Value, int channelId = Constants.NegInt32Value)
    {
        var ret = calibrationLaserService.ToggleEnableMarkMode(enable, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableL0K(bool enable, int pmtId = Constants.NegInt32Value, int channelId = Constants.NegInt32Value)
    {
        var ret = calibrationLaserService.ToggleEnableL0K(enable, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        var ret = calibrationLaserService.SetGain(cibInformations, gain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetGain(double gain, int pmtId = Constants.NegInt32Value, int channelId = Constants.NegInt32Value)
    {
        var ret = calibrationLaserService.SetGain(gain, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSaturation(double saturation)
    {
        var ret = calibrationLaserService.SetSaturation(saturation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<CIBInformation> GetCIBInformations()
    {
        var ret = calibrationLaserService.GetCIBInformations();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<(int PmtId, IReadOnlyList<int> ChannelIdList)> GetIsUsedCIBConfigList()
    {
        var ret = calibrationLaserService.GetCIBConfigList();

        return ret.IsSuccess
            ? ret.Anything
                .Where(t => t.IsUsed)
                .Select(t => (t.PmtId, t.ChannelIdList))
                .ToList()
            : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<IReadOnlyList<double>> GetCIBOfPMTDataList(int count, int pmtId, int channel)
    {
        var ret = calibrationLaserService.GetCIBOfPMTDataList(count, pmtId, channel);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<IReadOnlyList<double>> GetCIBOfPMTDataList(int count, CIBInformation cibInformation)
    {
        var ret = calibrationLaserService.GetCIBOfPMTDataList(count, cibInformation.PMTId, cibInformation.ChannelId);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<DarkFieldPmtDataDto> GetCIBOfPMTDataList()
    {
        var ret = calibrationLaserService.GetCIBOfPMTDataList();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public async Task<IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>>> GetCIBOfSenseDataListAsync(int count, int pmtId)
    {
        var channelIdList = GetIsUsedCIBConfigList().Single(t => t.PmtId == pmtId).ChannelIdList;
        var result = new IReadOnlyList<IReadOnlyList<double>>[channelIdList.Count];

        await Task.WhenAll(channelIdList.Select((channelId, index) => Task.Run(() => result[index] = GetCIBOfSenseDataList(count, pmtId, channelId))));

        return [.. result];
    }

    public IReadOnlyList<IReadOnlyList<double>> GetCIBOfSenseDataList(int count, int pmtId, int channelId)
    {
        var ret = calibrationLaserService.GetCIBOfSenseDataList(count, pmtId, channelId);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<DarkFieldPmtDelayDto> GetCIBDelayList()
    {
        var ret = calibrationLaserService.GetCIBDelayList();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetCIBDelayList(IReadOnlyList<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        var ret = calibrationLaserService.SetCIBDelayList(darkFieldPmtDelayDtoList);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SendCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId)
    {
        var ret = calibrationLaserService.SetCIBChirp(gainList, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetCIBMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bit)
    {
        var ret = calibrationLaserService.SetCIBMMD(cibInformation, logGainMul128U12Bits, gainS16Bit);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SendPmtGain(List<string> pmtData, List<string> igData, int pmtId, int channelId)
    {
        var ret = calibrationLaserService.SendPMTGain(pmtData, igData, pmtId, channelId);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    [Obsolete]
    public (double Ecs, double AfMotor) RuntimeAfCalibration(
        CIBConfiguration cibConfiguration,
        Point position,
        LaserLightInformation laserLightInformation,
        bool isAppliedDefaultRtfcParam = true,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = OpticsMagTypeEnum.High,
        StageSpeedEnum xStageSpeedEnum = StageSpeedEnum.Low,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright,
        string? saveImageFileDirectory = null,
        Guid? logGuid = null,
        string? logName = null
    )
    {
        var lightInformation = isAppliedDefaultRtfcParam ? null : laserLightInformation;
        Point? point = isAppliedDefaultRtfcParam && calChipSiteModelEnum is not CalChipSiteModelEnum.ChuckModel ? null : position;

        var ret = calibrationLaserService.RuntimeAfCalibration(calChipSiteModelEnum, pmtId, lightInformation?.Coefficient, point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var rtfcResult = ret.Anything;
        var settingDarkFieldAutoFocusParam = calChipSiteModelEnum switch
        {
            CalChipSiteModelEnum.ChuckModel => new SettingDarkFieldAutoFocusParam() { ChuckEcsValue = rtfcResult.Ecs, ChuckMotorValue = rtfcResult.AfMotor, IsEnableChuck = true },
            CalChipSiteModelEnum.DswModel => new SettingDarkFieldAutoFocusParam() { DswEcsValue = rtfcResult.Ecs, DswMotorValue = rtfcResult.AfMotor, IsEnableDsw = true },
            CalChipSiteModelEnum.HazeModel => new SettingDarkFieldAutoFocusParam() { HazeEcsValue = rtfcResult.Ecs, HazeMotorValue = rtfcResult.AfMotor, IsEnableHaze = true },
            _ => throw new ArgumentOutOfRangeException(nameof(calChipSiteModelEnum), calChipSiteModelEnum, null)
        };
        afViewModel.SetDarkFieldAutoFocus(settingDarkFieldAutoFocusParam, yOpticsMagTypeEnum, calChipSiteModelEnum);
        using var darkFieldImageDto = GetDarkFieldLineScanImage(
            calChipSiteModelEnum,
            position,
            (false, calibrationSetting.SettingCommonParam.MainLaserLightInformation),
            false,
            cibConfiguration,
            800,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            pmtId,
            stageCoordinateSystemEnum: stageCoordinateSystemEnum); // 模板匹配只能通道3(1, 2特征不明显)

        if (saveImageFileDirectory is not null)
        {
            var rtfcResultImagePath = $"{saveImageFileDirectory}\\RTFCThumb\\logTitle\\{calChipSiteModelEnum}Guid{logGuid}.jpg";
            darkFieldImageDto.Image.Save(rtfcResultImagePath);
            File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(rtfcResultImagePath), darkFieldImageDto.Bytes);
            if (logGuid is not null && logName is not null)
                logger.LogHtmlInformation($"{logName} RTFC", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    point,
                    calChipSiteModelEnum,
                    pmtId,
                    laserLightInformation,
                    ret.Anything.Ecs,
                    ret.Anything.AfMotor,
                    HtmlTab = new HtmlTab(new
                    {
                        RTFCResultImage = new HtmlImage(rtfcResultImagePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                    })
                }), logGuid.Value.LoggingHtml());
        }

        return ret.IsSuccess ? rtfcResult : throw new CugaException(ret.ErrorMsg);
    }

    public (double Ecs, double AfMotor) RuntimeAfCalibration(
        CIBConfiguration cibConfiguration,
        Point position,
        LaserLightInformation laserLightInformation,
        ProductivityInformation productivityInformation,
        bool isAppliedDefaultRtfcParam = true,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright,
        string? saveImageFileDirectory = null,
        Guid? logGuid = null,
        string? logName = null
    )
    {
        var lightInformation = isAppliedDefaultRtfcParam ? null : laserLightInformation;
        Point? point = isAppliedDefaultRtfcParam && calChipSiteModelEnum is not CalChipSiteModelEnum.ChuckModel ? null : position;

        var ret = calibrationLaserService.RuntimeAfCalibration(calChipSiteModelEnum, pmtId, lightInformation?.Coefficient, point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var rtfcResult = ret.Anything;
        var settingDarkFieldAutoFocusParam = calChipSiteModelEnum switch
        {
            CalChipSiteModelEnum.ChuckModel => new SettingDarkFieldAutoFocusParam() { ChuckEcsValue = rtfcResult.Ecs, ChuckMotorValue = rtfcResult.AfMotor, IsEnableChuck = true },
            CalChipSiteModelEnum.DswModel => new SettingDarkFieldAutoFocusParam() { DswEcsValue = rtfcResult.Ecs, DswMotorValue = rtfcResult.AfMotor, IsEnableDsw = true },
            CalChipSiteModelEnum.HazeModel => new SettingDarkFieldAutoFocusParam() { HazeEcsValue = rtfcResult.Ecs, HazeMotorValue = rtfcResult.AfMotor, IsEnableHaze = true },
            _ => throw new ArgumentOutOfRangeException(nameof(calChipSiteModelEnum), calChipSiteModelEnum, null)
        };
        afViewModel.SetDarkFieldAutoFocus(settingDarkFieldAutoFocusParam, calChipSiteModelEnum);
        using var darkFieldImageDto = GetDarkFieldLineScanImage(
            calChipSiteModelEnum,
            position,
            (false, calibrationSetting.SettingCommonParam.MainLaserLightInformation),
            false,
            cibConfiguration,
            productivityInformation,
            800,
            pmtId,
            stageCoordinateSystemEnum: stageCoordinateSystemEnum); // 模板匹配只能通道3(1, 2特征不明显)

        if (saveImageFileDirectory is not null)
        {
            var rtfcResultImagePath = $"{saveImageFileDirectory}\\RTFCThumb\\logTitle\\{calChipSiteModelEnum}Guid{logGuid}.jpg";
            darkFieldImageDto.Image.Save(rtfcResultImagePath);
            File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(rtfcResultImagePath), darkFieldImageDto.Bytes);
            if (logGuid is not null && logName is not null)
                logger.LogHtmlInformation($"{logName} RTFC", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    point,
                    calChipSiteModelEnum,
                    pmtId,
                    laserLightInformation,
                    ret.Anything.Ecs,
                    ret.Anything.AfMotor,
                    HtmlTab = new HtmlTab(new
                    {
                        RTFCResultImage = new HtmlImage(rtfcResultImagePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                    })
                }), logGuid.Value.LoggingHtml());
        }

        return ret.IsSuccess ? rtfcResult : throw new CugaException(ret.ErrorMsg);
    }

    [Obsolete]
    public int GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum opticsMagTypeEnum, bool isCuttingPixelHeight = true)
    {
        var ret = calibrationLaserService.GetDarkFieldLineScanImageYPixelHeight(opticsMagTypeEnum, isCuttingPixelHeight);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public int GetDarkFieldLineScanImageYPixelHeight(ProductivityInformation productivityInformation, bool isCuttingPixelHeight = true)
    {
        var ret = calibrationLaserService.GetDarkFieldLineScanImageYPixelHeight(productivityInformation, isCuttingPixelHeight);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    [Obsolete]
    public bool TrySendAodFile(
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        if (customPrescanAod.IsCustomPrescanAod == false)
        {
            Guard.IsNotNull(customPrescanAod.LaserLightInformation, nameof(customPrescanAod.LaserLightInformation));

            SetPrescanAODWaveProfileByCoefficient(yOpticsMagTypeEnum, customPrescanAod.LaserLightInformation.Coefficient);
        }
        else
            Guard.IsNull(customPrescanAod.LaserLightInformation, nameof(customPrescanAod.LaserLightInformation));

        if (isCustomChirpAod == false)
        {
            SetChirpAODWaveProfile(yOpticsMagTypeEnum);
        }

        return true;
    }

    public bool TrySendAodFile(
        ProductivityInformation productivityInformation,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        if (customPrescanAod.IsCustomPrescanAod == false)
        {
            Guard.IsNotNull(customPrescanAod.LaserLightInformation, nameof(customPrescanAod.LaserLightInformation));

            SetPrescanAODWaveProfileByCoefficient(productivityInformation, customPrescanAod.LaserLightInformation.Coefficient);
        }
        else
            Guard.IsNull(customPrescanAod.LaserLightInformation, nameof(customPrescanAod.LaserLightInformation));

        if (isCustomChirpAod == false)
        {
            SetChirpAODWaveProfile(productivityInformation);
        }

        return true;
    }

    #region DOE

    public double ReadDOECurrentAngle()
    {
        var ret = calibrationLaserService.ReadDOECurrentAngle();
        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetDOEAngle(double angle)
    {
        var ret = calibrationLaserService.SetDOEAngle(angle);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    #endregion DOE

    [Obsolete]
    public List<DarkFieldImageDto> GetDarkFieldLineScanImageList(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CIBConfiguration cibConfiguration,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        try
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
                    break;

                case StageCoordinateSystemEnum.Dark:
                    stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(position, calChipSiteModelEnum);
                    break;

                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }

            if (TrySendAodFile(yOpticsMagTypeEnum, customPrescanAod, isCustomChirpAod, out var errorMessage) == false) throw new CugaException(errorMessage);

            // 采图模式下发
            ToggleCIBControlModeAndProfileType(cibConfiguration, pmtId, -1);

            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(position, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, isAutoFocus, isForward);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
                    break;

                case StageCoordinateSystemEnum.Dark:
                    stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(position);
                    break;

                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }
        }
    }

    public List<DarkFieldImageDto> GetDarkFieldLineScanImageList(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        int xWidthPixel,
        ProductivityInformation productivityInformation,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CIBConfiguration cibConfiguration,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        try
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
                    break;

                case StageCoordinateSystemEnum.Dark:
                    stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(position, calChipSiteModelEnum);
                    break;

                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }

            if (TrySendAodFile(productivityInformation, customPrescanAod, isCustomChirpAod, out var errorMessage) == false) throw new CugaException(errorMessage);

            // 采图模式下发
            ToggleCIBControlModeAndProfileType(cibConfiguration, pmtId, -1);

            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(position, xWidthPixel, productivityInformation, pmtId, stageCoordinateSystemEnum, isAutoFocus, isForward);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
                    break;

                case StageCoordinateSystemEnum.Dark:
                    stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(position);
                    break;

                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }
        }
    }

    [Obsolete]
    public DarkFieldImageDto GetDarkFieldLineScanImage(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        CIBConfiguration cIbConfiguration,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        var result = GetDarkFieldLineScanImageList(
            calChipSiteModelEnum,
            position,
            xWidthPixel,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            pmtId,
            stageCoordinateSystemEnum,
            cIbConfiguration,
            customPrescanAod,
            isCustomChirpAod,
            isForward,
            isAutoFocus);

        var darkFieldImageDto = result.Single(t => t.ChannelId == channelId);

        foreach (var item in result.Where(t => t.ChannelId != channelId).Select(t => t.Image))
        {
            using var _ = item;
        }

        return darkFieldImageDto;
    }

    public DarkFieldImageDto GetDarkFieldLineScanImage(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        CIBConfiguration cIbConfiguration,
        ProductivityInformation productivityInformation,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        var result = GetDarkFieldLineScanImageList(
            calChipSiteModelEnum,
            position,
            xWidthPixel,
            productivityInformation,
            pmtId,
            stageCoordinateSystemEnum,
            cIbConfiguration,
            customPrescanAod,
            isCustomChirpAod,
            isForward,
            isAutoFocus);

        var darkFieldImageDto = result.Single(t => t.ChannelId == channelId);

        foreach (var item in result.Where(t => t.ChannelId != channelId).Select(t => t.Image))
        {
            using var _ = item;
        }

        return darkFieldImageDto;
    }

    /// <summary>
    /// PTP行扫
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    /// <param name="yOpticsMagTypeEnum"></param>
    /// <param name="xStageSpeedEnum"></param>
    /// <param name="pmtId"></param>
    /// <param name="stageCoordinateSystemEnum"></param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="customPrescanAod"></param>
    /// <param name="isCustomChirpAod"></param>
    /// <param name="isForward"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns></returns>
    /// <exception cref="CugaException"></exception>
    [Obsolete]
    public List<DarkFieldRawScanImageDto> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CIBConfiguration cibConfiguration,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        try
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }

            if (TrySendAodFile(yOpticsMagTypeEnum, customPrescanAod, isCustomChirpAod, out var errorMessage) == false) throw new CugaException(errorMessage);

            // 采图模式下发
            var toggleCIBModeRet = calibrationLaserService.ToggleCIBControlTypeAndProfileType(cibConfiguration, pmtId, -1);
            if (toggleCIBModeRet.IsSuccess == false) throw new CugaException(toggleCIBModeRet.ErrorMsg);

            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(startPosition, endPosition, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, isAutoFocus, isForward);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }
        }
    }

    public List<DarkFieldRawScanImageDto> GetDarkFieldLineScanImageList(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point startPosition,
        Point endPosition,
        ProductivityInformation productivityInformation,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CIBConfiguration cibConfiguration,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        try
        {
            var startMachinePosition = startPosition;
            var endMachinePosition = endPosition;

            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    startMachinePosition = stageViewModel.DarkFieldToMachinePosition(startPosition);
                    endMachinePosition = stageViewModel.DarkFieldToMachinePosition(endPosition);

                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                case StageCoordinateSystemEnum.Dark:
                    startMachinePosition = stageViewModel.DarkFieldToMachinePosition(startPosition);
                    endMachinePosition = stageViewModel.DarkFieldToMachinePosition(endPosition);

                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }

            if (TrySendAodFile(productivityInformation, customPrescanAod, isCustomChirpAod, out var errorMessage) == false) throw new CugaException(errorMessage);

            // 采图模式下发
            ToggleCIBControlModeAndProfileType(cibConfiguration, pmtId, -1);

            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(startMachinePosition, endMachinePosition, productivityInformation, pmtId, StageCoordinateSystemEnum.Machine, isAutoFocus, isForward);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(startPosition, calChipSiteModelEnum);
                    break;

                case StageCoordinateSystemEnum.Dark:
                    stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }
        }
    }

    public DarkFieldRawScanImageDto GetDarkFieldLineScanImage(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point startPosition,
        Point endPosition,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        CIBConfiguration cIbConfiguration,
        ProductivityInformation productivityInformation,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        var result = GetDarkFieldLineScanImageList(
            calChipSiteModelEnum,
            startPosition,
            endPosition,
            productivityInformation,
            pmtId,
            stageCoordinateSystemEnum,
            cIbConfiguration,
            customPrescanAod,
            isCustomChirpAod,
            isForward,
            isAutoFocus);

        return result.Single(t => t.ChannelId == channelId);
    }

    /// <summary>
    /// PTP行扫
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    /// <param name="productivityInformation"></param>
    /// <param name="pmtId"></param>
    /// <param name="stageCoordinateSystemEnum"></param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="customPrescanAod"></param>
    /// <param name="isCustomChirpAod"></param>
    /// <param name="isForward"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns></returns>
    /// <exception cref="CugaException"></exception>
    public List<DarkFieldRawScanImageDto> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        ProductivityInformation productivityInformation,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CIBConfiguration cibConfiguration,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        try
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }

            if (TrySendAodFile(productivityInformation, customPrescanAod, isCustomChirpAod, out var errorMessage) == false) throw new CugaException(errorMessage);

            // 采图模式下发
            var toggleCIBModeRet = calibrationLaserService.ToggleCIBControlTypeAndProfileType(cibConfiguration, pmtId, -1);
            if (toggleCIBModeRet.IsSuccess == false) throw new CugaException(toggleCIBModeRet.ErrorMsg);

            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(startPosition, endPosition, productivityInformation, pmtId, stageCoordinateSystemEnum, isAutoFocus, isForward);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                    break;
            }
        }
    }

    /// <summary>
    /// 行扫采图分割
    /// </summary>
    /// <param name="positionList"></param>
    /// <param name="xWidthPixel"></param>
    /// <param name="yOpticsMagTypeEnum"></param>
    /// <param name="xStageSpeedEnum"></param>
    /// <param name="pmtId"></param>
    /// <param name="stageCoordinateSystemEnum"></param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="customPrescanAod"></param>
    /// <param name="isCustomChirpAod"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="CugaException"></exception>
    [Obsolete]
    public List<List<DarkFieldImageDto>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> positionList,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CIBConfiguration cibConfiguration,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        bool isAutoFocus = true)
    {
        var xSize = cacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>()
            .SingleOrDefault(t => t.ProductivityInformation.OpticsMagType == (int)yOpticsMagTypeEnum
                                  && t.ProductivityInformation.StageSpeedType == (int)xStageSpeedEnum);
        if (xSize is null || xSize.IsOk == false) ThrowHelper.ThrowArgumentException("Invalid Laser X Pixel Size Item");

        switch (stageCoordinateSystemEnum)
        {
            case StageCoordinateSystemEnum.Machine:
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(positionList.First());
                break;

            case StageCoordinateSystemEnum.Dark:
                stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(positionList.First());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null);
        }

        if (TrySendAodFile(yOpticsMagTypeEnum, customPrescanAod, isCustomChirpAod, out var errorMessage) == false) throw new CugaException(errorMessage);

        // 采图模式下发
        var toggleCIBModeRet = calibrationLaserService.ToggleCIBControlTypeAndProfileType(cibConfiguration, pmtId, -1);
        if (toggleCIBModeRet.IsSuccess == false) throw new CugaException(toggleCIBModeRet.ErrorMsg);

        var ret = calibrationLaserService.GetChuckDarkFieldRowLineScanImageList(
            positionList,
            xWidthPixel,
            xSize.XPixelSize,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            pmtId,
            stageCoordinateSystemEnum,
            isAutoFocus);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    /// <summary>
    /// 行扫采图分割
    /// </summary>
    /// <param name="positionList"></param>
    /// <param name="xWidthPixel"></param>
    /// <param name="productivityInformation"></param>
    /// <param name="pmtId"></param>
    /// <param name="stageCoordinateSystemEnum"></param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="customPrescanAod"></param>
    /// <param name="isCustomChirpAod"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="CugaException"></exception>
    public List<List<DarkFieldImageDto>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> positionList,
        int xWidthPixel,
        ProductivityInformation productivityInformation,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CIBConfiguration cibConfiguration,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        bool isAutoFocus = true)
    {
        var xSize = cacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>()
            .SingleOrDefault(t => t.ProductivityInformation == productivityInformation);
        if (xSize is null || xSize.IsOk == false) ThrowHelper.ThrowArgumentException("Invalid Laser X Pixel Size Item");

        switch (stageCoordinateSystemEnum)
        {
            case StageCoordinateSystemEnum.Machine:
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(positionList.First());
                break;

            case StageCoordinateSystemEnum.Dark:
                stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(positionList.First());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null);
        }

        if (TrySendAodFile(productivityInformation, customPrescanAod, isCustomChirpAod, out var errorMessage) == false) throw new CugaException(errorMessage);

        // 采图模式下发
        var toggleCIBModeRet = calibrationLaserService.ToggleCIBControlTypeAndProfileType(cibConfiguration, pmtId, -1);
        if (toggleCIBModeRet.IsSuccess == false) throw new CugaException(toggleCIBModeRet.ErrorMsg);

        var ret = calibrationLaserService.GetChuckDarkFieldRowLineScanImageList(
            positionList,
            xWidthPixel,
            xSize.XPixelSize,
            productivityInformation,
            pmtId,
            stageCoordinateSystemEnum,
            isAutoFocus);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    /// <summary>
    /// 行扫采图分割
    /// </summary>
    /// <param name="positionList"></param>
    /// <param name="customPrescanAod"></param>
    /// <param name="isCustomChirpAod"></param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="xWidthPixel"></param>
    /// <param name="yOpticsMagTypeEnum"></param>
    /// <param name="xStageSpeedEnum"></param>
    /// <param name="pmtId"></param>
    /// <param name="channelId"></param>
    /// <param name="stageCoordinateSystemEnum"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns>指定通道的分割结果集合</returns>
    /// <exception cref="CugaException"></exception>
    [Obsolete]
    public List<DarkFieldImageDto> GetChuckDarkFieldRowLineScanImage(
        List<Point> positionList,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        CIBConfiguration cibConfiguration,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        bool isAutoFocus = true)
    {
        var temp = GetChuckDarkFieldRowLineScanImageList(
            positionList,
            xWidthPixel,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            pmtId,
            stageCoordinateSystemEnum,
            cibConfiguration,
            customPrescanAod,
            isCustomChirpAod,
            isAutoFocus);

        var result = temp.SelectMany(t => t).Where(t => t.ChannelId == channelId).ToList();

        foreach (var item in temp.SelectMany(t => t).Where(t => t.ChannelId != channelId).Select(t => t.Image))
        {
            using var _ = item;
        }

        return result.Count > 0 ? result : throw new CugaException("Get Dark Field Line Scan Image failed");
    }

    /// <summary>
    /// 行扫采图分割
    /// </summary>
    /// <param name="positionList"></param>
    /// <param name="customPrescanAod"></param>
    /// <param name="isCustomChirpAod"></param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="productivityInformation"></param>
    /// <param name="xWidthPixel"></param>
    /// <param name="pmtId"></param>
    /// <param name="channelId"></param>
    /// <param name="stageCoordinateSystemEnum"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns>指定通道的分割结果集合</returns>
    /// <exception cref="CugaException"></exception>
    public List<DarkFieldImageDto> GetChuckDarkFieldRowLineScanImage(
        List<Point> positionList,
        (bool IsCustomPrescanAod, LaserLightInformation? LaserLightInformation) customPrescanAod,
        bool isCustomChirpAod,
        CIBConfiguration cibConfiguration,
        ProductivityInformation productivityInformation,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        bool isAutoFocus = true)
    {
        var temp = GetChuckDarkFieldRowLineScanImageList(
            positionList,
            xWidthPixel,
            productivityInformation,
            pmtId,
            stageCoordinateSystemEnum,
            cibConfiguration,
            customPrescanAod,
            isCustomChirpAod,
            isAutoFocus);

        var result = temp.SelectMany(t => t).Where(t => t.ChannelId == channelId).ToList();

        foreach (var item in temp.SelectMany(t => t).Where(t => t.ChannelId != channelId).Select(t => t.Image))
        {
            using var _ = item;
        }

        return result.Count > 0 ? result : throw new CugaException("Get Dark Field Line Scan Image failed");
    }

    #region 模板匹配

    /// <summary>
    /// 匹配模板: 从旧位置到匹配后位置
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="darkFieldImageDto">匹配的原图</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">明场位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="saveResultImageFileDirectory">匹配后成功的[保存的匹配图片的文件目录]</param>
    /// <param name="logGuid">记录日志: id</param>
    /// <param name="logName">记录日志: 名称</param>
    /// <param name="logResultTitle">记录日志: 匹配后成功的[标题]</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <exception cref="AlgorithmException"></exception>
    /// <returns>是否成功</returns>
    [Obsolete]
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        DarkFieldImageDto darkFieldImageDto,
        int pmtId,
        Point position,
        string templateFilePath,
        string? saveResultImageFileDirectory,
        Guid? logGuid,
        string? logName,
        string? logResultTitle,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum)
    {
        resultPosition = Point.Origin;
        resultScore = 0;
        resultAngle = 0;
        resultImageFilePath = string.Empty;

        var ySize = cacheProvider.GetOrDefaultArray<LaserPixelSizeItemDto>()
            .SingleOrDefault(t => t.ProductivityInformation.OpticsMagType == (int)yOpticsMagTypeEnum
                                  && t.ProductivityInformation.StageSpeedType == (int)xStageSpeedEnum
                                  && t.PmtId == pmtId);
        if (ySize is null || ySize.IsOk == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Laser Pixel Size is Empty or not verify."), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Laser Pixel Size is Empty or not verify", nameof(ReviewViewModel));
            return false;
        }

        var xSize = cacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>()
            .SingleOrDefault(t => t.ProductivityInformation.OpticsMagType == (int)yOpticsMagTypeEnum
                                  && t.ProductivityInformation.StageSpeedType == (int)xStageSpeedEnum);
        if (xSize is null || xSize.IsOk == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Laser X Pixel Size is Empty or not verify."), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Laser X Pixel Size is Empty or not verify", nameof(ReviewViewModel));
            return false;
        }

        var isSuccess = calibrationAlgorithmService.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out var templateId);
        using var _1 = templateId;
        if (isSuccess == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Read Template Failed!"), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Read Template Failed!", nameof(ReviewViewModel));

            return false;
        }

        try
        {
            using var image = isForward ? darkFieldImageDto.Image : darkFieldImageDto.Image.HorizontalFlip();
            isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out var markPoint, out var offset, out resultScore, out resultAngle);
            if (isSuccess == false)
            {
                var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
                resultImageFilePath = $"{FileHelper.GetFileFullName(templateFilePath)}_Error\\Score({resultScore:f3},{templateMatchScoreThreshold})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                darkFieldImageDto.Image.Save(resultImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(resultImageFilePath), darkFieldImageDto.Bytes);
                if (logGuid is not null && logName is not null)
                    logger.LogHtmlError($"{logName} Error: Try Math Template To Offset Failed.{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                    {
                        darkFieldImageDto.PmtId,
                        darkFieldImageDto.ChannelId,
                        darkFieldImageDto.Width,
                        XWidthPixel = xWidthPixel,
                        OpticsMagTypeEnum = yOpticsMagTypeEnum,
                        StageSpeedEnum = xStageSpeedEnum,
                        OriginPosition = position,
                        Score = resultScore,
                        TemplateMatchScoreThreshold = templateMatchScoreThreshold,
                        HtmlTab = new HtmlTab(new
                        {
                            OriginImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                            TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Error: Try Math Template To Offset Failed", nameof(ReviewViewModel));

                return false;
            }

            if (saveResultImageFileDirectory is not null)
            {
                resultImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                using var temp = darkFieldImageDto.Image.DrawCrossLine(isForward ? markPoint : new Point(xWidthPixel - markPoint.X, markPoint.Y));

                temp.Save(resultImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(resultImageFilePath), darkFieldImageDto.Bytes);
            }

            if (stageCoordinateSystemEnum == StageCoordinateSystemEnum.Machine)
            {
                var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
                offset = new Point(xDirection * offset.X, yDirection * offset.Y);
            }

            var actualOffset = new Point(offset.X * xSize.XPixelSize, offset.Y * ySize.YPixelSize);
            resultPosition = position + (Vector)actualOffset;

            if (logGuid is not null && logName is not null && logResultTitle is not null)
                logger.LogHtmlInformation($"{logName} Match Template:{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                {
                    XWidthPixel = xWidthPixel,
                    OpticsMagTypeEnum = yOpticsMagTypeEnum,
                    StageSpeedEnum = xStageSpeedEnum,
                    OriginPosition = position,
                    ResultPosition = resultPosition,
                    ResultOffset = actualOffset,
                    ResultScore = resultScore,
                    ResultAngle = resultAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(resultImageFilePath, description: "ResultImage", htmlImageOverlays: [new HtmlImageCrossOverlay(isForward ? markPoint : new Point(xWidthPixel - markPoint.X, markPoint.Y)), new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), description: "TemplateImage", htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), logGuid.Value.LoggingHtml());
            return true;
        }
        finally
        {
            var tryCleanTemplate = calibrationAlgorithmService.TryCleanTemplate(algorithmTemplateTypeEnum, templateId);
            if (tryCleanTemplate == false)
            {
                if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Clean Template Failed."), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Clean Template Failed", nameof(ReviewViewModel));
            }
        }
    }

    /// <summary>
    /// 匹配模板: 从旧位置到匹配后位置
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="darkFieldImageDto">匹配的原图</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">明场位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="saveResultImageFileDirectory">匹配后成功的[保存的匹配图片的文件目录]</param>
    /// <param name="logGuid">记录日志: id</param>
    /// <param name="logName">记录日志: 名称</param>
    /// <param name="logResultTitle">记录日志: 匹配后成功的[标题]</param>
    /// <param name="productivityInformation"></param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <exception cref="AlgorithmException"></exception>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        DarkFieldImageDto darkFieldImageDto,
        int pmtId,
        Point position,
        string templateFilePath,
        string? saveResultImageFileDirectory,
        Guid? logGuid,
        string? logName,
        string? logResultTitle,
        ProductivityInformation productivityInformation,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum)
    {
        resultPosition = Point.Origin;
        resultScore = 0;
        resultAngle = 0;
        resultImageFilePath = string.Empty;

        var ySize = cacheProvider.GetOrDefaultArray<LaserPixelSizeItemDto>()
            .SingleOrDefault(t => t.ProductivityInformation.OpticsMagType == productivityInformation.OpticsMagType
                                  && t.PmtId == pmtId);
        if (ySize is null || ySize.IsOk == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Laser Pixel Size is Empty or not verify."), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Laser Pixel Size is Empty or not verify", nameof(ReviewViewModel));
            return false;
        }

        var xSize = cacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>().SingleOrDefault(t => t.ProductivityInformation == productivityInformation);
        if (xSize is null || xSize.IsOk == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Laser X Pixel Size is Empty or not verify."), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Laser X Pixel Size is Empty or not verify", nameof(ReviewViewModel));
            return false;
        }

        var isSuccess = calibrationAlgorithmService.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out var templateId);
        using var _1 = templateId;
        if (isSuccess == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Read Template Failed!"), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Read Template Failed!", nameof(ReviewViewModel));

            return false;
        }

        try
        {
            using var image = isForward ? darkFieldImageDto.Image : darkFieldImageDto.Image.HorizontalFlip();
            isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out var markPoint, out var offset, out resultScore, out resultAngle);
            if (isSuccess == false)
            {
                var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
                resultImageFilePath = $"{FileHelper.GetFileFullName(templateFilePath)}_Error\\Score({resultScore:f3},{templateMatchScoreThreshold})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                darkFieldImageDto.Image.Save(resultImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(resultImageFilePath), darkFieldImageDto.Bytes);
                if (logGuid is not null && logName is not null)
                    logger.LogHtmlError($"{logName} Error: Try Math Template To Offset Failed.{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                    {
                        darkFieldImageDto.PmtId,
                        darkFieldImageDto.ChannelId,
                        darkFieldImageDto.Width,
                        XWidthPixel = xWidthPixel,
                        productivityInformation,
                        OriginPosition = position,
                        Score = resultScore,
                        TemplateMatchScoreThreshold = templateMatchScoreThreshold,
                        HtmlTab = new HtmlTab(new
                        {
                            OriginImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                            TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Error: Try Math Template To Offset Failed", nameof(ReviewViewModel));

                return false;
            }

            if (saveResultImageFileDirectory is not null)
            {
                resultImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                using var temp = darkFieldImageDto.Image.DrawCrossLine(isForward ? markPoint : new Point(xWidthPixel - markPoint.X, markPoint.Y));

                temp.Save(resultImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(resultImageFilePath), darkFieldImageDto.Bytes);
            }

            if (stageCoordinateSystemEnum == StageCoordinateSystemEnum.Machine)
            {
                var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
                offset = new Point(xDirection * offset.X, yDirection * offset.Y);
            }

            var actualOffset = new Point(offset.X * xSize.XPixelSize, offset.Y * ySize.YPixelSize);
            resultPosition = position + (Vector)actualOffset;

            if (logGuid is not null && logName is not null && logResultTitle is not null)
                logger.LogHtmlInformation($"{logName} Match Template:{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                {
                    XWidthPixel = xWidthPixel,
                    productivityInformation,
                    OriginPosition = position,
                    ResultPosition = resultPosition,
                    ResultOffset = actualOffset,
                    ResultScore = resultScore,
                    ResultAngle = resultAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(resultImageFilePath, description: "ResultImage", htmlImageOverlays: [new HtmlImageCrossOverlay(isForward ? markPoint : new Point(xWidthPixel - markPoint.X, markPoint.Y)), new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), description: "TemplateImage", htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), logGuid.Value.LoggingHtml());
            return true;
        }
        finally
        {
            var tryCleanTemplate = calibrationAlgorithmService.TryCleanTemplate(algorithmTemplateTypeEnum, templateId);
            if (tryCleanTemplate == false)
            {
                if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Clean Template Failed."), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Clean Template Failed", nameof(ReviewViewModel));
            }
        }
    }

    /// <summary>
    /// 匹配模板: 从旧位置到匹配后位置，包含暗场采图
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">明场位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="saveResultImageFileDirectory">匹配后成功的[保存的匹配图片的文件目录]</param>
    /// <param name="logGuid">记录日志: id</param>
    /// <param name="logName">记录日志: 名称</param>
    /// <param name="logResultTitle">记录日志: 匹配后成功的[标题]</param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="laserLightInformation">功率</param>
    /// <param name="isAutoFocus"></param>
    /// <exception cref="AlgorithmException"></exception>
    /// <returns>是否成功</returns>
    [Obsolete]
    public bool TryGetMatchPositionByScanImage(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        Point position,
        string templateFilePath,
        string? saveResultImageFileDirectory,
        Guid? logGuid,
        string? logName,
        string? logResultTitle,
        CIBConfiguration cibConfiguration,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        LaserLightInformation? laserLightInformation = null,
        bool isAutoFocus = true)
    {
        if (laserLightInformation is null) laserLightInformation = calibrationSetting.SettingCommonParam.MainLaserLightInformation;

        using var darkFieldImageDto = GetDarkFieldLineScanImage(
            calChipSiteModelEnum,
            position,
            (false, laserLightInformation),
            false,
            cibConfiguration,
            xWidthPixel,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            pmtId,
            stageCoordinateSystemEnum: stageCoordinateSystemEnum,
            isForward: isForward,
            isAutoFocus: isAutoFocus); // 模板匹配只能通道3(1, 2特征不明显)
        return TryGetMatchPosition(
            algorithmTemplateTypeEnum,
            darkFieldImageDto,
            pmtId,
            position,
            templateFilePath,
            saveResultImageFileDirectory,
            logGuid,
            logName,
            logResultTitle,
            out resultPosition,
            out resultScore,
            out resultAngle,
            out resultImageFilePath,
            isForward,
            xWidthPixel,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            stageCoordinateSystemEnum);
    }

    /// <summary>
    /// 匹配模板: 从旧位置到匹配后位置，包含暗场采图
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">明场位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="saveResultImageFileDirectory">匹配后成功的[保存的匹配图片的文件目录]</param>
    /// <param name="logGuid">记录日志: id</param>
    /// <param name="logName">记录日志: 名称</param>
    /// <param name="logResultTitle">记录日志: 匹配后成功的[标题]</param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="productivityInformation"></param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="laserLightInformation">功率</param>
    /// <param name="isAutoFocus"></param>
    /// <exception cref="AlgorithmException"></exception>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPositionByScanImage(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        Point position,
        string templateFilePath,
        string? saveResultImageFileDirectory,
        Guid? logGuid,
        string? logName,
        string? logResultTitle,
        CIBConfiguration cibConfiguration,
        ProductivityInformation productivityInformation,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        LaserLightInformation? laserLightInformation = null,
        bool isAutoFocus = true)
    {
        if (laserLightInformation is null) laserLightInformation = calibrationSetting.SettingCommonParam.MainLaserLightInformation;

        using var darkFieldImageDto = GetDarkFieldLineScanImage(
            calChipSiteModelEnum,
            position,
            (false, laserLightInformation),
            false,
            cibConfiguration,
            productivityInformation,
            xWidthPixel,
            pmtId,
            stageCoordinateSystemEnum: stageCoordinateSystemEnum,
            isForward: isForward,
            isAutoFocus: isAutoFocus); // 模板匹配只能通道3(1, 2特征不明显)
        return TryGetMatchPosition(
            algorithmTemplateTypeEnum,
            darkFieldImageDto,
            pmtId,
            position,
            templateFilePath,
            saveResultImageFileDirectory,
            logGuid,
            logName,
            logResultTitle,
            productivityInformation,
            out resultPosition,
            out resultScore,
            out resultAngle,
            out resultImageFilePath,
            isForward,
            xWidthPixel,
            stageCoordinateSystemEnum);
    }

    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">匹配旧的位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="laserLightInformation"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns>是否成功</returns>
    [Obsolete]
    public bool TryGetMatchPositionByScanImage(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        Point position,
        string templateFilePath,
        CIBConfiguration cibConfiguration,
        out Point resultPosition,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        LaserLightInformation? laserLightInformation = null,
        bool isAutoFocus = true)
    {
        return TryGetMatchPositionByScanImage(
            algorithmTemplateTypeEnum,
            calChipSiteModelEnum,
            pmtId,
            position,
            templateFilePath,
            null,
            null,
            null,
            null,
            cibConfiguration,
            out resultPosition,
            out _,
            out _,
            out _,
            isForward,
            xWidthPixel,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            laserLightInformation: laserLightInformation,
            isAutoFocus: isAutoFocus);
    }

    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">匹配旧的位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="cibConfiguration">采图模式</param>
    /// <param name="productivityInformation"></param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="laserLightInformation"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPositionByScanImage(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        Point position,
        string templateFilePath,
        CIBConfiguration cibConfiguration,
        ProductivityInformation productivityInformation,
        out Point resultPosition,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        LaserLightInformation? laserLightInformation = null,
        bool isAutoFocus = true)
    {
        return TryGetMatchPositionByScanImage(
            algorithmTemplateTypeEnum,
            calChipSiteModelEnum,
            pmtId,
            position,
            templateFilePath,
            null,
            null,
            null,
            null,
            cibConfiguration,
            productivityInformation,
            out resultPosition,
            out _,
            out _,
            out _,
            isForward,
            xWidthPixel,
            laserLightInformation: laserLightInformation,
            isAutoFocus: isAutoFocus);
    }

    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">匹配的位置</param>
    /// <param name="templateFilePath">模板</param>
    /// <param name="cIbConfiguration">采图模式</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="laserLightInformation"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns>是否成功</returns>
    [Obsolete]
    public bool TryGetMatchPositionByScanImage(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        Point position,
        string templateFilePath,
        CIBConfiguration cIbConfiguration,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        LaserLightInformation? laserLightInformation = null,
        bool isAutoFocus = true)
    {
        return TryGetMatchPositionByScanImage(
            algorithmTemplateTypeEnum,
            calChipSiteModelEnum,
            pmtId,
            position,
            templateFilePath,
            null,
            null,
            null,
            null,
            cIbConfiguration,
            out resultPosition,
            out resultScore,
            out resultAngle,
            out resultImageFilePath,
            isForward,
            xWidthPixel,
            yOpticsMagTypeEnum,
            xStageSpeedEnum,
            laserLightInformation: laserLightInformation,
            isAutoFocus: isAutoFocus);
    }

    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">匹配的位置</param>
    /// <param name="templateFilePath">模板</param>
    /// <param name="cIbConfiguration">采图模式</param>
    /// <param name="productivityInformation"></param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="laserLightInformation"></param>
    /// <param name="isAutoFocus"></param>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        Point position,
        string templateFilePath,
        CIBConfiguration cIbConfiguration,
        ProductivityInformation productivityInformation,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        LaserLightInformation? laserLightInformation = null,
        bool isAutoFocus = true)
    {
        return TryGetMatchPositionByScanImage(
            algorithmTemplateTypeEnum,
            calChipSiteModelEnum,
            pmtId,
            position,
            templateFilePath,
            null,
            null,
            null,
            null,
            cIbConfiguration,
            productivityInformation,
            out resultPosition,
            out resultScore,
            out resultAngle,
            out resultImageFilePath,
            isForward,
            xWidthPixel,
            laserLightInformation: laserLightInformation,
            isAutoFocus: isAutoFocus);
    }

    #endregion 模板匹配

    #endregion 服务
}