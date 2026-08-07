using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.HardwareType;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.INC;
using MathNet.Numerics;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(OpticsINCViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsINCViewModel : CalibrationViewModelBase<OpticsINCCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "INC" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial OpticsINCDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<OpticsINCDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OpticsINCDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial OpticsINCCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial OpticsINCDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        Guard.IsNotNull(ApplicationCookie.HardwareStateConfig);

        if (ApplicationCookie.OpticsIlluminationModeEnums.Contains(OpticsIlluminationModeEnum.OI) &&
            ApplicationCookie.HardwareStateConfig.MotorHardwares[HardwareMotorTypeEnum.OIINC].Enabled == false)
        {
            DialogWindowProvider.ShowDialog("Please enable the OI INC motor!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (ApplicationCookie.OpticsIlluminationModeEnums.Contains(OpticsIlluminationModeEnum.NI) &&
            ApplicationCookie.HardwareStateConfig.MotorHardwares[HardwareMotorTypeEnum.NIINC].Enabled == false)
        {
            DialogWindowProvider.ShowDialog("Please enable the NI INC motor!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<OpticsINCCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<OpticsINCDTO>(cancellationToken);

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
                StageViewModel.SetAbsoluteStageTheta(0d);
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
                CalibratingItem = new OpticsINCDTO();

                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

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

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation);
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

            var currentMotorAbsoluteValue = OpticsViewModel.GetINCMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum);

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
                Cache.Item.StartRoughINCMotorAbsoluteValue,
                Cache.Item.StepRoughINCMotorAbsoluteValue,
                Cache.Item.StopRoughINCMotorAbsoluteValue,
                Cache.Item.RangeRefinedINCMotorAbsoluteValue,
                Cache.Item.StepRefinedINCMotorAbsoluteValue,
                Cache.SmoothWindowSize,
                currentMotorAbsoluteValue,
                detectImageDirectory
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
                Logger.LogHtmlInformation("INC", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                await CatchINCAsync(Generate.LinearRange(
                    Cache.Item.StartRoughINCMotorAbsoluteValue,
                    Cache.Item.StepRoughINCMotorAbsoluteValue,
                    Cache.Item.StopRoughINCMotorAbsoluteValue));

                Algorithm(CalibratingItem);
                Guard.IsNotNull(CalibratingItem.MaxItem);

                await CatchINCAsync(Generate.LinearRange(
                    CalibratingItem.MaxItem.INCMotorAbsoluteValue - Cache.Item.RangeRefinedINCMotorAbsoluteValue,
                    Cache.Item.StepRefinedINCMotorAbsoluteValue,
                    CalibratingItem.MaxItem.INCMotorAbsoluteValue + Cache.Item.RangeRefinedINCMotorAbsoluteValue));

                Algorithm(CalibratingItem);
                CalibratingItem.IsCalibrated = true;

                var htmlBullet = new HtmlBullet(new
                {
                    CalibratingItem.MaxItem.INCMotorAbsoluteValue,
                    CalibratingItem.MaxItem.PMTValue,
                    CalibratingItem.MaxItem.RawImageFilePath,
                    HtmlImage = new HtmlImage(CalibratingItem.MaxItem.ImageFilePath),
                    ScatterPlotControl = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (CalibratingItem.IsCalibrated)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;

                async Task CatchINCAsync(IReadOnlyList<double> incMotorAbsoluteValues)
                {
                    Guard.IsNotEmpty(incMotorAbsoluteValues);

                    foreach (var incMotorAbsoluteValue in incMotorAbsoluteValues)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        OpticsViewModel.SetINCMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum, incMotorAbsoluteValue);

                        using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                            Cache.ProductivityInformation,
                            StageCoordinateSystemEnum.Dark,
                            hazeBFPosition,
                            Cache.Item.ImageWidth,
                            Cache.Item.CIBInformation,
                            (false, CalChipSiteModelEnum.HazeModel),
                            (false, Cache.Item.OpticsConfiguration),
                            (false, Cache.Item.CIBConfiguration),
                            (false, Cache.Item.LaserLightInformation),
                            false,
                            cancellationToken);

                        var imageFilePath = Path.Combine(detectImageDirectory, $"{incMotorAbsoluteValue:0.###}", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.SaveImage(imageFilePath);

                        using var hImage = darkFieldImage.Image.ToHImage();
                        var itemItem = new OpticsINCDTOItem
                        {
                            INCMotorAbsoluteValue = incMotorAbsoluteValue,
                            ImageFilePath = imageFilePath,
                            RawImageFilePath = darkFieldImage.RawImageFilePath,
                            PMTValue = hImage.GetIntensity().Average
                        };

                        CalibratingItem.Items = [.. ((IReadOnlyList<OpticsINCDTOItem>)[.. CalibratingItem.Items, itemItem]).OrderBy(t => t.INCMotorAbsoluteValue)];

                        Logger.LogHtmlInformation($"{incMotorAbsoluteValue:0.###}mm", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            itemItem.PMTValue,
                            itemItem.RawImageFilePath,
                            HtmlImage = new HtmlImage(itemItem.ImageFilePath)
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }
            }
            finally
            {
                OpticsViewModel.SetINCMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum, currentMotorAbsoluteValue);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AlgorithmAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0) return;

        await InvokeVerifyAsync(async () =>
        {
            Logger.LogHtmlInformation("Algorithm Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.SmoothWindowSize
            }), HtmlLogUniqueId.LoggingHtml());

            await Task.WhenAll(SelectedReviewItems.Select(inc => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                Algorithm(inc);
                inc.IsVerified = false;
                inc.IsCalibrated = true;

                Logger.LogHtmlInformation($"{inc.ProductivityInformation}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    inc.MaxItem?.INCMotorAbsoluteValue,
                    inc.MaxItem?.PMTValue,
                    ScatterPlotControl = new HtmlContainer([.. inc.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                }), HtmlLogUniqueId.LoggingHtml());
            }, cancellationToken)));

            var result = SelectedReviewItems.All(t => t.IsCalibrated);

            DialogWindowProvider.ShowDialog($"Algorithm {(result ? "OK" : "Failed")}",
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
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
                    selectedReviewItem.MaxItem?.INCMotorAbsoluteValue,
                    selectedReviewItem.MaxItem?.PMTValue,
                    selectedReviewItem.MaxItem?.RawImageFilePath,
                    Image = string.IsNullOrWhiteSpace(selectedReviewItem.MaxItem?.ImageFilePath)
                        ? (BaseHtmlElement)new HtmlComment("The image was not saved. For details, see the raw file path.")
                        : new HtmlImage(Guard.IsNotNullAndReturn(selectedReviewItem.MaxItem).ImageFilePath),
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

    private void Algorithm(OpticsINCDTO opticsINC)
    {
        var smoothPoints = Filter.MovMean([.. opticsINC.Items.Select(t => new Point(t.INCMotorAbsoluteValue, t.PMTValue))], Cache.SmoothWindowSize);

        double maxMotorAbsoluteValue;
        if (HostEnvironment.IsDevelopment())
        {
            maxMotorAbsoluteValue = smoothPoints.Maxima(t => t.Y).First().X;
        }
        else
        {
            var (_, results) = Extremumor.FindMaxima(smoothPoints);
            maxMotorAbsoluteValue = results.Maxima(t => t.Y).First().X;
        }

        opticsINC.MaxItem = opticsINC.Items.MinBy(t => Math.Abs(t.INCMotorAbsoluteValue - maxMotorAbsoluteValue));
    }

    private bool Save(IReadOnlyList<OpticsINCDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto,
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<OpticsINCDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .DistinctBy(t => t.ProductivityInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.ProductivityInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.ProductivityInformations.Select(productivityInformation =>
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
