using Core.Models.Enums.Optics;
using Core.Models.Exceptions;
using Core.Models.Models.Common.AODWaveform;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(ConfigViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ConfigViewModel(
    ICalibrationConfigService calibrationConfigService
) : ViewModelBase
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

    public IReadOnlyList<PrescanAODWaveformProfile> GetPrescanAODWaveProfiles(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var ret = calibrationConfigService.GetPrescanAODWaveProfiles(opticsMagTypeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<ChirpAODWaveformProfile> GetChirpAODWaveProfiles(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var ret = calibrationConfigService.GetChirpAODWaveProfiles(opticsMagTypeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }
}