using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.PMT;
using Cuga.Interface.Calibration;
using Cuga.Interface.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Semix.CoreLib;
using Semix.GRPC.DTO;
using System.IO;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed partial class CalibrationLaserServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService,
    CalibrationSetting calibrationSetting) : BaseService<ICgCalibLaserService, ICgDiagIlluminationOpticsService>, ICalibrationLaserService
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

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserBeamPosition()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadLaserBeamPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Empty, Point.Empty))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserOriginPosition()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadLaserBeamOriginPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Empty, Point.Empty))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<bool> AdjustmentOfReflector(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service?.LaserBeamAdjust(new SxParamObj<bool>(isEnable)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetLaserPowerMeterLightIntensity()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SendOpticsMagType(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.RefreshMag(new SxParamObj<CgMagTypeEnum>(yOpticsMagTypeEnum.ToCgMagTypeEnum())));
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByCoefficient(OpticsMagTypeEnum yOpticsMagTypeEnum, double coefficient)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPrescan(new SxParamObj<(CgMagTypeEnum yOpticsMagTypeEnum, double coefficient)>((yOpticsMagTypeEnum.ToCgMagTypeEnum(), coefficient))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendSaturationValue(double val)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDCSaturation(new SxParamObj<double>(val)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByList(DarkFieldPrescanDto darkFieldPrescanDto)
    {
        var sxExecuteRet = Invoke(() => Service?.SendPrescanFileIllumination(new SxParamObj<(short regNum, short zeroNum, List<byte> sendData)>((darkFieldPrescanDto.RegNum, darkFieldPrescanDto.ZeroNum, darkFieldPrescanDto.PrescanByteList))));
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendChirpAodByList(DarkFieldChirpAodWaveDto darkFieldChirpAodWaveDto)
    {
        var sxExecuteRet = Invoke(() => Service?.SetChirpCalibration(new SxParamObj<(List<byte> sendData, int totalNum, int zeronum)>((darkFieldChirpAodWaveDto.ChirpAodWaveByteList, darkFieldChirpAodWaveDto.RegNum, darkFieldChirpAodWaveDto.ZeroNum))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<int>> GetUsedPmtIdList()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<List<double>> GetPmtDataList(int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service?.GetPMTDataAppoint(new SxParamObj<(int pmtId, int channel)>((pmtId, channelId))));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<double>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<List<double>>("Pmt Value List is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<List<DarkFieldPmtDataDto>> GetPmtDataList()
    {
        var pmtRet = Invoke(() => Service?.GetPMTDataALL());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldPmtDataDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDataDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDataModel => new DarkFieldPmtDataDto().AdaptIn(pmtDataModel)));

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<double>>> GetPmtSenseDataList(int pmtId, int channelId, int count)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<List<DarkFieldPmtDelayDto>> GetPmtDelayList()
    {
        var pmtRet = Invoke(() => Service?.GetPMTDelay());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldPmtDelayDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDelayDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDelayModel => new DarkFieldPmtDelayDto().AdaptIn(pmtDelayModel)));

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<bool> SetPmtDelayList(List<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        var pmtDelayModel = darkFieldPmtDelayDtoList.Select(item => item.AdaptTo()).ToList();

        var pmtRet = Invoke(() => Service?.SetPMTDelay(new SxParamObj<List<CgPMTDelayModel>>(pmtDelayModel)));

        return pmtRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(pmtRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPmtGain(double[] gains, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SendPmtGain(List<string> pmtData, List<string> igData, int pmtId, int channel)
    {
        var sxExecuteRet = Invoke(() => Service?.SendPMTGain(new SxParamObj<(List<string> pmtData, List<string> igData, int pmtId, int channel)>((pmtData, igData, pmtId, channel))));

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

    public SxExecuteRet<bool> ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetAODNO(new SxParamObj<int>(opticsAodWorkingModeEnum.ToOpticsAodWorkingMode())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAodDelayValue(OpticsMagTypeEnum yOpticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        var sxExecuteRet = Invoke(() => Service?.SetMagAndWaveZero(new SxParamObj<(CgMagTypeEnum yOpticsMagTypeEnum, int? chirpAodDelay, int? prescanAodDelay)>((yOpticsMagTypeEnum.ToCgMagTypeEnum(), Convert.ToInt32(chirpAodDelay), Convert.ToInt32(prescanAodDelay)))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableMarkMode(bool enable)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleEnableAutoGain(bool enable)
    {
        var sxExecuteRet = Invoke(() => Service?.SetAGC(new SxParamObj<bool>(enable)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable)
    {
        var sxExecuteRet = Invoke(() => Service?.SetL0K(new SxParamObj<bool>(enable)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(double gain)
    {
        var sxExecuteRet = Invoke(() => Service?.SendDC(new SxParamObj<(bool, double)>((false, gain))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.GetSpeedInfo(new SxParamObj<CgMagTypeEnum>(yOpticsMagTypeEnum.ToCgMagTypeEnum())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<int>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToInt32(sxExecuteRet.Anything.YPixel));
    }

    public SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(Point position,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod)
    {
        if (TrySendAodFile(yOpticsMagTypeEnum, customPrescanAod, isCustomChirpAod, out var errorMessage) == false)
            return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>(errorMessage, []);

        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => Invoke(() => Service?.GetImg(new SxParamObj<M2CCollectImgParamDTO>(new M2CCollectImgParamDTO
            {
                Mag = yOpticsMagTypeEnum.ToSxMagEnum(),
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
                Mag = yOpticsMagTypeEnum.ToSxMagEnum(),
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

    public SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod)
    {
        if (TrySendAodFile(yOpticsMagTypeEnum, customPrescanAod, isCustomChirpAod, out var errorMessage) == false)
            return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>(errorMessage, []);

        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetImgPTP(new SxParamObj<M2CCollectImgParamDTO>(new M2CCollectImgParamDTO
            {
                Mag = yOpticsMagTypeEnum.ToSxMagEnum(),
                Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                Pos = isForward ? startPosition.ToSxPointD() : endPosition.ToSxPointD(),
                Pos2 = isForward ? endPosition.ToSxPointD() : startPosition.ToSxPointD(),
                PMT = pmtId,
                IsSingle = true, /*是否单向*/
                AF = isAutoFocus ? 0 : 1 /*是否开启自动聚焦*/
            }))),
            _ => throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null)
        };
        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldImageDto>(darkFieldImagesRet.Anything.Count);

        foreach (var c2MImgModel in darkFieldImagesRet.Anything)
        {
            var rawBytes = File.ReadAllBytes(c2MImgModel.Url);
            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(rawBytes);
            result.Add(new DarkFieldImageDto { Image = image, Matrix = matrix, Bytes = rawBytes }.AdaptIn(c2MImgModel));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<DarkFieldImageDto>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod)
    {
        if (TrySendAodFile(yOpticsMagTypeEnum, customPrescanAod, isCustomChirpAod, out var errorMessage) == false)
            return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDto>>>(errorMessage, []);

        if (machinePositionList.Count < 2
            || machinePositionList.Any(t => t.Y - machinePositionList[0].Y == 0) == false // 检查y是否相同
            || machinePositionList.Zip(machinePositionList.Skip(1), (current, next) => current.X <= next.X).All(b => b) == false) // 检查x是否递增
            throw new ArgumentOutOfRangeException(nameof(machinePositionList), machinePositionList, null);

        var directionRect = calibrationStageService.GetMachineDirection();
        if (directionRect.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDto>>>(directionRect.ErrorMsg, []);
        var directionX = directionRect.Anything.XDirection;

        var picturePixelHeightRet = GetDarkFieldLineScanImageYPixelHeight(yOpticsMagTypeEnum);
        if (picturePixelHeightRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDto>>>(picturePixelHeightRet.ErrorMsg, []);
        var height = picturePixelHeightRet.Anything;

        var scanLineXPixelSize = calibrationSetting.SettingCommonParam.GetScanLineXPixelSize(yOpticsMagTypeEnum, xStageSpeedEnum);
        var extendWidth = xWidthPixel * scanLineXPixelSize / 2.0;

        // 计算采图的起点终点机械坐标
        var startPoint = new Point(machinePositionList[0].X - extendWidth, machinePositionList[0].Y);
        var endPoint = new Point(machinePositionList.Last().X + extendWidth * 3, machinePositionList[0].Y); // 后面多采集一段，防止最后一段数据不全

        // 从起点到终点采图，输出三通道长图片
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetImgPTP(new SxParamObj<M2CCollectImgParamDTO>(new M2CCollectImgParamDTO
            {
                Mag = yOpticsMagTypeEnum.ToSxMagEnum(),
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
            .Select(Convert.ToInt32)
            .ToList();

        var splitImagesAllChannels = new List<List<DarkFieldImageDto>>();
        foreach (var channelId in Enumerable.Range(0, 3))
        {
            var rawBytes = File.ReadAllBytes(darkFieldImagesRet.Anything[channelId].Url);
            var (_, bodyBytesStartIndex, bodyBytesLength) = calibrationAlgorithmService.GetSize(rawBytes);
            ReadOnlySpan<byte> span = rawBytes.AsSpan().Slice(bodyBytesStartIndex, bodyBytesLength);

            var splitImages = new List<DarkFieldImageDto>();
            foreach (var (index, pointer) in pointerList.Select((t, i) => (Index: i, Pointer: t)))
            {
                var pointerTemp = pointer - pointer % heightPixelOfByte; // dieWidthPixel不是整数倍, 需要对齐
                byte[] array;
                if (pointerTemp + splitImageLength > rawBytes.Length)
                {
                    if (index != pointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                    var offset = xWidthPixel - (rawBytes.Length - pointerTemp) / heightPixelOfByte;

                    pointerTemp += offset * heightPixelOfByte;
                    array = span[pointerTemp..].ToArray();
                }
                else
                {
                    array = span.Slice(pointerTemp, splitImageLength).ToArray();
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