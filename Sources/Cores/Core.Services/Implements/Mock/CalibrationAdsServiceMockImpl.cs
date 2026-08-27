using Core.Models.Helper;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationAdsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationAdsServiceMockImpl : ICalibrationAdsService
{
    private double _x1Value = 1.11;
    private double _x2Value = 2.22;
    private double _y1Value = 3.33;
    private double _y2Value = 4.44;
    private double _y3Value = 5.55;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double X1, double X2)> GetSensorXSpeedFeedForwardValue(bool isPositive)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((_x1Value, _x2Value));
    }

    public SxExecuteRet<bool> SetSensorXSpeedFeedForwardValue(bool isPositive, (double X1, double X2) value)
    {
        _x1Value = value.X1;
        _x2Value = value.X2;

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Y1, double Y2, double Y3)> GetSensorYSpeedFeedForwardValue(bool isPositive)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((_y1Value, _y2Value, _y3Value));
    }

    public SxExecuteRet<bool> SetSensorYSpeedFeedForwardValue(bool isPositive, (double Y1, double Y2, double Y3) value)
    {
        _y1Value = value.Y1;
        _y2Value = value.Y2;
        _y3Value = value.Y3;

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Z1, double Z2, double Z3)> GetSensorSpeedZ1Z2Z3Value()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble()));
    }

    public SxExecuteRet<bool> SetSensorFeedForwardPressureValue(double pressureValue1, double pressureValue2, double pressureValue3)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<(double PressureValue1, double PressureValue2, double PressureValue3)>> GetSensorAllPressureTraceBufferList(TimeSpan timeSpan)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 5000).Select(_ => (Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble())).ToList());
    }

    public async Task<SxExecuteRet<List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>>> GetSensorHeightRollPitchTraceBufferListAsync(CancellationToken cancellationToken)
    {
        await cancellationToken.WaitUntilCanceledAsync();

        return SxExecuteRetHelper.CreateSuccess(Enumerable.Range(1, 1).Select(_ => (Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble())).ToList());
    }

    public async Task<SxExecuteRet<(List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2, List<double> Height, List<double> Roll, List<double> Pitch, List<double> X_Speed, List<double> Y_Speed)>> GetSensorSpeedZ1Z2Z3TraceBufferListAsync(CancellationToken cancellationToken)
    {
        await cancellationToken.WaitUntilCanceledAsync();
        var lists = Enumerable.Range(1, 3).Select(_ => Enumerable.Range(1, 5000).Select(_ => (double)Random.Shared.Next(2400, 2500)).ToList()).ToList();
        var listHrp = Enumerable.Range(1, 3).Select(_ => Enumerable.Range(1, 5000).Select(_ => (double)Random.Shared.Next(50, 60)).ToList()).ToList();
        var listsV = Enumerable.Range(1, 2).Select(_ => Enumerable.Range(1, 5000).Select(_ => (double)Random.Shared.Next(45, 55)).ToList()).ToList();

        return SxExecuteRetHelper.CreateSuccess(
            (lists[0], lists[1], lists[2], listHrp[0], listHrp[1], listHrp[2], listsV[0], listsV[1]));
    }

    public async Task<SxExecuteRet<(List<double> X0, List<double> X1, List<double> Y0, List<double> Y1, List<double> Speed)>> GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferListAsync(bool isAxisX, CancellationToken cancellationToken)
    {
        await cancellationToken.WaitUntilCanceledAsync();
        var lists = Enumerable.Range(1, 5).Select(_ => Enumerable.Range(1, 1000).Select(_ => Random.Shared.NextDouble()).ToList()).ToList();
        lists[4] = [.. lists[4].Select((t, i) => i < 10 ? 0 : t)];

        return SxExecuteRetHelper.CreateSuccess((lists[0], lists[1], lists[2], lists[3], lists[4]));
    }

    public SxExecuteRet<bool> SetAdsXyEnabled(bool isEnabled)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}