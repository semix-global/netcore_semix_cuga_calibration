using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Autofocus;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationAfService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationAfServiceImpl(ICalibrationMicroscopeService microscopeService) : BaseService<ICgCalibrationService>, ICalibrationAfService
{
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

    public SxExecuteRet<bool> ToggleBrightFieldEnable(bool isEnable)
    {
        var sxExecuteRet = isEnable ? Invoke(() => Service!.OpenReviewMode()) : Invoke(() => Service!.OpenEcsTestMode());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleDarkFieldEnable(bool enable)
    {
        var sxExecuteRet = enable ? Invoke(() => Service!.OpenDarkFieldMode()) : Invoke(() => Service!.OpenNscTestMode());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleCalChipSiteModelEnum(CalChipSiteModelEnum calChipSiteModelEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.SetCalChip(calChipSiteModelEnum.ToCgCalChipType()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(bool IsEnable, CalChipSiteModelEnum CalChipSiteModelEnum)> GetBrightFieldStatus()
    {
        Thread.Sleep(120);
        var sxExecuteRet = Invoke(() => Service!.GetAutofocusData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(bool IsReview, CalChipSiteModelEnum CalChipSiteModelEnum)>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess((sxExecuteRet.Anything.Mode == AutofocusMode.Review, CalChipSiteModelEnum.ChuckModel));
    }

    public SxExecuteRet<double> GetSensorEcsValue()
    {
        Thread.Sleep(120);
        var sxExecuteRet = Invoke(() => Service!.GetAutofocusData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0.0d)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything.Ecs));
    }

    public SxExecuteRet<double> GetSensorAverageEcsValue()
    {
        Thread.Sleep(120);
        var sxExecuteRet = Invoke(() => Service!.GetAutofocusData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0.0d)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything.ECSAVG));
    }

    public SxExecuteRet<bool> SetSensorEcsValue(double ecs)
    {
        var sxExecuteRet = Invoke(() => Service!.SetEcs(Convert.ToInt32(ecs)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorMicroscopeObjValue(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        var ret = microscopeService.MicroscopeMagnificationEnumToCgMicroscopeLens(microscopeMagnificationEnum);
        if (ret.IsSuccess == false) return SxExecuteRetHelper.CreateError(ret.Msg, false);

        var sxExecuteRet = Invoke(() => Service!.SetAFMicroscopeObj(ret.Anything));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> GetSensorNscCurveIsOk()
    {
        var sxExecuteRet = Invoke(() => Service!.GetAutofocusData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.Signal == 1);
    }

    public SxExecuteRet<(double F, double N)> GetSensorFnValue(bool isA)
    {
        var sxExecuteRet = Invoke(() => Service!.GetAutofocusData());

        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<(double F, double N)>(sxExecuteRet.Msg, (0, 0));

        var f = Convert.ToDouble(isA ? sxExecuteRet.Anything.FA : sxExecuteRet.Anything.FB);
        var n = Convert.ToDouble(isA ? sxExecuteRet.Anything.NA : sxExecuteRet.Anything.NB);

        return SxExecuteRetHelper.CreateSuccess((f, n));
    }

    public SxExecuteRet<double> GetSensorCurrentValue(bool isA)
    {
        var sxExecuteRet = Invoke(() => Service!.GetAutofocusData());

        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0.0d);

        var led = Convert.ToDouble(isA ? sxExecuteRet.Anything.LedA : sxExecuteRet.Anything.LedB);

        return SxExecuteRetHelper.CreateSuccess(led);
    }

    public SxExecuteRet<bool> SetSensorCurrentValue(bool isA, double current)
    {
        var sxExecuteRet = isA
            ? Invoke(() => Service!.SetLedA(Convert.ToUInt16(current)))
            : Invoke(() => Service!.SetLedB(Convert.ToUInt16(current)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Offset, double Gain)> GetSensorNscCompensationCoefficient()
    {
        var sxExecuteRet = Invoke(() => Service!.GetAutofocusData());

        return sxExecuteRet.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess<(double Offset, double Gain)>((sxExecuteRet.Anything.NSCOffset, sxExecuteRet.Anything.NSCGain / 1000d))
            : SxExecuteRetHelper.CreateError<(double Offset, double Gain)>(sxExecuteRet.Msg, (0, 0));
    }

    public SxExecuteRet<bool> SetSensorNscCompensationCoefficient(double offset, double gain)
    {
        /*
         * (NSC原始最大-NSC原始最小)/2
         * 5000/(最大-偏置)   放大1000倍
         */
        var sxExecuteRet = Invoke(() => Service!.SetNSCProperty(Convert.ToInt32(offset), Convert.ToInt32(gain * 1000)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<double>> GetSensorAfErrorTraceBufferList(TimeSpan timeSpan)
    {
        var sxExecuteRet = Invoke(() => Service!.GetAFTraceBuff(Convert.ToInt32(timeSpan.TotalMilliseconds)));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<double>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.AFERROR.Count == 0) return SxExecuteRetHelper.CreateError<List<double>>("Af error trace buffer is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.AFERROR.Select(Convert.ToDouble).ToList());
    }

    public SxExecuteRet<List<double>> GetSensorNscTraceBufferList(TimeSpan timeSpan)
    {
        var sxExecuteRet = Invoke(() => Service!.GetAFTraceBuff(Convert.ToInt32(timeSpan.TotalMilliseconds)));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<double>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Nsc.Count == 0) return SxExecuteRetHelper.CreateError<List<double>>("Nsc Trace buffer is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.Nsc.Select(Convert.ToDouble).ToList());
    }

    public SxExecuteRet<List<(double Ecs, double Nsc, double Lvdt)>> GetNscCompensationCoefficientTraceBufferList(double startEcs, double endEcs, double speedEcs, TimeSpan timeSpan)
    {
        var sxExecuteRet = Invoke(() => Service!.GetUniformAFDiagnosisData(Convert.ToInt32(startEcs), Convert.ToInt32(endEcs), Convert.ToInt32(speedEcs), Convert.ToInt32(timeSpan.TotalMilliseconds)));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<(double Ecs, double Nsc, double Lvdt)>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Ecs.Count == 0 ||
            sxExecuteRet.Anything.Nsc.Count == 0 ||
            sxExecuteRet.Anything.Lvdt.Count == 0 ||
            sxExecuteRet.Anything.Ecs.Count != sxExecuteRet.Anything.Nsc.Count ||
            sxExecuteRet.Anything.Nsc.Count != sxExecuteRet.Anything.Lvdt.Count) return SxExecuteRetHelper.CreateError<List<(double Ecs, double Nsc, double Lvdt)>>("Nsc Trace buffer is empty", []);

        return SxExecuteRetHelper.CreateSuccess<List<(double Ecs, double Nsc, double Lvdt)>>([.. sxExecuteRet.Anything.Ecs.Select((t, i) => (t, sxExecuteRet.Anything.Nsc[i], sxExecuteRet.Anything.Lvdt[i]))]);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckStandardEcsValue(MicroscopeMagnificationEnum microscopeMagnificationEnum, double standardEcsValue)
    {
        var ret = microscopeService.MicroscopeMagnificationEnumToCgMicroscopeLens(microscopeMagnificationEnum);
        if (ret.IsSuccess == false) return SxExecuteRetHelper.CreateError(ret.Msg, false);

        var sxExecuteRet = Invoke(() => Service!.WriteMicroscopeEcs(ret.Anything, Convert.ToUInt16(standardEcsValue)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldChuckCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service!.SetAFReviewCentricity(position.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorBrightFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.UndefinedModel:
                var sxExecuteRet1 = Invoke(() => Service!.SetAFBFCalChipLULoaction(position.ToCgPoint()));

                return sxExecuteRet1.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet1.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.DswModel:
                var sxExecuteRet2 = Invoke(() => Service!.SetAFBFCalChipRULoaction(position.ToCgPoint()));

                return sxExecuteRet2.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet2.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.ShinyWaferModel:
                var sxExecuteRet3 = Invoke(() => Service!.SetAFBFCalChipRDLoaction(position.ToCgPoint()));

                return sxExecuteRet3.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet3.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.HazeModel:
                var sxExecuteRet4 = Invoke(() => Service!.SetAFBFCalChipLDLoaction(position.ToCgPoint()));

                return sxExecuteRet4.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet4.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            default:
                return ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<bool>>(nameof(calChipSiteModelEnum));
        }
    }

    public SxExecuteRet<bool> SetSensorBrightFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.UndefinedModel:
                var sxExecuteRet1 = Invoke(() => Service!.SetAFBFCalChipLUHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet1.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet1.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.DswModel:
                var sxExecuteRet2 = Invoke(() => Service!.SetAFBFCalChipRUHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet2.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet2.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.ShinyWaferModel:
                var sxExecuteRet3 = Invoke(() => Service!.SetAFBFCalChipRDHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet3.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet3.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.HazeModel:
                var sxExecuteRet4 = Invoke(() => Service!.SetAFBFCalChipLDHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet4.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet4.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            default:
                return ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<bool>>(nameof(calChipSiteModelEnum));
        }
    }

    public SxExecuteRet<bool> SetSensorDarkFieldChuckCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service!.SetAFInspectionCentricity(position.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.UndefinedModel:
                var sxExecuteRet1 = Invoke(() => Service!.SetAFDFCalChipLULoaction(position.ToCgPoint()));

                return sxExecuteRet1.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet1.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.DswModel:
                var sxExecuteRet2 = Invoke(() => Service!.SetAFDFCalChipRULoaction(position.ToCgPoint()));

                return sxExecuteRet2.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet2.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.ShinyWaferModel:
                var sxExecuteRet3 = Invoke(() => Service!.SetAFDFCalChipRDLoaction(position.ToCgPoint()));

                return sxExecuteRet3.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet3.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.HazeModel:
                var sxExecuteRet4 = Invoke(() => Service!.SetAFDFCalChipLDLoaction(position.ToCgPoint()));

                return sxExecuteRet4.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet4.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            default:
                return ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<bool>>(nameof(calChipSiteModelEnum));
        }
    }

    public SxExecuteRet<bool> SetSensorDarkFieldChuckStandardEcsValue(double standardEcsValue)
    {
        var sxExecuteRet = Invoke(() => Service!.SetAFInspectionHeight(Convert.ToUInt16(standardEcsValue)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSensorDarkFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.UndefinedModel:
                var sxExecuteRet1 = Invoke(() => Service!.SetAFDFCalChipLUHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet1.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet1.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.DswModel:
                var sxExecuteRet2 = Invoke(() => Service!.SetAFDFCalChipRUHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet2.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet2.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.ShinyWaferModel:
                var sxExecuteRet3 = Invoke(() => Service!.SetAFDFCalChipRDHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet3.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet3.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            case CalChipSiteModelEnum.HazeModel:
                var sxExecuteRet4 = Invoke(() => Service!.SetAFDFCalChipLDHeight(Convert.ToUInt16(standardEcsValue)));

                return sxExecuteRet4.IsSuccess == false
                    ? SxExecuteRetHelper.CreateError(sxExecuteRet4.Msg, false)
                    : SxExecuteRetHelper.CreateSuccess(true);

            default:
                return ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<bool>>(nameof(calChipSiteModelEnum));
        }
    }

    public SxExecuteRet<bool> SetDarkFieldAutoFocusMotorAbsoluteValue(double value)
    {
        var sxExecuteRet = Invoke(() => Service!.SetAFPos(value));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(Point[] tracebuffer, double k)> NscDiagnosis()
    {
        return SxExecuteRetHelper.CreateSuccess<(Point[], double)>(([Point.Origin], 1d));
    }
}