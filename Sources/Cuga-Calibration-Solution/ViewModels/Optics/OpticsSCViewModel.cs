using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.SC;
using Core.Utilities.SourceGenerators.Attributes;
using Humanizer;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.Text;

namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(OpticsSCViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsSCViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override string CalibrateFileName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Image Param" },
        new() { StepName = "Alignment" },
        new() { StepName = "Find DSW Position" },
        new() { StepName = "SC" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private OpticsSCDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<OpticsSCDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsSCDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private OpticsSCCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private OpticsSCDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private MicroscopeCalChipCache _microscopeCalChipCache = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();
        MicroscopeCalChipCache = RecipeCacheProvider.GetOrDefault<MicroscopeCalChipCache>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses =
            [
                .. ApplicationCookie.OpticsIlluminationModeEnums.Select(t => new OpticsIlluminationModeStatus { SelectedItem = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<OpticsSCCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<OpticsSCDTO>();

        Calibrations =
        [
            ..Calibrations
                .Where(t => ApplicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationModeEnum))
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.OpticsIlluminationModeEnum).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

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

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.DSWFindBFMachinePosition));

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new OpticsSCDTO();

                return true;

            case 1:
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.DSWFindBFMachinePosition != Point.Origin
                    ? Cache.Item.DSWFindBFMachinePosition
                    : MicroscopeCalChip.DswItem.BrightFieldMachinePosition));

                return true;

            case 3:
                return true;

            case 4:
                CalibratingStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibratingStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum
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
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.GetProductivityInformations(Cache.OpticsIlluminationModeEnum).Contains(Cache.Item.ProductivityInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var alignmentResult = StageViewModel.Alignment(
                MicroscopeCalChipCache.LowSite1,
                MicroscopeCalChipCache.LowSite2,
                MicroscopeCalChipCache.HighSite1,
                MicroscopeCalChipCache.HighSite2,
                MicroscopeCalChipCache.LowMicroscopeLensInformation,
                MicroscopeCalChipCache.HighMicroscopeLensInformation,
                MicroscopeCalChipCache.AlgorithmWaferTypeEnum,
                CalChipSiteModelEnum.DswModel);

            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition((alignmentResult.MarkPoint1 + (Vector)alignmentResult.MarkPoint2) / 2d));

            Cache.Item.AlignmentResult = alignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            Cache.Item.DSWFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.DSWFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var (currentMotorAbsoluteValueL1, currentMotorAbsoluteValueL3) = OpticsViewModel.GetSCMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.DSWFindBFMachinePosition,
                Cache.Item.ScanLength,
                Cache.Item.StartLambda,
                Cache.Item.StepLambda,
                Cache.Item.StopLambda,
                Cache.Item.LambdaToL1Coefficient,
                Cache.Item.LambdaToL3Coefficient,
                Cache.Item.SCMotorAbsoluteValueL1Center,
                Cache.Item.SCMotorAbsoluteValueL3Center,
                Cache.Item.IsL1ToL2Direction,
                Cache.Item.IsL2ToL3Direction,
                Cache.Item.CenterECS,
                Cache.Item.RangeECS,
                currentMotorAbsoluteValueL1,
                currentMotorAbsoluteValueL3,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum;
            CalibratingItem.Items = [];
            CalibratingItem.MaxItem = null;
            CalibratingItem.IsCalibrated = false;

            var dswBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.DSWFindBFMachinePosition);
            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(dswBFPosition);

            var startCurrentDSWBFPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.Item.ProductivityInformation,
                Cache.Item.CIBInformation,
                dswBFPosition,
                Cache.Item.MicroscopeLensInformation);

            try
            {
                Logger.LogHtmlInformation("Relay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                AfViewModel.ToggleBrightFieldEnable(false);

                var lambdas = Generate.LinearRangeContainsEdge(Cache.Item.StartLambda, Cache.Item.StepLambda, Cache.Item.StopLambda);
                Guard.IsGreaterThan(lambdas.Length, 2);

                var isSuccess = true;
                foreach (var lambda in lambdas)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{lambda:0.###}λ", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    var l1 = Cache.Item.SCMotorAbsoluteValueL1Center + (Cache.Item.IsL1ToL2Direction ? -1d : 1d) * lambda * Cache.Item.LambdaToL1Coefficient;
                    var l3 = Cache.Item.SCMotorAbsoluteValueL3Center + (Cache.Item.IsL2ToL3Direction ? 1d : -1d) * lambda * Cache.Item.LambdaToL3Coefficient;

                    OpticsViewModel.SetSCMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, (l1, l3));

                    var item = new OpticsSCDTOItem
                    {
                        Lambda = lambda,
                        SCMotorAbsoluteValueL1 = l1,
                        SCMotorAbsoluteValueL3 = l3
                    };
                    CalibratingItem.Items = [.. CalibratingItem.Items, item];

                    var startECS = Cache.Item.CenterECS - Cache.Item.RangeECS;
                    var stopECS = Cache.Item.CenterECS + Cache.Item.RangeECS;

                    using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                        Cache.Item.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        startCurrentDSWBFPosition,
                        startCurrentDSWBFPosition + new Vector(Cache.Item.ScanLength, 0),
                        startECS,
                        stopECS,
                        Cache.Item.CIBInformation,
                        (true, null),
                        (false, Cache.Item.OpticsConfiguration),
                        (false, Cache.Item.CIBConfiguration),
                        (false, Cache.Item.LaserLightInformation),
                        false,
                        cancellationToken);

                    try
                    {
                        var bestFocus = CalibrationAlgorithmService.GetBestFocus(darkFieldImage.Image, startECS, stopECS);
                        item.BestFocus = bestFocus;
                        item.BestFocus.RawImageFilePath = darkFieldImage.RawImageFilePath;

                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            item.Lambda,
                            item.SCMotorAbsoluteValueL1,
                            item.SCMotorAbsoluteValueL3,
                            item.BestFocus.RawImageFilePath,
                            XStrehlRatioScatterPlotControl = new HtmlContainer([.. item.BestFocus.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                            YStrehlRatioScatterPlotControl = new HtmlContainer([.. item.BestFocus.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        item.BestFocus.RawImageFilePath = darkFieldImage.RawImageFilePath;
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            Exception = ex,
                            item.Lambda,
                            item.SCMotorAbsoluteValueL1,
                            item.SCMotorAbsoluteValueL3,
                            item.BestFocus.RawImageFilePath
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }

                CalibratingItem.IsCalibrated = isSuccess;

                var htmlBullet = new HtmlBullet(new
                {
                    dswBFPosition,
                    startCurrentDSWBFPosition,
                    CalibratingItem.MaxItem?.BestFocus.BestYStrehlRatioPoint,
                    CalibratingItem.Lambda,
                    CalibratingItem.SCMotorAbsoluteValueL1,
                    CalibratingItem.SCMotorAbsoluteValueL3,
                    ScatterPlotControl = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (CalibratingItem.IsCalibrated)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;
            }
            finally
            {
                OpticsViewModel.SetSCMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, (currentMotorAbsoluteValueL1, currentMotorAbsoluteValueL3));
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(dswBFPosition);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.OpticsIlluminationModeEnum))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.OpticsIlluminationModeEnum.Humanize();

                /*if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }*/

                Cache.OpticsIlluminationModeEnum = selectedReviewItem.OpticsIlluminationModeEnum;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.OpticsIlluminationModeEnum
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.MaxItem?.BestFocus.BestYStrehlRatioPoint,
                    selectedReviewItem.Lambda,
                    selectedReviewItem.SCMotorAbsoluteValueL1,
                    selectedReviewItem.SCMotorAbsoluteValueL3,
                    selectedReviewItem.IsVerified,
                    SuccessPlot = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (selectedReviewItem.IsOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }
            }

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            var result = SelectedReviewItems.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private bool Save(IReadOnlyList<OpticsSCDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
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

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}