using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Constants;
using Net.Utilities.Enums;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(LaserViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class LaserViewModel(
    ICalibrationLaserService calibrationLaserService,
    ILogger<LaserViewModel> logger,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting,
    StageViewModel stageViewModel,
    AfViewModel afViewModel) : ViewModelBase
{
    private double MainCoefficient => calibrationSetting.SettingCommonParam.MainCoefficient;
    #region 服务

    public bool Connect()
    {
        var ret = calibrationLaserService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public (Point PD1, Point PD2) GetLaserBeamPosition()
    {
        var ret = calibrationLaserService.GetLaserBeamPosition();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
        return (ret.Anything.PD1, ret.Anything.PD2);
    }

    public (Point PD1, Point PD2) GetLaserOriginPosition()
    {
        var ret = calibrationLaserService.GetLaserOriginPosition();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
        return (ret.Anything.PD1, ret.Anything.PD2);
    }

    public void AdjustmentOfReflector(bool isEnable)
    {
        var ret = calibrationLaserService.AdjustmentOfReflector(isEnable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetLaserPowerMeterLightIntensity()
    {
        var ret = calibrationLaserService.GetLaserPowerMeterLightIntensity();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
        return ret.Anything;
    }

    public DarkFieldPrescanDto ReadPrescanByFile(string filePath, double coefficient)
    {
        var ret = calibrationLaserService.ReadPrescanByFile(filePath, coefficient);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public DarkFieldPrescanDto SetUploadPrescanListByRate(DarkFieldPrescanDto darkFieldPrescanDto, List<double> prescanRateList)
    {
        var ret = calibrationLaserService.SetPrescanByRate(darkFieldPrescanDto, prescanRateList);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SendOpticsMagType(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        var ret = calibrationLaserService.SendOpticsMagType(yOpticsMagTypeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SendPrescanByCoefficient(OpticsMagTypeEnum yOpticsMagTypeEnum, double coefficient)
    {
        var ret = calibrationLaserService.SendPrescanByCoefficient(yOpticsMagTypeEnum, coefficient);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SendSaturationValue(double val)
    {
        var ret = calibrationLaserService.SendSaturationValue(val);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SendPrescanByList(DarkFieldPrescanDto darkFieldPrescanDto)
    {
        var ret = calibrationLaserService.SendPrescanByList(darkFieldPrescanDto);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public DarkFieldChirpAodWaveDto ReadChirpAodByCustomFile(string filePath)
    {
        var ret = calibrationLaserService.ReadChirpAodByCustomFile(filePath);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public DarkFieldChirpAodWaveDto ReadChirpAodByConfigFile(string filePath)
    {
        var ret = calibrationLaserService.ReadChirpAodByConfigFile(filePath);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public (bool IsSuccess, DarkFieldChirpAodWaveDto Result) GetChirpAodByChangeRateFromFile(DarkFieldChirpAodWaveDto currentDarkFieldChirpAodWaveDto, double rateChange)
    {
        try
        {
            var ret = calibrationLaserService.GetChirpAodByChangeRateFromFile(currentDarkFieldChirpAodWaveDto, rateChange);

            return ret.IsSuccess ? (true, ret.Anything) : (false, ret.Anything);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Get chirp AOD by change rate from file failed", nameof(LaserViewModel));
            return (false, new DarkFieldChirpAodWaveDto());
        }
    }

    public void SendChirpAodByList(DarkFieldChirpAodWaveDto darkFieldChirpAodDto)
    {
        var ret = calibrationLaserService.SendChirpAodByList(darkFieldChirpAodDto);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public List<double> GetSensorPmtValueList()
    {
        var ret = calibrationLaserService.GetSensorPmtValueList();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<double> GetAnyPmtValueList(int pmtId, int channel)
    {
        var ret = calibrationLaserService.GetAnyPmtValueList(pmtId, channel);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<DarkFieldPmtDataDto> GetPmtDataList()
    {
        var ret = calibrationLaserService.GetPmtDataList();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SendPmtGainToCib(List<string> pmtData, List<string> igData, int pmtId, int channel)
    {
        var ret = calibrationLaserService.SendPmtGainToCib(pmtData, igData, pmtId, channel);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public List<DarkFieldPmtDelayDto> GetCibSamplePmtDelayList()
    {
        var ret = calibrationLaserService.GetCibSamplePmtDelayList();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetCibSamplePmtDelayList(List<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        var ret = calibrationLaserService.SetCibSamplePmtDelayList(darkFieldPmtDelayDtoList);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        var ret = calibrationLaserService.ToggleOpticsPolarization(opticsPolarizationTypeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum)
    {
        var ret = calibrationLaserService.ToggleOpticsAodWorkingMode(opticsAodWorkingModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetAodDelayValue(OpticsMagTypeEnum yOpticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        var ret = calibrationLaserService.SetAodDelayValue(yOpticsMagTypeEnum, prescanAodDelay, chirpAodDelay);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableAutoGain(bool enable)
    {
        var ret = calibrationLaserService.ToggleEnableAutoGain(enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableL0K(bool enable)
    {
        var ret = calibrationLaserService.ToggleEnableL0K(enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetGain(double gain)
    {
        var ret = calibrationLaserService.SetGain(gain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public int GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        var ret = calibrationLaserService.GetDarkFieldLineScanImageYPixelHeight(yOpticsMagTypeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetDarkFieldLineScanImageXSizePerPixel(OpticsMagTypeEnum yOpticsMagTypeEnum, StageSpeedEnum xStageSpeedEnum)
    {
        var ret = calibrationLaserService.GetDarkFieldLineScanImageXSizePerPixel(yOpticsMagTypeEnum, xStageSpeedEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<DarkFieldImageDto> GetDarkFieldLineScanImageList(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod,
        SettingDarkFieldAutoFocusParam? settingDarkFieldAutoFocus,
        bool isForward = true)
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

            var isAutoFocus = afViewModel.SetDarkFieldAutoFocus(settingDarkFieldAutoFocus, yOpticsMagTypeEnum, calChipSiteModelEnum);
            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(position, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, isAutoFocus, isForward, customPrescanAod, isCustomChirpAod);

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

    public DarkFieldImageDto GetDarkFieldLineScanImage(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod,
        SettingDarkFieldAutoFocusParam? settingDarkFieldAutoFocus,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        bool isForward = true)
    {
        var result = GetDarkFieldLineScanImageList(calChipSiteModelEnum, position, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, customPrescanAod, isCustomChirpAod, settingDarkFieldAutoFocus, isForward);

        var darkFieldImageDto = result.Single(t => t.ChannelId == channelId);

        foreach (var item in result.Where(t => t.ChannelId != channelId).Select(t => t.Image))
        {
            using var _ = item;
        }

        return darkFieldImageDto;
    }

    public List<DarkFieldImageDto> GetDarkFieldLineScanImageListByNotAutoFocus(
        Point position,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod,
        bool isForward = true)
    {
        try
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(position);
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

            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(position, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, false, isForward, customPrescanAod, isCustomChirpAod);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            switch (stageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(position);
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

    public DarkFieldImageDto GetDarkFieldLineScanImageByNotAutoFocus(
        Point position,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        bool isForward = true)
    {
        var result = GetDarkFieldLineScanImageListByNotAutoFocus(position, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, customPrescanAod, isCustomChirpAod, isForward);

        var darkFieldImageDto = result.Single(t => t.ChannelId == channelId);

        foreach (var item in result.Where(t => t.ChannelId != channelId).Select(t => t.Image))
        {
            using var _ = item;
        }

        return darkFieldImageDto;
    }

    public List<DarkFieldImageDto> GetDarkFieldLineScanImageList(
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod,
        bool isForward = true)
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

            var isAutoFocus = afViewModel.SetDarkFieldAutoFocus(null, yOpticsMagTypeEnum, calChipSiteModelEnum);
            var ret = calibrationLaserService.GetDarkFieldLineScanImageList(startPosition, endPosition, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, isAutoFocus, isForward, customPrescanAod, isCustomChirpAod);

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

    public List<List<DarkFieldImageDto>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> positionList,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod)
    {
        switch (stageCoordinateSystemEnum)
        {
            case StageCoordinateSystemEnum.Machine:
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(positionList.First());
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null);
        }

        var isAutoFocus = afViewModel.SetDarkFieldAutoFocus(null, yOpticsMagTypeEnum, CalChipSiteModelEnum.ChuckModel);

        var ret = calibrationLaserService.GetChuckDarkFieldRowLineScanImageList(positionList, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, isAutoFocus, customPrescanAod, isCustomChirpAod);
        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<DarkFieldImageDto> GetChuckDarkFieldRowLineScanImage(
        List<Point> positionList,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        int pmtId = CalibrationConstantsHelper.MainPmtId,
        int channelId = CalibrationConstantsHelper.MainChannelId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum)
    {
        var temp = GetChuckDarkFieldRowLineScanImageList(positionList, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum, pmtId, stageCoordinateSystemEnum, customPrescanAod, isCustomChirpAod);

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
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="laserPixelSizeItemDtoList">暗场相机的像素尺寸列表</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">明场位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="saveResultImageFileDirectory">匹配后成功的[保存的匹配图片的文件目录]</param>
    /// <param name="logGuid">记录日志: id</param>
    /// <param name="logName">记录日志: 名称</param>
    /// <param name="logResultTitle">记录日志: 匹配后成功的[标题]</param>
    /// <param name="settingDarkFieldAutoFocus">自动聚焦参数，为空时默认使用CalibrationSetting缓存</param>
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
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        IEnumerable<LaserPixelSizeItemDto> laserPixelSizeItemDtoList,
        int pmtId,
        Point position,
        string templateFilePath,
        string? saveResultImageFileDirectory,
        Guid? logGuid,
        string? logName,
        string? logResultTitle,
        SettingDarkFieldAutoFocusParam? settingDarkFieldAutoFocus,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum,
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        double coefficient = ConstantHelper.NegValue)
    {
        if (coefficient == ConstantHelper.NegValue) coefficient = MainCoefficient;

        resultPosition = Point.Empty;
        resultScore = 0;
        resultAngle = 0;
        resultImageFilePath = string.Empty;

        var ySize = laserPixelSizeItemDtoList.SingleOrDefault(t => t.PmtId == pmtId && t.OpticsMagTypeEnum == yOpticsMagTypeEnum);
        if (ySize is null || ySize.IsOk == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Laser Pixel Size is Empty or not verify."), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Laser Pixel Size is Empty or not verify", nameof(ReviewViewModel));
            return false;
        }

        var xSizePerPixel = GetDarkFieldLineScanImageXSizePerPixel(yOpticsMagTypeEnum, xStageSpeedEnum);

        var isSuccess = calibrationAlgorithmService.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out var templateId);
        using var _1 = templateId;
        if (isSuccess == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Read Template Failed!"), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Read Template Failed!", nameof(ReviewViewModel));

            throw new AlgorithmException("Read Template Failed!");
        }

        try
        {
            using var darkFieldImageDto = GetDarkFieldLineScanImage(
                calChipSiteModelEnum,
                position,
                (false, coefficient),
                false,
                settingDarkFieldAutoFocus,
                xWidthPixel,
                yOpticsMagTypeEnum,
                xStageSpeedEnum,
                pmtId,
                stageCoordinateSystemEnum: stageCoordinateSystemEnum,
                isForward: isForward); // 模板匹配只能通道3(1, 2特征不明显)

            var originImageFilePath = string.Empty;

            using var image = isForward ? darkFieldImageDto.Image : HalconHelper.HorizontalFlip(darkFieldImageDto.Image);
            isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out var markPoint, out var offset, out resultScore, out resultAngle);
            if (isSuccess == false)
            {
                var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
                originImageFilePath = $"{FileHelper.GetFileFullName(templateFilePath)}_Error\\Score({resultScore:f3},{templateMatchScoreThreshold})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, originImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(originImageFilePath), darkFieldImageDto.Bytes);
                if (logGuid is not null && logName is not null)
                    logger.LogHtmlInformation($"{logName} Error: Try Math Template To Offset Failed.{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                    {
                        darkFieldImageDto.PmtId,
                        darkFieldImageDto.ChannelId,
                        darkFieldImageDto.Width,
                        XWidthPixel = xWidthPixel,
                        OpticsMagTypeEnum = yOpticsMagTypeEnum,
                        StageSpeedEnum = xStageSpeedEnum,
                        OriginPosition = position.ToShortString(),
                        Score = resultScore,
                        TemplateMatchScoreThreshold = templateMatchScoreThreshold,
                        HtmlTab = new HtmlTab(new
                        {
                            OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                            TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Error: Try Math Template To Offset Failed", nameof(ReviewViewModel));

                throw new AlgorithmException("Try Template Match To Offset Error");
            }

            if (saveResultImageFileDirectory is not null)
            {
                originImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, originImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(originImageFilePath), darkFieldImageDto.Bytes);
            }

            //if (isForward == false) offset.X = -offset.X;

            if (stageCoordinateSystemEnum == StageCoordinateSystemEnum.Machine) offset.X = -offset.X;

            var actualOffset = new Point(offset.X * xSizePerPixel, offset.Y * ySize.YPixelSize);
            resultPosition = position + actualOffset;
            using var darkFieldImageDtoResult = GetDarkFieldLineScanImage(
                calChipSiteModelEnum,
                resultPosition,
                (false, coefficient),
                false,
                settingDarkFieldAutoFocus,
                xWidthPixel,
                yOpticsMagTypeEnum,
                xStageSpeedEnum,
                pmtId,
                stageCoordinateSystemEnum: stageCoordinateSystemEnum,
                isForward: isForward); // 模板匹配只能通道3(1, 2特征不明显)

            if (saveResultImageFileDirectory is not null)
            {
                resultImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Result_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDtoResult.Image, resultImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(resultImageFilePath), darkFieldImageDtoResult.Bytes);
            }

            if (logGuid is not null && logName is not null && logResultTitle is not null)
                logger.LogHtmlInformation($"{logName} Match Template:{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                {
                    darkFieldImageDtoResult.PmtId,
                    darkFieldImageDtoResult.ChannelId,
                    darkFieldImageDtoResult.Width,
                    XWidthPixel = xWidthPixel,
                    OpticsMagTypeEnum = yOpticsMagTypeEnum,
                    StageSpeedEnum = xStageSpeedEnum,
                    OriginPosition = position.ToShortString(),
                    ResultPosition = resultPosition.ToShortString(),
                    ResultOffset = actualOffset.ToShortString(),
                    ResultScore = resultScore,
                    ResultAngle = resultAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(originImageFilePath, description: "ResultImage", htmlImageOverlays: [new HtmlImageCrossOverlay(isForward ? markPoint : new Point(xWidthPixel - markPoint.X, markPoint.Y))]),
                        OriginImage = new HtmlImage(originImageFilePath, description: "OriginImage", htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
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

                ThrowHelper.ThrowInvalidOperationException("Try Clean Template Failed!", new AlgorithmException());
            }
        }
    }


    /// <summary>
    /// 固定高度采图匹配模板: 从旧位置到匹配后位置
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="laserPixelSizeItemDtoList">暗场相机的像素尺寸列表</param>
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
    public bool TryGetMatchPositionByNotAutoFocus(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        IEnumerable<LaserPixelSizeItemDto> laserPixelSizeItemDtoList,
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
        StageCoordinateSystemEnum stageCoordinateSystemEnum = CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
        double coefficient = ConstantHelper.NegValue)
    {
        if (coefficient == ConstantHelper.NegValue) coefficient = MainCoefficient;

        resultPosition = Point.Empty;
        resultScore = 0;
        resultAngle = 0;
        resultImageFilePath = string.Empty;

        var ySize = laserPixelSizeItemDtoList.SingleOrDefault(t => t.PmtId == pmtId && t.OpticsMagTypeEnum == yOpticsMagTypeEnum);
        if (ySize is null || ySize.IsOk == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Laser Pixel Size is Empty or not verify."), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Laser Pixel Size is Empty or not verify", nameof(ReviewViewModel));
            return false;
        }

        var xSizePerPixel = GetDarkFieldLineScanImageXSizePerPixel(yOpticsMagTypeEnum, xStageSpeedEnum);

        var isSuccess = calibrationAlgorithmService.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out var templateId);
        using var _1 = templateId;
        if (isSuccess == false)
        {
            if (logGuid is not null && logName is not null) logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment($"{logName} Error: Read Template Failed!"), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Read Template Failed!", nameof(ReviewViewModel));

            throw new AlgorithmException("Read Template Failed!");
        }

        try
        {
            using var darkFieldImageDto = GetDarkFieldLineScanImageByNotAutoFocus(
                position,
                (false, coefficient),
                false,
                xWidthPixel,
                yOpticsMagTypeEnum,
                xStageSpeedEnum,
                pmtId,
                stageCoordinateSystemEnum: stageCoordinateSystemEnum,
                isForward: isForward);

            var originImageFilePath = string.Empty;

            using var image = isForward ? darkFieldImageDto.Image : HalconHelper.HorizontalFlip(darkFieldImageDto.Image);
            isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out var markPoint, out var offset, out resultScore, out resultAngle);
            if (isSuccess == false)
            {
                var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
                originImageFilePath = $"{FileHelper.GetFileFullName(templateFilePath)}_Error\\Score({resultScore:f3},{templateMatchScoreThreshold})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, originImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(originImageFilePath), darkFieldImageDto.Bytes);
                if (logGuid is not null && logName is not null)
                    logger.LogHtmlInformation($"{logName} Error: Try Math Template To Offset Failed.{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                    {
                        darkFieldImageDto.PmtId,
                        darkFieldImageDto.ChannelId,
                        darkFieldImageDto.Width,
                        XWidthPixel = xWidthPixel,
                        OpticsMagTypeEnum = yOpticsMagTypeEnum,
                        StageSpeedEnum = xStageSpeedEnum,
                        OriginPosition = position.ToShortString(),
                        Score = resultScore,
                        TemplateMatchScoreThreshold = templateMatchScoreThreshold,
                        HtmlTab = new HtmlTab(new
                        {
                            OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                            TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Error: Try Math Template To Offset Failed", nameof(ReviewViewModel));

                throw new AlgorithmException("Try Template Match To Offset Error");
            }

            if (saveResultImageFileDirectory is not null)
            {
                originImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, originImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(originImageFilePath), darkFieldImageDto.Bytes);
            }

            //if (isForward == false) offset.X = -offset.X;

            if (stageCoordinateSystemEnum == StageCoordinateSystemEnum.Machine) offset.X = -offset.X;

            var actualOffset = new Point(offset.X * xSizePerPixel, offset.Y * ySize.YPixelSize);
            resultPosition = position + actualOffset;
            using var darkFieldImageDtoResult = GetDarkFieldLineScanImageByNotAutoFocus(
                resultPosition,
                (false, coefficient),
                false,
                xWidthPixel,
                yOpticsMagTypeEnum,
                xStageSpeedEnum,
                pmtId,
                stageCoordinateSystemEnum: stageCoordinateSystemEnum,
                isForward: isForward); // 模板匹配只能通道3(1, 2特征不明显)

            if (saveResultImageFileDirectory is not null)
            {
                resultImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Result_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDtoResult.Image, resultImageFilePath);
                File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(resultImageFilePath), darkFieldImageDtoResult.Bytes);
            }

            if (logGuid is not null && logName is not null && logResultTitle is not null)
                logger.LogHtmlInformation($"{logName} Match Template:{logResultTitle}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                {
                    darkFieldImageDtoResult.PmtId,
                    darkFieldImageDtoResult.ChannelId,
                    darkFieldImageDtoResult.Width,
                    XWidthPixel = xWidthPixel,
                    OpticsMagTypeEnum = yOpticsMagTypeEnum,
                    StageSpeedEnum = xStageSpeedEnum,
                    OriginPosition = position.ToShortString(),
                    ResultPosition = resultPosition.ToShortString(),
                    ResultOffset = actualOffset.ToShortString(),
                    ResultScore = resultScore,
                    ResultAngle = resultAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(originImageFilePath, description: "ResultImage", htmlImageOverlays: [new HtmlImageCrossOverlay(isForward ? markPoint : new Point(xWidthPixel - markPoint.X, markPoint.Y))]),
                        OriginImage = new HtmlImage(originImageFilePath, description: "OriginImage", htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
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

                ThrowHelper.ThrowInvalidOperationException("Try Clean Template Failed!", new AlgorithmException());
            }
        }
    }


    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="laserPixelSizeItemDtoList">暗场相机的像素尺寸列表</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">匹配旧的位置</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        IEnumerable<LaserPixelSizeItemDto> laserPixelSizeItemDtoList,
        int pmtId,
        Point position,
        string templateFilePath,
        out Point resultPosition,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum)
    {
        return TryGetMatchPosition(algorithmTemplateTypeEnum, calChipSiteModelEnum, laserPixelSizeItemDtoList, pmtId, position, templateFilePath, null, null, null, null, null,
            out resultPosition, out _, out _, out _, isForward, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum);
    }

    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="calChipSiteModelEnum">chuck位置</param>
    /// <param name="laserPixelSizeItemDtoList">暗场相机的像素尺寸列表</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="position">匹配的位置</param>
    /// <param name="templateFilePath">模板</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        IEnumerable<LaserPixelSizeItemDto> laserPixelSizeItemDtoList,
        int pmtId,
        Point position,
        string templateFilePath,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        bool isForward = true,
        int xWidthPixel = CalibrationConstantsHelper.MainXWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum = CalibrationConstantsHelper.MainOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum = CalibrationConstantsHelper.MainStageSpeedEnum)
    {
        return TryGetMatchPosition(algorithmTemplateTypeEnum, calChipSiteModelEnum, laserPixelSizeItemDtoList, pmtId, position, templateFilePath, null, null, null, null, null,
            out resultPosition, out resultScore, out resultAngle, out resultImageFilePath, isForward, xWidthPixel, yOpticsMagTypeEnum, xStageSpeedEnum);
    }

    #endregion 模板匹配

    #endregion 服务
}