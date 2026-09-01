using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(AfViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class AfViewModel(
    ICalibrationAfService calibrationAfService,
    ICalibrationEFEMService calibrationEFEMService,
    ILogger<AfViewModel> logger) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationAfService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleBrightFieldEnable(bool isEnable)
    {
        if (isEnable)
        {
            var (_, calChipSiteModelEnum) = GetBrightFieldStatus();

            var isChuckLoadedWaferRet = calibrationEFEMService.IsChuckLoadedWafer();
            if (isChuckLoadedWaferRet.IsSuccess == false) throw new CugaException(isChuckLoadedWaferRet.ErrorMsg);

            if (isChuckLoadedWaferRet.Anything == false && calChipSiteModelEnum == CalChipSiteModelEnum.ChuckModel)
            {
                logger.LogWarning("Chuck hasn't loaded a wafer. so toggle ecs model!");
                isEnable = false;
            }
        }

        var ret = calibrationAfService.ToggleBrightFieldEnable(isEnable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleDarkFieldEnable(bool isEnable)
    {
        var ret = calibrationAfService.ToggleDarkFieldEnable(isEnable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleCalChipSiteModelEnum(CalChipSiteModelEnum calChipSiteModelEnum)
    {
        var ret = calibrationAfService.ToggleCalChipSiteModelEnum(calChipSiteModelEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (bool IsReview, CalChipSiteModelEnum CalChipSiteModelEnum) GetBrightFieldStatus()
    {
        var ret = calibrationAfService.GetBrightFieldStatus();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetNmPerEcs()
    {
        var ret = calibrationAfService.GetNmPerEcs();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetEcsPerOffsetMotorMm()
    {
        var ret = calibrationAfService.GetEcsPerOffsetMotorMm();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetSensorEcsValue()
    {
        var ret = calibrationAfService.GetSensorEcsValue();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetSensorAverageEcsValue()
    {
        var ret = calibrationAfService.GetSensorAverageEcsValue();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorEcsValue(double ecs)
    {
        var ret = calibrationAfService.SetSensorEcsValue(ecs);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (double Min, double Max) GetEcsMoveRange()
    {
        var ret = calibrationAfService.GetEcsMoveRange();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorMicroscopeObjValue(MicroscopeLensInformation microscopeLensInformation)
    {
        var ret = calibrationAfService.SetSensorMicroscopeObjValue(microscopeLensInformation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void GetSensorNscCurveIsOk()
    {
        var ret = calibrationAfService.GetSensorNscCurveIsOk();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (double F, double N) GetSensorFnValue(bool isA)
    {
        var ret = calibrationAfService.GetSensorFnValue(isA);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetSensorCurrentValue(bool isA)
    {
        var ret = calibrationAfService.GetSensorCurrentValue(isA);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorCurrentValue(bool isA, double currentA)
    {
        var ret = calibrationAfService.SetSensorCurrentValue(isA, currentA);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (double Offset, double Gain) GetSensorNscCompensation()
    {
        var ret = calibrationAfService.GetSensorNscCompensation();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void ResetSensorNscCompensation()
    {
        var ret = calibrationAfService.SetSensorNscCompensation(0d, 1d);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorNscCompensation(double offset, double gain)
    {
        var ret = calibrationAfService.SetSensorNscCompensation(offset, gain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (double KA, double OffsetA, double KB, double OffsetB) GetFAFBCompensation()
    {
        var ret = calibrationAfService.GetFAFBCompensation();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void ResetFAFBCompensation()
    {
        var ret = calibrationAfService.SetFAFBCompensation(0d, 0d, 0d, 0d);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetFAFBCompensation(double ka, double offsetA, double kb, double offsetB)
    {
        var ret = calibrationAfService.SetFAFBCompensation(ka, offsetA, kb, offsetB);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public List<double> GetSensorAfErrorTraceBufferList(TimeSpan timeSpan)
    {
        logger.LogInformation("Start TraceBuffer");

        var ret = calibrationAfService.GetSensorAfErrorTraceBufferList(timeSpan);

        logger.LogInformation("End TraceBuffer");

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<double> GetSensorNscTraceBufferList(TimeSpan timeSpan)
    {
        logger.LogInformation("Start TraceBuffer");

        var ret = calibrationAfService.GetSensorNscTraceBufferList(timeSpan);

        logger.LogInformation("End TraceBuffer");

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<(double Ecs, double Nsc, double AFError, double Lvdt, double Fa, double Na, double Fb, double Nb)> GetSensorNscTraceBufferList(double startEcs, double endEcs, double speedEcs, TimeSpan timeSpan)
    {
        logger.LogInformation("Start TraceBuffer");

        var ret = calibrationAfService.GetSensorNscTraceBufferList(startEcs, endEcs, speedEcs, timeSpan);

        logger.LogInformation("End TraceBuffer");

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<(double Trigger, double X, double Ecs)> GetZAndXSyncModeTraceBufferList(TimeSpan timeSpan)
    {
        logger.LogInformation("Start TraceBuffer");

        var ret = calibrationAfService.GetZAndXSyncModeTraceBufferList(timeSpan);

        logger.LogInformation("End TraceBuffer");

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorBrightFieldChuckCenterMachinePositionValue(Point position)
    {
        var ret = calibrationAfService.SetSensorBrightFieldChuckCenterMachinePositionValue(position);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorBrightFieldChuckStandardEcsValue(MicroscopeLensInformation microscopeLensInformation, double standardEcsValue)
    {
        var ret = calibrationAfService.SetSensorBrightFieldChuckStandardEcsValue(microscopeLensInformation, standardEcsValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorBrightFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        var ret = calibrationAfService.SetSensorBrightFieldCalChipCenterMachinePositionValue(calChipSiteModelEnum, position);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorBrightFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        var ret = calibrationAfService.SetSensorBrightFieldCalChipStandardEcsValue(calChipSiteModelEnum, standardEcsValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorDarkFieldChuckCenterMachinePositionValue(Point position)
    {
        var ret = calibrationAfService.SetSensorDarkFieldChuckCenterMachinePositionValue(position);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorDarkFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position)
    {
        var ret = calibrationAfService.SetSensorDarkFieldCalChipCenterMachinePositionValue(calChipSiteModelEnum, position);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorDarkFieldChuckStandardEcsValue(double standardEcsValue)
    {
        var ret = calibrationAfService.SetSensorDarkFieldChuckStandardEcsValue(standardEcsValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorDarkFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue)
    {
        var ret = calibrationAfService.SetSensorDarkFieldCalChipStandardEcsValue(calChipSiteModelEnum, standardEcsValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetDarkFieldAutoFocusMotorAbsoluteValue(double value)
    {
        var ret = calibrationAfService.SetDarkFieldAutoFocusMotorAbsoluteValue(value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetDarkFieldAutoFocusMotorAbsoluteValue()
    {
        var ret = calibrationAfService.GetDarkFieldAutoFocusMotorAbsoluteValue();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public (double min, double max) GetDarkFieldAutoFocusMotorMoveRange()
    {
        var ret = calibrationAfService.GetDarkFieldAutoFocusMotorMoveRange();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetDarkField(CalChipSiteModelEnum calChipSiteModelEnum, double ecs, double offsetMotor)
    {
        ToggleBrightFieldEnable(false);
        ToggleCalChipSiteModelEnum(calChipSiteModelEnum);

        if (calChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel)
            SetSensorDarkFieldChuckStandardEcsValue(ecs);
        else
            SetSensorDarkFieldCalChipStandardEcsValue(calChipSiteModelEnum, ecs);
        SetDarkFieldAutoFocusMotorAbsoluteValue(offsetMotor);
    }

    public double GetSensorNscRelativeZero()
    {
        var ret = calibrationAfService.GetSensorNscRelativeZero();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    #endregion 服务
}