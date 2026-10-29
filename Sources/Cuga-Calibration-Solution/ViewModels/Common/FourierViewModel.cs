using CommunityToolkit.Diagnostics;
using Core.Models.Exceptions;
using Core.Models.Models.Common.Pattern;
using Core.Services.Implements.Mock;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(FourierViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class FourierViewModel(
    ICalibrationFourierService calibrationFourierService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationFourierService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public void Home(int channelId, bool isGoToZero = true)
    {
        var ret = calibrationFourierService.Home(channelId, isGoToZero);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetRods(int channelId, double[] rodPositions)
    {
        var ret = calibrationFourierService.SetRods(channelId, rodPositions);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public BitmapImage GetImage(
        ProductivityInformation productivityInformation,
        LaserLightInformation laserLightInformation,
        Point dfPosition,
        double scanLength,
        int channelId,
        bool isCustomWaveform)
    {
        var ret = calibrationFourierService.GetImage(
            productivityInformation,
            laserLightInformation,
            dfPosition,
            scanLength,
            channelId,
            isCustomWaveform);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public ICalibrationFourierService.FourierMotorConfiguration GetMotorConfiguration()
    {
        var ret = calibrationFourierService.GetMotorConfiguration();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetChannel3XTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue)
    {
        var ret = calibrationFourierService.SetChannel3XTurntable(angleMotorAbsoluteValue, motorAbsoluteValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChannel3XPush(double motorAbsoluteValue)
    {
        var ret = calibrationFourierService.SetChannel3XPush(motorAbsoluteValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChannel3XReflectionPush(double motorAbsoluteValue)
    {
        var ret = calibrationFourierService.SetChannel3XReflectionPush(motorAbsoluteValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChannel3XDiffractionPush(double motorAbsoluteValue)
    {
        var ret = calibrationFourierService.SetChannel3XDiffractionPush(motorAbsoluteValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChannel3YTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue)
    {
        var ret = calibrationFourierService.SetChannel3YTurntable(angleMotorAbsoluteValue, motorAbsoluteValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void UseSimulatorImage(string? simulatorImageFilePath)
    {
        var mock = Guard.IsAssignableToTypeAndReturn<CalibrationFourierServiceMockImpl>(calibrationFourierService);

        mock.SimulatorImageFilePath = simulatorImageFilePath;
    }
}