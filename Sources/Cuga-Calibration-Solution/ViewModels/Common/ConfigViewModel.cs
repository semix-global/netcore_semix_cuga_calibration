using Core.Models.Enums.Optics;
using Core.Models.Exceptions;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
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

    public string GetDeviceCode()
    {
        var ret = calibrationConfigService.GetDeviceCode();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public string GetAppliedCalibrateResultFilePath()
    {
        var ret = calibrationConfigService.GetCalibrationFilePath();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<PrescanAODWaveformProfile> GetPrescanAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, ProductivityInformation productivityInformation)
    {
        var ret = calibrationConfigService.GetPrescanAODWaveProfiles(opticsIlluminationModeEnum, productivityInformation);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<ChirpAODWaveformProfile> GetChirpAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, ProductivityInformation productivityInformation)
    {
        var ret = calibrationConfigService.GetChirpAODWaveProfiles(opticsIlluminationModeEnum, productivityInformation);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetPrescanAODWaveformConfiguration(OpticsIlluminationModeEnum opticsIlluminationModeEnum, ProductivityInformation productivityInformation, string filePath)
    {
        var ret = calibrationConfigService.SetPrescanAODWaveformConfiguration(opticsIlluminationModeEnum, productivityInformation, filePath);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChirpAODWaveformConfiguration(OpticsIlluminationModeEnum opticsIlluminationModeEnum, ProductivityInformation productivityInformation, string filePath)
    {
        var ret = calibrationConfigService.SetChirpAODWaveformConfiguration(opticsIlluminationModeEnum, productivityInformation, filePath);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }
}