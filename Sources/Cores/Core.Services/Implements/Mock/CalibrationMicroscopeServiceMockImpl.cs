using Core.Models.Helper;
using Core.Models.Models.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationMicroscopeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationMicroscopeServiceMockImpl : ICalibrationMicroscopeService
{
    private static readonly Random Random = new();
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<CgMicroscopeLens> MicroscopeMagnificationInfoToCgMicroscopeLens(MicroscopeMagnificationInfo microscopeMagnificationInfo)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(microscopeMagnificationInfo.AdaptTo().LensCode);
    }

    public SxExecuteRet<MicroscopeMagnificationInfo> CgMicroscopeLensToMicroscopeMagnificationInfo(CgMicroscopeLens cgMicroscopeLens)
    {
        Thread.Sleep(100);
        var lensList = GetLensList();
        var microscopeMagnificationInfo = new MicroscopeMagnificationInfo();
        return SxExecuteRetHelper.CreateSuccess(microscopeMagnificationInfo.AdaptIn(lensList.Anything.SingleOrDefault(t => t.LensCode == cgMicroscopeLens)!));
    }

    public SxExecuteRet<MicroscopeMagnificationInfo> GetMagnification()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_microscopeMagnificationInfo);
    }

    public SxExecuteRet<bool> SwitchMagnification(MicroscopeMagnificationInfo microscopeMagnificationInfo)
    {
        Thread.Sleep(100);

        _microscopeMagnificationInfo = microscopeMagnificationInfo;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SwitchMagnificationNotAutoFocus(MicroscopeMagnificationInfo microscopeMagnificationInfo)
    {
        Thread.Sleep(100);

        _microscopeMagnificationInfo = microscopeMagnificationInfo;

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

    public SxExecuteRet<List<CgMicroscopeInfo>> GetLensList()
    {
        var infoList = new List<CgMicroscopeInfo>
        {
            new()
            {
                Lens = 5,
                LensCode = CgMicroscopeLens.One,
                LensName = "5X"
            },
            new()
            {
                Lens = 10,
                LensCode = CgMicroscopeLens.Two,
                LensName = "10X"
            },
            new()
            {
                Lens = 50,
                LensCode = CgMicroscopeLens.Three,
                LensName = "50X"
            },
            new()
            {
                Lens = 100,
                LensCode = CgMicroscopeLens.Four,
                LensName = "100X"
            },
            new()
            {
                Lens = 150,
                LensCode = CgMicroscopeLens.Five,
                LensName = "150X"
            }
        };
        return SxExecuteRetHelper.CreateSuccess(infoList);
    }
}