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
using Core.Models.Models.Optics.Relay;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Humanizer;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(OpticsRelayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsRelayViewModel : CalibrationViewModelBase<OpticsRelayCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override string CalibrateFileName => Cache.OpticsIlluminationModeEnum.Humanize();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Image Param" },
        new() { StepName = "Alignment" },
        new() { StepName = "Find Z Sync DSW Position" },
        new() { StepName = "Z Sync Relay" },
        new() { StepName = "Find X/Z Sync DSW Position" },
        new() { StepName = "X/Z Sync Relay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial OpticsRelayDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<OpticsIlluminationModeStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<OpticsRelayDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OpticsRelayDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial OpticsRelayCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial OpticsRelayDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipCache MicroscopeCalChipCache { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (ApplicationCookie.OpticsIlluminationModeEnums.Contains(OpticsIlluminationModeEnum.OI) &&
            ApplicationCookie.HardwareStateConfig.MotorHardwares[HardwareMotorTypeEnum.OIRelay].Enabled == false)
        {
            DialogWindowProvider.ShowDialog("Please enable the OI Relay motor!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (ApplicationCookie.OpticsIlluminationModeEnums.Contains(OpticsIlluminationModeEnum.NI) &&
            ApplicationCookie.HardwareStateConfig.MotorHardwares[HardwareMotorTypeEnum.NIRelay].Enabled == false)
        {
            DialogWindowProvider.ShowDialog("Please enable the NI Relay motor!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        MicroscopeCalChipCache = ApplicationCookieService.GetCache<MicroscopeCalChipCache>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<OpticsRelayCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<OpticsRelayDTO>(cancellationToken);

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
                .OrderBy(t => t.OpticsIlluminationModeEnum)
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
                return true;

            case 4:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.DSWFindBFMachinePosition), CalChipSiteModelEnum.DswModel);

                return true;

            case 5:
                return true;

            case 6:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.XZDSWFindBFMachinePosition), CalChipSiteModelEnum.DswModel);

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
                CalibratingItem = new OpticsRelayDTO();

                return true;

            case 1:
                return true;

            case 2:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.DSWFindBFMachinePosition == Point.Origin
                        ? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.DswModel)
                        : Cache.Item.DSWFindBFMachinePosition), CalChipSiteModelEnum.DswModel);

                return true;

            case 3:
                return true;

            case 4:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.XZDSWFindBFMachinePosition == Point.Origin
                        ? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.DswModel)
                        : Cache.Item.XZDSWFindBFMachinePosition), CalChipSiteModelEnum.DswModel);

                return true;

            case 5:
                return true;

            case 6:
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
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.Item.ProductivityInformation;

            DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
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

            var currentMotorAbsoluteValue = OpticsViewModel.GetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum);
            var nmPerEcs = AfViewModel.GetNmPerEcs();
            // ECS/mm relay电机值增大, chuck焦点向下移动, chuck焦点向下移动 ecs增大 mm
            var defaultSlope = 1d / Cache.Item.DefaultRelayMotorRatio /* mm */
                               * 1e6d /* mm 转为 nm */
                               * Math.Cos(Math.DegreeAngleToRadianAngle(Cache.Item.OpticsIlluminationDegreeAngle)) /* 转为垂直方向焦点移动的距离 */
                               / nmPerEcs; /* 转为 ECS */

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
                Cache.Item.ImageWidth,
                Cache.Item.OpticsIlluminationDegreeAngle,
                Cache.Item.DefaultRelayMotorRatio,
                Cache.Item.StartRelayMotorAbsoluteValue,
                Cache.Item.StepRelayMotorAbsoluteValue,
                Cache.Item.StopRelayMotorAbsoluteValue,
                Cache.Item.CenterRoughECS,
                Cache.Item.RangeRoughECS,
                Cache.Item.StepRoughECS,
                Cache.Item.RangeRefinedECS,
                Cache.Item.StepRefinedECS,
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
            CalibratingItem.FitRelayPoints = [];
            CalibratingItem.RelayMotorRatio = 0d;

            var dswBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.DSWFindBFMachinePosition);
            StageViewModel.SetBrightFieldAbsoluteStageXy(dswBFPosition, CalChipSiteModelEnum.DswModel);

            var currentDSWBFPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.Item.ProductivityInformation,
                Cache.Item.CIBInformation,
                dswBFPosition,
                Cache.Item.MicroscopeLensInformation);
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

                Guard.IsGreaterThan(relayMotorAbsoluteValues.Length, 2);
                foreach (var relayMotorAbsoluteValue in relayMotorAbsoluteValues)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{relayMotorAbsoluteValue:0.###}mm", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, relayMotorAbsoluteValue);

                    var itemItem = new OpticsRelayDTOItem { RelayMotorAbsoluteValue = relayMotorAbsoluteValue };
                    CalibratingItem.Items = [.. ((IReadOnlyList<OpticsRelayDTOItem>)[.. CalibratingItem.Items, itemItem]).OrderBy(t => t.RelayMotorAbsoluteValue)];

                    var deltaECS = (relayMotorAbsoluteValue - currentMotorAbsoluteValue) * defaultSlope;

                    await CatchImageAsync(Generate.LinearRange(
                        Cache.Item.CenterRoughECS + deltaECS - Cache.Item.RangeRoughECS,
                        Cache.Item.StepRoughECS,
                        Cache.Item.CenterRoughECS + deltaECS + Cache.Item.RangeRoughECS));
                    Guard.IsNotNullAndReturn(itemItem.MaxItem);

                    await CatchImageAsync(Generate.LinearRange(
                        itemItem.MaxItem.ECS - Cache.Item.RangeRefinedECS,
                        Cache.Item.StepRefinedECS,
                        itemItem.MaxItem.ECS + Cache.Item.RangeRefinedECS));
                    Guard.IsNotNullAndReturn(itemItem.MaxItem);

                    if (CalibratingItem.Items.Count > 1)
                    {
                        var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                            Vector<double>.Build.Dense([.. CalibratingItem.Items.Select(t => t.RelayMotorAbsoluteValue)]),
                            Vector<double>.Build.Dense([.. CalibratingItem.Items.Select(t => Guard.IsNotNullAndReturn(t.MaxItem).ECS)]));

                        defaultSlope = slope;
                        CalibratingItem.Slope = slope;
                        CalibratingItem.Intercept = intercept;
                        CalibratingItem.RSquared = rSquared;
                        CalibratingItem.FitRelayPoints = [.. CalibratingItem.Items.Index().Select(t => new Point(t.Item.RelayMotorAbsoluteValue, yPredicted[t.Index]))];
                        CalibratingItem.RelayMotorRatio = 1 / (
                            CalibratingItem.Slope /* ECS/mm */
                            * nmPerEcs /* 分子 ECS 转为 nm */
                            / 1e6d /* 分母mm 转为 nm */
                            / Math.Cos(Math.DegreeAngleToRadianAngle(Cache.Item.OpticsIlluminationDegreeAngle)) /* 转为照明方向移动的距离 */
                        );
                    }

                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        defaultSlope,
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
                                currentDSWBFPosition,
                                Cache.Item.ImageWidth,
                                Cache.Item.CIBInformation,
                                (true, null),
                                (false, Cache.Item.OpticsConfiguration),
                                (false, Cache.Item.CIBConfiguration),
                                (false, Cache.Item.LaserLightInformation),
                                false,
                                cancellationToken,
                                isAutoFocus: false);

                            var quality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImage.Image, HtmlLogUniqueId);

                            var filePath = Path.Combine(currentDetectImageDirectory, $"{ecs:0.###}ECS_{quality:0.###}Quality_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                            darkFieldImage.Image.SaveImage(filePath);

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

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    dswBFPosition,
                    currentDSWBFPosition,
                    CalibratingItem.Slope,
                    CalibratingItem.Intercept,
                    CalibratingItem.RSquared,
                    PlotDataSource = new HtmlContainer([.. CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            finally
            {
                OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, currentMotorAbsoluteValue);
                StageViewModel.SetBrightFieldAbsoluteStageXy(dswBFPosition, CalChipSiteModelEnum.DswModel);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            Cache.Item.XZDSWFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.XZDSWFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step6Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var currentMotorAbsoluteValue = OpticsViewModel.GetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum);
            var nmPerEcs = AfViewModel.GetNmPerEcs();
            var defaultSlope = CalibratingItem.Slope;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.XZDSWFindBFMachinePosition,
                Cache.Item.XZScanLength,
                Cache.Item.StartXZRelayMotorAbsoluteValue,
                Cache.Item.StepXZRelayMotorAbsoluteValue,
                Cache.Item.StopXZRelayMotorAbsoluteValue,
                Cache.Item.XZCenterECS,
                Cache.Item.XZRangeECS,
                Cache.Threshold,
                currentMotorAbsoluteValue,
                nmPerEcs,
                defaultSlope,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.XZItems = [];
            CalibratingItem.XZSlope = 0d;
            CalibratingItem.XZIntercept = 0d;
            CalibratingItem.XZRSquared = 0d;
            CalibratingItem.XZFitRelayPoints = [];
            CalibratingItem.RelayMotorRatio = 0d;
            CalibratingItem.MinRelayMotorAbsoluteValue = 0d;
            CalibratingItem.MaxRelayMotorAbsoluteValue = 0d;
            CalibratingItem.IsCalibrated = false;

            var xZDSWBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.XZDSWFindBFMachinePosition);
            StageViewModel.SetBrightFieldAbsoluteStageXy(xZDSWBFPosition, CalChipSiteModelEnum.DswModel);

            var startCurrentXZDSWBFPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.Item.ProductivityInformation,
                Cache.Item.CIBInformation,
                xZDSWBFPosition,
                Cache.Item.MicroscopeLensInformation);

            try
            {
                Logger.LogHtmlInformation("Relay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                AfViewModel.ToggleBrightFieldEnable(false);

                var relayMotorAbsoluteValues = Generate.LinearRange(Cache.Item.StartXZRelayMotorAbsoluteValue, Cache.Item.StepXZRelayMotorAbsoluteValue, Cache.Item.StopXZRelayMotorAbsoluteValue);
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
                Guard.IsGreaterThan(relayMotorAbsoluteValues.Length, 2);
                foreach (var relayMotorAbsoluteValue in relayMotorAbsoluteValues)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{relayMotorAbsoluteValue:0.###}mm", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.OpticsIlluminationModeEnum, relayMotorAbsoluteValue);

                    var item = new OpticsRelayDTOXZItem { RelayMotorAbsoluteValue = relayMotorAbsoluteValue };
                    CalibratingItem.XZItems = [.. ((IReadOnlyList<OpticsRelayDTOXZItem>)[.. CalibratingItem.XZItems, item]).OrderBy(t => t.RelayMotorAbsoluteValue)];

                    var deltaECS = (relayMotorAbsoluteValue - currentMotorAbsoluteValue) * defaultSlope;
                    var startECS = Cache.Item.XZCenterECS + deltaECS - Cache.Item.XZRangeECS;
                    var stopECS = Cache.Item.XZCenterECS + deltaECS + Cache.Item.XZRangeECS;

                    using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                        Cache.Item.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        startCurrentXZDSWBFPosition,
                        startCurrentXZDSWBFPosition + new Vector(Cache.Item.XZScanLength, 0),
                        startECS,
                        stopECS,
                        Cache.Item.CIBInformation,
                        (true, null),
                        (false, Cache.Item.OpticsConfiguration),
                        (false, Cache.Item.CIBConfiguration),
                        (false, Cache.Item.LaserLightInformation),
                        false,
                        cancellationToken);

                    var bestFocus = CalibrationAlgorithmService.GetBestFocus(darkFieldImage.Image, startECS, stopECS, HtmlLogUniqueId);
                    item.BestFocus = bestFocus;
                    item.BestFocus.RawImageFilePath = darkFieldImage.RawImageFilePath;

                    if (CalibratingItem.XZItems.Count > 1)
                    {
                        var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                            Vector<double>.Build.Dense([.. CalibratingItem.XZItems.Select(t => t.RelayMotorAbsoluteValue)]),
                            Vector<double>.Build.Dense([
                                .. CalibratingItem.XZItems.Select(t => Cache.Item.OpticsStrehlRatioQualityTypeEnum switch
                                {
                                    OpticsStrehlRatioQualityTypeEnum.XStrehlRatio => t.BestFocus.BestXStrehlRatioECS,
                                    OpticsStrehlRatioQualityTypeEnum.YStrehlRatio => t.BestFocus.BestYStrehlRatioECS,
                                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(Cache.Item.OpticsStrehlRatioQualityTypeEnum))
                                })
                            ]));

                        defaultSlope = slope;
                        CalibratingItem.XZSlope = slope;
                        CalibratingItem.XZIntercept = intercept;
                        CalibratingItem.XZRSquared = rSquared;
                        CalibratingItem.XZFitRelayPoints = [.. CalibratingItem.XZItems.Index().Select(t => new Point(t.Item.RelayMotorAbsoluteValue, yPredicted[t.Index]))];
                        CalibratingItem.RelayMotorRatio = 1 / (
                            CalibratingItem.XZSlope /* ECS/mm */
                            * nmPerEcs /* 分子 ECS 转为 nm */
                            / 1e6d /* 分母mm 转为 nm */
                            / Math.Cos(Math.DegreeAngleToRadianAngle(Cache.Item.OpticsIlluminationDegreeAngle)) /* 转为照明方向移动的距离 */
                        );
                    }

                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        defaultSlope,
                        XStrehlRatioPlotDataSource = new HtmlContainer([.. item.BestFocus.XStrehlRatioPlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
                        YStrehlRatioPlotDataSource = new HtmlContainer([.. item.BestFocus.YStrehlRatioPlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
                        item.BestFocus.RawImageFilePath
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                CalibratingItem.MinRelayMotorAbsoluteValue = CalibratingItem.FitRelayPoints[0].X;
                CalibratingItem.MaxRelayMotorAbsoluteValue = CalibratingItem.FitRelayPoints[^1].X;
                CalibratingItem.IsCalibrated = CalibratingItem.RSquared >= Cache.Threshold;

                var htmlBullet = new HtmlBullet(new
                {
                    xZDSWBFPosition,
                    startCurrentXZDSWBFPosition,
                    CalibratingItem.XZSlope,
                    CalibratingItem.XZIntercept,
                    CalibratingItem.XZRSquared,
                    CalibratingItem.RelayMotorRatio,
                    CalibratingItem.MinRelayMotorAbsoluteValue,
                    CalibratingItem.MaxRelayMotorAbsoluteValue,
                    PlotDataSource = new HtmlContainer([.. CalibratingItem.XZPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
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
                StageViewModel.SetBrightFieldAbsoluteStageXy(xZDSWBFPosition, CalChipSiteModelEnum.DswModel);
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
                    selectedReviewItem.MinRelayMotorAbsoluteValue,
                    selectedReviewItem.MaxRelayMotorAbsoluteValue,
                    selectedReviewItem.IsVerified,
                    SuccessPlot = new HtmlContainer([.. selectedReviewItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
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
                dto.Clone(),
                .. Calibrations.Where(t => t.OpticsIlluminationModeEnum != dto.OpticsIlluminationModeEnum)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<OpticsRelayDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsIlluminationModeEnums.Select(t => new OpticsIlluminationModeStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationModeEnum))
                .DistinctBy(t => t.OpticsIlluminationModeEnum)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.OpticsIlluminationModeEnum).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.OpticsIlluminationModeEnums.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsIlluminationModeEnums.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.OpticsIlluminationModeEnum == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}