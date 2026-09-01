using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Config;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Interface.Calibration;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationConfigServiceImpl : BaseService<ICgCalibConfigService>, ICalibrationConfigService
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

    public Task<SxExecuteRet<SysUserDTO>> LoginAsync(SysUserDTO user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<string> GetDeviceCode()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<string> GetDeviceCUGAVersion()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<string> GetCalibrationFilePath()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<SysUserDTO>> GetRegisteredUsersInformation()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetPrescanAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChirpAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<HardwareStateConfig> LoadHardwareConfigs()
    {
        throw new NotImplementedException();
    }
}