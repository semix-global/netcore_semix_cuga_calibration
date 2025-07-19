using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models.Setting;
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
    ILogger<AfViewModel> logger,
    CalibrationSetting calibrationSetting) : ViewModelBase
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

    public void SetSensorMicroscopeObjValue(MicroscopeMagnificationEnum microscopeMagnificationEnum)
    {
        var ret = calibrationAfService.SetSensorMicroscopeObjValue(microscopeMagnificationEnum);

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

    public (double Offset, double Gain) GetSensorNscCompensationCoefficient()
    {
        var ret = calibrationAfService.GetSensorNscCompensationCoefficient();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorNscCompensationCoefficient(double offset, double gain)
    {
        var ret = calibrationAfService.SetSensorNscCompensationCoefficient(offset, gain);

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

    public List<(double Ecs, double Nsc, double Lvdt)> GetNscCompensationCoefficientTraceBufferList(double startEcs, double endEcs, double speedEcs, TimeSpan timeSpan)
    {
        logger.LogInformation("Start TraceBuffer");

        var ret = calibrationAfService.GetNscCompensationCoefficientTraceBufferList(startEcs, endEcs, speedEcs, timeSpan);

        logger.LogInformation("End TraceBuffer");

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorBrightFieldChuckCenterMachinePositionValue(Point position)
    {
        var ret = calibrationAfService.SetSensorBrightFieldChuckCenterMachinePositionValue(position);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorBrightFieldChuckStandardEcsValue(MicroscopeMagnificationEnum microscopeMagnificationEnum, double standardEcsValue)
    {
        var ret = calibrationAfService.SetSensorBrightFieldChuckStandardEcsValue(microscopeMagnificationEnum, standardEcsValue);

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

    public bool SetDarkFieldAutoFocus(SettingDarkFieldAutoFocusParam? settingDarkFieldAutoFocus, OpticsMagTypeEnum opticsMagTypeEnum, CalChipSiteModelEnum calChipSiteModelEnum)
    {
        ToggleCalChipSiteModelEnum(calChipSiteModelEnum);

        var darkAutoFocusParam = settingDarkFieldAutoFocus ?? opticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => calibrationSetting.LowMagSettingDarkFieldAutoFocusParam,
            OpticsMagTypeEnum.Middle => calibrationSetting.MiddleMagSettingDarkFieldAutoFocusParam,
            OpticsMagTypeEnum.High => calibrationSetting.HighMagSettingDarkFieldAutoFocusParam,
            _ => throw new ArgumentOutOfRangeException(nameof(opticsMagTypeEnum), opticsMagTypeEnum, null)
        };

        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.ChuckModel:
                if (darkAutoFocusParam.IsEnableChuck)
                {
                    SetSensorDarkFieldChuckStandardEcsValue(darkAutoFocusParam.ChuckEcsValue);
                    SetDarkFieldAutoFocusMotorAbsoluteValue(darkAutoFocusParam.ChuckMotorValue);
                }
                else
                {
                    ToggleBrightFieldEnable(false);
                    SetSensorEcsValue(darkAutoFocusParam.ChuckEcsValue);
                }

                break;

            case CalChipSiteModelEnum.DswModel:
                if (darkAutoFocusParam.IsEnableDsw)
                {
                    SetSensorDarkFieldCalChipStandardEcsValue(calChipSiteModelEnum, darkAutoFocusParam.DswEcsValue);
                    SetDarkFieldAutoFocusMotorAbsoluteValue(darkAutoFocusParam.DswMotorValue);
                }
                else
                {
                    ToggleBrightFieldEnable(false);
                    SetSensorEcsValue(darkAutoFocusParam.DswEcsValue);
                }

                break;

            case CalChipSiteModelEnum.UndefinedModel:
                if (darkAutoFocusParam.IsEnableUndefined)
                {
                    SetSensorDarkFieldCalChipStandardEcsValue(calChipSiteModelEnum, darkAutoFocusParam.UndefinedEcsValue);
                    SetDarkFieldAutoFocusMotorAbsoluteValue(darkAutoFocusParam.UndefinedMotorValue);
                }
                else
                {
                    ToggleBrightFieldEnable(false);
                    SetSensorEcsValue(darkAutoFocusParam.UndefinedEcsValue);
                }

                break;

            case CalChipSiteModelEnum.HazeModel:
                if (darkAutoFocusParam.IsEnableHaze)
                {
                    SetSensorDarkFieldCalChipStandardEcsValue(calChipSiteModelEnum, darkAutoFocusParam.HazeEcsValue);
                    SetDarkFieldAutoFocusMotorAbsoluteValue(darkAutoFocusParam.HazeMotorValue);
                }
                else
                {
                    ToggleBrightFieldEnable(false);
                    SetSensorEcsValue(darkAutoFocusParam.HazeEcsValue);
                }

                break;

            case CalChipSiteModelEnum.ShinyWaferModel:
                if (darkAutoFocusParam.IsEnableShinyWafer)
                {
                    SetSensorDarkFieldCalChipStandardEcsValue(calChipSiteModelEnum, darkAutoFocusParam.ShinyWaferEcsValue);
                    SetDarkFieldAutoFocusMotorAbsoluteValue(darkAutoFocusParam.ShinyWaferMotorValue);
                }
                else
                {
                    ToggleBrightFieldEnable(false);
                    SetSensorEcsValue(darkAutoFocusParam.ShinyWaferEcsValue);
                }

                break;
        }

        return calChipSiteModelEnum switch
        {
            CalChipSiteModelEnum.ChuckModel => darkAutoFocusParam.IsEnableChuck,
            CalChipSiteModelEnum.DswModel => darkAutoFocusParam.IsEnableDsw,
            CalChipSiteModelEnum.UndefinedModel => darkAutoFocusParam.IsEnableUndefined,
            CalChipSiteModelEnum.HazeModel => darkAutoFocusParam.IsEnableHaze,
            CalChipSiteModelEnum.ShinyWaferModel => darkAutoFocusParam.IsEnableShinyWafer,
            _ => throw new ArgumentOutOfRangeException(nameof(calChipSiteModelEnum), calChipSiteModelEnum, null)
        };
    }

    public (Point[] traceBuffer, double k) NscDiagnosis(double afEcs, CalChipSiteModelEnum calChipSiteModelEnum)
    {
        SetSensorDarkFieldCalChipStandardEcsValue(calChipSiteModelEnum, afEcs);

        var ret = calibrationAfService.NscDiagnosis();
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        return (ret.Anything.tracebuffer, ret.Anything.k);
    }

    #endregion 服务
}