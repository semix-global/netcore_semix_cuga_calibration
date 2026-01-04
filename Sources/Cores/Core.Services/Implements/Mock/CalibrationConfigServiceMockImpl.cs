using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
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

    public SxExecuteRet<string> GetDeviceCode()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess("Demo");
    }

    public SxExecuteRet<string> GetCalibrationFilePath()
    {
        var fileCacheDirectoryPath = $"{options.Value.AppHomeDirectory}\\CalibrationResult";
        DirectoryHelper.CreateDirectoryIfNotExists(fileCacheDirectoryPath);
        var filesName = Directory.GetFiles(fileCacheDirectoryPath);

        var filePath = filesName.Length > 0 ? filesName.Last() : $"{fileCacheDirectoryPath}\\Result_{Constants.LongFileDateTimeFormat}.dat";

        return SxExecuteRetHelper.CreateSuccess(filePath);
    }

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<PrescanAODWaveformProfile>>([
            AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode1, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\prescan_high$5175$0$600$02$0$0$.txt")),
            AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode2, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\prescan_high$5175$0$600$02$0$0$.txt")),
            AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode3, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\prescan_high$5175$0$600$02$0$0$.txt")),
            AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode4, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\prescan_high$5175$0$600$02$0$0$.txt"))
        ]);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ChirpAODWaveformProfile>>([
            AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode1, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\chirp_high$2897$1500$600$03$0$0$.txt")),
            AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode2, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\chirp_high$2897$1500$600$03$0$0$.txt")),
            AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode3, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\chirp_high$2897$1500$600$03$0$0$.txt")),
            AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode4, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\chirp_high$2897$1500$600$03$0$0$.txt"))
        ]);
    }

    public SxExecuteRet<bool> SetPrescanAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator.PrescanAODWaveformFileExtension, "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChirpAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator.ChirpAODWaveformFileExtension, "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}