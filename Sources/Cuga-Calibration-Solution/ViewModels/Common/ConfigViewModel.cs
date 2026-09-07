using Core.Models.Exceptions;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Config;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(ConfigViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ConfigViewModel(ICalibrationConfigService calibrationConfigService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationConfigService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public async Task<SysUserDTO> LoginAsync(SysUserDTO user, CancellationToken cancellationToken)
    {
        var ret = await calibrationConfigService.LoginAsync(user, cancellationToken);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public string GetDeviceCode()
    {
        var ret = calibrationConfigService.GetDeviceCode();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public string GetDeviceCUGAVersion()
    {
        var ret = calibrationConfigService.GetDeviceCUGAVersion();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public string GetAppliedCalibrateResultFilePath()
    {
        var ret = calibrationConfigService.GetCalibrationFilePath();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<SysUserDTO> GetRegisteredUsersInformation()
    {
        var ret = calibrationConfigService.GetRegisteredUsersInformation();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<PrescanAODWaveformProfile> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        var ret = calibrationConfigService.GetPrescanAODWaveProfiles(productivityInformation);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<ChirpAODWaveformProfile> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        var ret = calibrationConfigService.GetChirpAODWaveProfiles(productivityInformation);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetPrescanAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        var ret = calibrationConfigService.SetPrescanAODWaveformConfiguration(productivityInformation, filePath);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChirpAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        var ret = calibrationConfigService.SetChirpAODWaveformConfiguration(productivityInformation, filePath);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public HardwareStateConfig GetHardwareConfigs()
    {
        var ret = calibrationConfigService.LoadHardwareConfigs();

        if (ret.IsSuccess == false) throw new Exception(ret.ErrorMsg);

        return ret.Anything;
    }

    /// <summary>
    /// 获取算法配置的原始图像高度、起始Y像素、结束Y像素（B2配置的原始图像可能是裁过的）
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="productivityInformation">产率</param>
    /// <returns>原始图像高度、起始Y像素、结束Y像素</returns>
    public (double OriginalHeight, double StartYPixel, double EndYPixel) GetDefaultImageYPixelHeight(ProductivityInformation productivityInformation)
    {
        var ret = calibrationConfigService.GetDefaultImageYPixelHeight(productivityInformation);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }
}