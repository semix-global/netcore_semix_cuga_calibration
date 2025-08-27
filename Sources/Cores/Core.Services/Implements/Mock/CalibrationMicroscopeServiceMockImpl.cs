using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
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
    private MicroscopeLensInformation _microscopeLensInformation = new()
    {
        Magnification = 5,
        LensCode = 1,
        LensName = "5X"
    };

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<MicroscopeLensInformation>> GetMicroscopeLensInformationList()
    {
        Thread.Sleep(100);

        var microscopeLensInformationList = new List<MicroscopeLensInformation>
        {
            new()
            {
                Magnification = 5,
                LensCode = 1,
                LensName = "5X"
            },
            new()
            {
                Magnification = 10,
                LensCode = 2,
                LensName = "10X"
            },
            new()
            {
                Magnification = 50,
                LensCode = 3,
                LensName = "50X"
            },
            new()
            {
                Magnification = 100,
                LensCode = 4,
                LensName = "100X"
            },
            new()
            {
                Magnification = 150,
                LensCode = 5,
                LensName = "150X"
            }
        };

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<MicroscopeLensInformation>>(microscopeLensInformationList);
    }

    public SxExecuteRet<CgMicroscopeLens> MicroscopeLensInfoToCgMicroscopeLens(MicroscopeLensInformation microscopeLensInformation)
    {
        var sxExecuteRet = GetMicroscopeLensInformationList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, CgMicroscopeLens.None);

        var result = sxExecuteRet.Anything.SingleOrDefault(m => m.Magnification == microscopeLensInformation.Magnification);

        return result is null
            ? SxExecuteRetHelper.CreateError("Microscope Lens Information is not single", CgMicroscopeLens.None)
            : SxExecuteRetHelper.CreateSuccess(result.AdaptTo().LensCode);
    }

    public SxExecuteRet<MicroscopeLensInformation> CgMicroscopeLensToMicroscopeLensInfo(CgMicroscopeLens cgMicroscopeLens)
    {
        var sxExecuteRet = GetMicroscopeLensInformationList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, MicroscopeLensInformation.Default);

        var result = sxExecuteRet.Anything
            .SingleOrDefault(t => t.AdaptTo().LensCode == cgMicroscopeLens);

        return result is null
            ? SxExecuteRetHelper.CreateError("Microscope Lens Information is not single", MicroscopeLensInformation.Default)
            : SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<MicroscopeLensInformation> GetCurrentMicroscopeLensInformation()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_microscopeLensInformation);
    }

    public SxExecuteRet<bool> SwitchMicroscopeLensInformationNotAutoFocus(MicroscopeLensInformation microscopeLensInformation)
    {
        Thread.Sleep(100);

        _microscopeLensInformation = microscopeLensInformation;

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