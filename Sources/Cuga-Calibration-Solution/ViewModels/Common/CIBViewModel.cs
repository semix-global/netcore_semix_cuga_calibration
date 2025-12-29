using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(CIBViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class CIBViewModel(
    StageViewModel stageViewModel,
    LaserViewModel laserViewModel,
    ICalibrationCIBService calibrationCIBService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationCIBService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<CIBInformation> GetCIBInformations()
    {
        var ret = calibrationCIBService.GetCIBInformations();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var ret = calibrationCIBService.ToggleEnableAGC(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        var ret = calibrationCIBService.ToggleProfileMode(cibInformations, cibProfileModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var ret = calibrationCIBService.ToggleEnableL0K(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        var ret = calibrationCIBService.SetGain(cibInformations, gain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableMarkMode(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var ret = calibrationCIBService.ToggleEnableMarkMode(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetCIBConfiguration(IReadOnlyList<CIBInformation> cibInformations, CIBConfiguration cIbConfiguration)
    {
        ToggleEnableAGC(cibInformations, cIbConfiguration.IsAutoGainControl);

        if (cIbConfiguration.IsAutoGainControl == false) SetGain(cibInformations, cIbConfiguration.Gain);

        ToggleEnableL0K(cibInformations, cIbConfiguration.IsL0K);

        ToggleProfileMode(cibInformations, cIbConfiguration.CIBProfileMode);
    }

    public void SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits, double maxLogGain)
    {
        var ret = calibrationCIBService.SetMMD(cibInformation, logGainMul128U12Bits, gainS16Bits, maxLogGain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors)
    {
        var ret = calibrationCIBService.SetLightMatching(cibInformations, digitalGainPlusMultiplicativeFactors);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetIlluminationProfile(IReadOnlyList<CIBInformation> cibInformations, IReadOnlyList<double> illuminationProfiles)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<CIBDelayDTO> GetDelays(IReadOnlyList<CIBInformation> cibInformations)
    {
        var ret = calibrationCIBService.GetDelays(cibInformations);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetDelays(IReadOnlyList<CIBDelayDTO> delays)
    {
        var ret = calibrationCIBService.SetDelays(delays);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public async Task<IReadOnlyList<DarkFieldImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true,
        bool isMoveStage = true)
    {
        try
        {
            if (isMoveStage)
            {
                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(position, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position, calChipSiteModelEnum);
                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                        break;
                }
            }

            if (customCIBConfiguration.IsCustom == false)
            {
                Guard.IsNotNull(customCIBConfiguration.CIBConfiguration);

                SetCIBConfiguration(cibInformations, customCIBConfiguration.CIBConfiguration);
            }
            else
                Guard.IsNull(customCIBConfiguration.CIBConfiguration);

            if (customPrescanAODWaveform.IsCustom == false)
            {
                Guard.IsNotNull(customPrescanAODWaveform.LaserLightInformation);

                laserViewModel.SetPrescanAODWaveProfileByCoefficient(productivityInformation, customPrescanAODWaveform.LaserLightInformation.Coefficient);
            }
            else
                Guard.IsNull(customPrescanAODWaveform.LaserLightInformation);

            if (isCustomChirpAODWaveform == false)
            {
                laserViewModel.SetChirpAODWaveProfile(productivityInformation);
            }

            var ret = await calibrationCIBService.GetPMTImagesAsync(
                productivityInformation,
                stageCoordinateSystemEnum,
                position,
                cibInformations,
                imageWidth,
                isForward,
                isAutoFocus,
                cancellationToken);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            if (isMoveStage)
            {
                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(position, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position, calChipSiteModelEnum);
                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                        break;
                }
            }
        }
    }

    public async Task<DarkFieldImageDTO> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point position,
        CIBInformation cibInformation,
        int imageWidth,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        var darkFieldImages = await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            calChipSiteModelEnum,
            position,
            [cibInformation],
            imageWidth,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            cancellationToken,
            isForward,
            isAutoFocus);

        return darkFieldImages.Single();
    }

    public async Task<IReadOnlyList<DarkFieldRawScanImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true,
        bool isMoveStage = true)
    {
        try
        {
            if (isMoveStage)
            {
                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(startPosition, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(startPosition, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition, calChipSiteModelEnum);
                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                        break;
                }
            }

            if (customCIBConfiguration.IsCustom == false)
            {
                Guard.IsNotNull(customCIBConfiguration.CIBConfiguration);

                SetCIBConfiguration(cibInformations, customCIBConfiguration.CIBConfiguration);
            }
            else
                Guard.IsNull(customCIBConfiguration.CIBConfiguration);

            if (customPrescanAODWaveform.IsCustom == false)
            {
                Guard.IsNotNull(customPrescanAODWaveform.LaserLightInformation);

                laserViewModel.SetPrescanAODWaveProfileByCoefficient(productivityInformation, customPrescanAODWaveform.LaserLightInformation.Coefficient);
            }
            else
                Guard.IsNull(customPrescanAODWaveform.LaserLightInformation);

            if (isCustomChirpAODWaveform == false)
            {
                laserViewModel.SetChirpAODWaveProfile(productivityInformation);
            }

            var ret = await calibrationCIBService.GetPMTImagesAsync(
                productivityInformation,
                stageCoordinateSystemEnum,
                startPosition,
                endPosition,
                cibInformations,
                isForward,
                isAutoFocus,
                cancellationToken);

            return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        }
        finally
        {
            if (isMoveStage)
            {
                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(startPosition, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(startPosition, calChipSiteModelEnum);
                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition, calChipSiteModelEnum);
                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));
                        break;
                }
            }
        }
    }

    public async Task<DarkFieldRawScanImageDTO> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        CalChipSiteModelEnum calChipSiteModelEnum,
        Point startPosition,
        Point endPosition,
        CIBInformation cibInformation,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true)
    {
        var darkFieldImages = await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            calChipSiteModelEnum,
            startPosition,
            endPosition,
            [cibInformation],
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            cancellationToken,
            isForward,
            isAutoFocus);

        return darkFieldImages.Single();
    }
}