using System.IO;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
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

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserBeamPosition()
    {
        Thread.Sleep(100);

        _curPosition = new Point(Random.Next(240, 300), Random.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserOriginPosition()
    {
        Thread.Sleep(100);

        _curPosition = new Point(Random.Next(240, 300), Random.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<bool> AdjustmentOfReflector(bool isEnable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetLaserPowerMeterLightIntensity()
    {
        Thread.Sleep(100);
        return SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(Random.Next(1, 30) * _coefficient));
    }

    public SxExecuteRet<double> LightLevelToLightCoefficient(double level)
    {
        return SxExecuteRetHelper.CreateSuccess(level / 200);
    }

    public SxExecuteRet<double> LightCoefficientToLightLevel(double coefficient)
    {
        return SxExecuteRetHelper.CreateSuccess(coefficient * 200);
    }

    public SxExecuteRet<DarkFieldPrescanDto> ReadPrescanByFile(string filePath, double coefficient)
    {
        return _calibrationLaserServiceImpl.ReadPrescanByFile(filePath, coefficient);
    }

    public SxExecuteRet<DarkFieldPrescanDto> SetPrescanByRate(DarkFieldPrescanDto darkFieldPrescanDto, List<double> prescanRateList)
    {
        return _calibrationLaserServiceImpl.SetPrescanByRate(darkFieldPrescanDto, prescanRateList);
    }

    public SxExecuteRet<bool> SendOpticsMagType(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        Thread.Sleep(100);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByCoefficient(OpticsMagTypeEnum yOpticsMagTypeEnum, double coefficient)
    {
        Thread.Sleep(100);
        _coefficient = coefficient;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByList(DarkFieldPrescanDto darkFieldPrescanDto)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByCustomFile(string filePath)
    {
        return _calibrationLaserServiceImpl.ReadChirpAodByCustomFile(filePath);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByConfigFile(string filePath)
    {
        return _calibrationLaserServiceImpl.ReadChirpAodByConfigFile(filePath);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> GetChirpAodByChangeRateFromFile(DarkFieldChirpAodWaveDto currentDarkFieldChirpAodWaveDto, double rateChange)
    {
        return _calibrationLaserServiceImpl.GetChirpAodByChangeRateFromFile(currentDarkFieldChirpAodWaveDto, rateChange);
    }

    public SxExecuteRet<bool> SendChirpAodByList(DarkFieldChirpAodWaveDto darkFieldChirpAodDto)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAodDelayValue(OpticsMagTypeEnum yOpticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
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

    public SxExecuteRet<bool> ToggleProfileType(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId)
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

    public SxExecuteRet<List<(int PmtId, bool IsUsed, List<int> ChannelIdList)>> GetPmtConfigList()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<List<(int PmtId, bool IsUsed, List<int> ChannelIdList)>>([.. Enumerable.Range(1, 15).Select(t => (t, true, (List<int>)[1, 2, 3]))]);
    }

    public SxExecuteRet<List<List<double>>> GetPmtDataList(int count, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(new List<List<double>> { Enumerable.Range(1, 800).Select(_ => Random.NextDouble() * 3950).ToList() });
    }

    public SxExecuteRet<List<DarkFieldPmtDataDto>> GetPmtDataList()
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

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<double>>> GetPmtSenseDataList(int count, int pmtId, int channelId)
    {
        Thread.Sleep(2000);

        var result = new List<List<double>>(count);

        for (var i = 0; i < count; i++)
        {
            result.Add([.. Enumerable.Range(1, 800).Select(_ => Random.NextDouble() * 3950)]);
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<DarkFieldPmtDelayDto>> GetPmtDelayList()
    {
        var pmtDelayDtoList = new List<DarkFieldPmtDelayDto>();
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
                pmtDelayDtoList.Add(pMtDelayDto);
            }
        }

        return SxExecuteRetHelper.CreateSuccess(pmtDelayDtoList);
    }

    public SxExecuteRet<bool> SetPmtDelayList(List<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPmtGain(double[] gains, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPmtGain(List<string> pmtData, List<string> igData, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double AfMotor)> RuntimeAfCalibration(CalChipSiteModelEnum calChipSiteModelEnum, Point position, double coefficient)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble()));
    }

    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum yOpticsMagTypeEnum, bool isCuttingPixelHeight)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(1080);
    }

    public SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(Point position,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Data\\test.raw"));

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
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var uri = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Data\\test.raw");

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
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Data\\test.raw"));

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