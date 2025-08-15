using System.IO;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Core.Models.Models.Setting;
using Core.Models.Models.Pattern;


#if NET
using Core.Services.Implements.GRPC;
#else
using Core.Services.Implements.WCF;
#endif

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationLaserServiceMockImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService,
    CalibrationSetting calibrationSetting) : ICalibrationLaserService
{
    private static readonly Random Random = new();
    private readonly CalibrationLaserServiceImpl _calibrationLaserServiceImpl = new(calibrationAlgorithmService, calibrationStageService, calibrationConfigService, calibrationSetting);

    private Point _curPosition = new(0, 0);

    private double _coefficient = 1;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamPoint()
    {
        Thread.Sleep(100);

        _curPosition = new Point(Random.Next(240, 300), Random.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamOriginPoint()
    {
        Thread.Sleep(100);

        _curPosition = new Point(Random.Next(240, 300), Random.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<bool> AdjustBeamStabilizer(bool isEnable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetOpticalPowerMeter()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(Random.Next(1, 30) * _coefficient));
    }

    public SxExecuteRet<double> LevelToCoefficient(double level)
    {
        return SxExecuteRetHelper.CreateSuccess(level / 200);
    }

    public SxExecuteRet<double> CoefficientToLevel(double coefficient)
    {
        return SxExecuteRetHelper.CreateSuccess(coefficient * 200);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByCustomFile(string filePath)
    {
        return _calibrationLaserServiceImpl.ReadChirpAodByCustomFile(filePath);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> GetChirpAodByChangeRateFromFile(DarkFieldChirpAodWaveDto currentDarkFieldChirpAodWaveDto, double rateChange)
    {
        return _calibrationLaserServiceImpl.GetChirpAodByChangeRateFromFile(currentDarkFieldChirpAodWaveDto, rateChange);
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(OpticsMagTypeEnum opticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum opticsMagTypeEnum, double coefficient)
    {
        Thread.Sleep(100);

        _coefficient = coefficient;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetPrescanAODWaveProfileList(IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfileList)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChirpAODWaveProfileList(IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfileList)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleCIBControlTypeAndProfileType(CIBConfiguration cIbConfiguration, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableAutoGainControl(bool enable, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleProfileMode(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableMarkMode(bool enable, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(double gain, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSaturation(double saturation)
    {
        Thread.Sleep(100);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>> GetCIBConfigList()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>>([.. Enumerable.Range(1, 15).Select(t => (t, true, (List<int>)[1, 2, 3]))]);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfPMTDataList(int count, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<double>>>(new List<List<double>> { Enumerable.Range(1, 800).Select(_ => Random.NextDouble() * 3950).ToList() });
    }

    public SxExecuteRet<IReadOnlyList<DarkFieldPmtDataDto>> GetCIBOfPMTDataList()
    {
        var result = new List<DarkFieldPmtDataDto>();

        for (var i = 1; i < 16; i++)
        {
            for (var j = 1; j < 4; j++)
            {
                var pmtDataDto = new DarkFieldPmtDataDto
                {
                    PmtId = i,
                    Channel = j,
                    LineCount = 800,
                    Data = [.. Enumerable.Range(1, 800).Select(_ => Random.NextDouble())]
                };
                result.Add(pmtDataDto);
            }
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldPmtDataDto>>(result);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfSenseDataList(int count, int pmtId, int channelId)
    {
        Thread.Sleep(2000);

        var result = new List<List<double>>(count);

        for (var i = 0; i < count; i++)
        {
            result.Add([.. Enumerable.Range(1, 800).Select(_ => Random.NextDouble() * 3950)]);
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<double>>>(result);
    }

    public SxExecuteRet<IReadOnlyList<DarkFieldPmtDelayDto>> GetCIBDelayList()
    {
        var result = new List<DarkFieldPmtDelayDto>();

        for (var i = 1; i < 16; i++)
        {
            for (var j = 1; j < 4; j++)
            {
                var pMtDelayDto = new DarkFieldPmtDelayDto
                {
                    PmtId = i,
                    ChannelId = j,
                    PmtDelay = Random.Next(240, 300)
                };
                result.Add(pMtDelayDto);
            }
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldPmtDelayDto>>(result);
    }

    public SxExecuteRet<bool> SetCIBDelayList(IReadOnlyList<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPMTGain(List<string> pmtData, List<string> igData, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double AfMotor)> RuntimeAfCalibration(CalChipSiteModelEnum calChipSiteModelEnum, double? coefficient = null, Point? position = null)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble()));
    }

    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum opticsMagTypeEnum, bool isCuttingPixelHeight)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(1080);
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
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw"));

        var result = new List<DarkFieldImageDto>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
            result.Add(new DarkFieldImageDto { PmtId = pmtId, ChannelId = i + 1, Bytes = bytes, Image = image, Matrix = matrix });
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
        var uri = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw");

        var result = new List<DarkFieldRawScanImageDto>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            result.Add(new DarkFieldRawScanImageDto { PmtId = pmtId, ChannelId = i + 1, Url = uri });
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
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw"));

        var result = new List<List<DarkFieldImageDto>>(machinePositionList.Count);

        foreach (var temp in machinePositionList.Select(_ => new List<DarkFieldImageDto>(3)))
        {
            foreach (var i in Enumerable.Range(0, 3))
            {
                var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
                temp.Add(new DarkFieldImageDto { PmtId = pmtId, ChannelId = i + 1, Bytes = bytes, Image = image, Matrix = matrix });
            }

            result.Add(temp);
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }
}