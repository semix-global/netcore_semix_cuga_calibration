using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;
using C2MFFRangeModel = Core.Models.Models.Common.Fourier.C2MFFRangeModel;
using FFCH = Core.Models.Models.Common.Fourier.FFCH;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationFourierServiceMockImpl : ICalibrationFourierService
{
    private int _simulatorImageIndex;

    public string[] SimulatorImageFilePaths
    {
        get;
        set
        {
            _simulatorImageIndex = 0;

            field = value;
        }
    } = [];

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> Home(int channelId)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetRods(int channelId, double[] rodPositions)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<BitmapImage> GetImage(
        ProductivityInformation productivityInformation,
        LaserLightInformation laserLightInformation,
        Point dfPosition,
        double scanLength,
        int channelId)
    {
        Thread.Sleep(100);

        var filePath = SimulatorImageFilePaths.ElementAtOrDefault(_simulatorImageIndex++ % SimulatorImageFilePaths.Length) ?? string.Empty;

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        return SxExecuteRetHelper.CreateSuccess(File.Exists(filePath)
            ? BitmapHelper.OpenImage(filePath)
            : BitmapImage.Random(Convert.ToInt32(scanLength), Convert.ToInt32(scanLength), 10));

#pragma warning restore IDISP004
#pragma warning restore IDE0079
    }

    public SxExecuteRet<C2MFFRangeModel> GetFourierConfig() => SxExecuteRetHelper.CreateSuccess(new C2MFFRangeModel
    {
        RodNum = 46,
        CH12MinPOS = 0,
        CH12MaxPOS = 55
    });

    public SxExecuteRet<bool> FF_Move_CH3X(int rpos, double lpos, double ppos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> FF_Move_CH3Y(int rpos, double lpos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetFFRACT(CgFFCHEnum ch)
    {
        return SxExecuteRetHelper.CreateSuccess(1.0);
    }

    public SxExecuteRet<double> GetFFLACT(CgFFCHEnum ch)
    {
        return SxExecuteRetHelper.CreateSuccess(1.0);
    }

    public SxExecuteRet<double> GetFFPACT(CgFFCHEnum ch)
    {
        return SxExecuteRetHelper.CreateSuccess(1.0);
    }

    public SxExecuteRet<bool> SetFFRPOS_CH3(FFCH ch, double pos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFLPOS_CH3(FFCH ch, double pos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFPPOS_CH3(FFCH ch, double pos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }
}