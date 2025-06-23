using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationAfService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationAfServiceMockImpl : ICalibrationAfService
{
    private static readonly Random Random = new();

    private double _ecsValue;
    private double _currentAValue;
    private double _currentBValue;
    private double _nscOffsetValue;
    private double _nscGainValue;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleBrightFieldEnable(bool enable)
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

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetSensorEcsValue()
    {
        Thread.Sleep(100);
        _ecsValue = Random.NextDouble() * 1000;

        return SxExecuteRetHelper.CreateSuccess(_ecsValue);
    }

    public SxExecuteRet<double> GetSensorAverageEcsValue()
    {
        Thread.Sleep(100);
        _ecsValue = Random.NextDouble() * 1000;

        return SxExecuteRetHelper.CreateSuccess(_ecsValue);
    }

    public SxExecuteRet<(bool IsReview, double CurrentEcsValue)> GetSensorIsReviewValue()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((true, _ecsValue));
    }

    public SxExecuteRet<bool> SetSensorEcsValue(double ecs)
    {
        Thread.Sleep(100);
        _ecsValue = ecs;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorMicroscopeObjValue(MicroscopeMagnificationEnum microscopeMagnificationEnum)
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

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble()));
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

    public SxExecuteRet<(double Offset, double Gain)> GetSensorNscCompensationCoefficient()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((_nscOffsetValue, _nscGainValue));
    }

    public SxExecuteRet<bool> SetSensorNscCompensationCoefficient(double offset, double gain)
    {
        _nscOffsetValue = offset;
        _nscGainValue = gain;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<double>> GetSensorAfErrorTraceBufferList(TimeSpan timeSpan)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 1000).Select(_ => Random.NextDouble()).ToList());
    }

    public SxExecuteRet<List<double>> GetSensorNscTraceBufferList(TimeSpan timeSpan)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 1000).Select(_ => Random.NextDouble()).ToList());
    }

    public SxExecuteRet<List<(double Ecs, double Nsc, double Lvdt)>> GetNscCompensationCoefficientTraceBufferList(double startEcs, double endEcs, double speedEcs, TimeSpan timeSpan)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 1000).Select(_ => (Random.NextDouble(), Random.NextDouble(), Random.NextDouble())).ToList());
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckCenterMachinePositionValue(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckStandardEcsValue(MicroscopeMagnificationEnum microscopeMagnificationEnum, double standardEcsValue)
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

    public SxExecuteRet<(double Ecs, double Score)> CalChipDswAfRtfc(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble()));
    }

    public SxExecuteRet<(double Ecs, double Score)> CalChipHazeAfRtfc(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble()));
    }

    public SxExecuteRet<(double Ecs, double Height)> ChuckAfRtfc(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble()));
    }

    public SxExecuteRet<(Point[] tracebuffer, double k)> NscDiagnosis()
    {
        return SxExecuteRetHelper.CreateSuccess<(Point[], double)>(([Point.Empty], 1d));
    }
}