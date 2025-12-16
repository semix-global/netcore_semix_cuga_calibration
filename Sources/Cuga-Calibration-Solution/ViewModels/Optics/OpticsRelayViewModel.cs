using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Optics.Relay;
using Humanizer;
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
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(OpticsRelayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsRelayViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override string CalibrateFileName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Position" },
        new() { StepName = "Relay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private OpticsRelayDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<OpticsRelayDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsRelayDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private OpticsRelayCache _cache = new();

    [ObservableProperty]
    private OpticsRelayDTO[] _calibrations = [];

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
                .. ApplicationCookie.OpticsIlluminationModeEnums.Select(t => new OpticsIlluminationModeCalibrationStatus { SelectedItem = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<OpticsRelayCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<OpticsRelayDTO>();

        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationModeEnum))
                .Select(t =>
                {
                    CalibrationStatuses.Single(tt => tt.SelectedItem == t.OpticsIlluminationModeEnum).IsCalibrated = t.IsCalibrated;

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
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));

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
                CalibratingItem = new OpticsRelayDTO();

                return true;

            case 1:
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition != Point.Origin
                    ? Cache.Item.FindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.DswItem).BrightFieldMachinePosition));

                return true;

            case 2:
                return true;

            case 3:
                CalibrationStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"AOD Delay Offset {Cache.OpticsIlluminationModeEnum.Humanize()} Ok!");

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
            StageViewModel.SetAbsoluteStageTheta(0);
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.DswItem).BrightFieldMachinePosition));

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.GetProductivityInformations(Cache.OpticsIlluminationModeEnum).Contains(Cache.Item.ProductivityInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.FindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var currentMotorAbsoluteValue = OpticsViewModel.GetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum);
            var nmPerEcs = AfViewModel.GetNmPerEcs();
            var detectImageDirectory = ImageFileDirectory;
            // ECS/mm relay电机值增大, chuck焦点向下移动, chuck焦点向下移动 ecs增大 mm
            var defaultSlope = 1d / Cache.Item.DefaultRelayMotorRatio /* mm */
                               * 1e6d /* mm 转为 nm */
                               * Math.Cos(MathUtils.DegreeAngleToRadianAngle(Cache.Item.OpticsIlluminationDegreeAngle)) /* 转为垂直方向焦点移动的距离 */
                               / nmPerEcs; /* 转为 ECS */

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.CIBInformation,
                Cache.Item.FindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.OpticsIlluminationDegreeAngle,
                Cache.Item.DefaultRelayMotorRatio,
                Cache.Item.StartRelayMotorAbsoluteValue,
                Cache.Item.StepRelayMotorAbsoluteValue,
                Cache.Item.StopRelayMotorAbsoluteValue,
                Cache.Item.StartRoughECS,
                Cache.Item.StepRoughECS,
                Cache.Item.StopRoughECS,
                Cache.Item.RangeRefinedECS,
                Cache.Item.StepRefinedECS,
                Cache.Threshold,
                currentMotorAbsoluteValue,
                nmPerEcs,
                detectImageDirectory,
                defaultSlope
            }), HtmlLogUniqueId.LoggingHtml());

            var brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(brightFieldPosition);

            CalibratingItem.OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum;
            CalibratingItem.Items = [];
            CalibratingItem.Slope = 0d;
            CalibratingItem.Intercept = 0d;
            CalibratingItem.RSquared = 0d;
            CalibratingItem.RelayMotorRatio = 0d;
            CalibratingItem.FitRelayPoints = [];
            CalibratingItem.MinRelayMotorAbsoluteValue = 0d;
            CalibratingItem.MaxRelayMotorAbsoluteValue = 0d;
            CalibratingItem.IsCalibrated = false;

            try
            {
                Logger.LogHtmlInformation("Relay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                AfViewModel.ToggleBrightFieldEnable(false);

                var relayMotorAbsoluteValues = Generate.LinearRange(Cache.Item.StartRelayMotorAbsoluteValue, Cache.Item.StepRelayMotorAbsoluteValue, Cache.Item.StopRelayMotorAbsoluteValue);
                foreach (var relayMotorAbsoluteValue in relayMotorAbsoluteValues)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Logger.LogHtmlInformation($"{relayMotorAbsoluteValue:0.###}mm", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, relayMotorAbsoluteValue);

                    var opticsRelayDTOItem = new OpticsRelayDTOItem { RelayMotorAbsoluteValue = relayMotorAbsoluteValue };
                    CalibratingItem.Items = [.. CalibratingItem.Items, opticsRelayDTOItem];

                    var deltaECS = (relayMotorAbsoluteValue - currentMotorAbsoluteValue) * defaultSlope;

                    CatchImage(Generate.LinearRange(
                        Cache.Item.StartRoughECS + deltaECS,
                        Cache.Item.StepRoughECS,
                        Cache.Item.StopRoughECS + deltaECS));
                    GuardUtils.IsNotNullAndReturn(opticsRelayDTOItem.MaxItem);

                    CatchImage(Generate.LinearRange(
                        opticsRelayDTOItem.MaxItem.ECS - Cache.Item.RangeRefinedECS,
                        Cache.Item.StepRefinedECS,
                        opticsRelayDTOItem.MaxItem.ECS + Cache.Item.RangeRefinedECS));
                    GuardUtils.IsNotNullAndReturn(opticsRelayDTOItem.MaxItem);

                    if (CalibratingItem.Items.Count > 1)
                    {
                        var (slope, intercept, rSquared, yPredicted) = PolynomialLeastSquares.Polynomial1Fit(
                            Vector<double>.Build.DenseOfEnumerable(CalibratingItem.Items.Select(t => t.RelayMotorAbsoluteValue)),
                            Vector<double>.Build.DenseOfEnumerable(CalibratingItem.Items.Select(t => GuardUtils.IsNotNullAndReturn(t.MaxItem).ECS)));

                        defaultSlope = slope;
                        CalibratingItem.Slope = slope;
                        CalibratingItem.Intercept = intercept;
                        CalibratingItem.RSquared = rSquared;
                        CalibratingItem.FitRelayPoints = [.. CalibratingItem.Items.Index().Select(t => new Point(t.Item.RelayMotorAbsoluteValue, yPredicted[t.Index]))];
                        CalibratingItem.RelayMotorRatio = 1 / (
                            CalibratingItem.Slope /* ECS/mm */
                            * nmPerEcs /* 分子 ECS 转为 nm */
                            / 1e6d /* 分母mm 转为 nm */
                            / Math.Cos(MathUtils.DegreeAngleToRadianAngle(Cache.Item.OpticsIlluminationDegreeAngle)) /* 转为照明方向移动的距离 */
                        );
                    }

                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        defalutSlope = defaultSlope,
                        opticsRelayDTOItem.MaxItem.ECS,
                        opticsRelayDTOItem.MaxItem.Quality,
                        opticsRelayDTOItem.MaxItem.RawImageFilePath,
                        Image = new HtmlImage(opticsRelayDTOItem.MaxItem.ImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());

                    continue;

                    void CatchImage(IReadOnlyList<double> ecses)
                    {
                        Guard.IsNotEmpty(ecses);

                        var currentDetectImageDirectory = Path.Combine(detectImageDirectory, $"{relayMotorAbsoluteValue:0.###}mm_[{ecses[0]:0.###}ECS, {ecses[^1]:0.###}ECS]_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat)}");

                        foreach (var ecs in ecses)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            Logger.LogHtmlInformation($"{ecs:0.###}ECS", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                            AfViewModel.SetSensorEcsValue(ecs);

                            using var darkFieldImage = LaserViewModel.GetDarkFieldLineScanImage(
                                Cache.OpticsIlluminationModeEnum,
                                Cache.Item.ProductivityInformation,
                                CalChipSiteModelEnum.DswModel,
                                StageCoordinateSystemEnum.Dark,
                                brightFieldPosition,
                                (false, Cache.Item.LaserLightInformation),
                                false,
                                Cache.Item.CIBInformation,
                                Cache.Item.CIBConfiguration,
                                Cache.Item.ImageWidth,
                                isAutoFocus: false);

                            var quality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImage.Image);

                            var filePath = Path.Combine(currentDetectImageDirectory, $"{ecs:0.###}ECS_{quality:0.###}Quality_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                            darkFieldImage.Image.Save(filePath);

                            var item = new OpticsRelayDTOItem.Item { ECS = ecs, Quality = quality, ImageFilePath = filePath, RawImageFilePath = darkFieldImage.RawImageFilePath };
                            opticsRelayDTOItem.Qualitys = [.. ((IReadOnlyList<OpticsRelayDTOItem.Item>)[.. opticsRelayDTOItem.Qualitys, item]).OrderBy(t => t.ECS)];

                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                            {
                                item.ECS,
                                item.Quality,
                                item.ImageFilePath,
                                item.RawImageFilePath
                            }), HtmlLogUniqueId.LoggingHtml());
                        }
                    }
                }

                CalibratingItem.MinRelayMotorAbsoluteValue = CalibratingItem.FitRelayPoints[0].X;
                CalibratingItem.MaxRelayMotorAbsoluteValue = CalibratingItem.FitRelayPoints[^1].X;
                CalibratingItem.IsCalibrated = CalibratingItem.RSquared >= Cache.Threshold;

                var htmlBullet = new HtmlBullet(new
                {
                    CalibratingItem.Slope,
                    CalibratingItem.Intercept,
                    CalibratingItem.RSquared,
                    CalibratingItem.RelayMotorRatio,
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
                OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, currentMotorAbsoluteValue);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(brightFieldPosition);
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
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems)
            {
                selectedReviewItem.IsCalibrated = selectedReviewItem.RSquared >= Cache.Threshold;
                selectedReviewItem.IsVerified = selectedReviewItem.IsCalibrated;

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.Slope,
                    selectedReviewItem.Intercept,
                    selectedReviewItem.RSquared,
                    selectedReviewItem.RelayMotorRatio,
                    selectedReviewItem.IsVerified,
                    SuccessPlot = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (selectedReviewItem.IsOk)
                    Logger.LogHtmlInformation($"OK: {selectedReviewItem.OpticsIlluminationModeEnum.Humanize()}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{selectedReviewItem.OpticsIlluminationModeEnum.Humanize()}: {selectedReviewItem.IsOk}");
                    Logger.LogHtmlError($"Error: {selectedReviewItem.OpticsIlluminationModeEnum.Humanize()}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
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

    private bool Save(IReadOnlyList<OpticsRelayDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.OpticsIlluminationModeEnum != dto.OpticsIlluminationModeEnum),
                dto.Clone()
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}