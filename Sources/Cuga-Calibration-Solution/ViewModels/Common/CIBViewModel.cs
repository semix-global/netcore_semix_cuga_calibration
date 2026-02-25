using System.IO;
using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Helper;
using Core.Models.Models.Common.AutoFocus;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(CIBViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class CIBViewModel(
    ILogger<CIBViewModel> logger,
    ICacheProvider cacheProvider,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    CalibrationSetting calibrationSetting,
    ICalibrationCIBService calibrationCIBService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationLaserService calibrationLaserService) : ViewModelBase
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
        var ret = calibrationCIBService.SetAGC(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        var ret = calibrationCIBService.SetCIBProfileModeEnum(cibInformations, cibProfileModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var ret = calibrationCIBService.SetL0K(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        var ret = calibrationCIBService.SetGain(cibInformations, gain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetCIBConfiguration(IReadOnlyList<CIBInformation> cibInformations, CIBConfiguration cIbConfiguration)
    {
        ToggleEnableAGC(cibInformations, cIbConfiguration.IsAutoGainControl);

        if (cIbConfiguration.IsAutoGainControl == false) SetGain(cibInformations, cIbConfiguration.Gain);

        ToggleEnableL0K(cibInformations, cIbConfiguration.IsL0K);

        ToggleProfileMode(cibInformations, cIbConfiguration.CIBProfileMode);
    }

    public void SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits)
    {
        var ret = calibrationCIBService.SetMMD(cibInformation, logGainMul128U12Bits, gainS16Bits);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors)
    {
        var ret = calibrationCIBService.SetLightMatching(cibInformations, digitalGainPlusMultiplicativeFactors);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetIlluminationProfile(IReadOnlyList<CIBInformation> cibInformations, IReadOnlyList<double> illuminationProfiles)
    {
        var ret = calibrationCIBService.SetIlluminationProfile(cibInformations, illuminationProfiles);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
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

    public IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>> GetCIBMMDGains(IReadOnlyList<CIBInformation> cibInformations, double startGain, double stepGain, double stopGain)
    {
        var ret = calibrationCIBService.GetCIBMMDGains(cibInformations, startGain, stepGain, stopGain);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public async Task<IReadOnlyList<DarkFieldImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        (bool IsCustom, CalChipSiteModelEnum? calChipSiteModelEnum) customCalChip,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true)
        => await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            position,
            cibInformations,
            customCalChip,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            async () =>
            {
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
        );

    public async Task<DarkFieldImageDTO> GetPMTImageAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        CIBInformation cibInformation,
        int imageWidth,
        (bool IsCustom, CalChipSiteModelEnum? calChipSiteModelEnum) customCalChip,
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
            position,
            [cibInformation],
            imageWidth,
            customCalChip,
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
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? calChipSiteModelEnum) customCalChip,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true)
        => await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            startPosition,
            cibInformations,
            customCalChip,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            async () =>
            {
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
        );

    public async Task<DarkFieldRawScanImageDTO> GetPMTImageAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        CIBInformation cibInformation,
        (bool IsCustom, CalChipSiteModelEnum? calChipSiteModelEnum) customCalChip,
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
            startPosition,
            endPosition,
            [cibInformation],
            customCalChip,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            cancellationToken,
            isForward,
            isAutoFocus);

        return darkFieldImages.Single();
    }

    public async Task<IReadOnlyList<DarkFieldImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? calChipSiteModelEnum) customCalChip,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        double startECS,
        double stopECS,
        CancellationToken cancellationToken,
        bool isForward = true)
        => await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            startPosition,
            cibInformations,
            customCalChip,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            async () =>
            {
                var ret = await calibrationCIBService.GetPMTImagesAsync(
                    productivityInformation,
                    stageCoordinateSystemEnum,
                    startPosition,
                    endPosition,
                    cibInformations,
                    startECS,
                    stopECS,
                    isForward,
                    cancellationToken);

                return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
            }
        );

    public async Task<DarkFieldImageDTO> GetPMTImageAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        CIBInformation cibInformation,
        (bool IsCustom, CalChipSiteModelEnum? calChipSiteModelEnum) customCalChip,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        double startECS,
        double stopECS,
        CancellationToken cancellationToken,
        bool isForward = true)
    {
        var darkFieldImages = await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            startPosition,
            endPosition,
            [cibInformation],
            customCalChip,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            startECS,
            stopECS,
            cancellationToken,
            isForward);

        return darkFieldImages.Single();
    }

    public async Task<RuntimeAfCalibrationResultDTO> RuntimeAFCalibrationAsync(
        ProductivityInformation productivityInformation,
        CalChipSiteModelEnum calChipSiteModelEnum,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        CIBInformation cibInformation,
        int imageWidth,
        CIBConfiguration cibConfiguration,
        LaserLightInformation laserLightInformation,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true,
        bool isDefaultParam = true,
        string? saveImageFileDirectory = null,
        Guid? logGuid = null,
        string? logName = null)
    {
        var bfPosition = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright => position,
            StageCoordinateSystemEnum.Dark => position,
            StageCoordinateSystemEnum.Machine => stageViewModel.MachineToBrightFieldPosition(position),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(stageCoordinateSystemEnum))
        };

        var ret = calibrationCIBService.RuntimeAFCalibration(
            calChipSiteModelEnum,
            productivityInformation,
            cibInformation,
            isDefaultParam && calChipSiteModelEnum is not CalChipSiteModelEnum.ChuckModel ? null : bfPosition,
            isDefaultParam ? null : laserLightInformation);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        if (ret.Anything.isAFServo)
            afViewModel.SetDarkField(calChipSiteModelEnum, ret.Anything.ECS, ret.Anything.Motor);
        else
            ThrowHelper.ThrowNotSupportedException("Relay Servo is not supported.");
        //opticsViewModel.SetRelayMotorAbsoluteValue(productivityInformation.OpticsIlluminationModeEnum, ret.Anything.Motor);

        var verifyPosition = bfPosition + new Vector(0, (cibInformation.PMTId - CalibrationConstantsHelper.MainPmtId) * calibrationSetting.SettingCommonParam.PMTInterval);

        var darkFieldImageDto = await GetPMTImageAsync(
            productivityInformation,
            StageCoordinateSystemEnum.Bright,
            verifyPosition,
            cibInformation,
            imageWidth,
            (false, calChipSiteModelEnum),
            (false, cibConfiguration),
            (false, laserLightInformation),
            false,
            cancellationToken,
            isForward,
            isAutoFocus);
        var quality = calibrationAlgorithmService.GetDarkFieldQuality(darkFieldImageDto.Image);

        var verifyImageFilePath = string.Empty;
        if (saveImageFileDirectory is not null)
        {
            verifyImageFilePath = Path.Combine(saveImageFileDirectory, "RTFCThumb", $"{calChipSiteModelEnum}Guid{logGuid}.jpg");
            darkFieldImageDto.Image.Save(verifyImageFilePath);

            if (logGuid is not null && logName is not null)
                logger.LogHtmlInformation($"{logName} RTFC", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    productivityInformation,
                    calChipSiteModelEnum,
                    stageCoordinateSystemEnum,
                    position,
                    cibInformation,
                    imageWidth,
                    cibConfiguration,
                    laserLightInformation,
                    cancellationToken,
                    isForward,
                    isAutoFocus,
                    isDefaultParam,
                    ret.Anything.ECS,
                    ret.Anything.Motor,
                    ret.Anything.isAFServo,
                    darkFieldImageDto.RawImageFilePath,
                    HtmlTab = new HtmlTab(new
                    {
                        RTFCResultImage = new HtmlImage(verifyImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                    })
                }), logGuid.Value.LoggingHtml());
        }

        var rtfcResult = new RuntimeAfCalibrationResultDTO
        {
            ECSValue = ret.Anything.ECS,
            MotorValue = ret.Anything.Motor,
            DarkFieldFilePath = verifyImageFilePath,
            RawImageFilePath = darkFieldImageDto.RawImageFilePath,
            DarkFieldQuality = quality
        };

        return ret.IsSuccess ? rtfcResult : throw new CugaException(ret.ErrorMsg);
    }

    private async Task<T> GetPMTImagesAsync<T>(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? calChipSiteModelEnum) customCalChip,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        Func<Task<T>> func)
    {
        try
        {
            if (customCalChip.IsCustom == false)
            {
                Guard.IsNotNull(customCalChip.calChipSiteModelEnum);

                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, customCalChip.calChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(position, customCalChip.calChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position, customCalChip.calChipSiteModelEnum.Value);

                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));

                        break;
                }
            }
            else
                Guard.IsNull(customCalChip.calChipSiteModelEnum);

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

            return await func();
        }
        finally
        {
            if (customCalChip.IsCustom == false)
            {
                Guard.IsNotNull(customCalChip.calChipSiteModelEnum);

                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, customCalChip.calChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(position, customCalChip.calChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(position, customCalChip.calChipSiteModelEnum.Value);

                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));

                        break;
                }
            }
            else
                Guard.IsNull(customCalChip.calChipSiteModelEnum);
        }
    }
}