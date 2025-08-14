using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationMicroscopeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationMicroscopeServiceImpl : BaseService<ICgCalibrationService>, ICalibrationMicroscopeService
{
    private List<CgMicroscopeInfo>? _cgMicroscopeInfoList;

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

    public SxExecuteRet<CgMicroscopeLens> MicroscopeMagnificationInfoToCgMicroscopeLens(MicroscopeMagnificationInfo microscopeMagnificationInfo)
    {
        var sxExecuteRet = GetLensList();
        var cgMicroscopeInfo = sxExecuteRet.Anything.SingleOrDefault(m => m.LensCode == (CgMicroscopeLens)microscopeMagnificationInfo.MagnificationCode);

        return sxExecuteRet.IsSuccess == false || cgMicroscopeInfo is null
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default(CgMicroscopeLens))
            : SxExecuteRetHelper.CreateSuccess(cgMicroscopeInfo.LensCode);
    }

    public SxExecuteRet<MicroscopeMagnificationInfo> CgMicroscopeLensToMicroscopeMagnificationInfo(CgMicroscopeLens cgMicroscopeLens)
    {
        var sxExecuteRet = GetLensList();
        var cgLens = sxExecuteRet.Anything.SingleOrDefault(m => m.LensCode == cgMicroscopeLens);
        if (cgLens is null) SxExecuteRetHelper.CreateError("cgLens is null", default(MicroscopeMagnificationInfo));
        var cgMicroscopeInfo = new MicroscopeMagnificationInfo();

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, cgMicroscopeInfo)
            : SxExecuteRetHelper.CreateSuccess(cgMicroscopeInfo.AdaptIn(cgLens!));
    }

    public SxExecuteRet<MicroscopeMagnificationInfo> GetMagnification()
    {
        var sxExecuteRet = Invoke(() => Service!.GetCurrentMicroscopeInfo());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new MicroscopeMagnificationInfo())
            : CgMicroscopeLensToMicroscopeMagnificationInfo(sxExecuteRet.Anything.LensCode);
    }

    public SxExecuteRet<bool> SwitchMagnification(MicroscopeMagnificationInfo microscopeMagnificationInfo)
    {
        var microscopeMagnificationInfoToCgMicroscopeLens = MicroscopeMagnificationInfoToCgMicroscopeLens(microscopeMagnificationInfo);
        if (microscopeMagnificationInfoToCgMicroscopeLens.IsSuccess == false) return SxExecuteRetHelper.CreateError(microscopeMagnificationInfoToCgMicroscopeLens.Msg, false);

        var sxExecuteRet = Invoke(() => Service!.SwitchMicroscopeLensNoCorrection(microscopeMagnificationInfoToCgMicroscopeLens.Anything.ToESxMicroscopeLens()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SwitchMagnificationNotAutoFocus(MicroscopeMagnificationInfo microscopeMagnificationInfo)
    {
        var microscopeMagnificationInfoToCgMicroscopeLens = MicroscopeMagnificationInfoToCgMicroscopeLens(microscopeMagnificationInfo);
        if (microscopeMagnificationInfoToCgMicroscopeLens.IsSuccess == false) return SxExecuteRetHelper.CreateError(microscopeMagnificationInfoToCgMicroscopeLens.Msg, false);

        var sxExecuteRet = Invoke(() => Service!.SwitchMicroscopeNoMode(microscopeMagnificationInfoToCgMicroscopeLens.Anything.ToESxMicroscopeLens().ToCgMicroscopeLens()));

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
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default(double))
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything));
    }

    public SxExecuteRet<(double min, double max)> GetVoltageRange()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadVoltLimit());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default((double min, double max)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(sxExecuteRet.Anything.lower), Convert.ToDouble(sxExecuteRet.Anything.upper)));
    }

    public SxExecuteRet<List<CgMicroscopeInfo>> GetLensList()
    {
        if (_cgMicroscopeInfoList is not null) return SxExecuteRetHelper.CreateSuccess(_cgMicroscopeInfoList);

        var sxExecuteRet = Invoke(() => Service!.GetLensList());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, new List<CgMicroscopeInfo>());
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<List<CgMicroscopeInfo>>("Lens List is empty", []);

        _cgMicroscopeInfoList = sxExecuteRet.Anything;

        return SxExecuteRetHelper.CreateSuccess(_cgMicroscopeInfoList);
    }
}