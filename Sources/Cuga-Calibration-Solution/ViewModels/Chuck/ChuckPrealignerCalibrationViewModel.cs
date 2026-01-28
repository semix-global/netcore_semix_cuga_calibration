using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.Helper;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckPrealignerCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckPrealignerCalibrationViewModel(EFEMWindowViewModel efemWindowViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Center offset" },
        new() { StepName = "Low MarkSite1" },
        new() { StepName = "Low MarkSite2" },
        new() { StepName = "High MarkSite1" },
        new() { StepName = "High MarkSite2" },
        new() { StepName = "P5" },
        new() { StepName = "Calibration Result" }
    ];

    #region 界面相关

    [ObservableProperty]
    private ChuckPrealignerDTO _calibrateDTO = new();

    [ObservableProperty]
    private ChuckPrealignerDTOItem _calibrateItem = new();

    #endregion 界面相关

    #region Review

    [ObservableProperty]
    private ChuckPrealignerDTO? _reviewDto;

    #endregion Review

    #region 缓存

    [ObservableProperty]
    private ChuckPrealignerCache _cache = new();

    [ObservableProperty]
    private ChuckPrealignerDTO _calibration = new();

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto _chuckCenter = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopePixelSizeItemDto>(out var microscopePixelSizeItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopePixelSizeItems = microscopePixelSizeItems;

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeCentricityItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckGantryDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckGlobalScaleErrorDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterAndThetaItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckPrealignerCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckPrealignerDTO>();

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        Cache.FindWaferCenterOffset1 = Point.Origin;
        Cache.FindWaferCenterOffset2 = Point.Origin;
        Cache.FindWaferCenterOffset3 = Point.Origin;
        Cache.FindWaferCenterOffset4 = Point.Origin;
        Cache.FindWaferCenterOffset5 = Point.Origin;
        Cache.FindWaferCenterOffset6 = Point.Origin;
        Cache.FindWaferCenterOffset7 = Point.Origin;
        Cache.FindWaferCenterOffset8 = Point.Origin;
        Cache.WaferCenterThumb1 = [];
        Cache.WaferCenterThumb2 = [];
        Cache.WaferCenterThumb3 = [];
        Cache.WaferCenterThumb4 = [];
        Cache.WaferCenterThumb5 = [];
        Cache.WaferCenterThumb6 = [];
        Cache.WaferCenterThumb7 = [];
        Cache.WaferCenterThumb8 = [];

        if (await ReloadWaferAsync(false) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please Reload Wafer!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                StageViewModel.SetAbsoluteStageTheta(0);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite1.Location);
                return true;
            case 1:
                Cache.LowSite2.Location = Cache.LowSite1.Location + (Vector)new Point(Cache.DiePitchWidth * Cache.ReticleDieCountX, 0);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite2.Location);
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                Cache.HighSite1.Location = Cache.LowSite1.Location;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite1.Location);
                return true;

            case 3:
                Cache.HighSite2.Location = Cache.LowSite2.Location;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite2.Location);
                return true;

            case 6:
                IsCalibrated = true;
                return true;

            default:
                return true;
        }
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 2:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite1.Location);
                return true;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite2.Location);
                return true;

            case 4:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite1.Location);
                return true;

            case 5:
                StageViewModel.SetAbsoluteStageTheta(0);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite2.Location);
                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    public async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new { Cache.TeachingPositionThreshold }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

            var (offsetPosition, bitmapMemoryBytes) = StageViewModel.FindWaferCenterByManually(Point.Origin, [
                Cache.FindWaferCenterOffset1,
                Cache.FindWaferCenterOffset2,
                Cache.FindWaferCenterOffset3,
                Cache.FindWaferCenterOffset4,
                Cache.FindWaferCenterOffset5,
                Cache.FindWaferCenterOffset6,
                Cache.FindWaferCenterOffset7,
                Cache.FindWaferCenterOffset8
            ]);

            Cache.OffsetPosition = offsetPosition;

            if (bitmapMemoryBytes.Count > 0)
            {
                Cache.WaferCenterThumb1 = bitmapMemoryBytes[0];
                Cache.WaferCenterThumb2 = bitmapMemoryBytes[1];
                Cache.WaferCenterThumb3 = bitmapMemoryBytes[2];
                Cache.WaferCenterThumb4 = bitmapMemoryBytes[3];
                Cache.WaferCenterThumb5 = bitmapMemoryBytes[4];
                Cache.WaferCenterThumb6 = bitmapMemoryBytes[5];
                Cache.WaferCenterThumb7 = bitmapMemoryBytes[6];
                Cache.WaferCenterThumb8 = bitmapMemoryBytes[7];
                for (var i = 0; i < bitmapMemoryBytes.Count; i++)
                {
                    var waferCenterThumbPath = $"{ImageFileDirectory}\\WaferCenterThumb\\WaferCenterThumb{i + 1}_Guid{HtmlLogUniqueId}.jpg";
                    var waferCenterThumbBitmapSource = BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(bitmapMemoryBytes[i]);
                    BitmapSourceHelper.Save(waferCenterThumbBitmapSource, waferCenterThumbPath);
                    Logger.LogHtmlInformation($"Find center edge image {i}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        HtmlTab = new HtmlTab(new
                        {
                            WaferCenterThumb = new HtmlImage(waferCenterThumbPath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }

            var result = Math.Abs(Cache.OffsetPosition.X) < Cache.TeachingPositionThreshold
                         && Math.Abs(Cache.OffsetPosition.Y) < Cache.TeachingPositionThreshold;

            var chuckPrealignerItem = new ChuckPrealignerDTOItem { OffsetPosition = Cache.OffsetPosition };

            GetPrealignerP8Result(chuckPrealignerItem);

            CalibrateItem = chuckPrealignerItem.Clone();

            Logger.LogHtmlInformation($"Init center result {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckPrealignerItem.OffsetPosition,
                chuckPrealignerItem.EfemLoadWaferStagePosition,
                chuckPrealignerItem.NewEfemLoadWaferStagePosition,
                Cache.FindWaferCenterOffset1,
                Cache.FindWaferCenterOffset2,
                Cache.FindWaferCenterOffset3,
                Cache.FindWaferCenterOffset4,
                Cache.FindWaferCenterOffset5,
                Cache.FindWaferCenterOffset6,
                Cache.FindWaferCenterOffset7,
                Cache.FindWaferCenterOffset8
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            Cache.LowSite1.Location = position;
            var lowTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            Cache.LowSiteTemplateFilePath = lowTemplateFilePath;

            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var lowTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(lowTemplateFilePath);

            var resultLowSite1 = StageViewModel.MarkAlignSite1(Cache.LowSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
            if (resultLowSite1.Template is null)
            {
                return false;
            }

            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultLowSite1.Template.Thumb), lowTemplateImageFilePath);
            Cache.LowSite1 = resultLowSite1;
            Cache.LowSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.LowSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.DiePitchWidth,
                Cache.ReticleDieCountX,
                Cache.WaferRadius,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowMicroscopeLensInformation.LensName,
                Cache.LowSite1.Location,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplate = new HtmlImage(lowTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, position, Cache.LowMicroscopeLensInformation, Cache.LowSiteTemplateFilePath, out var lowPositionResult) == false) return false;

            Cache.LowSite2.Location = lowPositionResult;
            var resultLowSite2 = StageViewModel.MarkAlignSite2(Cache.LowSite1, Cache.AlgorithmWaferTypeEnum);
            Cache.LowSite2 = resultLowSite2;
            Cache.LowSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.LowSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowMicroscopeLensInformation.LensName,
                Cache.LowSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            if (Cache.HighMicroscopeLensInformation.LensCode <= Cache.LowMicroscopeLensInformation.LensCode)
            {
                DialogWindowProvider.ShowDialog("The high magnification less than or equal low magnification! Please select correct magnification!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var position = StageViewModel.GetBrightFieldStagePosition();
            Cache.HighSite1.Location = position;

            var highTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            Cache.HighSiteTemplateFilePath = highTemplateFilePath;

            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var highTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(highTemplateFilePath);

            var resultHighSite1 = StageViewModel.MarkAlignSite1(Cache.HighSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
            if (resultHighSite1.Template is null) return false;

            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultHighSite1.Template.Thumb), highTemplateImageFilePath);
            Cache.HighSite1 = resultHighSite1;
            Cache.HighSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.HighSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeLensInformation.LensName,
                Cache.HighSite1.Location,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplate = new HtmlImage(highTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, position, Cache.HighMicroscopeLensInformation, Cache.HighSiteTemplateFilePath, out var highPositionResult) == false) return false;

            Cache.HighSite2.Location = highPositionResult;
            var resultHighSite2 = StageViewModel.MarkAlignSite2(Cache.HighSite1, Cache.AlgorithmWaferTypeEnum);
            Cache.HighSite2 = resultHighSite2;
            Cache.HighSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.HighSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeLensInformation.LensName,
                Cache.HighSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);

            var alignmentResultDto = StageViewModel.AlignmentVerify(
                Cache.LowSite1,
                Cache.LowSite2,
                Cache.HighSite1,
                Cache.HighSite2,
                Cache.LowMicroscopeLensInformation,
                Cache.HighMicroscopeLensInformation,
                Cache.AlgorithmWaferTypeEnum);

            CalibrateItem.EfemLoadWaferChuckAbsoluteAngle = Cache.Degrees = alignmentResultDto.Degrees;

            var result = Math.Abs(Cache.Degrees) < Cache.TeachingDegreesThreshold;

            Logger.LogHtmlInformation("Result Ok", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentParam = new HtmlQuote(new
                {
                    LowMagnification = Cache.LowMicroscopeLensInformation.LensName,
                    HighMagnification = Cache.HighMicroscopeLensInformation.LensName,
                    Cache.AlgorithmWaferTypeEnum,
                    LowLocation1 = Cache.LowSite1.Location,
                    LowLocation2 = Cache.LowSite2.Location,
                    HighLocation1 = Cache.HighSite1.Location,
                    HighLocation2 = Cache.HighSite2.Location
                }),
                Cache.TeachingDegreesThreshold,
                CalibrateItem.OffsetPosition,
                CalibrateItem.EfemLoadWaferStagePosition,
                CenterOffsetCalibrationResult = CalibrateItem.NewEfemLoadWaferStagePosition,
                AngleOffsetCalibrationResult = CalibrateItem.EfemLoadWaferChuckAbsoluteAngle
            }), HtmlLogUniqueId.LoggingHtml());

            if (IsAutoCalibrate == false)
                DialogWindowProvider.ShowDialog($"Chuck Prealigner Calibration {(result ? "Success" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return result;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step6CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(async () =>
        {
            CalibrateDTO.Items = [];
            for (var i = 0; i < Cache.Times; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"Times: {i + 1}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                var chuckPrealignerItem = CalibrateItem.Clone();
                if (await ReloadWaferVerifyActionAsync(chuckPrealignerItem, cancellationToken).ConfigureAwait(false) == false) return false;

                CalibrateDTO.Items =
                [
                    ..CalibrateDTO.Items,
                    chuckPrealignerItem
                ];

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    chuckPrealignerItem.OffsetPosition,
                    chuckPrealignerItem.EfemLoadWaferStagePosition,
                    CenterOffsetCalibrationResult = chuckPrealignerItem.NewEfemLoadWaferStagePosition,
                    AngleOffsetCalibrationResult = chuckPrealignerItem.EfemLoadWaferChuckAbsoluteAngle
                }), HtmlLogUniqueId.LoggingHtml());
            }

            CalibrateDTO.ResultItemDto = CalibrateItem.Clone();

            if (CalibrateDTO.Items.Count >= 3)
            {
                // 去除背景
                List<double> offsetXMovMeans = [.. MovMeanFilter.Smooth(3, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(CalibrateDTO.Items.Select(t => t.OffsetPosition.X)))];
                List<double> offsetYMovMeans = [.. MovMeanFilter.Smooth(3, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(CalibrateDTO.Items.Select(t => t.OffsetPosition.Y)))];
                List<double> angleMovMeans = [.. MovMeanFilter.Smooth(3, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(CalibrateDTO.Items.Select(t => t.EfemLoadWaferChuckAbsoluteAngle)))];

                var offsetPositionAverage = new Point(offsetXMovMeans.Average(), offsetYMovMeans.Average());
                var offsetAngleAverage = angleMovMeans.Average();

                Logger.LogHtmlInformation("Move Mean Result", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    offsetPositionAverage,
                    offsetAngleAverage,
                    ResultPlot = new HtmlContainer(CalibrateDTO.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts())
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var findWaferCenterOffsetResult = Math.Abs(CalibrateDTO.Items[0].OffsetPosition.X) < Cache.VerifyPositionThreshold
                                              && Math.Abs(CalibrateDTO.Items[0].OffsetPosition.Y) < Cache.VerifyPositionThreshold;
            var alignmentResult = Math.Abs(CalibrateDTO.Items[0].EfemLoadWaferChuckAbsoluteAngle) < Cache.VerifyDegreesThreshold;

            CalibrateDTO.IsCalibrated = findWaferCenterOffsetResult && alignmentResult;

            Guard.IsTrue(Save(CalibrateDTO, cancellationToken));

            Logger.LogHtmlInformation("Result Ok", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibrateDTO.ResultItemDto.OffsetPosition,
                CalibrateDTO.ResultItemDto.EfemLoadWaferStagePosition,
                CenterOffsetCalibrationResult = CalibrateDTO.ResultItemDto.NewEfemLoadWaferStagePosition,
                AngleOffsetCalibrationResult = CalibrateDTO.ResultItemDto.EfemLoadWaferChuckAbsoluteAngle
            }), HtmlLogUniqueId.LoggingHtml());

            if (IsAutoCalibrate == false)
                DialogWindowProvider.ShowDialog($"Chuck Prealigner Calibration {(CalibrateDTO.IsCalibrated ? "Success" : "Failed")}!", DialogButtonsEnum.OK, CalibrateDTO.IsCalibrated ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return CalibrateDTO.IsCalibrated;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeVerifyAsync(async () =>
        {
            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Review item is null! Please calibration first", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Review item is null! Please calibration first"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var result = await VerifyCalibrationAsync(ReviewDto, cancellationToken);

            if (IsAutoCalibrate == false)
                DialogWindowProvider.ShowDialog($"Chuck Prealigner Verify {(result ? "Success" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(ChuckPrealignerDTO selectChuckPrealignerDTO, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            selectChuckPrealignerDTO.IsVerified = false;

            var reviewDto = selectChuckPrealignerDTO.Clone();

            if (await ReloadWaferVerifyActionAsync(reviewDto.ResultItemDto, cancellationToken).ConfigureAwait(false) == false) return false;

            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

            var result = Math.Abs(reviewDto.ResultItemDto.OffsetPosition.X) < Cache.VerifyPositionThreshold
                         && Math.Abs(reviewDto.ResultItemDto.OffsetPosition.Y) < Cache.VerifyPositionThreshold
                         && Math.Abs(reviewDto.ResultItemDto.EfemLoadWaferChuckAbsoluteAngle) < Cache.VerifyDegreesThreshold;

            selectChuckPrealignerDTO.IsVerified = result;

            Guard.IsTrue(Save(selectChuckPrealignerDTO, cancellationToken));

            Logger.LogHtmlInformation($"Verify {(result ? "Success" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                VerifyPositionOffsetThreshold = Cache.VerifyPositionThreshold,
                VerifyAngleThreshold = Cache.VerifyDegreesThreshold,
                TeachingOffsetPosition = selectChuckPrealignerDTO.ResultItemDto.OffsetPosition,
                TeachingAngle = selectChuckPrealignerDTO.ResultItemDto.EfemLoadWaferChuckAbsoluteAngle,
                VerifyOffsetPosition = reviewDto.ResultItemDto.OffsetPosition,
                VerifyAngle = reviewDto.ResultItemDto.EfemLoadWaferChuckAbsoluteAngle
            }), HtmlLogUniqueId.LoggingHtml());

            if (result == false)
            {
                DialogWindowProvider.ShowDialog("Verify Chuck Prealigner calibration failed.Position Error Out Of The Threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            return result;
        }, cancellationToken);
    }

    // 此方法逻辑慎重修改，谨慎调试
    private async Task<bool> ReloadWaferAsync(bool isReviewLoadWafer, ChuckPrealignerDTOItem? chuckPrealignerDTOItem = null)
    {
        return await Task.Run(() =>
        {
            efemWindowViewModel.IsPrealigner = true;
            efemWindowViewModel.PrealignerIsOk = false;

            efemWindowViewModel.OffsetAngle = 0;
            efemWindowViewModel.OffsetPoint = Point.Origin;

            if (isReviewLoadWafer)
            {
                Guard.IsNotNull(chuckPrealignerDTOItem);

                if (Math.Abs(chuckPrealignerDTOItem.EfemLoadWaferChuckAbsoluteAngle) > Cache.TeachingDegreesThreshold)
                {
                    DialogWindowProvider.ShowDialog("The angle of the EFEM load wafer chuck is out of the threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                if (Math.Abs(chuckPrealignerDTOItem.OffsetPosition.X) > Cache.TeachingPositionThreshold || Math.Abs(chuckPrealignerDTOItem.OffsetPosition.Y) > Cache.TeachingPositionThreshold)
                {
                    DialogWindowProvider.ShowDialog("The offset of the EFEM load wafer chuck is out of the threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
                var point = new Point(-xDirection * chuckPrealignerDTOItem.OffsetPosition.X, -yDirection * chuckPrealignerDTOItem.OffsetPosition.Y);
                efemWindowViewModel.OffsetPoint = point;
                efemWindowViewModel.OffsetAngle = chuckPrealignerDTOItem.EfemLoadWaferChuckAbsoluteAngle;
            }

            WindowManagerService.ShowDialog(efemWindowViewModel);

            efemWindowViewModel.IsPrealigner = false;

            var result = efemWindowViewModel.SelectedFoupItem != null
                         && efemWindowViewModel is { PrealignerIsOk: true, SelectedFoupItem.IsLoadWafer: true };

            Logger.LogHtmlInformation($"ReloadWafer {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                AngleErrorThreshold = Cache.TeachingDegreesThreshold,
                PositionThreshold = Cache.TeachingPositionThreshold,
                efemWindowViewModel.OffsetAngle,
                efemWindowViewModel.OffsetPoint
            }), HtmlLogUniqueId.LoggingHtml());

            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> ReloadWaferVerifyActionAsync(ChuckPrealignerDTOItem chuckPrealignerItem, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            if (await ReloadWaferAsync(true, chuckPrealignerItem) == false)
            {
                DialogWindowProvider.ShowDialog("Please Reload Wafer!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please Reload Wafer!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var (offsetPosition, _) = StageViewModel.FindWaferCenterByManually(Point.Origin);

            var alignmentResultDto = StageViewModel.AlignmentVerify(
                Cache.LowSite1,
                Cache.LowSite2,
                Cache.HighSite1,
                Cache.HighSite2,
                Cache.LowMicroscopeLensInformation,
                Cache.HighMicroscopeLensInformation,
                Cache.AlgorithmWaferTypeEnum);

            chuckPrealignerItem.OffsetPosition = Cache.OffsetPosition = offsetPosition;
            chuckPrealignerItem.EfemLoadWaferChuckAbsoluteAngle = Cache.Degrees = alignmentResultDto.Degrees;

            GetPrealignerP8Result(chuckPrealignerItem);

            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    private void GetPrealignerP8Result(ChuckPrealignerDTOItem item)
    {
        var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

        var efemLoadWaferStagePosition = StageViewModel.GetEfemLoadWaferMachineStagePosition();

        var offsetPositionCalibrationResult = new Point(efemLoadWaferStagePosition.X - xDirection * item.OffsetPosition.X, efemLoadWaferStagePosition.Y - yDirection * item.OffsetPosition.Y);

        item.EfemLoadWaferStagePosition = efemLoadWaferStagePosition;
        item.NewEfemLoadWaferStagePosition = offsetPositionCalibrationResult;
    }


    private bool Save(ChuckPrealignerDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependPrealignerCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    #endregion 校准
}