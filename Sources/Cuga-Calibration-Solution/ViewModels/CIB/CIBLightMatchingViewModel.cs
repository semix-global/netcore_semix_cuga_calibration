using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Humanizer;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Core.Models.Models.CIB.LightMatching;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBLightMatchingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLightMatchingViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{Cache.OpticsIlluminationModeEnum.Humanize()}_{Cache.ProductivityInformation}";

    public override string CalibrateFileName => $"{Cache.OpticsIlluminationModeEnum.Humanize()}_{Cache.ProductivityInformation}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Select Productivity" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Find Silica Spheres Position" },
        new() { StepName = "Light Matching" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTO> _calibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeAndProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private CIBLightMatchingCache _cache = new();

    [ObservableProperty]
    private CIBLightMatchingDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var microscopeCalChip, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = microscopeCalChip;

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserAutoFocusDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserBeamStabilizerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                ..EnumHelper.Enums<OpticsIlluminationModeEnum>()
                    .Select(t => new OpticsIlluminationModeAndProductivityInformationCalibrationStatus()
                    {
                        SelectedItem = t,
                        ProductivityInformationCalibrationStatusList = [.. ProductivityInformationCalibrationStatus.CreateList(ApplicationCookie.NIOpticsMagTypeProductivityInformations)]
                    })
            ];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CIBLightMatchingCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<CIBLightMatchingDTO>();

        Calibrations =
        [
            .. Calibrations.Where(t => ApplicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationModeEnum)
                                       && t.OpticsIlluminationModeEnum switch
                                       {
                                           OpticsIlluminationModeEnum.OI => ApplicationCookie.OIProductivityInformations.Contains(t.ProductivityInformation),
                                           OpticsIlluminationModeEnum.NI => ApplicationCookie.NIProductivityInformations.Contains(t.ProductivityInformation),
                                           _ => ThrowHelper.ThrowArgumentException<bool>(nameof(t.OpticsIlluminationModeEnum))
                                       }
                                       && ApplicationCookie.OpticsApodizationModeEnums.Contains(t.OpticsApodizationModeEnum)
                                       && ApplicationCookie.OpticsPolarizationModeEnums.Contains(t.OpticsPolarizationModeEnum)
                                       && ApplicationCookie.CollectorPolarizationModeEnums.Contains(t.CollectorPolarizationModeEnum))
                .Select(t =>
                {
                    CalibrationStatuses
                        .Single(tt => tt.SelectedItem == t.OpticsIlluminationModeEnum)
                        .ProductivityInformationCalibrationStatusList
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .IsCalibrated = t.IsCalibrated;

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
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsIlluminationModeEnum)
                .ThenBy(t => t.ProductivityInformation)
        ];

        return Reviews.Any(t => t.IsCalibrated);
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
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition));

                return true;

            case 5:
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.SilicaSphereFindBFMachinePosition));

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
                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                CalibratingItems = [];

                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 4:

                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 5:
                CalibrationStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum)
                    .ProductivityInformationCalibrationStatusList
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation).IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"Light Matching {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.NIProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.SilicaSphereFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.SilicaSphereFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var currentOpticsApodizationModeEnum = OpticsViewModel.GetApodizationMode();
            var currentOpticsPolarizationModeEnum = OpticsViewModel.GetPolarizationMode();
            var currentCollectorPolarizationModeEnum = CollectorViewModel.GetPolarizationMode();
            var cibInformations = ApplicationCookie.CIBInformations;

            try
            {
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OpticsIlluminationModeEnum,
                    Cache.ProductivityInformation,
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.LaserLightInformation,
                    Cache.Item.HazeFindBFMachinePosition,
                    Cache.Item.SilicaSphereFindBFMachinePosition,
                    currentOpticsApodizationModeEnum,
                    currentOpticsPolarizationModeEnum,
                    currentCollectorPolarizationModeEnum,
                    cibInformations
                }), HtmlLogUniqueId.LoggingHtml());

                CalibratingItems = [];

                LaserViewModel.ToggleEnableAutoGainControl(true);
                LaserViewModel.ToggleProfileMode(CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetLightMatching(cibInformations, 0);

                var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

                Logger.LogHtmlInformation("Haze", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                await LightMatchingAsync(true, hazeBFPosition);

                var silicaSphereBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.SilicaSphereFindBFMachinePosition);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipDswDarkFieldAbsoluteStageXyByNotAutoFocus(silicaSphereBFPosition);

                Logger.LogHtmlInformation("Silica Sphere", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                await LightMatchingAsync(false, silicaSphereBFPosition);

                Guard.IsTrue(Save(CalibratingItems, cancellationToken));

                return CalibratingItems.All(t => t.IsCalibrated);

                async Task LightMatchingAsync(bool isHaze, Point bfPosition)
                {
                    foreach (var opticsApodizationModeEnum in ApplicationCookie.OpticsApodizationModeEnums)
                    {
                        foreach (var opticsPolarizationModeEnum in ApplicationCookie.OpticsPolarizationModeEnums)
                        {
                            foreach (var collectorPolarizationModeEnum in ApplicationCookie.CollectorPolarizationModeEnums)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                OpticsViewModel.SetApodizationMode(opticsApodizationModeEnum);
                                OpticsViewModel.SetPolarizationMode(opticsPolarizationModeEnum);
                                CollectorViewModel.SetPolarizationMode(collectorPolarizationModeEnum);

                                var item = isHaze
                                    ? new CIBLightMatchingDTO
                                    {
                                        OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum,
                                        ProductivityInformation = Cache.ProductivityInformation,
                                        OpticsApodizationModeEnum = opticsApodizationModeEnum,
                                        OpticsPolarizationModeEnum = opticsPolarizationModeEnum,
                                        CollectorPolarizationModeEnum = collectorPolarizationModeEnum,
                                        Items = [..cibInformations.Select(t => new CIBLightMatchingDTOItem { CIBInformation = t })]
                                    }
                                    : CalibratingItems.Single(t => t.OpticsIlluminationModeEnum == Cache.OpticsIlluminationModeEnum
                                                                   && t.ProductivityInformation == Cache.ProductivityInformation
                                                                   && t.OpticsApodizationModeEnum == opticsApodizationModeEnum
                                                                   && t.OpticsPolarizationModeEnum == opticsPolarizationModeEnum
                                                                   && t.CollectorPolarizationModeEnum == collectorPolarizationModeEnum);

                                if (isHaze) CalibratingItems = [..CalibratingItems, item];
                                else
                                {
                                    if (item.IsCalibrated == false) break;
                                }

                                Logger.LogHtmlInformation($"{item.OpticsApodizationModeEnum.Humanize()}, {item.OpticsPolarizationModeEnum.Humanize()}, {item.CollectorPolarizationModeEnum.Humanize()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                                {
                                    cibInformations
                                }), HtmlLogUniqueId.LoggingHtml());

                                var times = 0;
                                while (true)
                                {
                                    var cibPMTValues = await CIBViewModel.GetPMTValuesAsync(
                                        Cache.OpticsIlluminationModeEnum,
                                        Cache.ProductivityInformation,
                                        StageCoordinateSystemEnum.Dark,
                                        bfPosition,
                                        cibInformations,
                                        Cache.Item.ImageWidth,
                                        true,
                                        cancellationToken);

                                    await Task.WhenAll(cibPMTValues.Index().Select(t => Task.Run(() =>
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        var (index, darkFieldImage) = t;
                                        using var _ = darkFieldImage;

                                        var shorts = darkFieldImage.Matrix.AsSpan();
                                        double sum = 0;
                                        foreach (var v in shorts) sum += v;
                                        var pmtValue = sum / shorts.Length;

                                        var itemItem = item.Items[index];
                                        if (isHaze) itemItem.HazeItems = [..itemItem.HazeItems, new CIBLightMatchingDTOItem.Item { Value = pmtValue }];
                                        else itemItem.SilicaSphereItems = [..itemItem.SilicaSphereItems, new CIBLightMatchingDTOItem.Item { Value = pmtValue }];
                                    }, cancellationToken)));

                                    var results = (
                                        from itemItem in item.Items
                                        group itemItem by itemItem.CIBInformation.ChannelId
                                        into g
                                        orderby g.Key
                                        select (
                                            ChannelId: g.Key,
                                            Items: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
                                        )).ToArray();

                                    var resultList = new List<bool>();
                                    if (isHaze)
                                    {
                                        foreach (var (_, items) in results)
                                        {
                                            var channelIdAverage = items.Average(t => t.HazeItems[times].Value);

                                            foreach (var t in items)
                                            {
                                                t.HazeItems[times].Ratio = (t.HazeItems[times].Value - channelIdAverage) / channelIdAverage;
                                                t.DigitalGain = channelIdAverage / t.HazeItems[times].Value;
                                                CIBViewModel.SetLightMatching([t.CIBInformation], t.DigitalGainPlusMultiplicativeFactors);

                                                resultList.Add(t.HazeItems[times].AbsRatio <= Cache.HazeCalibratingThreshold);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var average = results
                                            .SelectMany(t => t.Items)
                                            .Select(t => t.SilicaSphereItems[times].Value)
                                            .Average();

                                        foreach (var (_, items) in results)
                                        {
                                            var channelIdAverage = items.Average(t => t.SilicaSphereItems[times].Value);
                                            var ratio = (channelIdAverage - average) / average;
                                            var multiplicativeFactors = average / channelIdAverage;
                                            foreach (var t in items)
                                            {
                                                t.SilicaSphereItems[times].Ratio = ratio;
                                                t.MultiplicativeFactors = multiplicativeFactors;
                                                CIBViewModel.SetLightMatching([t.CIBInformation], t.DigitalGainPlusMultiplicativeFactors);

                                                resultList.Add(t.SilicaSphereItems[times].AbsRatio <= Cache.SilicaSphereCalibratingThreshold);
                                            }
                                        }
                                    }

                                    var htmlBullet = new HtmlBullet(new
                                    {
                                        times,
                                        item.OpticsIlluminationModeEnum,
                                        item.ProductivityInformation,
                                        item.OpticsApodizationModeEnum,
                                        item.OpticsPolarizationModeEnum,
                                        item.CollectorPolarizationModeEnum,
                                        SuccessPlot = new HtmlContainer([.. item.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                                    });

                                    if (isHaze) item.IsCalibrated = resultList.All(t => t);
                                    else item.IsCalibrated = item.IsCalibrated && resultList.All(t => t);

                                    if (item.IsCalibrated)
                                    {
                                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                        break;
                                    }

                                    if (isHaze
                                            ? ++times > Cache.HazeCalibratingRetryTimes - 1
                                            : ++times > Cache.SilicaSphereCalibratingRetryTimes - 1)
                                    {
                                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                                        break;
                                    }

                                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                OpticsViewModel.SetApodizationMode(currentOpticsApodizationModeEnum);
                OpticsViewModel.SetPolarizationMode(currentOpticsPolarizationModeEnum);
                CollectorViewModel.SetPolarizationMode(currentCollectorPolarizationModeEnum);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems)
            {
                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    SuccessPlot = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                var title = $"{selectedReviewItem.OpticsIlluminationModeEnum.Humanize()}, {selectedReviewItem.ProductivityInformation}, {selectedReviewItem.OpticsApodizationModeEnum.Humanize()}, {selectedReviewItem.OpticsPolarizationModeEnum.Humanize()}, {selectedReviewItem.CollectorPolarizationModeEnum.Humanize()}";

                if (selectedReviewItem.IsVerified)
                    Logger.LogHtmlInformation($"OK: {title}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlError($"Error: {title}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
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

    private bool Save(IReadOnlyList<CIBLightMatchingDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations
                    .Where(t => t.OpticsIlluminationModeEnum != Cache.OpticsIlluminationModeEnum
                                || t.ProductivityInformation != Cache.ProductivityInformation),
                dto.Clone()
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}