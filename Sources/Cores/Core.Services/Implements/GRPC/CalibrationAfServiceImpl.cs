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
using Net.Utilities.Models;
using Semix.CoreLib;
using Semix.GRPC.DTO;

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

    public SxExecuteRet<bool> SetSensorCurrentValue(double current, bool isA)
    {
        var sxExecuteRet = isA
            ? Invoke(() => Service?.SetLedA(new SxParamObj<ushort>(Convert.ToUInt16(current))))
            : Invoke(() => Service?.SetLedB(new SxParamObj<ushort>(Convert.ToUInt16(current))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
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

    public SxExecuteRet<List<double>> GetSensorAfErrorTraceBufferList(TimeSpan timeSpan)
    {
        var sxExecuteRet = Invoke(() => Service?.GetAuotofocusTraceBufferData(new SxParamObj<TimeSpan>(timeSpan)));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<double>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.AFERROR.Count == 0) return SxExecuteRetHelper.CreateError<List<double>>("Af error trans buffer is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.AFERROR.Select(Convert.ToDouble).ToList());
    }

    public SxExecuteRet<bool> SetDarkFieldAutoFocusMotorAbsoluteValue(double value)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDFAutofocusMotorValue(new SxParamObj<double>(value)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double Score)> CalChipDswAfRtfc(Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.RunRealTimeAutofocusCalibration(new SxParamObj<(CgPoint position, uint type)>((position.ToCgPoint(), 0))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double Ecs, double Score)>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess<(double Ecs, double Score)>((sxExecuteRet.Anything.ecs, sxExecuteRet.Anything.score));
    }

    public SxExecuteRet<(double Ecs, double Score)> CalChipHazeAfRtfc(Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.RunRealTimeAutofocusCalibration(new SxParamObj<(CgPoint position, uint type)>((position.ToCgPoint(), 1))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double Ecs, double Score)>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess<(double Ecs, double Score)>((sxExecuteRet.Anything.ecs, sxExecuteRet.Anything.score));
    }

    public SxExecuteRet<(double Ecs, double Height)> ChuckAfRtfc(Point position)
    {
        var sxExecuteRet = Invoke(() => Service3?.RTFC(new SxParamObj<SxPointD>(position.ToSxPointD())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double Ecs, double Height)>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess<(double Ecs, double Height)>((sxExecuteRet.Anything.ECS, sxExecuteRet.Anything.Offset));
    }
}