using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Optics;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Enums.Files;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;
using C2MFFRangeModel = Core.Models.Models.Common.Fourier.C2MFFRangeModel;
using FFCH = Core.Models.Models.Common.Fourier.FFCH;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationFourierServiceMockImpl : ICalibrationFourierService
{
    private const int Width = 2448;
    private const int Height = 2048;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<HImage> GetFourierImage(int channelId)
    {
        using var bitmapImage = BitmapImage.Random(2048, 2044, 10);

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        return SxExecuteRetHelper.CreateSuccess(bitmapImage.ToHImage());

#pragma warning restore IDISP004
#pragma warning restore IDE0079
    }

    public SxExecuteRet<byte[]> GetFFReviewImgForTrigger(int id, ProductivityInformation productivityInformation, double level, Point pos, int width = 800)
    {
        Random Random = new();
        using var bitmapImage = BitmapImage.Random(Width, Height, 10);
        using var memorySteam = new MemoryStream();

        bitmapImage.Save(memorySteam, ImageTypeEnum.Bmp);

        return SxExecuteRetHelper.CreateSuccess(memorySteam.ToArray());
    }

    public SxExecuteRet<C2MFFRangeModel> GetFourierConfig()
    {
        var c2MFFRangeModel = new C2MFFRangeModel();

        return SxExecuteRetHelper.CreateSuccess(c2MFFRangeModel);
    }

    public SxExecuteRet<bool> FF_Move_CH12(FFCH channelId, List<(int rodnumber, double rodpos)> rodpostions)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> FF_Move_CH3X(int rpos, double lpos, double ppos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> FF_Move_CH3Y(int rpos, double lpos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFHome(FFCH ch)
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