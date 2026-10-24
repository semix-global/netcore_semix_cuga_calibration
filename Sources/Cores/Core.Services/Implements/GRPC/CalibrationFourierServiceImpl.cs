using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Interface.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationFourierServiceImpl : BaseService<ICgDiagFourierOpticsService>, ICalibrationFourierService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService();
            IsConnected = createService.IsSuccess;

            return createService;
        });
    }

    public SxExecuteRet<bool> Home(int channelId, bool isGoToZero = true)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetRods(int channelId, double[] rodPositions)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<BitmapImage> GetImage(ProductivityInformation productivityInformation, LaserLightInformation laserLightInformation, Point dfPosition, double scanLength, int channelId, bool isCustomWaveform)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<ICalibrationFourierService.FourierMotorConfiguration> GetMotorConfiguration()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChannel3XTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChannel3XPush(double motorAbsoluteValue)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChannel3XReflectionPush(double motorAbsoluteValue)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChannel3XDiffractionPush(double motorAbsoluteValue)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChannel3YTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue)
    {
        throw new NotImplementedException();
    }
}