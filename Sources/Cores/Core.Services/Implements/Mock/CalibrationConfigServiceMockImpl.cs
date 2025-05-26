using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Constants;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationConfigServiceMockImpl(IOptions<ApplicationSetting> options) : ICalibrationConfigService
{
    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<string> GetCalibrationFilePath()
    {
        var fileCacheDirectoryPath = $"{options.Value.AppHomeDirectory}\\CalibrationResult";
        var filesName = Directory.GetFiles(fileCacheDirectoryPath);

        var filePath = filesName.Length > 0 ? filesName.Last() : $"{fileCacheDirectoryPath}\\Result_{ConstantHelper.LongFileDateTimeFormat}.dat";
        return SxExecuteRetHelper.CreateSuccess(filePath);
    }

    public SxExecuteRet<string> GetPrescanFilePath(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        return SxExecuteRetHelper.CreateSuccess(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Data\\prescan_high$5175$0$600$02$.txt"));
    }

    public SxExecuteRet<string> GetChirpFilePath(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        return SxExecuteRetHelper.CreateSuccess(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Data\\chirp_high$2897$1500$600$03$.txt"));
    }
}