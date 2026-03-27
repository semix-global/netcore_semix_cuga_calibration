using Core.Models.Helper;
using Core.Services.Interfaces;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationFourierServiceMockImpl : ICalibrationFourierService
{
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
}