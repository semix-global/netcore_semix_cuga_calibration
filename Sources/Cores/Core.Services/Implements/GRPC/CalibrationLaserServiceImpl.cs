using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Cuga.Agent.Facade.Service.MachineFacade;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.PMT;
using Cuga.Interface.Calibration;
using Cuga.Interface.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Semix.GRPC.DTO;
using System.IO;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed partial class CalibrationLaserServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService,
    CalibrationSetting calibrationSetting) : BaseService<ICgCalibLaserService, ICgDiagIlluminationOpticsService, ICgFacadeSwathService>, ICalibrationLaserService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService();
            IsConnected = createService.IsSuccess;

            return createService;
        });
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamPoint()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadLaserBeamPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Origin, Point.Origin))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamOriginPoint()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadLaserBeamOriginPos());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Origin, Point.Origin))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<bool> AdjustBeamStabilizer(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service?.LaserBeamAdjust(new SxParamObj<bool>(isEnable)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetOpticalPowerMeter()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<double> LevelToCoefficient(double level)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<double> CoefficientToLevel(double coefficient)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.RefreshMag(new SxParamObj<CgMagTypeEnum>(opticsMagTypeEnum.ToCgMagTypeEnum())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetAODNO(new SxParamObj<int>(opticsAodWorkingModeEnum.ToOpticsAodWorkingMode())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service2?.SetPolarization(new SxParamObj<CgPolarizationTypeEnum>(opticsPolarizationTypeEnum.ToCgPolarizationTypeEnum())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(OpticsMagTypeEnum opticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        var sxExecuteRet = Invoke(() => Service?.SetMagAndWaveZero(new SxParamObj<(CgMagTypeEnum OpticsMagTypeEnum, int? chirpAodDelay, int? prescanAodDelay)>((opticsMagTypeEnum.ToCgMagTypeEnum(), Convert.ToInt32(chirpAodDelay), Convert.ToInt32(prescanAodDelay)))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum opticsMagTypeEnum, double coefficient)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetPrescanAODWaveProfileList(IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfileList)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChirpAODWaveProfileList(IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfileList)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleCIBControlTypeAndProfileType(CIBConfiguration cIbConfiguration, int pmtId, int channelId)
    {
        var toggleAutoGainRet = ToggleEnableAutoGainControl(cIbConfiguration.IsAutoGainControl, pmtId, channelId);
        if (toggleAutoGainRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleAutoGainRet.ErrorMsg, false);

        if (cIbConfiguration.IsAutoGainControl == false)
        {
            var setGainRet = SetGain(cIbConfiguration.Gain, pmtId, channelId);
            if (setGainRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setGainRet.ErrorMsg, false);
        }

        var toggleL0kRet = ToggleEnableL0K(cIbConfiguration.IsL0K, pmtId, channelId);
        if (toggleL0kRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleL0kRet.ErrorMsg, false);

        var toggleProfileTypeRet = ToggleProfileMode(cIbConfiguration.CIBProfileMode, pmtId, channelId);
        if (toggleProfileTypeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleProfileTypeRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableAutoGainControl(bool enable, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleProfileMode(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleEnableMarkMode(bool enable, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetGain(double gain, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetSaturation(double saturation)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDCSaturation(new SxParamObj<double>(saturation)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>> GetCIBConfigList()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfPMTDataList(int count, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<DarkFieldPmtDataDto>> GetCIBOfPMTDataList()
    {
        var pmtRet = Invoke(() => Service?.GetPMTDataALL());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldPmtDataDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDataDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDataModel => new DarkFieldPmtDataDto().AdaptIn(pmtDataModel)));

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldPmtDataDto>>(result);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfSenseDataList(int count, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<DarkFieldPmtDelayDto>> GetCIBDelayList()
    {
        var pmtRet = Invoke(() => Service?.GetPMTDelay());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldPmtDelayDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDelayDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDelayModel => new DarkFieldPmtDelayDto().AdaptIn(pmtDelayModel)));

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldPmtDelayDto>>(result);
    }

    public SxExecuteRet<bool> SetCIBDelayList(IReadOnlyList<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        var pmtDelayModel = darkFieldPmtDelayDtoList.Select(item => item.AdaptTo()).ToList();

        var pmtRet = Invoke(() => Service?.SetPMTDelay(new SxParamObj<List<CgPMTDelayModel>>(pmtDelayModel)));

        return pmtRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(pmtRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SendPMTGain(List<string> pmtData, List<string> igData, int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service?.SendPMTGain(new SxParamObj<(List<string> pmtData, List<string> igData, int pmtId, int channel)>((pmtData, igData, pmtId, channelId))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double AfMotor)> RuntimeAfCalibration(CalChipSiteModelEnum calChipSiteModelEnum, double? coefficient = null, Point? position = null)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum opticsMagTypeEnum, bool isCuttingPixelHeight)
    {
        if (isCuttingPixelHeight == false) throw new NotImplementedException();

        var sxExecuteRet = Invoke(() => Service?.GetSpeedInfo(new SxParamObj<CgMagTypeEnum>(opticsMagTypeEnum.ToCgMagTypeEnum())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<int>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToInt32(sxExecuteRet.Anything.YPixel));
    }

    public SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(Point position,
        int xWidthPixel,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => Invoke(() => Service?.GetImg(new SxParamObj<M2CCollectImgParamDTO>(new M2CCollectImgParamDTO
            {
                Mag = opticsMagTypeEnum.ToSxMagEnum(),
                Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                Width = xWidthPixel,
                Pos = position.ToSxPointD(),
                PMT = pmtId,
                IsSingle = true, /*是否单向*/
                IsForward = isForward, /*是否正向*/
                AF = isAutoFocus ? 0 : 1 /*是否开启自动聚焦*/
            }))),
            StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetImgStage(new SxParamObj<M2CCollectImgParamDTO>(new M2CCollectImgParamDTO
            {
                Mag = opticsMagTypeEnum.ToSxMagEnum(),
                Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                Width = xWidthPixel,
                Pos = position.ToSxPointD(),
                PMT = pmtId,
                IsSingle = true, /*是否单向*/
                IsForward = isForward, /*是否正向*/
                AF = isAutoFocus ? 0 : 1 /*是否开启自动聚焦*/
            }))),
            _ => throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null)
        };
        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldImageDto>(darkFieldImagesRet.Anything.Count);

        foreach (var c2MImgModel in darkFieldImagesRet.Anything)
        {
            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(c2MImgModel.Img);
            result.Add(new DarkFieldImageDto { Image = image, Matrix = matrix }.AdaptIn(c2MImgModel));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<DarkFieldRawScanImageDto>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetImgPTP(new SxParamObj<M2CCollectImgParamDTO>(new M2CCollectImgParamDTO
            {
                Mag = opticsMagTypeEnum.ToSxMagEnum(),
                Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                Pos = isForward ? startPosition.ToSxPointD() : endPosition.ToSxPointD(),
                Pos2 = isForward ? endPosition.ToSxPointD() : startPosition.ToSxPointD(),
                PMT = pmtId,
                IsSingle = true, /*是否单向*/
                AF = isAutoFocus ? 0 : 1 /*是否开启自动聚焦*/
            }))),
            _ => throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null)
        };
        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDto>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDto>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldRawScanImageDto>(darkFieldImagesRet.Anything.Count);

        foreach (var c2MImgModel in darkFieldImagesRet.Anything)
        {
            result.Add(new DarkFieldRawScanImageDto().AdaptIn(c2MImgModel));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<DarkFieldImageDto>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus)
    {
        if (machinePositionList.Count < 2
            || machinePositionList.Any(t => t.Y - machinePositionList[0].Y == 0) == false // 检查y是否相同
            || machinePositionList.Zip(machinePositionList.Skip(1), (current, next) => current.X <= next.X).All(b => b) == false) // 检查x是否递增
            throw new ArgumentOutOfRangeException(nameof(machinePositionList), machinePositionList, null);

        var directionRect = calibrationStageService.GetMachineDirection();
        if (directionRect.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDto>>>(directionRect.ErrorMsg, []);
        var directionX = directionRect.Anything.XDirection;

        var picturePixelHeightRet = GetDarkFieldLineScanImageYPixelHeight(opticsMagTypeEnum, true);
        if (picturePixelHeightRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDto>>>(picturePixelHeightRet.ErrorMsg, []);
        var height = picturePixelHeightRet.Anything;

        var scanLineXPixelSize = calibrationSetting.SettingCommonParam.GetScanLineXPixelSize(opticsMagTypeEnum, xStageSpeedEnum);
        var extendWidth = xWidthPixel * scanLineXPixelSize / 2.0;

        // 计算采图的起点终点机械坐标
        var startPoint = new Point(machinePositionList[0].X - extendWidth, machinePositionList[0].Y);
        var endPoint = new Point(machinePositionList.Last().X + extendWidth * 3, machinePositionList[0].Y); // 后面多采集一段，防止最后一段数据不全

        // 从起点到终点采图，输出三通道长图片
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetImgPTP(new SxParamObj<M2CCollectImgParamDTO>(new M2CCollectImgParamDTO
            {
                Mag = opticsMagTypeEnum.ToSxMagEnum(),
                Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                Pos = startPoint.ToSxPointD(),
                Pos2 = endPoint.ToSxPointD(),
                PMT = pmtId,
                IsSingle = true, /*是否单向*/
                AF = isAutoFocus ? 0 : 1 /*是否开启自动聚焦*/
            }))),
            _ => throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null)
        };

        // 获得stageMap两点x像素间隔（剪裁小图的宽度width）
        var heightPixelOfByte = height * 2;
        var splitImageLength = xWidthPixel * heightPixelOfByte;
        var pointerList = Enumerable
            .Range(0, machinePositionList.Count)
            .Select((count, index) => index == 0 ? 0 : count * (machinePositionList[1].X - machinePositionList[0].X) / xPixelSize * heightPixelOfByte)
            .Select(Convert.ToInt64)
            .ToList();

        var splitImagesAllChannels = new List<List<DarkFieldImageDto>>();
        foreach (var channelId in Enumerable.Range(0, 3))
        {
            using var fileSteam = File.OpenRead(darkFieldImagesRet.Anything[channelId].Url);
            using var binaryReader = new BinaryReader(fileSteam);

            var (_, bodyBytesStartIndex, bodyBytesLength) = Utilities.RawImageHelper.GetSize(binaryReader);

            var splitImages = new List<DarkFieldImageDto>();
            foreach (var (index, pointer) in pointerList.Select((t, i) => (Index: i, Pointer: t)))
            {
                var pointerTemp = pointer - pointer % heightPixelOfByte; // dieWidthPixel不是整数倍, 需要对齐
                byte[] array;
                if (pointerTemp + splitImageLength > bodyBytesLength)
                {
                    if (index != pointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                    var offset = xWidthPixel - (bodyBytesLength - pointerTemp) / heightPixelOfByte;

                    pointerTemp += offset * heightPixelOfByte;
                    pointerTemp -= pointerTemp % heightPixelOfByte; // dieWidthPixel不是整数倍, 需要对齐

                    fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                    array = binaryReader.ReadBytes();
                }
                else
                {
                    fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                    array = binaryReader.ReadBytes(splitImageLength);
                }

                var splitRawBytes = calibrationAlgorithmService.ToRawBytes(array, new Size(xWidthPixel, height));
                var (image, matrix, horizontalFlipRawBytes) = calibrationAlgorithmService.ToHorizontalFlipImageInfo(splitRawBytes);
                // 三通道图片分别用以上ROI集合裁剪
                var splitImageDto = new DarkFieldImageDto { PmtId = 8, ChannelId = channelId + 1, Bytes = horizontalFlipRawBytes, Image = image, Matrix = matrix, Height = height, Width = xWidthPixel };
                splitImages.Add(splitImageDto);
            }

            splitImagesAllChannels.Add(splitImages);
        }

        return SxExecuteRetHelper.CreateSuccess(splitImagesAllChannels);
    }
}