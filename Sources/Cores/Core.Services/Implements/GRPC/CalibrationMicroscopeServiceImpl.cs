using Core.Models.Enums.Microscope;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Interface.Calibration;
using Cuga.Interface.Diagnosis;
using Cuga.Interface.Facade;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;
using Semix.GRPC.DTO.Basic;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationMicroscopeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationMicroscopeServiceImpl : BaseService<ICgCalibMicroscopeService, ICgDiagReviewService, ICgFacadeReviewService>, ICalibrationMicroscopeService
{
    private List<CgMicroscopeInfo>? _cgMicroscopeInfoList;

    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService();
            IsConnected = createService.IsSuccess;

            return createService;
        });
    }

    public SxExecuteRet<CgMicroscopeLens> MicroscopeMagnificationEnumToCgMicroscopeLens(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        var sxExecuteRet = GetLensList();
        var cgMicroscopeInfo = sxExecuteRet.Anything.SingleOrDefault(m => m.Lens == microscopeMagnificationEnum.ToCgLens());

        return sxExecuteRet.IsSuccess == false || cgMicroscopeInfo is null
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default(CgMicroscopeLens))
            : SxExecuteRetHelper.CreateSuccess(cgMicroscopeInfo.LensCode);
    }

    public SxExecuteRet<MicroscopeMagnificationEnum> CgMicroscopeLensToMicroscopeMagnificationEnum(CgMicroscopeLens cgMicroscopeLens)
    {
        var sxExecuteRet = GetLensList();
        var cgMicroscopeInfo = sxExecuteRet.Anything.SingleOrDefault(m => m.LensCode == cgMicroscopeLens);

        return sxExecuteRet.IsSuccess == false || cgMicroscopeInfo is null
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default(MicroscopeMagnificationEnum))
            : SxExecuteRetHelper.CreateSuccess(cgMicroscopeInfo.Lens.ToMicroscopeMagnificationEnum());
    }

    public SxExecuteRet<MicroscopeMagnificationEnum> GetMagnification()
    {
        var sxExecuteRet = Invoke(() => Service?.GetMagnification());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default(MicroscopeMagnificationEnum))
            : CgMicroscopeLensToMicroscopeMagnificationEnum(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SwitchMagnification(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        var microscopeMagnificationEnumToCgMicroscopeLens = MicroscopeMagnificationEnumToCgMicroscopeLens(microscopeMagnificationEnum);
        if (microscopeMagnificationEnumToCgMicroscopeLens.IsSuccess == false) return SxExecuteRetHelper.CreateError(microscopeMagnificationEnumToCgMicroscopeLens.Msg, false);

        var sxExecuteRet = Invoke(() => Service3?.SwitchMicroscopeLensNoCorrection(new SxParamObj<ESxMicroscopelens>(microscopeMagnificationEnumToCgMicroscopeLens.Anything.ToESxMicroscopeLens())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SwitchMagnificationNotAutoFocus(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        var microscopeMagnificationEnumToCgMicroscopeLens = MicroscopeMagnificationEnumToCgMicroscopeLens(microscopeMagnificationEnum);
        if (microscopeMagnificationEnumToCgMicroscopeLens.IsSuccess == false) return SxExecuteRetHelper.CreateError(microscopeMagnificationEnumToCgMicroscopeLens.Msg, false);

        var sxExecuteRet = Invoke(() => Service?.SwitchMagnification(new SxParamObj<CgMicroscopeLens>(microscopeMagnificationEnumToCgMicroscopeLens.Anything)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetVoltage(double voltage)
    {
        var sxExecuteRet = Invoke(() => Service?.SetVoltage(new SxParamObj<double>(voltage)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetVoltage()
    {
        var sxExecuteRet = Invoke(() => Service2?.ReadVolt());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0d)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything));
    }

    public SxExecuteRet<(double min, double max)> GetVoltageRange()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadVoltLimit());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default((double min, double max)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(sxExecuteRet.Anything.Lower), Convert.ToDouble(sxExecuteRet.Anything.Upper)));
    }

    private SxExecuteRet<List<CgMicroscopeInfo>> GetLensList()
    {
        if (_cgMicroscopeInfoList is not null) return SxExecuteRetHelper.CreateSuccess(_cgMicroscopeInfoList);

        var sxExecuteRet = Invoke(() => Service2?.GetMicroscopeModels());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, new List<CgMicroscopeInfo>());
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<List<CgMicroscopeInfo>>("Lens List is empty", []);

        _cgMicroscopeInfoList = sxExecuteRet.Anything.Single(t => t.IsUsed).Info;

        return SxExecuteRetHelper.CreateSuccess(_cgMicroscopeInfoList);
    }
}