using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.Relay;
using Core.Utilities.SourceGenerators.Attributes;
using Humanizer;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
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
        new() { StepName = "Find DSW Position" },
        new() { StepName = "Relay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private OpticsRelayDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<OpticsRelayDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsRelayDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private OpticsRelayCache _cache = new();

    [DefaultCache]
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

        await LoadDependsAsync(cancellationToken);

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDto>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses =
            [
                .. ApplicationCookie.OpticsIlluminationModeEnums.Select(t => new OpticsIlluminationModeStatus { SelectedItem = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<OpticsRelayCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<OpticsRelayDTO>();

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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
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
                CalibratingItem = new OpticsRelayDTO();

                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.DSWFindBFMachinePosition != Point.Origin
                    ? Cache.Item.DSWFindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.DswItem).BrightFieldMachinePosition));

                return true;

            case 2:
                return true;

            case 3:
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
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.DSWFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.DSWFindBFMachinePosition
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

            var currentMotorAbsoluteValue = OpticsViewModel.GetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum);
            var nmPerEcs = AfViewModel.GetNmPerEcs();
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
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.DSWFindBFMachinePosition,
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
                defaultSlope,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

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

            var dswBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.DSWFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(dswBFPosition);

            try
            {
                Logger.LogHtmlInformation("Relay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                AfViewModel.ToggleBrightFieldEnable(false);

                var relayMotorAbsoluteValues = Generate.LinearRange(Cache.Item.StartRelayMotorAbsoluteValue, Cache.Item.StepRelayMotorAbsoluteValue, Cache.Item.StopRelayMotorAbsoluteValue);
                var closestIndex = relayMotorAbsoluteValues
                    .Index()
                    .OrderBy(x => Math.Abs(x.Item - currentMotorAbsoluteValue))
                    .First()
                    .Index;
                relayMotorAbsoluteValues =
                [
                    .. relayMotorAbsoluteValues.AsSpan()[closestIndex..],
                    .. relayMotorAbsoluteValues.AsSpan()[..closestIndex].ToArray().AsEnumerable().Reverse()
                ];

                foreach (var (index, relayMotorAbsoluteValue) in relayMotorAbsoluteValues.Index())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{relayMotorAbsoluteValue:0.###}mm", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, relayMotorAbsoluteValue);

                    var itemItem = new OpticsRelayDTOItem { RelayMotorAbsoluteValue = relayMotorAbsoluteValue };
                    CalibratingItem.Items = [.. ((IReadOnlyList<OpticsRelayDTOItem>)[.. CalibratingItem.Items, itemItem]).OrderBy(t => t.RelayMotorAbsoluteValue)];

                    var deltaECS = (index == 0
                                       ? Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI ? 1 : -1
                                       : 1)
                                   * (relayMotorAbsoluteValue - currentMotorAbsoluteValue) * defaultSlope;

                    await CatchImageAsync(Generate.LinearRange(
                        Cache.Item.StartRoughECS + deltaECS,
                        Cache.Item.StepRoughECS,
                        Cache.Item.StopRoughECS + deltaECS));
                    GuardUtils.IsNotNullAndReturn(itemItem.MaxItem);

                    await CatchImageAsync(Generate.LinearRange(
                        itemItem.MaxItem.ECS - Cache.Item.RangeRefinedECS,
                        Cache.Item.StepRefinedECS,
                        itemItem.MaxItem.ECS + Cache.Item.RangeRefinedECS));
                    GuardUtils.IsNotNullAndReturn(itemItem.MaxItem);

                    if (CalibratingItem.Items.Count > 1)
                    {
                        var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
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
                        itemItem.MaxItem.ECS,
                        itemItem.MaxItem.Quality,
                        itemItem.MaxItem.RawImageFilePath,
                        Image = new HtmlImage(itemItem.MaxItem.ImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());

                    continue;

                    async Task CatchImageAsync(IReadOnlyList<double> ecses)
                    {
                        Guard.IsNotEmpty(ecses);

                        var currentDetectImageDirectory = Path.Combine(detectImageDirectory, $"{relayMotorAbsoluteValue:0.###}mm_[{ecses[0]:0.###}ECS, {ecses[^1]:0.###}ECS]_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat)}");

                        foreach (var ecs in ecses)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            Logger.LogHtmlInformation($"{ecs:0.###}ECS", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                            AfViewModel.SetSensorEcsValue(ecs);

                            var itemItemData = new OpticsRelayDTOItem.Item { ECS = ecs };

                            using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                                Cache.Item.ProductivityInformation,
                                StageCoordinateSystemEnum.Dark,
                                dswBFPosition,
                                Cache.Item.CIBInformation,
                                Cache.Item.ImageWidth,
                                (false, CalChipSiteModelEnum.DswModel),
                                (false, Cache.Item.CIBConfiguration),
                                (false, Cache.Item.LaserLightInformation),
                                false,
                                cancellationToken,
                                isAutoFocus: false);

                            var quality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImage.Image);

                            var filePath = Path.Combine(currentDetectImageDirectory, $"{ecs:0.###}ECS_{quality:0.###}Quality_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                            darkFieldImage.Image.Save(filePath);

                            itemItemData.Quality = quality;
                            itemItemData.ImageFilePath = filePath;
                            itemItemData.RawImageFilePath = darkFieldImage.RawImageFilePath;

                            itemItem.Qualitys = [.. ((IReadOnlyList<OpticsRelayDTOItem.Item>)[.. itemItem.Qualitys, itemItemData]).OrderBy(t => t.ECS)];

                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                            {
                                itemItemData.ECS,
                                itemItemData.Quality,
                                itemItemData.ImageFilePath,
                                itemItemData.RawImageFilePath
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
                StageViewModel.SetAbsoluteStageTheta(0d);
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
                    Cache.OpticsIlluminationModeEnum,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

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

    private bool Save(IReadOnlyList<OpticsRelayDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.OpticsIlluminationModeEnum != dto.OpticsIlluminationModeEnum),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}