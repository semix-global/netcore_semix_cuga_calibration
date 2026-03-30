using Core.Models.Enums.Optics;
using Core.Models.Exceptions;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(OpticsViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class OpticsViewModel(
    ICalibrationOpticsService calibrationOpticsService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationOpticsService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<ProductivityInformation> GetProductivityInformations()
    {
        var ret = calibrationOpticsService.GetProductivityInformations();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var ret = calibrationOpticsService.GetDOEMotorAbsoluteValue(opticsIlluminationModeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var ret = calibrationOpticsService.SetDOEMotorAbsoluteValue(opticsIlluminationModeEnum, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var ret = calibrationOpticsService.GetRelayMotorAbsoluteValue(opticsIlluminationModeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var ret = calibrationOpticsService.SetRelayMotorAbsoluteValue(opticsIlluminationModeEnum, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var ret = calibrationOpticsService.GetINCMotorAbsoluteValue(opticsIlluminationModeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var ret = calibrationOpticsService.SetINCMotorAbsoluteValue(opticsIlluminationModeEnum, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (double L1, double L3) GetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var ret = calibrationOpticsService.GetSCMotorAbsoluteValue(opticsIlluminationModeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, (double L1, double L3) value)
    {
        var ret = calibrationOpticsService.SetSCMotorAbsoluteValue(opticsIlluminationModeEnum, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetCollectorPolarizationMotorAbsoluteValue(int channelId)
    {
        var ret = calibrationOpticsService.GetCollectorPolarizationMotorAbsoluteValue(channelId);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetCollectorPolarizationMotorAbsoluteValue(int channelId, double value)
    {
        var ret = calibrationOpticsService.SetCollectorPolarizationMotorAbsoluteValue(channelId, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleODFilter(bool isEnable)
    {
        var ret = calibrationOpticsService.ToggleODFilter(isEnable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public OpticsApodizationModeEnum GetApodizationMode()
    {
        var ret = calibrationOpticsService.GetApodizationMode();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetApodizationMode(OpticsApodizationModeEnum opticsApodizationModeEnum)
    {
        var ret = calibrationOpticsService.SetApodizationMode(opticsApodizationModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public OpticsPolarizationModeEnum GetPolarizationMode()
    {
        var ret = calibrationOpticsService.GetPolarizationMode();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetPolarizationMode(OpticsPolarizationModeEnum opticsPolarizationModeEnum)
    {
        var ret = calibrationOpticsService.SetPolarizationMode(opticsPolarizationModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public OpticsCollectorPolarizationModeEnum GetCollectorPolarizationMode()
    {
        var ret = calibrationOpticsService.GetCollectorPolarizationMode();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetCollectorPolarizationMode(OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        var ret = calibrationOpticsService.SetCollectorPolarizationMode(opticsCollectorPolarizationModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public OpticsCollectorPolarizationModeEnum GetCollectorPolarizationMode(int channelId)
    {
        var ret = calibrationOpticsService.GetCollectorPolarizationMode(channelId);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetCollectorPolarizationMode(int channelId, OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        var ret = calibrationOpticsService.SetCollectorPolarizationMode(channelId, opticsCollectorPolarizationModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetCIBConfiguration(OpticsConfiguration opticsConfiguration)
    {
        SetApodizationMode(opticsConfiguration.OpticsApodizationModeEnum);
        SetPolarizationMode(opticsConfiguration.OpticsPolarizationModeEnum);
        SetCollectorPolarizationMode(opticsConfiguration.OpticsCollectorPolarizationModeEnum);
    }
}