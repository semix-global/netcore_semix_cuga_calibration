using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.AutoFocus;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(CIBViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class CIBViewModel(
    ICalibrationCIBService calibrationCIBService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ILogger<CIBViewModel> logger,
    ICacheProvider cacheProvider,
    MicroscopeViewModel microscopeViewModel,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    OpticsViewModel opticsViewModel,
    LaserViewModel laserViewModel,
    CalibrationSetting calibrationSetting) : ViewModelBase
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

    public void SetAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var ret = calibrationCIBService.SetAGC(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        var ret = calibrationCIBService.SetCIBProfileModeEnum(cibInformations, cibProfileModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var ret = calibrationCIBService.SetL0K(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetMarker(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var ret = calibrationCIBService.SetMarker(cibInformations, enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        var ret = calibrationCIBService.SetGain(cibInformations, gain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetCIBConfiguration(IReadOnlyList<CIBInformation> cibInformations, CIBConfiguration cibConfiguration)
    {
        SetAGC(cibInformations, cibConfiguration.IsAutoGainControl);

        if (cibConfiguration.IsAutoGainControl == false) SetGain(cibInformations, cibConfiguration.Gain);

        SetL0K(cibInformations, cibConfiguration.IsL0K);

        SetCIBProfileModeEnum(cibInformations, cibConfiguration.CIBProfileMode);
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

    public IReadOnlyList<CIBDelayDTO> GetDelays(ProductivityInformation productivityInformation, IReadOnlyList<CIBInformation> cibInformations)
    {
        laserViewModel.ToggleOpticsMagType(productivityInformation);

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

    public void SetXPixelSize(ProductivityInformation productivityInformation, double xPixelSize)
    {
        var ret = calibrationCIBService.SetXPixelSize(productivityInformation, xPixelSize);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    #region 采图

    public void ToggleRTFCParam(ProductivityInformation productivityInformation)
    {
        laserViewModel.ToggleOpticsMagType(productivityInformation);

        var ret = calibrationCIBService.ToggleRTFCParam(productivityInformation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    #region X 采[单位置]短图

    public async Task<IReadOnlyList<DarkFieldImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point centerPosition,
        int imageWidth,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true,
        bool isKeepRawImageCIBProfileModeEnum = false,
        bool isCustomAFParam = false)
        => await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            centerPosition,
            cibInformations,
            customCalChip,
            customOpticsConfiguration,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            async () =>
            {
                if (isCustomAFParam) Guard.IsTrue(isAutoFocus);

                var ret = await calibrationCIBService.GetPMTImagesAsync(
                    productivityInformation,
                    stageCoordinateSystemEnum,
                    centerPosition,
                    imageWidth,
                    cibInformations,
                    isForward,
                    isAutoFocus,
                    isKeepRawImageCIBProfileModeEnum,
                    isCustomAFParam,
                    cancellationToken);

                return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
            }
        );

    public async Task<DarkFieldImageDTO> GetPMTImageAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point centerPosition,
        int imageWidth,
        CIBInformation cibInformation,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true,
        bool isKeepRawImageCIBProfileModeEnum = false,
        bool isCustomAFParam = false)
    {
        var darkFieldImages = await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            centerPosition,
            imageWidth,
            [cibInformation],
            customCalChip,
            customOpticsConfiguration,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            cancellationToken,
            isForward: isForward,
            isAutoFocus: isAutoFocus,
            isKeepRawImageCIBProfileModeEnum: isKeepRawImageCIBProfileModeEnum,
            isCustomAFParam: isCustomAFParam);

        return darkFieldImages.Single();
    }

    #endregion

    #region X 采[多位置]短图

    public async Task<IReadOnlyList<DarkFieldImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        IReadOnlyList<Point> centerPositions,
        int imageWidth,
        CIBInformation cibInformation,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isAutoFocus = true,
        bool isKeepRawImageCIBProfileModeEnum = false,
        bool isCustomAFParam = false)
        => await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            centerPositions[0],
            [cibInformation],
            customCalChip,
            customOpticsConfiguration,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            async () =>
            {
                if (isCustomAFParam) Guard.IsTrue(isAutoFocus);

                var ret = await calibrationCIBService.GetPMTImagesAsync(
                    productivityInformation,
                    stageCoordinateSystemEnum,
                    centerPositions,
                    imageWidth,
                    cibInformation,
                    isAutoFocus,
                    isKeepRawImageCIBProfileModeEnum,
                    isCustomAFParam,
                    cancellationToken);

                return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
            }
        );

    #endregion

    #region X 采[单位置]长图

    public async Task<IReadOnlyList<DarkFieldRawScanImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true,
        bool isKeepRawImageCIBProfileModeEnum = false,
        bool isCustomAFParam = false)
        => await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            startPosition,
            cibInformations,
            customCalChip,
            customOpticsConfiguration,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            async () =>
            {
                if (isCustomAFParam) Guard.IsTrue(isAutoFocus);

                var ret = await calibrationCIBService.GetPMTImagesAsync(
                    productivityInformation,
                    stageCoordinateSystemEnum,
                    startPosition,
                    stopPosition,
                    cibInformations,
                    isForward,
                    isAutoFocus,
                    isKeepRawImageCIBProfileModeEnum,
                    isCustomAFParam,
                    cancellationToken);

                return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
            }
        );

    public async Task<DarkFieldRawScanImageDTO> GetPMTImageAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        CIBInformation cibInformation,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isAutoFocus = true,
        bool isKeepRawImageCIBProfileModeEnum = false,
        bool isCustomAFParam = false)
    {
        var darkFieldImages = await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            startPosition,
            stopPosition,
            [cibInformation],
            customCalChip,
            customOpticsConfiguration,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            cancellationToken,
            isForward: isForward,
            isAutoFocus: isAutoFocus,
            isKeepRawImageCIBProfileModeEnum: isKeepRawImageCIBProfileModeEnum,
            isCustomAFParam: isCustomAFParam);

        return darkFieldImages.Single();
    }

    #endregion

    #region X/Z 同步采[单位置]短图

    public async Task<IReadOnlyList<DarkFieldImageDTO>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        double startECS,
        double stopECS,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isKeepRawImageCIBProfileModeEnum = false)
        => await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            startPosition,
            cibInformations,
            customCalChip,
            customOpticsConfiguration,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            async () =>
            {
                var ret = await calibrationCIBService.GetPMTImagesAsync(
                    productivityInformation,
                    stageCoordinateSystemEnum,
                    startPosition,
                    stopPosition,
                    startECS,
                    stopECS,
                    cibInformations,
                    isForward,
                    isKeepRawImageCIBProfileModeEnum,
                    cancellationToken);

                return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
            }
        );

    public async Task<DarkFieldImageDTO> GetPMTImageAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        double startECS,
        double stopECS,
        CIBInformation cibInformation,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isKeepRawImageCIBProfileModeEnum = false)
    {
        var darkFieldImages = await GetPMTImagesAsync(
            productivityInformation,
            stageCoordinateSystemEnum,
            startPosition,
            stopPosition,
            startECS,
            stopECS,
            [cibInformation],
            customCalChip,
            customOpticsConfiguration,
            customCIBConfiguration,
            customPrescanAODWaveform,
            isCustomChirpAODWaveform,
            cancellationToken,
            isForward: isForward,
            isKeepRawImageCIBProfileModeEnum: isKeepRawImageCIBProfileModeEnum);

        return darkFieldImages.Single();
    }

    public async Task<IReadOnlyList<DarkFieldImageDTO>> GetPMTImagesByOffsetAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        double startECS,
        double stopECS,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        CancellationToken cancellationToken,
        bool isForward = true,
        bool isKeepRawImageCIBProfileModeEnum = false)
    {
        var resulList = new List<DarkFieldImageDTO>();

        foreach (var currentCIBInformations in cibInformations
                     .GroupBy(t => t.PMTId)
                     .Select(gg => gg.ToArray()))
        {
            var currentStartPosition = GetCIBInformationPosition(
                stageCoordinateSystemEnum,
                productivityInformation,
                currentCIBInformations[0],
                startPosition);

            var currentStopPosition = GetCIBInformationPosition(
                stageCoordinateSystemEnum,
                productivityInformation,
                currentCIBInformations[0],
                stopPosition);

            resulList.AddRange(await GetPMTImagesAsync(
                productivityInformation,
                stageCoordinateSystemEnum,
                currentStartPosition,
                currentStopPosition,
                startECS,
                stopECS,
                currentCIBInformations,
                customCalChip,
                customOpticsConfiguration,
                customCIBConfiguration,
                customPrescanAODWaveform,
                isCustomChirpAODWaveform,
                cancellationToken,
                isForward: isForward,
                isKeepRawImageCIBProfileModeEnum: isKeepRawImageCIBProfileModeEnum));
        }

        return resulList;
    }

    #endregion

    private async Task<T> GetPMTImagesAsync<T>(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point centerPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        (bool IsCustom, CalChipSiteModelEnum? CalChipSiteModelEnum) customCalChip,
        (bool IsCustom, OpticsConfiguration? OpticsConfiguration) customOpticsConfiguration,
        (bool IsCustom, CIBConfiguration? CIBConfiguration) customCIBConfiguration,
        (bool IsCustom, LaserLightInformation? LaserLightInformation) customPrescanAODWaveform,
        bool isCustomChirpAODWaveform,
        Func<Task<T>> func)
    {
        try
        {
            if (customCalChip.IsCustom == false)
            {
                Guard.IsNotNull(customCalChip.CalChipSiteModelEnum);

                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(centerPosition, customCalChip.CalChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(centerPosition, customCalChip.CalChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(centerPosition, customCalChip.CalChipSiteModelEnum.Value);

                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));

                        break;
                }
            }
            else
                Guard.IsNull(customCalChip.CalChipSiteModelEnum);

            if (customOpticsConfiguration.IsCustom == false)
            {
                Guard.IsNotNull(customOpticsConfiguration.OpticsConfiguration);

                opticsViewModel.SetOpticsConfiguration(customOpticsConfiguration.OpticsConfiguration);
            }
            else
                Guard.IsNull(customOpticsConfiguration.OpticsConfiguration);

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
                Guard.IsNotNull(customCalChip.CalChipSiteModelEnum);

                switch (stageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(centerPosition, customCalChip.CalChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(centerPosition, customCalChip.CalChipSiteModelEnum.Value);

                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(centerPosition, customCalChip.CalChipSiteModelEnum.Value);

                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum));

                        break;
                }
            }
            else
                Guard.IsNull(customCalChip.CalChipSiteModelEnum);
        }
    }

    #endregion

    #region 坐标转换

    public Vector GetCIBInformationOffset(
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        ProductivityInformation productivityInformation,
        CIBInformation cibInformation,
        MicroscopeLensInformation? microscopeLensInformation = null,
        bool isLineCentricityOffset = true)
    {
        var (xDirection, yDirection) = stageViewModel.GetMachineDirection();

        var cibLineCentricities = cacheProvider.GetOrDefaultArray<CIBLineCentricityDTO>();

        var centerCIBLineCentricity = cibLineCentricities.SingleOrDefault(t => t.ProductivityInformation == productivityInformation && t.PmtId == calibrationSetting.SettingCommonParam.MainCIBInformation.PMTId);
        var currentCIBLineCentricity = cibLineCentricities.SingleOrDefault(t => t.ProductivityInformation == productivityInformation && t.PmtId == cibInformation.PMTId);

        var cartesianCIBLineCentricityOffset = Vector.Zero;

        if (centerCIBLineCentricity is not null && currentCIBLineCentricity is not null)
        {
            var offset = currentCIBLineCentricity.DFMachineCenterPosition - centerCIBLineCentricity.DFMachineCenterPosition;
            cartesianCIBLineCentricityOffset = new Vector(xDirection * offset.X, yDirection * offset.Y)
                                               - (isLineCentricityOffset
                                                   ? new Vector(0, (currentCIBLineCentricity.PmtId - centerCIBLineCentricity.PmtId) * calibrationSetting.SettingCommonParam.PMTInterval)
                                                   : Vector.Zero);
        }

        var cartesianOffset = cartesianCIBLineCentricityOffset
                              - (microscopeLensInformation is not null
                                  ? microscopeViewModel.GetMicroscopeLensInformationOffset(centerCIBLineCentricity?.MicroscopeLensInformation ?? microscopeLensInformation, microscopeLensInformation)
                                  : Vector.Zero);

        if (cibInformation.PMTId == calibrationSetting.SettingCommonParam.MainCIBInformation.PMTId && centerCIBLineCentricity?.MicroscopeLensInformation == microscopeLensInformation)
            Guard.IsTrue(cartesianOffset == Vector.Zero);

        return stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => cartesianOffset,
            StageCoordinateSystemEnum.Machine => new Vector(xDirection * cartesianOffset.X, yDirection * cartesianOffset.Y),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector>(nameof(stageCoordinateSystemEnum))
        };
    }

    public Point GetCIBInformationPosition(
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        ProductivityInformation productivityInformation,
        CIBInformation cibInformation,
        Point position,
        MicroscopeLensInformation? microscopeLensInformation = null)
        => position + GetCIBInformationOffset(
            stageCoordinateSystemEnum,
            productivityInformation,
            cibInformation,
            microscopeLensInformation,
            false);

    #endregion

    public async Task<RuntimeAfCalibrationResultDTO> RuntimeAFCalibrationAsync(
        ProductivityInformation productivityInformation,
        CalChipSiteModelEnum calChipSiteModelEnum,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point centerPosition,
        int imageWidth,
        CIBInformation cibInformation,
        OpticsConfiguration opticsConfiguration,
        CIBConfiguration cibConfiguration,
        LaserLightInformation laserLightInformation,
        string saveResultImageFileDirectory,
        Guid logGuid,
        CancellationToken cancellationToken)
    {
        var centerMachinePosition = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => stageViewModel.DarkFieldToMachinePosition(centerPosition),
            StageCoordinateSystemEnum.Machine => centerPosition,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(stageCoordinateSystemEnum))
        };

        var ret = calibrationCIBService.RuntimeAFCalibration(
            calChipSiteModelEnum,
            productivityInformation,
            cibInformation,
            centerMachinePosition,
            laserLightInformation);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        if (ret.Anything.isAFServo)
            afViewModel.SetDarkField(calChipSiteModelEnum, ret.Anything.ECS, ret.Anything.Motor);
        else
            ThrowHelper.ThrowNotSupportedException("Relay Servo is not supported.");
        //opticsViewModel.SetRelayMotorAbsoluteValue(productivityInformation.OpticsIlluminationModeEnum, ret.Anything.Motor);

        var darkFieldImageDto = await GetPMTImageAsync(
            productivityInformation,
            StageCoordinateSystemEnum.Machine,
            centerMachinePosition,
            imageWidth,
            cibInformation,
            (true, null),
            (false, opticsConfiguration),
            (false, cibConfiguration),
            (false, laserLightInformation),
            false,
            cancellationToken,
            isCustomAFParam: true);

        var quality = calibrationAlgorithmService.GetDarkFieldQuality(darkFieldImageDto.Image);

        var verifyImageFilePath = Path.Combine(saveResultImageFileDirectory, $"Origin_Score{calChipSiteModelEnum}_Quality{quality:0.###}_({logGuid:N}).jpg");
        darkFieldImageDto.Image.SaveImage(verifyImageFilePath);

        var rtfcResult = new RuntimeAfCalibrationResultDTO
        {
            CalChipSiteModelEnum = calChipSiteModelEnum,
            ProductivityInformation = productivityInformation.Clone(),
            ECSValue = ret.Anything.ECS,
            MotorValue = ret.Anything.Motor,
            DarkFieldFilePath = verifyImageFilePath,
            RawImageFilePath = darkFieldImageDto.RawImageFilePath,
            DarkFieldQuality = quality
        };

        logger.LogHtmlInformation("RTFC", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            productivityInformation,
            calChipSiteModelEnum,
            stageCoordinateSystemEnum,
            position = centerPosition,
            cibInformation,
            imageWidth,
            cibConfiguration,
            laserLightInformation,
            saveResultImageFileDirectory,
            rtfcResult.ECSValue,
            rtfcResult.MotorValue,
            rtfcResult.DarkFieldFilePath,
            rtfcResult.RawImageFilePath,
            rtfcResult.DarkFieldQuality,
            HtmlTab = new HtmlTab(new
            {
                RTFCResultImage = new HtmlImage(verifyImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
            })
        }), logGuid.LoggingHtml());

        return ret.IsSuccess ? rtfcResult : throw new CugaException(ret.ErrorMsg);
    }

    public bool TryGetMatchPosition(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point centerPosition,
        CIBInformation cibInformation,
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        DarkFieldImageDTO darkFieldImage,
        string templateFilePath,
        string saveResultImageFileDirectory,
        Guid logGuid,
        out Point resultPosition,
        out double matchScore,
        out double matchAngle,
        out string resultImageFilePath)
    {
        var (xDirection, yDirection) = stageViewModel.GetMachineDirection();

        logger.LogHtmlInformation("Template Match", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            productivityInformation,
            stageCoordinateSystemEnum,
            centerPosition,
            cibInformation,
            algorithmTemplateTypeEnum,
            DarkFieldImage = new HtmlQuote(darkFieldImage.ToHtmlAnonymous()),
            templateFilePath,
            saveResultImageFileDirectory,
            xDirection,
            yDirection
        }), logGuid.LoggingHtml());

        resultPosition = Point.Origin;
        matchScore = 0;
        matchAngle = 0;
        resultImageFilePath = string.Empty;

        var xSize = cacheProvider.GetOrDefaultArray<CIBXPixelSizeDTO>()
            .SingleOrDefault(t => t.ProductivityInformation == productivityInformation);
        if (xSize?.IsOk != true)
        {
            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Laser X Pixel Size is Empty or not verify."), logGuid.LoggingHtml());

            return false;
        }

        var ySize = cacheProvider.GetOrDefaultArray<CIBYPixelSizeDTO>()
            .SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == productivityInformation.OpticsIlluminationModeEnum
                                  && t.ProductivityInformation.OpticsMagType == productivityInformation.OpticsMagType
                                  && t.PmtId == cibInformation.PMTId);
        if (ySize?.IsOk != true)
        {
            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Laser Pixel Size is Empty or not verify."), logGuid.LoggingHtml());

            return false;
        }

        var readTemplateIsSuccess = calibrationAlgorithmService.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out var templateId);
        using var _1 = templateId;

        if (readTemplateIsSuccess == false)
        {
            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Read Template Failed!"), logGuid.LoggingHtml());

            return false;
        }

        bool isSuccess;
        bool cleanTemplateIsSuccess;
        try
        {
            var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);

            using var hImage = darkFieldImage.Image.ToHImage();
            using var horizontalFlipHImage = hImage.HorizontalFlip();

            using var image = darkFieldImage.IsForward ? darkFieldImage.Image : horizontalFlipHImage.ToBitmapImage();
            isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out var matchPoint, out var matchOffset, out matchScore, out matchAngle);

            resultImageFilePath = Path.Combine(isSuccess ? saveResultImageFileDirectory : $"{FileHelper.GetFileFullName(templateFilePath)}_Error", $"Origin_Score({matchScore:0.###},{templateMatchScoreThreshold:0.###})_Angle{matchAngle:0.###}_({logGuid:N}).jpg");
            using var temp = hImage.DrawCrossLine(darkFieldImage.IsForward ? matchPoint : new Point(darkFieldImage.Size.Width - 1 - matchPoint.X, matchPoint.Y));
            temp.Save(resultImageFilePath);

            var stageCoordinateSystemMatchOffset = stageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => (Vector)matchOffset,
                StageCoordinateSystemEnum.Machine => new Vector(xDirection * matchOffset.X, yDirection * matchOffset.Y),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector>(nameof(stageCoordinateSystemEnum))
            };

            var positionOffset = new Vector(stageCoordinateSystemMatchOffset.X * xSize.XPixelSize, stageCoordinateSystemMatchOffset.Y * ySize.YPixelSize);
            resultPosition = centerPosition + positionOffset;

            var htmlBullet = new HtmlBullet(new
            {
                matchPoint,
                matchOffset,
                matchScore,
                matchAngle,
                templateMatchScoreThreshold,
                stageCoordinateSystemMatchOffset,
                positionOffset,
                resultPosition,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(darkFieldImage.IsForward ? matchPoint : new Point(darkFieldImage.Size.Width - 1 - matchPoint.X, matchPoint.Y)), new HtmlImageCrossOverlay(true)]),
                    TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            });

            if (isSuccess) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, htmlBullet, logGuid.LoggingHtml());
            else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, htmlBullet, logGuid.LoggingHtml());
        }
        finally
        {
            cleanTemplateIsSuccess = calibrationAlgorithmService.TryCleanTemplate(algorithmTemplateTypeEnum, templateId);
        }

        if (cleanTemplateIsSuccess == false)
        {
            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Clean Template Failed!"), logGuid.LoggingHtml());

            return false;
        }

        return isSuccess;
    }
}