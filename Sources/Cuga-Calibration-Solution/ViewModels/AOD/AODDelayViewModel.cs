using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODDelayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
[DefaultCache(typeof(AODDelayDTO), true)]
[RecipeCache(typeof(AODDelayCache))]
public sealed partial class AODDelayViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "AOD Delay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AODDelayDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<AODDelayDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<AODDelayDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private AODDelayCache _cache = new();

    [ObservableProperty]
    private AODDelayDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDto>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses = [.. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t })];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<AODDelayCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<AODDelayDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibratingStatuses
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
                .OrderBy(t => t.ProductivityInformation)
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition));

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
                CalibratingItem = new AODDelayDTO();

                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 2:

                return true;

            case 3:
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .IsCalibrated = true;

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
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition
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

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.WaitTime,
                Cache.Item.StartRoughAODDelay,
                Cache.Item.StepRoughAODDelay,
                Cache.Item.StopRoughAODDelay,
                Cache.Item.RangeRefinedAODDelay,
                Cache.Item.StepRefinedAODDelay
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.Items = [];
            CalibratingItem.MaxItem = null;
            CalibratingItem.IsCalibrated = false;

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);

            try
            {
                Logger.LogHtmlInformation("AOD Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                await CatchAODDelayAsync(Generate.LinearRange(Cache.Item.StartRoughAODDelay, Cache.Item.StepRoughAODDelay, Cache.Item.StopRoughAODDelay));
                GuardUtils.IsNotNullAndReturn(CalibratingItem.MaxItem);

                await CatchAODDelayAsync(Generate.LinearRange(
                    CalibratingItem.MaxItem.AODDelay - Cache.Item.RangeRefinedAODDelay,
                    Cache.Item.StepRefinedAODDelay,
                    CalibratingItem.MaxItem.AODDelay + Cache.Item.RangeRefinedAODDelay));
                GuardUtils.IsNotNullAndReturn(CalibratingItem.MaxItem);

                CalibratingItem.IsCalibrated = true;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CalibratingItem.PrescanAODDelay,
                    CalibratingItem.ChirpAODDelay,
                    CalibratingItem.MaxItem.PMTValue,
                    CalibratingItem.MaxItem.RawImageFilePath,
                    Image = new HtmlImage(CalibratingItem.MaxItem.ImageFilePath),
                    ScatterPlotControl = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                }), HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;

                async Task CatchAODDelayAsync(IReadOnlyList<double> aodDelays)
                {
                    Guard.IsNotEmpty(aodDelays);

                    foreach (var aodDelay in aodDelays)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Logger.LogHtmlInformation($"{aodDelay:0.###}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                        var itemItem = new AODDelayDTOItem { AODDelay = aodDelay };

                        LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Coefficient);
                        LaserViewModel.SetAODDelayValue(Cache.ProductivityInformation, itemItem.PrescanAODDelay, itemItem.ChirpAODDelay);

                        await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

                        using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                            Cache.ProductivityInformation,
                            StageCoordinateSystemEnum.Dark,
                            hazeBFPosition,
                            Cache.Item.CIBInformation,
                            Cache.Item.ImageWidth,
                            (false, CalChipSiteModelEnum.HazeModel),
                            (false, Cache.Item.CIBConfiguration),
                            (true, null),
                            true,
                            cancellationToken);

                        var imageFilePath = Path.Combine(detectImageDirectory, $"{itemItem.AODDelay:0.###}", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.Save(imageFilePath);

                        itemItem.ImageFilePath = imageFilePath;
                        itemItem.RawImageFilePath = darkFieldImage.RawImageFilePath;
                        itemItem.PMTValue = darkFieldImage.Image.GetIntensity().Average;

                        CalibratingItem.Items = [.. ((IReadOnlyList<AODDelayDTOItem>)[.. CalibratingItem.Items, itemItem]).OrderBy(t => t.AODDelay)];

                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            itemItem.PrescanAODDelay,
                            itemItem.ChirpAODDelay,
                            itemItem.PMTValue,
                            itemItem.RawImageFilePath,
                            Image = new HtmlImage(itemItem.ImageFilePath)
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }
            }
            finally
            {
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);
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

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.ProductivityInformation.ToString();

                /*if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }*/

                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.PrescanAODDelay,
                    selectedReviewItem.ChirpAODDelay,
                    selectedReviewItem.MaxItem?.PMTValue,
                    selectedReviewItem.MaxItem?.RawImageFilePath,
                    Image = string.IsNullOrWhiteSpace(selectedReviewItem.MaxItem?.ImageFilePath) ? (BaseHtmlElement)new HtmlComment("The image was not saved. For details, see the raw file path.") : new HtmlImage(GuardUtils.IsNotNullAndReturn(selectedReviewItem.MaxItem).ImageFilePath),
                    ScatterPlotControl = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
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

    private bool Save(IReadOnlyList<AODDelayDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}