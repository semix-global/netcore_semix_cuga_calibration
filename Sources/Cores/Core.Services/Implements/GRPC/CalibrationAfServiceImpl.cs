using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Agent.Facade.Service.MachineFacade;
using Cuga.Data.DataStruct.Autofocus;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Stage;
using Cuga.Interface.Calibration;
using Cuga.Interface.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationAfService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationAfServiceImpl(ICalibrationMicroscopeService microscopeService) : BaseService<ICgCalibAutofocusService, ICgDiagAutofocusService, ICgFacadeSwathService>, ICalibrationAfService
{
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

    public SxExecuteRet<bool> ToggleBrightFieldEnable(bool enable)
    {
        var sxExecuteRet = enable ? Invoke(() => Service?.ChangeMode(new SxParamObj<CgAutofocusModeType>(CgAutofocusModeType.Review))) : Invoke(() => Service?.ChangeMode(new SxParamObj<CgAutofocusModeType>(CgAutofocusModeType.ECS)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleDarkFieldEnable(bool enable)
    {
        var sxExecuteRet = enable ? Invoke(() => Service?.ChangeMode(new SxParamObj<CgAutofocusModeType>(CgAutofocusModeType.Inspection))) : Invoke(() => Service?.ChangeMode(new SxParamObj<CgAutofocusModeType>(CgAutofocusModeType.Inspection_Diagnosis)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleCalChipSiteModelEnum(CalChipSiteModelEnum calChipSiteModelEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetCalchip(new SxParamObj<CgCalChipEnum>(calChipSiteModelEnum.ToCgCalChipEnum())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetSensorEcsValue()
    {
        Thread.Sleep(120);
        var sxExecuteRet = Invoke(() => Service2?.ReadData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0.0d)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything.Ecs));
    }

    public SxExecuteRet<double> GetSensorAverageEcsValue()
    {
        Thread.Sleep(120);
        var sxExecuteRet = Invoke(() => Service2?.ReadData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0.0d)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything.ECSAVG));
    }

    public SxExecuteRet<(bool IsReview, double CurrentEcsValue)> GetSensorIsReviewValue()
    {
        Thread.Sleep(120);
        var sxExecuteRet = Invoke(() => Service2?.ReadData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(bool IsReview, double EcsValue)>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess((sxExecuteRet.Anything.Mode == CgAutofocusModeType.Review, Convert.ToDouble(sxExecuteRet.Anything.Ecs)));
    }

    public SxExecuteRet<bool> SetSensorEcsValue(double ecs)
    {
        var sxExecuteRet = Invoke(() => Service2?.SetECS(new SxParamObj<int>(Convert.ToInt32(ecs))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorMicroscopeObjValue(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        var ret = microscopeService.MicroscopeMagnificationEnumToCgMicroscopeLens(microscopeMagnificationEnum);
        if (ret.IsSuccess == false) return SxExecuteRetHelper.CreateError(ret.Msg, false);

        var sxExecuteRet = Invoke(() => Service?.SetMicroscopeObj(new SxParamObj<CgMicroscopeLens>(ret.Anything)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> GetSensorNscCurveIsOk()
    {
        var sxExecuteRet = Invoke(() => Service2?.ReadData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.Signal == 1);
    }

    public SxExecuteRet<(double F, double N)> GetSensorFnValue(bool isA)
    {
        var sxExecuteRet = Invoke(() => Service2?.ReadData());

        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<(double F, double N)>(sxExecuteRet.Msg, (0, 0));

        var f = Convert.ToDouble(isA ? sxExecuteRet.Anything.FA : sxExecuteRet.Anything.FB);
        var n = Convert.ToDouble(isA ? sxExecuteRet.Anything.NA : sxExecuteRet.Anything.NB);

        return SxExecuteRetHelper.CreateSuccess((f, n));
    }

    public SxExecuteRet<double> GetSensorCurrentValue(bool isA)
    {
        var sxExecuteRet = Invoke(() => Service2?.ReadData());

        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0.0d);

        var led = Convert.ToDouble(isA ? sxExecuteRet.Anything.LedA : sxExecuteRet.Anything.LedB);

        return SxExecuteRetHelper.CreateSuccess(led);
    }

    public SxExecuteRet<bool> SetSensorCurrentValue(bool isA, double current)
    {
        var sxExecuteRet = isA
            ? Invoke(() => Service?.SetLedA(new SxParamObj<ushort>(Convert.ToUInt16(current))))
            : Invoke(() => Service?.SetLedB(new SxParamObj<ushort>(Convert.ToUInt16(current))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Offset, double Gain)> GetSensorNscCompensationCoefficient()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetSensorNscCompensationCoefficient(double offset, double gain)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<List<double>> GetSensorAfErrorTraceBufferList(TimeSpan timeSpan)
    {
        var sxExecuteRet = Invoke(() => Service?.GetAuotofocusTraceBufferData(new SxParamObj<TimeSpan>(timeSpan)));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<double>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.AFERROR.Count == 0) return SxExecuteRetHelper.CreateError<List<double>>("Af error trace buffer is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.AFERROR.Select(Convert.ToDouble).ToList());
    }

    public SxExecuteRet<List<double>> GetSensorNscTraceBufferList(TimeSpan timeSpan)
    {
        var sxExecuteRet = Invoke(() => Service?.GetAuotofocusTraceBufferData(new SxParamObj<TimeSpan>(timeSpan)));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<double>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Nsc.Count == 0) return SxExecuteRetHelper.CreateError<List<double>>("Nsc trace buffer is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.Nsc.Select(Convert.ToDouble).ToList());
    }

    public SxExecuteRet<List<(double Ecs, double Nsc, double Lvdt)>> GetNscCompensationCoefficientTraceBufferList(double startEcs, double endEcs, double speedEcs, TimeSpan timeSpan)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckStandardEcsValue(MicroscopeMagnificationEnum microscopeMagnificationEnum, double standardEcsValue)
    {
        var ret = microscopeService.MicroscopeMagnificationEnumToCgMicroscopeLens(microscopeMagnificationEnum);
        if (ret.IsSuccess == false) return SxExecuteRetHelper.CreateError(ret.Msg, false);

        var sxExecuteRet = Invoke(() => Service?.SetMicroscopeEcs(new SxParamObj<(CgMicroscopeLens lens, ushort ecs)>((ret.Anything, Convert.ToUInt16(standardEcsValue)))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.SetBFCalChipMachinePosition(new SxParamObj<(CgCalChipEnum calhip, CgPoint position)>((CgCalChipEnum.None, position.ToCgPoint()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.SetBFCalChipMachinePosition(new SxParamObj<(CgCalChipEnum calhip, CgPoint position)>((calChipSiteModelEnum.ToCgCalChipEnum(), position.ToCgPoint()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        var sxExecuteRet = Invoke(() => Service?.SetBFCalChipMachineHeight(new SxParamObj<(CgCalChipEnum calhip, ushort height)>((calChipSiteModelEnum.ToCgCalChipEnum(), Convert.ToUInt16(standardEcsValue)))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldChuckCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDFCalChipMachinePosition(new SxParamObj<(CgCalChipEnum calhip, CgPoint position)>((CgCalChipEnum.None, position.ToCgPoint()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDFCalChipMachinePosition(new SxParamObj<(CgCalChipEnum calhip, CgPoint position)>((calChipSiteModelEnum.ToCgCalChipEnum(), position.ToCgPoint()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldChuckStandardEcsValue(double standardEcsValue)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDFCenterStandardEcs(new SxParamObj<ushort>(Convert.ToUInt16(standardEcsValue))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDFCalChipMachineHeight(new SxParamObj<(CgCalChipEnum calhip, ushort height)>((calChipSiteModelEnum.ToCgCalChipEnum(), Convert.ToUInt16(standardEcsValue)))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDarkFieldAutoFocusMotorAbsoluteValue(double value)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDFAutofocusMotorValue(new SxParamObj<double>(value)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(Point[] tracebuffer, double k)> NscDiagnosis()
    {
        return SxExecuteRetHelper.CreateSuccess<(Point[], double)>(([Point.Origin], 1d));
    }
}