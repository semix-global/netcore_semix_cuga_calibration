using CommunityToolkit.Diagnostics;
using Core.Models.Exceptions;
using Core.Models.Models.Common.Fourier;
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

    public void Home(int channelId)
    {
        var ret = calibrationFourierService.Home(channelId);

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

    public C2MFFRangeModel GetFourierConfig()
    {
        var ret = calibrationFourierService.GetFourierConfig();
        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public bool SetFFRPOS_CH3(FFCH ch, double pos)
    {
        var ret = calibrationFourierService.SetFFRPOS_CH3(ch, pos);
        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public bool SetFFLPOS_CH3(FFCH ch, double pos)
    {
        var ret = calibrationFourierService.SetFFLPOS_CH3(ch, pos);
        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public bool SetFFPPOS_CH3(FFCH ch, double pos)
    {
        var ret = calibrationFourierService.SetFFPPOS_CH3(ch, pos);
        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public void UseSimulatorImage(string? simulatorImageFilePath)
    {
        var mock = Guard.IsAssignableToTypeAndReturn<CalibrationFourierServiceMockImpl>(calibrationFourierService);

        mock.SimulatorImageFilePath = simulatorImageFilePath;
    }
}