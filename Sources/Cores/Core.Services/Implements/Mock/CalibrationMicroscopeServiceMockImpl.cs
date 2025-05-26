using Core.Models.Enums.Microscope;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationMicroscopeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationMicroscopeServiceMockImpl : ICalibrationMicroscopeService
{
    private static readonly Random Random = new();
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<CgMicroscopeLens> MicroscopeMagnificationEnumToCgMicroscopeLens(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(microscopeMagnificationEnum.ToCgMicroscopeLens());
    }

    public SxExecuteRet<MicroscopeMagnificationEnum> CgMicroscopeLensToMicroscopeMagnificationEnum(CgMicroscopeLens cgMicroscopeLens)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(cgMicroscopeLens.ToMicroscopeMagnificationEnum());
    }

    public SxExecuteRet<MicroscopeMagnificationEnum> GetMagnification()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_microscopeMagnificationEnum);
    }

    public SxExecuteRet<bool> SwitchMagnification(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        Thread.Sleep(100);

        _microscopeMagnificationEnum = microscopeMagnificationEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SwitchMagnificationNotAutoFocus(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        Thread.Sleep(100);

        _microscopeMagnificationEnum = microscopeMagnificationEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetVoltage(double voltage)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetVoltage()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Random.NextDouble());
    }

    public SxExecuteRet<(double min, double max)> GetVoltageRange()
    {
        return SxExecuteRetHelper.CreateSuccess((0d, 1640d));
    }
}