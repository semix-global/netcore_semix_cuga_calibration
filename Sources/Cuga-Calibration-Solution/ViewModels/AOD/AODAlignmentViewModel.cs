using System.IO;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Humanizer;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODAlignmentViewModel : CalibrationViewModelBase
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
        new() { StepName = "AOD Alignment" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AODAlignmentDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeAndProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<AODAlignmentDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<AODAlignmentDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private AODAlignmentCache _cache = new();

    [ObservableProperty]
    private AODAlignmentDTO[] _calibrations = [];

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
                ..ApplicationCookie.OpticsIlluminationModeEnums
                    .Select(t => new OpticsIlluminationModeAndProductivityInformationCalibrationStatus
                    {
                        SelectedItem = t,
                        ProductivityInformationCalibrationStatusList = [.. ProductivityInformationCalibrationStatus.CreateList(ApplicationCookie.GetProductivityInformations(t))]
                    })
            ];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<AODAlignmentCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<AODAlignmentDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationModeEnum)
                            && ApplicationCookie.GetProductivityInformations(t.OpticsIlluminationModeEnum).Contains(t.ProductivityInformation))
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

                return true;

            case 1:
                CalibratingItem = new AODAlignmentDTO();

                return true;

            case 2:
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 3:
                CalibrationStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum)
                    .ProductivityInformationCalibrationStatusList
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation).IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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

            return ApplicationCookie.OpticsIlluminationModeEnums.Contains(Cache.OpticsIlluminationModeEnum);
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

            return ApplicationCookie.GetProductivityInformations(Cache.OpticsIlluminationModeEnum).Contains(Cache.ProductivityInformation);
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
                Cache.Item.LaserLightInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.CIBInformation
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
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.CIBInformation,
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
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.CIBInformation,
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.Item.StartPrescanFrequency,
                Cache.Item.StepPrescanFrequency,
                Cache.Item.StopPrescanFrequency,
                Cache.Item.RangeSkipFitCount,
                Cache.Threshold,
                detectImageDirectory,
                Cache.ProductivityInformation.YPixel
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum;
            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.Items = [];
            CalibratingItem.Slope = 0d;
            CalibratingItem.Intercept = 0d;
            CalibratingItem.RSquared = 0d;
            CalibratingItem.FitAlignmentPoints = [];

            Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;

            Logger.LogHtmlInformation("Alignment", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            foreach (var prescanFrequency in Generate.LinearRange(Cache.Item.StartPrescanFrequency, Cache.Item.StepPrescanFrequency, Cache.Item.StopPrescanFrequency))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var generatePrescanAODWaveformParam = Cache.Item.FlatnessGeneratePrescanAODWaveformParam.Clone();

                generatePrescanAODWaveformParam.WithFrequencyFlatness(prescanFrequency);
                generatePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                var (aodWaveformResultItem, exceptionItem) = AODWaveformGenerator.GeneratePrescanAODWaveform(generatePrescanAODWaveformParam.AdaptTo(), cancellationToken);
                if (aodWaveformResultItem.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exceptionItem);

                var itemItem = new AODAlignmentDTOItem
                {
                    PrescanFrequency = prescanFrequency,
                    PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResultItem),
                    PrescanAODWaveformResultFilePath = aodWaveformResultItem.FilePath
                };

                LaserViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, itemItem.PrescanAODWaveformProfiles);

                using var darkFieldImage = LaserViewModel.GetDarkFieldLineScanImage(
                    Cache.OpticsIlluminationModeEnum,
                    Cache.ProductivityInformation,
                    CalChipSiteModelEnum.HazeModel,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition),
                    (true, null),
                    false,
                    Cache.Item.CIBInformation,
                    Cache.Item.CIBConfiguration,
                    Cache.Item.ImageWidth);

                var imageFilePath = Path.Combine(detectImageDirectory, $"{itemItem.PrescanFrequency:0.###}", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                darkFieldImage.Image.Save(imageFilePath);

                itemItem.ImageFilePath = imageFilePath;
                itemItem.RawImageFilePath = darkFieldImage.RawImageFilePath;
                itemItem.ImageHorizontalProjects = darkFieldImage.Image.GetHorizontalProjects();

                CalibratingItem.Items = [.. CalibratingItem.Items, itemItem];

                Logger.LogHtmlInformation($"{itemItem.PrescanFrequency:0.###}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                    itemItem.PrescanAODWaveformResultFilePath,
                    PrescanAODWaveformProfiles = new HtmlTable([.. itemItem.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                    itemItem.RawImageFilePath,
                    HtmlImage = new HtmlImage(itemItem.ImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var skipItemItems = CalibratingItem.Items.Skip(Cache.Item.RangeSkipFitCount).SkipLast(Cache.Item.RangeSkipFitCount).ToArray();
            var (slope, intercept, rSquared, yPredicted) = PolynomialLeastSquares.Polynomial1Fit(
                Vector<double>.Build.DenseOfEnumerable(skipItemItems.Select(t => t.PrescanFrequency)),
                Vector<double>.Build.DenseOfEnumerable(skipItemItems.Select(t => (double)GuardUtils.IsNotNullAndReturn(t.ImageHorizontalProjectsMaxPixel))));

            CalibratingItem.Slope = slope;
            CalibratingItem.Intercept = intercept;
            CalibratingItem.RSquared = rSquared;
            CalibratingItem.FitAlignmentPoints = [.. skipItemItems.Index().Select(t => new Point(t.Item.PrescanFrequency, yPredicted[t.Index]))];
            CalibratingItem.IsCalibrated = CalibratingItem.RSquared >= Cache.Threshold;

            var htmlBullet = new HtmlBullet(new
            {
                CalibratingItem.Slope,
                CalibratingItem.Intercept,
                CalibratingItem.RSquared,
                ScatterPlotControl = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            });

            if (CalibratingItem.IsCalibrated)
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            else
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

            Guard.IsTrue(Save([CalibratingItem], cancellationToken));

            return CalibratingItem.IsCalibrated;
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
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems)
            {
                var title = $"{selectedReviewItem.OpticsIlluminationModeEnum.ToDescriptionOrString()}_{selectedReviewItem.ProductivityInformation}";

                /*if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }*/

                Cache.OpticsIlluminationModeEnum = selectedReviewItem.OpticsIlluminationModeEnum;
                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.OpticsIlluminationModeEnum,
                    Cache.ProductivityInformation,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.Slope,
                    selectedReviewItem.Intercept,
                    selectedReviewItem.RSquared,
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

    private bool Save(IReadOnlyList<AODAlignmentDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.OpticsIlluminationModeEnum != dto.OpticsIlluminationModeEnum
                                           || t.ProductivityInformation != dto.ProductivityInformation),
                dto.Clone()
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}