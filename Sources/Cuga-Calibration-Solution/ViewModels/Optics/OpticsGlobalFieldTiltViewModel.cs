using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.HardwareType;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.GlobalFieldTilt;
using Core.Models.Models.Optics.Relay;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Humanizer;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(OpticsGlobalFieldTiltViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsGlobalFieldTiltViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override string CalibrateFileName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override IReadOnlyList<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Image Param" },
        new() { StepName = "P5" },
        new() { StepName = "Global Field Tilt" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private GlobalFieldTiltDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<GlobalFieldTiltDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<GlobalFieldTiltDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private GlobalFieldTiltCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private GlobalFieldTiltDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private MicroscopeCalChipCache _microscopeCalChipCache = new();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 校准控制业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        Guard.IsNotNull(ApplicationCookie.HardwareStateConfig);

        if (ApplicationCookie.OpticsIlluminationModeEnums.Contains(OpticsIlluminationModeEnum.OI) &&
            ApplicationCookie.HardwareStateConfig.MotorHardwares[HardwareMotorTypeEnum.OIDOE].Enabled == false)
        {
            DialogWindowProvider.ShowDialog("Please enable the OI DOE motor!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (ApplicationCookie.OpticsIlluminationModeEnums.Contains(OpticsIlluminationModeEnum.NI) &&
            ApplicationCookie.HardwareStateConfig.MotorHardwares[HardwareMotorTypeEnum.NIDOE].Enabled == false)
        {
            DialogWindowProvider.ShowDialog("Please enable the NI DOE motor!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        MicroscopeCalChipCache = ApplicationCookieService.GetCache<MicroscopeCalChipCache>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<GlobalFieldTiltCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<GlobalFieldTiltDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.OriginDOEPos = OpticsViewModel.GetDOEMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .OrderBy(t => t.OpticsIlluminationModeEnum)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;
            case 1 or 2:
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.CalChipSiteModelEnum switch
                    {
                        CalChipSiteModelEnum.ChuckModel => StageViewModel.BrightFieldToMachinePosition(Cache.Item.FindPosition),
                        CalChipSiteModelEnum.DswModel => MicroscopeCalChip.DSWBrightFieldMachineAffinePosition,
                        _ => ThrowHelper.ThrowNotSupportedException<Point>("Current CalChip Mode Is Not Supported!")
                    }), Cache.CalChipSiteModelEnum);
                return true;

            case 3:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return true;
        }
    }

    #endregion 校准控制业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.CalChipSiteModelEnum
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.OpticsIlluminationModeEnums.Contains(Cache.OpticsIlluminationModeEnum);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.Item.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.PMTIds
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation)
                   && ApplicationCookie.GetProductivityInformations(Cache.OpticsIlluminationModeEnum).Contains(Cache.Item.ProductivityInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.Item.ProductivityInformation;

            DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            try
            {
                Cache.UmPerEcs = AfViewModel.GetNmPerEcs() / 1000;

                Cache.Item.FindPosition = new Point(
                    (Cache.Item.ImageCollectionConfiguration.StartPoint.X + Cache.Item.ImageCollectionConfiguration.EndPoint.X) / 2,
                    Cache.Item.ImageCollectionConfiguration.StartPoint.Y
                );
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.FindPosition);

                var zLimitMin = Cache.Item.CenterECS - Cache.Item.RangeECS;
                var zLimitMax = Cache.Item.CenterECS + Cache.Item.RangeECS;

                Cache.Item.ImageCollectionConfiguration.ZStartEcs = zLimitMin;
                Cache.Item.ImageCollectionConfiguration.ZEndEcs = zLimitMax;
                Cache.Item.ImageCollectionConfiguration.XSpeedValue = Cache.Item.ProductivityInformation.XSpeedValue;
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OriginDOEPos,
                    Cache.Item.ObliqueAngle,
                    Cache.UmPerEcs,
                    Cache.PmtInterval,
                    Cache.Item.RetryCount,
                    ImageFileDirectory = detectImageDirectory,
                    Cache.Item.IsMultiPMTOnceCollection,
                    Cache.Item.AlgorithmImageQualityTypeEnum,
                    Cache.Item.FindPosition,
                    Cache.Item.CenterECS,
                    Cache.Item.RangeECS,
                    Cache.Item.RangeRefinedECS,
                    Cache.Item.StepRefinedECS,
                    StartMachinePosition = StageViewModel.DarkFieldToMachinePosition(Cache.Item.ImageCollectionConfiguration.ExtensionStartPoint),
                    EndMachinePosition = StageViewModel.DarkFieldToMachinePosition(Cache.Item.ImageCollectionConfiguration.ExtensionEndPoint),
                    afEcsLimitMin = zLimitMin,
                    afEcsLimitMax = zLimitMax
                }), HtmlLogUniqueId.LoggingHtml());

                CalibratingItem = new GlobalFieldTiltDTO
                {
                    OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum
                };

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);

                var result = false;
                var retryCount = Cache.Item.ProductivityInformation.OpticsIlluminationModeEnum is OpticsIlluminationModeEnum.NI ? 1 : Cache.Item.RetryCount;

                for (var times = 0; times < retryCount; times++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"Multiple PMT Best Focus:Times {times}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    var globalFieldTiltDTOItem = new GlobalFieldTiltDTOItem
                    {
                        AppliedDOEPos = OpticsViewModel.GetDOEMotorAbsoluteValue(Cache.Item.ProductivityInformation.OpticsIlluminationModeEnum)
                    };
                    CalibratingItem.Items = [.. CalibratingItem.Items, globalFieldTiltDTOItem];

                    if (await GetGlobalFieldTiltResultAsync(globalFieldTiltDTOItem, cancellationToken) == false)
                        return false;

                    result = globalFieldTiltDTOItem.GlobalFieldTiltError < Cache.Item.Threshold;

                    if (Cache.Item.ProductivityInformation.OpticsIlluminationModeEnum is OpticsIlluminationModeEnum.OI)
                    {
                        if (result || Math.Abs(globalFieldTiltDTOItem.ReviseDOEPos) < 0.001) break;

                        var appliedDOEPos = globalFieldTiltDTOItem.AppliedDOEPos - Math.Round(globalFieldTiltDTOItem.ReviseDOEPos, 3);

                        OpticsViewModel.SetDOEMotorAbsoluteValue(Cache.Item.ProductivityInformation.OpticsIlluminationModeEnum, appliedDOEPos);
                    }
                }

                var resultDTOItem = CalibratingItem.Items.OrderBy(t => t.GlobalFieldTiltError).First();

                CalibratingItem.ResultItem = resultDTOItem.Clone();
                CalibratingItem.IsCalibrated = result;

                var htmlBullet = new HtmlBullet(new
                {
                    Result = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                    Details = new HtmlQuote(resultDTOItem.ToFlatnessHtmlAnonymous())
                });

                if (CalibratingItem.IsCalibrated)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;
            }
            catch (Exception e)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Calibration Failed! {e.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                OpticsViewModel.SetDOEMotorAbsoluteValue(Cache.Item.ProductivityInformation.OpticsIlluminationModeEnum, Cache.OriginDOEPos);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        var detectImageDirectory = ImageFileDirectory;
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.IsDarkFieldAlignment;
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.Item.ProductivityInformation;
            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.OpticsIlluminationModeEnum))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.OpticsIlluminationModeEnum.Humanize();
                Cache.OpticsIlluminationModeEnum = selectedReviewItem.OpticsIlluminationModeEnum;

                Guard.IsNotNull(selectedReviewItem.ResultItem, "Selected Review Item Result Is Null!");

                if (Cache.OpticsIlluminationModeEnum is not OpticsIlluminationModeEnum.OI)
                {
                    selectedReviewItem.IsVerified = true;
                    continue;
                }

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindPosition), Cache.CalChipSiteModelEnum);

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.OpticsIlluminationModeEnum,
                    Cache.Item.ProductivityInformation,
                    Cache.VerifyQualityThreshold
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                OpticsViewModel.SetDOEMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, selectedReviewItem.ResultItem.AppliedDOEPos);

                await CIBViewModel.RuntimeAFCalibrationAsync(
                    Cache.Item.ProductivityInformation,
                    Cache.CalChipSiteModelEnum,
                    StageCoordinateSystemEnum.Machine,
                    StageViewModel.DarkFieldToMachinePosition(Cache.Item.FindPosition),
                    800,
                    Cache.Item.CIBInformation,
                    Cache.Item.OpticsConfiguration,
                    Cache.Item.CIBConfiguration,
                    Cache.Item.LaserLightInformation,
                    detectImageDirectory,
                    HtmlLogUniqueId,
                    cancellationToken);

                selectedReviewItem.ResultItem.BestFocusChannelItems = [];
                foreach (var pmtId in Cache.PMTIds.OrderBy(t => t))
                {
                    var verifyPosition = new Point
                        (Cache.Item.FindPosition.X, Cache.Item.FindPosition.Y + (pmtId - CalibrationConstantsHelper.MainPmtId) * CalibrationSetting.SettingCommonParam.PMTInterval);
                    var darkFieldImageDto = await CIBViewModel.GetPMTImageAsync(
                        Cache.Item.ProductivityInformation,
                        StageCoordinateSystemEnum.Bright,
                        verifyPosition,
                        1000,
                        ApplicationCookie.CIBInformations.Single(t => t.PMTId == pmtId && t.ChannelId == Cache.Item.CIBInformation.ChannelId),
                        (false, Cache.CalChipSiteModelEnum),
                        (false, Cache.Item.OpticsConfiguration),
                        (false, Cache.Item.CIBConfiguration),
                        (false, Cache.Item.LaserLightInformation),
                        false,
                        cancellationToken);
                    var quality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImageDto.Image);

                    var imageFilePath = $@"{ImageFileDirectory}\Verify\{selectedReviewItem.OpticsIlluminationModeEnum}_PMT{pmtId}_Guid{HtmlLogUniqueId.LoggingHtml()}.jpg";
                    darkFieldImageDto.Image.SaveImage(imageFilePath);

                    var bestFocusItem = new GlobalFieldTiltDTOItem.Item
                    {
                        PmtId = pmtId,
                        ChannelId = Cache.Item.CIBInformation.ChannelId,
                        XBestFocusQuality = quality,
                        RawFilePath = darkFieldImageDto.RawImageFilePath,
                        FilePath = imageFilePath
                    };
                    selectedReviewItem.ResultItem.BestFocusChannelItems = [.. selectedReviewItem.ResultItem.BestFocusChannelItems, bestFocusItem];

                    Logger.LogHtmlInformation($"PMT {pmtId} Verify", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        verifyPosition,
                        quality,
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(imageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());

                    var qualitys = selectedReviewItem.ResultItem.BestFocusChannelItems.Select(t => t.XBestFocusQuality).ToArray();
                    var qualityError = qualitys.Max() - qualitys.Min();
                    selectedReviewItem.IsVerified = Math.Abs(qualityError) < Cache.VerifyQualityThreshold;

                    var htmlBullet = new HtmlBullet(new
                    {
                        selectedReviewItem.ResultItem.AppliedDOEPos,
                        qualityError,
                        Qualitys = new HtmlPlot2DLinesChart([
                            ("Qualitys", [..qualitys.Select((t, i) => new Point(i, t))])
                        ], "unit: score")
                    });

                    if (selectedReviewItem.IsOk)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                    {
                        errorMessageStringBuilder.AppendLine($"{title}: Error");
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    }
                }
            }

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            var result = SelectedReviewItems.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"Verify : {(result ? "OK" : "Failed")}",
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> GetGlobalFieldTiltResultAsync(GlobalFieldTiltDTOItem globalFieldTiltDTOItem, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(ImageFileDirectory, $"{Cache.Item.ProductivityInformation}");
        try
        {
            OpticsViewModel.SetDOEMotorAbsoluteValue(Cache.Item.ProductivityInformation.OpticsIlluminationModeEnum, globalFieldTiltDTOItem.AppliedDOEPos);

            if (Cache.Item.AlgorithmImageQualityTypeEnum is AlgorithmImageQualityTypeEnum.Laplace)
                await TraversalECSFunctionAsync();
            else
                await BestFocusFunctionAsync();

            var xVector = Vector<double>.Build.DenseOfEnumerable([.. globalFieldTiltDTOItem.BestFocusChannelItems.Select(t => (t.PmtId - globalFieldTiltDTOItem.BestFocusChannelItems[0].PmtId) * Cache.PmtInterval)]);
            var yVector = Vector<double>.Build.DenseOfEnumerable([.. globalFieldTiltDTOItem.BestFocusChannelItems.Select(t => (t.XBestFocusEcs - globalFieldTiltDTOItem.BestFocusChannelItems[0].XBestFocusEcs) * Cache.UmPerEcs)]);
            var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(xVector, yVector);


            var doeReviseAngle = Cache.Item.ProductivityInformation.OpticsIlluminationModeEnum is OpticsIlluminationModeEnum.OI
                ? Math.Atan(slope / Math.Sin(Cache.Item.ObliqueAngle * Math.PI / 180)) * 180 / Math.PI
                : slope;

            globalFieldTiltDTOItem.ReviseDOEPos = doeReviseAngle;
            globalFieldTiltDTOItem.Slope = slope;
            globalFieldTiltDTOItem.Intercept = intercept;
            globalFieldTiltDTOItem.RSquared = rSquared;
            globalFieldTiltDTOItem.OriginPoints = [.. globalFieldTiltDTOItem.BestFocusChannelItems.Select(t => new Point((t.PmtId - globalFieldTiltDTOItem.BestFocusChannelItems[0].PmtId) * Cache.PmtInterval, t.XBestFocusEcs))];
            globalFieldTiltDTOItem.FitPoints = [.. globalFieldTiltDTOItem.OriginPoints.Select((t, i) => new Point(t.X, yPredicted[i]))];
            globalFieldTiltDTOItem.GlobalFieldTiltError = Math.Abs((globalFieldTiltDTOItem.BestFocusChannelItems.Last().XBestFocusEcs - globalFieldTiltDTOItem.BestFocusChannelItems.First().XBestFocusEcs) * Cache.UmPerEcs);

            Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Result = new HtmlQuote(globalFieldTiltDTOItem.ToFlatnessHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        }
        catch (Exception e)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"Get Global Field Tilt Result Error: {e.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        async Task BestFocusFunctionAsync()
        {
            var darkFieldImageDtoList = Cache.Item.IsMultiPMTOnceCollection
                ? await CIBViewModel.GetPMTImagesAsync(
                    Cache.Item.ProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    Cache.Item.ImageCollectionConfiguration.ExtensionStartPoint,
                    Cache.Item.ImageCollectionConfiguration.ExtensionEndPoint,
                    Cache.Item.ImageCollectionConfiguration.ExtensionStartEcs,
                    Cache.Item.ImageCollectionConfiguration.ExtensionEndEcs,
                    [.. Cache.PMTIds.OrderBy(t => t).Select(t => ApplicationCookie.CIBInformations.Single(tt => tt.PMTId == t && tt.ChannelId == Cache.Item.CIBInformation.ChannelId))],
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.Item.OpticsConfiguration),
                    (false, Cache.Item.CIBConfiguration),
                    (false, Cache.Item.LaserLightInformation),
                    false,
                    cancellationToken)
                : await CIBViewModel.GetPMTImagesByOffsetAsync(
                    Cache.Item.ProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    Cache.Item.ImageCollectionConfiguration.ExtensionStartPoint,
                    Cache.Item.ImageCollectionConfiguration.ExtensionEndPoint,
                    Cache.Item.ImageCollectionConfiguration.ExtensionStartEcs,
                    Cache.Item.ImageCollectionConfiguration.ExtensionEndEcs,
                    [.. Cache.PMTIds.OrderBy(t => t).Select(t => ApplicationCookie.CIBInformations.Single(tt => tt.PMTId == t && tt.ChannelId == Cache.Item.CIBInformation.ChannelId))],
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.Item.OpticsConfiguration),
                    (false, Cache.Item.CIBConfiguration),
                    (false, Cache.Item.LaserLightInformation),
                    false,
                    cancellationToken);

            globalFieldTiltDTOItem.BestFocusChannelItems =
            [
                ..darkFieldImageDtoList.Where(t => t.CIBInformation.ChannelId == Cache.Item.CIBInformation.ChannelId).Select(t => new GlobalFieldTiltDTOItem.Item
                {
                    PmtId = t.CIBInformation.PMTId,
                    ChannelId = t.CIBInformation.ChannelId,
                    RawFilePath = t.RawImageFilePath
                })
            ];
            foreach (var bestFocusChannelItems in globalFieldTiltDTOItem.BestFocusChannelItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = $"(PMT{bestFocusChannelItems.PmtId},Channel{bestFocusChannelItems.ChannelId})_Guid({Guid.NewGuid()}).jpg";
                var originImageFilePath = Path.Combine(filePath, "Origin", fileName);
                var linearImageFilePath = Path.Combine(filePath, "Linear", fileName);

                using var bitmapImage = new BitmapImage(bestFocusChannelItems.RawFilePath);
                bitmapImage.SaveImage(originImageFilePath);

                var linerImage = bitmapImage.ToLinearImage();
                linerImage.SaveImage(linearImageFilePath);

                bestFocusChannelItems.FilePath = originImageFilePath;
                bestFocusChannelItems.LinearFilePath = linearImageFilePath;

                /*if (Cache.Item.AlgorithmImageQualityTypeEnum == AlgorithmImageQualityTypeEnum.StrehlRatio)
                {
                    var inputDarkFieldImage = Cache.Item.CIBConfiguration is { IsAutoGainControl: true, CIBProfileMode: CIBProfileModeEnum.PMTLog }
                        ? linerImage
                        : image;

                    Point[] xFitPoints = [];
                    Point[] xQualityPoints = [];
                    try
                    {
                        var (
                            _,
                            _,
                            _,
                            _,
                            _,
                            _,
                            _,
                            _,
                            _) = CalibrationAlgorithmService.GetXYStrehlRatios(
                            inputDarkFieldImage,
                            out _,
                            out var _,
                            out var _,
                            out _,
                            out _,
                            out _,
                            out _);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Get XYStrehlRatio Failed.Error: {ex}"), HtmlLogUniqueId.LoggingHtml());
                    }

                    bestFocusChannelItems.XQualitys = [.. xQualityPoints];
                    bestFocusChannelItems.XFitPositions = xFitPoints;

                    var xBestFocusXPixel = xFitPoints.Length > 0 ? xFitPoints.Maxima(t => t.Y).First().X : 0;

                    var pixelPerEcs = image.GetSize().Width / (Cache.Item.RangeECS * 2);
                    bestFocusChannelItems.XBestFocusEcs = (Cache.Item.CenterECS - Cache.Item.RangeECS) + xBestFocusXPixel / pixelPerEcs;
                    bestFocusChannelItems.XBestFocusQuality = xFitPoints.Maxima(t => t.Y).First().Y;
                }
                else
                    throw new NotImplementedException("It‘s not implemented in this version.");*/
            }
        }

        async Task TraversalECSFunctionAsync()
        {
            AfViewModel.ToggleBrightFieldEnable(false);

            foreach (var pmtId in Cache.PMTIds.OrderBy(t => t))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Logger.LogHtmlInformation($"PMT {pmtId}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                var detectImageDirectory = Path.Combine(ImageFileDirectory, "Traversal", $"{pmtId}");

                var channelItem = new GlobalFieldTiltDTOItem.Item
                {
                    PmtId = pmtId,
                    ChannelId = Cache.Item.CIBInformation.ChannelId
                };
                globalFieldTiltDTOItem.BestFocusChannelItems = [.. globalFieldTiltDTOItem.BestFocusChannelItems, channelItem];

                Logger.LogHtmlInformation("Roughly", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                var findPosition = Cache.Item.ImageCollectionConfiguration.StartPoint + (Vector)new Point(0, (pmtId - CalibrationConstantsHelper.MainPmtId) * Cache.PmtInterval);
                await CatchImageAsync(Generate.LinearRange(
                    Cache.Item.CenterECS - Cache.Item.RangeECS,
                    Cache.Item.StepECS,
                    Cache.Item.CenterECS + Cache.Item.RangeECS));

                Logger.LogHtmlInformation("Refined", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                findPosition = Cache.Item.ImageCollectionConfiguration.EndPoint + (Vector)new Point(0, (pmtId - CalibrationConstantsHelper.MainPmtId) * Cache.PmtInterval);
                await CatchImageAsync(Generate.LinearRange(
                    channelItem.XBestFocusEcs - Cache.Item.RangeRefinedECS,
                    Cache.Item.StepRefinedECS,
                    channelItem.XBestFocusEcs + Cache.Item.RangeRefinedECS));

                async Task CatchImageAsync(IReadOnlyList<double> ecses)
                {
                    Guard.IsNotEmpty(ecses);

                    var currentDetectImageDirectory = Path.Combine(detectImageDirectory, $"[{ecses[0]:0.###}ECS, {ecses[^1]:0.###}ECS]_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat)}");

                    var resultList = new List<OpticsRelayDTOItem.Item>();
                    foreach (var ecs in ecses)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        AfViewModel.SetSensorEcsValue(ecs);
                        var itemItemData = new OpticsRelayDTOItem.Item { ECS = ecs };
                        using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                            Cache.Item.ProductivityInformation,
                            StageCoordinateSystemEnum.Dark,
                            findPosition,
                            Cache.Item.ImageWidth,
                            ApplicationCookie.CIBInformations.Single(t => t.PMTId == pmtId && t.ChannelId == Cache.Item.CIBInformation.ChannelId),
                            (false, Cache.CalChipSiteModelEnum),
                            (false, Cache.Item.OpticsConfiguration),
                            (false, Cache.Item.CIBConfiguration),
                            (false, Cache.Item.LaserLightInformation),
                            false,
                            cancellationToken,
                            isAutoFocus: false);

                        var quality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImage.Image);

                        var imageFilePath = Path.Combine(currentDetectImageDirectory, $"{ecs:0.###}ECS_{quality:0.###}Quality_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.SaveImage(imageFilePath);

                        itemItemData.Quality = quality;
                        itemItemData.ImageFilePath = imageFilePath;
                        itemItemData.RawImageFilePath = darkFieldImage.RawImageFilePath;
                        resultList.Add(itemItemData);

                        channelItem.XFitPositions = [.. ((IReadOnlyList<Point>)[.. channelItem.XFitPositions, new Point(ecs, quality)]).OrderBy(t => t.X)];

                        Logger.LogHtmlInformation($"{ecs:0.###}ECS", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                        {
                            findPosition,
                            quality,
                            darkFieldImage.RawImageFilePath,
                            Image = new HtmlImage(imageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    var resultItem = resultList.OrderByDescending(t => t.Quality).First();

                    channelItem.XBestFocusEcs = resultItem.ECS;
                    channelItem.RawFilePath = resultItem.RawImageFilePath;
                    channelItem.FilePath = resultItem.ImageFilePath;
                    channelItem.XBestFocusQuality = resultItem.Quality;

                    Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        channelItem.XBestFocusEcs,
                        channelItem.XBestFocusQuality,
                        channelItem.RawFilePath,
                        Qualitys = new HtmlContainer(channelItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()),
                        Image = new HtmlImage(channelItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }
        }
    }

    private bool Save(IReadOnlyList<GlobalFieldTiltDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto,
                .. Calibrations.Where(t => t.OpticsIlluminationModeEnum != dto.OpticsIlluminationModeEnum)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<GlobalFieldTiltDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsIlluminationModeEnums.Select(t => new OpticsIlluminationModeStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationModeEnum))
                .DistinctBy(t => t.OpticsIlluminationModeEnum)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.OpticsIlluminationModeEnum).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.OpticsIlluminationModeEnums.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.ReviewCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsIlluminationModeEnums.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.OpticsIlluminationModeEnum == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}