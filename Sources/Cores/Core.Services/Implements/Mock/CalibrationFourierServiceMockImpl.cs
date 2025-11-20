using Core.Models.Helper;
using Core.Services.Interfaces;
using Core.Utilities;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationFourierServiceMockImpl : ICalibrationFourierService
{
    private static readonly Random Random = new();

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<HImage> GetFourierImage(int channelId)
    {
        using var bitmapImage = BitmapImageGenerate.GenerateRandomImage(2048, 2044, 10, Random);

        return SxExecuteRetHelper.CreateSuccess(bitmapImage.ToHImage());
    }
}