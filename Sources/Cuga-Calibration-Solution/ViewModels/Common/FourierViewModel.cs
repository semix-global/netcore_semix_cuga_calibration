using CommunityToolkit.Diagnostics;
using Core.Models.Exceptions;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Common.Pattern;
using Core.Services.Implements.Mock;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Optics;
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
        int channelId)
    {
        var ret = calibrationFourierService.GetImage(
            productivityInformation,
            laserLightInformation,
            dfPosition,
            scanLength,
            channelId);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public C2MFFRangeModel GetFourierConfig()
    {
        var ret = calibrationFourierService.GetFourierConfig();
        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public bool FF_Move_CH3X(int rpos, double lpos, double ppos)
    {
        var ret = calibrationFourierService.FF_Move_CH3X(rpos, lpos, ppos);
        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public bool FF_Move_CH3Y(int rpos, double lpos)
    {
        var ret = calibrationFourierService.FF_Move_CH3Y(rpos, lpos);
        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public double GetFFRACT(CgFFCHEnum ch)
    {
        var ret = calibrationFourierService.GetFFRACT(ch);
        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetFFLACT(CgFFCHEnum ch)
    {
        var ret = calibrationFourierService.GetFFLACT(ch);
        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetFFPACT(CgFFCHEnum ch)
    {
        var ret = calibrationFourierService.GetFFPACT(ch);
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

    public void UseSimulatorImages(string[] simulatorImageFilePaths)
    {
        var mock = Guard.IsAssignableToTypeAndReturn<CalibrationFourierServiceMockImpl>(calibrationFourierService);

        mock.SimulatorImageFilePaths = simulatorImageFilePaths;
    }
}