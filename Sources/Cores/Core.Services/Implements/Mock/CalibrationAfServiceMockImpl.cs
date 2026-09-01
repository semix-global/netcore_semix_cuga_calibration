using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using MiniExcelLibs;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationAfService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationAfServiceMockImpl : ICalibrationAfService
{
    private static readonly string[] NSCCuveFilePaths = Directory.EnumerateFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\NSCSCurve"), "*.csv")
        .OrderBy(t => t)
        .ToArray();

    private CalChipSiteModelEnum _calChipSiteModelEnum;
    private double _ecsValue = Convert.ToDouble(Path.GetFileNameWithoutExtension(NSCCuveFilePaths[0]).Split(['E', 'C', 'S'], StringSplitOptions.RemoveEmptyEntries)[^1]);
    private double _currentAValue;
    private double _currentBValue;
    private double _nscOffsetValue;
    private double _nscGainValue;
    private double _ka;
    private double _offsetA;
    private double _kb;
    private double _offsetB;

    private int _nextNSCCurveFilePathIndex;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleBrightFieldEnable(bool isEnable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleDarkFieldEnable(bool enable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleCalChipSiteModelEnum(CalChipSiteModelEnum calChipSiteModelEnum)
    {
        Thread.Sleep(100);
        _calChipSiteModelEnum = calChipSiteModelEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(bool IsEnable, CalChipSiteModelEnum CalChipSiteModelEnum)> GetBrightFieldStatus()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((true, _calChipSiteModelEnum));
    }

    public SxExecuteRet<double> GetNmPerEcs()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(200d);
    }

    public SxExecuteRet<double> GetEcsPerOffsetMotorMm()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(23d);
    }

    public SxExecuteRet<double> GetSensorEcsValue()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_ecsValue);
    }

    public SxExecuteRet<double> GetSensorAverageEcsValue()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_ecsValue);
    }

    public SxExecuteRet<bool> SetSensorEcsValue(double ecs)
    {
        Thread.Sleep(100);
        _ecsValue = ecs;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Min, double Max)> GetEcsMoveRange()
    {
        return SxExecuteRetHelper.CreateSuccess((0d, 10000d));
    }

    public SxExecuteRet<bool> SetSensorMicroscopeObjValue(MicroscopeLensInformation microscopeLensInformation)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> GetSensorNscCurveIsOk()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double F, double N)> GetSensorFnValue(bool isA)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.Shared.NextDouble(), Random.Shared.NextDouble()));
    }

    public SxExecuteRet<double> GetSensorCurrentValue(bool isA)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(isA ? _currentAValue : _currentBValue);
    }

    public SxExecuteRet<bool> SetSensorCurrentValue(bool isA, double current)
    {
        if (isA)
            _currentAValue = current;
        else
            _currentBValue = current;

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Offset, double Gain)> GetSensorNscCompensation()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((_nscOffsetValue, _nscGainValue));
    }

    public SxExecuteRet<bool> SetSensorNscCompensation(double offset, double gain)
    {
        _nscOffsetValue = offset;
        _nscGainValue = gain;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double KA, double OffsetA, double KB, double OffsetB)> GetFAFBCompensation()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((_ka, _offsetA, _kb, _offsetB));
    }

    public SxExecuteRet<bool> SetFAFBCompensation(double ka, double offsetA, double kb, double offsetB)
    {
        _ka = ka;
        _offsetA = offsetA;
        _kb = kb;
        _offsetB = offsetB;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<double>> GetSensorAfErrorTraceBufferList(TimeSpan timeSpan)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 1000).Select(_ => Random.Shared.NextDouble()).ToList());
    }

    public SxExecuteRet<List<double>> GetSensorNscTraceBufferList(TimeSpan timeSpan)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 1000).Select(_ => Random.Shared.NextDouble()).ToList());
    }

    public SxExecuteRet<List<(double Ecs, double Nsc, double AFError, double Lvdt, double Fa, double Na, double Fb, double Nb)>> GetSensorNscTraceBufferList(double startEcs, double endEcs, double speedEcs, TimeSpan timeSpan)
    {
        Interlocked.Increment(ref _nextNSCCurveFilePathIndex);

        var path = NSCCuveFilePaths[_nextNSCCurveFilePathIndex % NSCCuveFilePaths.Length];

        var rows = MiniExcel.Query(path, true).Cast<IDictionary<string, object>>();
        var dataList = rows.Select(t =>
            (Convert.ToDouble(t["ECS"]),
                Convert.ToDouble(t["NSC"]),
                Convert.ToDouble(t["NSC"]),
                Convert.ToDouble(t["ECS"]),
                Convert.ToDouble(t["FA"]),
                Convert.ToDouble(t["NA"]),
                Convert.ToDouble(t["FB"]),
                Convert.ToDouble(t["NB"]))
        ).ToList();

        _ecsValue = Convert.ToDouble(Path.GetFileNameWithoutExtension(path).Split(['E', 'C', 'S'], StringSplitOptions.RemoveEmptyEntries)[^1]);

        return SxExecuteRetHelper.CreateSuccess(dataList);
    }

    public SxExecuteRet<List<(double Trigger, double X, double Ecs)>> GetZAndXSyncModeTraceBufferList(TimeSpan timeSpan)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 1000).Select(_ => (Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble())).ToList());
    }

    public SxExecuteRet<double> GetSensorNscRelativeZero()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(0d);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckCenterMachinePositionValue(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckStandardEcsValue(MicroscopeLensInformation microscopeLensInformation, double standardEcsValue)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldChuckCenterMachinePositionValue(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldChuckStandardEcsValue(double standardEcsValue)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDarkFieldAutoFocusMotorAbsoluteValue(double value)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetDarkFieldAutoFocusMotorAbsoluteValue()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(16d);
    }

    public SxExecuteRet<(double, double)> GetDarkFieldAutoFocusMotorMoveRange()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((12d, 32d));
    }
}