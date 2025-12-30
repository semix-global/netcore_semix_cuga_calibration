using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationMicroscopeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationMicroscopeServiceImpl : BaseService<ICgCalibrationService>, ICalibrationMicroscopeService
{
    private IReadOnlyList<MicroscopeLensInformation>? _microscopeLensInformationList;

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

    public SxExecuteRet<IReadOnlyList<MicroscopeLensInformation>> GetMicroscopeLensInformations()
    {
        if (_microscopeLensInformationList is not null) return SxExecuteRetHelper.CreateSuccess(_microscopeLensInformationList);

        var sxExecuteRet = Invoke(() => Service?.GetLensList());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<MicroscopeLensInformation>>(sxExecuteRet.ErrorMsg, []);
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<IReadOnlyList<MicroscopeLensInformation>>("Microscope Lens Information is empty", []);

        _microscopeLensInformationList =
        [
            ..sxExecuteRet.Anything.Select(t => MicroscopeLensInformation.Default.Clone().AdaptIn(t)).OrderBy(t => t)
        ];

        Guard.IsTrue(_microscopeLensInformationList.DistinctBy(t => t).Count() == _microscopeLensInformationList.Count, "Microscope Information is not unique");

        return SxExecuteRetHelper.CreateSuccess(_microscopeLensInformationList);
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
        var sxExecuteRet = Invoke(() => Service!.GetCurrentMicroscopeInfo());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, MicroscopeLensInformation.Default)
            : CgMicroscopeLensToMicroscopeLensInfo(sxExecuteRet.Anything.LensCode);
    }

    public SxExecuteRet<bool> SwitchMicroscopeLensInformationNotAutoFocus(MicroscopeLensInformation microscopeLensInformation)
    {
        var sxExecuteRet = Invoke(() => Service!.SwitchMicroscopeNoMode(microscopeLensInformation.AdaptTo().LensCode));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetVoltage(double voltage)
    {
        var sxExecuteRet = Invoke(() => Service!.WriteMicroscopeVolt(Convert.ToInt32(voltage)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetVoltage()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadMicroscopeVolt());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything));
    }

    public SxExecuteRet<(double min, double max)> GetVoltageRange()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadVoltLimit());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default((double min, double max)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(sxExecuteRet.Anything.lower), Convert.ToDouble(sxExecuteRet.Anything.upper)));
    }
}