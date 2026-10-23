using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationFourierServiceMockImpl : ICalibrationFourierService
{
    public string? SimulatorImageFilePath { get; set; }

    public SxExecuteRet<bool> Connect() => SxExecuteRetHelper.CreateSuccess(true);

    public SxExecuteRet<bool> Home(int channelId, bool isGoToZero) => SxExecuteRetHelper.CreateSuccess(true);

    public SxExecuteRet<bool> SetRods(int channelId, double[] rodPositions) => SxExecuteRetHelper.CreateSuccess(true);

    public SxExecuteRet<BitmapImage> GetImage(
        ProductivityInformation productivityInformation,
        LaserLightInformation laserLightInformation,
        Point dfPosition,
        double scanLength,
        int channelId,
        bool isCustomWaveform)
    {
        Thread.Sleep(100);

        var filePath = SimulatorImageFilePath ?? string.Empty;

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        return SxExecuteRetHelper.CreateSuccess(File.Exists(filePath)
            ? BitmapHelper.OpenImage(filePath)
            : BitmapImage.Random(Convert.ToInt32(scanLength), Convert.ToInt32(scanLength), 10));

#pragma warning restore IDISP004
#pragma warning restore IDE0079
    }

    public SxExecuteRet<ICalibrationFourierService.FourierMotorConfiguration> GetMotorConfiguration() => SxExecuteRetHelper.CreateSuccess(new ICalibrationFourierService.FourierMotorConfiguration(
        SideRodTotalCount: 46,
        SideMinMotorAbsoluteValue: 0d,
        SideMaxMotorAbsoluteValue: 55d,
        CenterXMinMotorAbsoluteValue: -100d,
        CenterXMaxMotorAbsoluteValue: 100d,
        CenterPushMinMotorAbsoluteValue: 0d,
        CenterPushMaxMotorAbsoluteValue: 100d,
        CenterYMinMotorAbsoluteValue: 0d,
        CenterYMaxMotorAbsoluteValue: 100d));

    public SxExecuteRet<bool> SetChannel3XTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue) => SxExecuteRetHelper.CreateSuccess(true);

    public SxExecuteRet<bool> SetChannel3XPush(double motorAbsoluteValue) => SxExecuteRetHelper.CreateSuccess(true);

    public SxExecuteRet<bool> SetChannel3YTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue) => SxExecuteRetHelper.CreateSuccess(true);
}