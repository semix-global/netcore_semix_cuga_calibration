using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed partial class CalibrationLaserServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService)
    : BaseService<ICgCalibrationService>, ICalibrationLaserService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserBeamPosition()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadLaserBeamPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Empty, Point.Empty))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserOriginPosition()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadLaserBeamOriginPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Empty, Point.Empty))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<bool> AdjustmentOfReflector(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service!.LaserBeamAdjust(isEnable));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetLaserPowerMeterLightIntensity()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SendOpticsMagType(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.RefreshMag(Convert.ToInt32(yOpticsMagTypeEnum.ToCgMagTypeEnum())));
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByCoefficient(OpticsMagTypeEnum yOpticsMagTypeEnum, double coefficient)
    {
        var sxExecuteRet = Invoke(() => Service!.SetPrescan(Convert.ToInt32(yOpticsMagTypeEnum.ToCgMagTypeEnum()), coefficient));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendSaturationValue(double val)
    {
        var sxExecuteRet = Invoke(() => Service!.SetDCSaturation(val));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByList(DarkFieldPrescanDto darkFieldPrescanDto)
    {
        var sxExecuteRet = Invoke(() => Service!.SendPrescanFile_Illumination(darkFieldPrescanDto.RegNum, darkFieldPrescanDto.ZeroNum, darkFieldPrescanDto.PrescanByteList));
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendChirpAodByList(DarkFieldChirpAodWaveDto darkFieldChirpAodWaveDto)
    {
        var sxExecuteRet = Invoke(() => Service!.SetChirp_Calibration(darkFieldChirpAodWaveDto.ChirpAodWaveByteList, darkFieldChirpAodWaveDto.RegNum, darkFieldChirpAodWaveDto.ZeroNum));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<int>> GetUsedPmtIdList()
    {
        var sxExecuteRet = Invoke(() => Service!.GetPMTUsedID());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<int>>(sxExecuteRet.Msg, []);

        return SxExecuteRetHelper.CreateSuccess<List<int>>([
            .. sxExecuteRet.Anything
                .GroupBy(t => t.id)
                .Where(t => t.Distinct().Count() == 3)
                .Select(t => t.Key)
        ]);
    }

    public SxExecuteRet<List<double>> GetPmtDataList(int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service!.GetPMTData_Appoint(pmtId, channelId));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<double>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<List<double>>("Pmt Value List is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<List<DarkFieldPmtDataDto>> GetPmtDataList()
    {
        var pmtRet = Invoke(() => Service!.GetPMTDataALL());

        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldPmtDataDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDataDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDataModel => new DarkFieldPmtDataDto().AdaptIn(pmtDataModel)));

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<double>>> GetPmtSenseDataList(int pmtId, int channelId, int count)
    {
        var sxExecuteRet = Invoke(() => Service!.GetSenseData(count, pmtId, channelId));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<double>>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Data.Count != count) return SxExecuteRetHelper.CreateError<List<List<double>>>("Pmt Sense Value List is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.Data);
    }

    public SxExecuteRet<List<DarkFieldPmtDelayDto>> GetPmtDelayList()
    {
        var pmtRet = Invoke(() => Service!.GetPMTDelay());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldPmtDelayDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDelayDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDelayModel => new DarkFieldPmtDelayDto().AdaptIn(pmtDelayModel)));

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<bool> SetPmtDelayList(List<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        var pmtDelayModel = darkFieldPmtDelayDtoList.Select(item => item.AdaptTo()).ToList();

        var pmtRet = Invoke(() => Service!.SetPMTDelay(pmtDelayModel));

        return pmtRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(pmtRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPmtGain(string pmtGainFilePath, int pmtId, int channelId)
    {
        var resultString = File.ReadAllLines(pmtGainFilePath).Select(t => t.Trim()).Where(t => string.IsNullOrWhiteSpace(t) == false).ToList();
        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) throw new ArgumentException("filePath value error.");

        short[] values = [.. resultString.Select(str => Convert.ToInt16(str, 16))];

        var result = new List<byte>();
        foreach (var compArray in values.Select(BitConverter.GetBytes))
        {
            result.Add(compArray[1]);
            result.Add(compArray[0]);
        }

        var sxExecuteRet = Invoke(() => Service!.SendChirp(result, pmtId, channelId));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPmtGain(List<string> pmtData, List<string> igData, int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service!.SendPMTGain(pmtData, igData, pmtId, channelId));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.SetPolarization(opticsPolarizationTypeEnum.ToCgPolarizationTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.SetAOD_NO(opticsAodWorkingModeEnum.ToOpticsAodWorkingMode()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAodDelayValue(OpticsMagTypeEnum yOpticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        var sxExecuteRet = Invoke(() => Service!.SetMagAndWaveZero(yOpticsMagTypeEnum.ToCgMagTypeEnum(), Convert.ToInt32(chirpAodDelay), Convert.ToInt32(prescanAodDelay)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableAutoGain(bool enable)
    {
        var sxExecuteRet = Invoke(() => Service!.SetAGC(enable));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable)
    {
        var sxExecuteRet = Invoke(() => Service!.SetL0K(enable));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(double gain)
    {
        var sxExecuteRet = Invoke(() => Service!.SendDC(false, gain));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.GetSpeedInfo(yOpticsMagTypeEnum.ToSxMagEnum()));

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
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => Invoke(() => Service!.GetImg_Mag(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                xWidthPixel,
                position.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否正向*/isForward: isForward,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
            StageCoordinateSystemEnum.Machine => Invoke(() => Service!.LoadRawImg_Mag_ToStagePos(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                xWidthPixel,
                position.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否正向*/isForward: isForward,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
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
            StageCoordinateSystemEnum.Machine => Invoke(() => Service!.LoadRawImg_Mag_PTP(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                isForward ? startPosition.ToSxPointD() : endPosition.ToSxPointD(),
                isForward ? endPosition.ToSxPointD() : startPosition.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
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

        var extendWidth = xWidthPixel * xPixelSize / 2.0;

        // 计算采图的起点终点机械坐标
        var startPoint = new Point(machinePositionList[0].X - extendWidth, machinePositionList[0].Y);
        var endPoint = new Point(machinePositionList.Last().X + extendWidth * 3, machinePositionList[0].Y); // 后面多采集一段，防止最后一段数据不全

        // 从起点到终点采图，输出三通道长图片
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service!.LoadRawImg_Mag_PTP(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                startPoint.ToSxPointD(),
                endPoint.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
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