using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
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
    private MicroscopeLensInformation _defaultMicroscopeLensInformation = MicroscopeLensInformation.Default.Clone().AdaptIn(new CgMicroscopeInfo
    {
        Lens = 5,
        LensCode = CgMicroscopeLens.One,
        LensName = "5X"
    });

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<MicroscopeLensInformation>> GetMicroscopeLensInformations()
    {
        Thread.Sleep(100);

        var microscopeLensInformations = new[]
        {
            MicroscopeLensInformation.Default.Clone().AdaptIn(new CgMicroscopeInfo
            {
                Lens = 5,
                LensCode = CgMicroscopeLens.One,
                LensName = "5X"
            }),
            MicroscopeLensInformation.Default.Clone().AdaptIn(new CgMicroscopeInfo
            {
                Lens = 10,
                LensCode = CgMicroscopeLens.Two,
                LensName = "10X"
            }),
            MicroscopeLensInformation.Default.Clone().AdaptIn(new CgMicroscopeInfo
            {
                Lens = 50,
                LensCode = CgMicroscopeLens.Three,
                LensName = "50X"
            }),
            MicroscopeLensInformation.Default.Clone().AdaptIn(new CgMicroscopeInfo
            {
                Lens = 100,
                LensCode = CgMicroscopeLens.Four,
                LensName = "100X"
            }),
            MicroscopeLensInformation.Default.Clone().AdaptIn(new CgMicroscopeInfo
            {
                Lens = 10,
                LensCode = CgMicroscopeLens.Five,
                LensName = "10X-IR"
            })
        };

        Guard.IsTrue(microscopeLensInformations.DistinctBy(t => t).Count() == microscopeLensInformations.Length, "Microscope Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<MicroscopeLensInformation>>([.. microscopeLensInformations.OrderBy(t => t)]);
    }

    public SxExecuteRet<MicroscopeLensInformation> CgMicroscopeLensToMicroscopeLensInfo(CgMicroscopeLens cgMicroscopeLens)
    {
        var sxExecuteRet = GetMicroscopeLensInformations();
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

        return SxExecuteRetHelper.CreateSuccess(_defaultMicroscopeLensInformation);
    }

    public SxExecuteRet<bool> SwitchMicroscopeLensInformationNotAutoFocus(MicroscopeLensInformation microscopeLensInformation)
    {
        Thread.Sleep(100);

        _defaultMicroscopeLensInformation = microscopeLensInformation;

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

        return SxExecuteRetHelper.CreateSuccess(Random.Shared.NextDouble());
    }

    public SxExecuteRet<(double min, double max)> GetVoltageRange()
    {
        return SxExecuteRetHelper.CreateSuccess((0d, 1640d));
    }
}