using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODDelayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODDelayViewModel : CalibrationViewModelBase<AODDelayCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "AOD Delay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial AODDelayDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<AODDelayDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<AODDelayDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial AODDelayCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial AODDelayDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken: cancellationToken);

        Cache = ApplicationCookieService.GetCache<AODDelayCache>(cancellationToken: cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<AODDelayDTO>(cancellationToken: cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

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
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.HazeFindBFMachinePosition == Point.Origin
                        ? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)
                        : Cache.Item.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new AODDelayDTO();

                return true;

            case 1:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.HazeFindBFMachinePosition == Point.Origin
                        ? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)
                        : Cache.Item.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);

                return true;

            case 2:
                return true;

            case 3:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
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

            StageViewModel.SetAbsoluteStageTheta(0d);
            Cache.Item.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
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
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.StartRoughAODDelay,
                Cache.Item.StepRoughAODDelay,
                Cache.Item.StopRoughAODDelay,
                Cache.Item.RangeRefinedAODDelay,
                Cache.Item.StepRefinedAODDelay
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.Items = [];
            CalibratingItem.MaxItemAODDelay = null;
            CalibratingItem.IsCalibrated = false;

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(hazeBFPosition, CalChipSiteModelEnum.HazeModel);

            try
            {
                Logger.LogHtmlInformation("AOD Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var prescanCache = ApplicationCookieService.GetOrDefault<PrescanAODWaveformElectrodeDelayCache>(false, cancellationToken: cancellationToken);
                var prescanResult = prescanCache.Results.SingleOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                              && t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType);
                if (prescanResult is null)
                {
                    const string comment = "Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!";
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());

                    DialogWindowProvider.ShowDialog(comment, dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Error);

                    return false;
                }

                CalibratingItem.PrescanAODDelay = prescanResult.GeneratePrescanAODWaveformParam.ZeroSampleCount;

                await CatchAODDelayAsync(Generate.LinearRange(Cache.Item.StartRoughAODDelay, Cache.Item.StepRoughAODDelay, Cache.Item.StopRoughAODDelay));

                Algorithm(CalibratingItem);
                Guard.IsNotNull(CalibratingItem.MaxItemAODDelay);

                await CatchAODDelayAsync(Generate.LinearRange(
                    CalibratingItem.MaxItemAODDelay.Value - Cache.Item.RangeRefinedAODDelay < 0d ? 0d : CalibratingItem.MaxItemAODDelay.Value - Cache.Item.RangeRefinedAODDelay,
                    Cache.Item.StepRefinedAODDelay,
                    CalibratingItem.MaxItemAODDelay.Value + Cache.Item.RangeRefinedAODDelay));

                Algorithm(CalibratingItem);
                CalibratingItem.IsCalibrated = true;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CalibratingItem.PrescanAODDelay,
                    CalibratingItem.ChirpAODDelay,
                    PlotDataSource = new HtmlContainer([.. CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
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

                        var itemItem = new AODDelayDTOItem { ChirpAODDelay = aodDelay };

                        LaserViewModel.SetAODDelayValue(Cache.ProductivityInformation, CalibratingItem.PrescanAODDelay, Cache.Item.LaserLightInformation.Coefficient, itemItem.ChirpAODDelay);

                        using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                            Cache.ProductivityInformation,
                            StageCoordinateSystemEnum.Dark,
                            hazeBFPosition,
                            Cache.Item.ImageWidth,
                            Cache.Item.CIBInformation,
                            (false, CalChipSiteModelEnum.HazeModel),
                            (false, Cache.Item.OpticsConfiguration),
                            (false, Cache.Item.CIBConfiguration),
                            (true, null),
                            true,
                            cancellationToken);

                        var imageFilePath = Path.Combine(detectImageDirectory, $"{itemItem.ChirpAODDelay:0.###}", $"{DateTimeHelper.DateTime2String(DateTime.Now, format: Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.SaveImage(imageFilePath);

                        itemItem.ImageFilePath = imageFilePath;
                        itemItem.RawImageFilePath = darkFieldImage.RawImageFilePath;

                        using var hImage = darkFieldImage.Image.ToHImage();
                        itemItem.PMTValue = hImage.GetIntensity().Average;

                        CalibratingItem.Items = [.. CalibratingItem.Items.Append(itemItem).OrderBy(t => t.ChirpAODDelay)];

                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            CalibratingItem.PrescanAODDelay,
                            itemItem.ChirpAODDelay,
                            itemItem.PMTValue,
                            itemItem.RawImageFilePath,
                            Image = new HtmlImage(itemItem.ImageFilePath)
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }

                void Algorithm(AODDelayDTO aodDelay)
                {
                    var points = aodDelay.Items.Select(t => new Point(t.ChirpAODDelay, t.PMTValue)).ToArray();
                    aodDelay.MaxItemAODDelay = points.Maxima(t => t.Y).First().X;
                }
            }
            finally
            {
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(hazeBFPosition, CalChipSiteModelEnum.HazeModel);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);

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
                    PlotDataSource = new HtmlContainer([.. selectedReviewItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
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
                dialogButtonsEnum: DialogButtonsEnum.OK,
                dialogIconEnum: result ? DialogIconEnum.Information : DialogIconEnum.Warning);

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
                dto.Clone(),
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken: cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken: cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<AODDelayDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .DistinctBy(t => t.ProductivityInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.OpticsMagTypeProductivityInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.ProductivityInformation == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}