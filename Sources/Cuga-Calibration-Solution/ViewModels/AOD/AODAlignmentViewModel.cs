using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using Net.Utilities.Helpers.Helpers.Files;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODAlignmentViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public string AODWaveformDirectoryPath => Path.Combine(AppHomeDirectory, "AODWaveform", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ResultAODWaveformDirectoryPath => Path.Combine(AppHomeDirectory, "Result", "AODWaveform", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Find Position" },
        new() { StepName = "AOD Alignment" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AODAlignmentDto _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<AODAlignmentDto> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<AODAlignmentDto> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private AODAlignmentCache _cache = new();

    [ObservableProperty]
    private AODAlignmentDto[] _calibrations = [];

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
                .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<AODAlignmentCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<AODAlignmentDto>();

        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibrationStatuses.Single(tt => tt.ProductivityInformation == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

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

        return Reviews.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new AODAlignmentDto();

                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition != Point.Origin
                    ? Cache.Item.FindBFMachinePosition
                    : MicroscopeCalChip.HazeBrightFieldMachinePosition));

                return true;

            case 1:
                return true;

            case 2:
                CalibrationStatuses.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"AOD Alignment {Cache.ProductivityInformation} Ok!");

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
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.FindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var detectImageDirectory = ImageFileDirectory;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                detectImageDirectory,
                Cache.ProductivityInformation,
                Cache.Item.FindBFMachinePosition,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.LaserLightInformation,
                Cache.Item.PMTId,
                Cache.Item.ChannelId,
                Cache.Item.ImageWidth,
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.Item.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
                Cache.Item.StartPrescanFrequency,
                Cache.Item.StepPrescanFrequency,
                Cache.Item.StopPrescanFrequency
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem = new AODAlignmentDto
            {
                ProductivityInformation = Cache.ProductivityInformation,
                PMTId = Cache.Item.PMTId,
                ChannelId = Cache.Item.ChannelId
            };
            Cache.Item.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;

            var yPixelHeight = LaserViewModel.GetDarkFieldLineScanImageYPixelHeight(Cache.ProductivityInformation);

            foreach (var prescanFrequency in Generate.LinearRange(Cache.Item.StartPrescanFrequency, Cache.Item.StepPrescanFrequency, Cache.Item.StopPrescanFrequency))
            {
                var item = new AODAlignmentItemDto { PrescanFrequency = prescanFrequency };

                cancellationToken.ThrowIfCancellationRequested();

                var generatePrescanAODWaveformParam = Cache.Item.GeneratePrescanAODWaveformParam.Clone();

                generatePrescanAODWaveformParam.WithFrequencyFlatness(prescanFrequency);
                generatePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                var (aodWaveformResultItem, exceptionItem) = AODWaveformGenerator.GeneratePrescanAODWaveform(generatePrescanAODWaveformParam.AdaptTo(), cancellationToken);
                if (aodWaveformResultItem.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exceptionItem);

                item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResultItem);
                item.PrescanAODWaveformResultFilePath = aodWaveformResultItem.FilePath;

                LaserViewModel.SetPrescanAODWaveProfiles(item.PrescanAODWaveformProfiles);

                using var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                    CalChipSiteModelEnum.HazeModel,
                    StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition),
                    (true, null),
                    false,
                    Cache.Item.CIBConfiguration,
                    Cache.ProductivityInformation,
                    xWidthPixel: Cache.Item.ImageWidth,
                    pmtId: Cache.Item.PMTId,
                    channelId: Cache.Item.ChannelId,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);

                item.ImageFilePath = $"{detectImageDirectory}\\{item.PrescanFrequency:0.###}_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg";
                darkFieldImageDto.Image.Save(item.ImageFilePath);
                item.ImageProjectionYs = darkFieldImageDto.ProjectionYs;
                item.ImageProjectionYsMaxPixel = Vector<double>.Build.DenseOfEnumerable(item.ImageProjectionYs).MaximumIndex();

                CalibratingItem.Items = [.. CalibratingItem.Items, item];

                Logger.LogHtmlInformation($"{item.PrescanFrequency:0.###}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    generatePrescanAODWaveformParam = new HtmlQuote(generatePrescanAODWaveformParam.ToHtmlAnonymous()),
                    item.PrescanAODWaveformResultFilePath,
                    PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                    HtmlImage = new HtmlImage(item.ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    ImageProjectionYs = new HtmlPlot2DLinesChart([(string.Empty, item.ImageProjectionYs.ToPoints()),], string.Empty),
                    item.ImageProjectionYsMaxPixel
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var itemPoints = CalibratingItem.ItemPoints.Skip(1).SkipLast(1).ToArray();
            var (slope, intercept, rSquared, yPredicted) = PolynomialLeastSquares.Polynomial1Fit(Vector<double>.Build.DenseOfEnumerable(itemPoints.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(itemPoints.Select(t => t.Y)));
            CalibratingItem.Slope = slope;
            CalibratingItem.Intercept = intercept;
            CalibratingItem.RSquared = rSquared;
            CalibratingItem.ItemFitPoints = [.. itemPoints.Select((t, i) => new Point(t.X, yPredicted[i]))];

            Cache.Item.GeneratePrescanAODWaveformParam.BandWidth = Math.Abs(yPixelHeight / CalibratingItem.Slope);
            Cache.Item.GeneratePrescanAODWaveformParam.CenterFrequency = (yPixelHeight / 2d - CalibratingItem.Intercept) / CalibratingItem.Slope;
            Cache.Item.GeneratePrescanAODWaveformParam.FlatnessTime = yPixelHeight * 4d;
            Cache.Item.GeneratePrescanAODWaveformParam.DirectoryPath = ResultAODWaveformDirectoryPath;

            var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.Item.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
            if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

            CalibratingItem.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
            CalibratingItem.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.Item.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
                CalibratingItem.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. CalibratingItem.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                Result = new HtmlPlot2DLinesChart([
                    (nameof(CalibratingItem.ItemPoints), CalibratingItem.ItemPoints),
                    (nameof(CalibratingItem.ItemFitPoints), CalibratingItem.ItemFitPoints)
                ], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
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
            foreach (var selectedReviewItem in SelectedReviewItems)
            {
                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                selectedReviewItem.IsVerified = true;

                Guard.IsTrue(Save(selectedReviewItem, cancellationToken));

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Result = new HtmlPlot2DLinesChart([
                        (nameof(selectedReviewItem.ItemPoints), selectedReviewItem.ItemPoints),
                        (nameof(selectedReviewItem.ItemFitPoints), selectedReviewItem.ItemFitPoints)
                    ], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());
            }


            DialogWindowProvider.ShowDialog("Verify OK");

            return true;
        }).ConfigureAwait(false);
    }

    private bool Save(AODAlignmentDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
            dto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}