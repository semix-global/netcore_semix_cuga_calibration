using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationConfigServiceImpl : BaseService<ICgCalibrationService>, ICalibrationConfigService
{
    private string[]? _prescanChirpFilePaths;

    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<string> GetCalibrationFilePath()
    {
        var sxExecuteRet = Invoke(() => Service!.GetCalibrationFilePath());
        return SxExecuteRetHelper.CreateSuccess($"{sxExecuteRet.Anything}.dat");
    }

    public SxExecuteRet<string> GetPrescanFilePath(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = GetPrescanChirpFilePaths();

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, string.Empty)
            : SxExecuteRetHelper.CreateSuccess(opticsMagTypeEnum switch
            {
                OpticsMagTypeEnum.Low => sxExecuteRet.Anything.Single(t => t.Contains("prescan_low")),
                OpticsMagTypeEnum.Middle => sxExecuteRet.Anything.Single(t => t.Contains("prescan_mid")),
                OpticsMagTypeEnum.High => sxExecuteRet.Anything.Single(t => t.Contains("prescan_high")),
                _ => throw new ArgumentOutOfRangeException(nameof(opticsMagTypeEnum), opticsMagTypeEnum, null)
            });
    }

    public SxExecuteRet<string> GetChirpFilePath(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = GetPrescanChirpFilePaths();

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, string.Empty)
            : SxExecuteRetHelper.CreateSuccess(opticsMagTypeEnum switch
            {
                OpticsMagTypeEnum.Low => sxExecuteRet.Anything.Single(t => t.Contains("chirp_low")),
                OpticsMagTypeEnum.Middle => sxExecuteRet.Anything.Single(t => t.Contains("chirp_mid")),
                OpticsMagTypeEnum.High => sxExecuteRet.Anything.Single(t => t.Contains("chirp_high")),
                _ => throw new ArgumentOutOfRangeException(nameof(opticsMagTypeEnum), opticsMagTypeEnum, null)
            });
    }

    private SxExecuteRet<string[]> GetPrescanChirpFilePaths()
    {
        if (_prescanChirpFilePaths is not null) return SxExecuteRetHelper.CreateSuccess(_prescanChirpFilePaths);

        var sxExecuteRet = Invoke(() => Service!.GetAWGFilePath());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<string[]>(sxExecuteRet.ErrorMsg, []);

        _prescanChirpFilePaths = Directory.GetFiles(sxExecuteRet.Anything);

        return SxExecuteRetHelper.CreateSuccess(_prescanChirpFilePaths);
    }
}