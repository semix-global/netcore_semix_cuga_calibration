using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.CollectPolarization;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;
using Point = Net.Utilities.Models.Geometries.Point;


namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(CollectionPolarizationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionPolarizationViewModel : CalibrationViewModelBase<CollectPolarizationCache>
{
    #region 界面相关

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Image Param" },
        new() { StepName = "Find Position" },
        new() { StepName = "Polarization P" },
        new() { StepName = "Polarization S" }
    ];

    [ObservableProperty]
    public partial IReadOnlyList<CollectPolarizationDTO> Reviews { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    public partial IReadOnlyList<CollectPolarizationDTO> CalibratingItems { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [RecipeCache]
    [ObservableProperty]
    public override partial CollectPolarizationCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CollectPolarizationDTO[] Calibrations { get; set; } = [];

    /// <summary>
    /// 激光光强信息列表
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<LaserLightInformation> LaserLightInformations { get; set; } = [];

    #endregion 缓存

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CollectPolarizationCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<CollectPolarizationDTO>(cancellationToken);

        // 方案限定CIB配置，小光强
        Cache.CIBConfiguration = new CIBConfiguration
        {
            Gain = 0,
            CIBProfileMode = CIBProfileModeEnum.PMTVoltage,
            IsAutoGainControl = false,
            IsL0K = false
        };
        LaserLightInformations = [.. ApplicationCookie.LaserLightInformations.Where(t => t.Coefficient < 0.5)];

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
                .OrderBy(t => t.OpticsCollectorPolarizationMode)
                .ThenBy(t => t.ChannelId)
        ];

        return Reviews.All(t => t.IsCalibrated);
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

                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition != Point.Origin
                    ? Cache.FindBFMachinePosition
                    : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));
                return true;

            case 3:

                Cache.OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.S;
                Cache.OpticsCollectorPolarizationMode = OpticsCollectorPolarizationModeEnum.P;

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
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition != Point.Origin
                    ? Cache.FindBFMachinePosition
                    : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 1:
                Cache.OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.S;
                Cache.OpticsCollectorPolarizationMode = OpticsCollectorPolarizationModeEnum.P;

                return true;
            case 2:
                Cache.OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.P;
                Cache.OpticsCollectorPolarizationMode = OpticsCollectorPolarizationModeEnum.S;

                return true;

            default:

                return true;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        return Task.FromResult(true);
    }

    #endregion

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.PMTId,
                CIBConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation)
                   && ApplicationCookie.CIBInformationPMTIds.Contains(Cache.PMTId);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0d);
            Cache.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.PMTId,
                CIBConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous()),
                Cache.FindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var result = await GetResultAsync(cancellationToken).ConfigureAwait(false);

            Guard.IsTrue(Save(CalibratingItems, cancellationToken));

            return result;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var result = await GetResultAsync(cancellationToken).ConfigureAwait(false);

            Guard.IsTrue(Save(CalibratingItems, cancellationToken));

            Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
            {
                Result = new HtmlTable([
                    .. Calibrations
                        .OrderBy(t => t.ChannelId)
                        .ThenBy(t => t.OpticsCollectorPolarizationMode)
                        .Select(t => new
                        {
                            t.ChannelId,
                            t.OpticsCollectorPolarizationMode,
                            t.NDFRotaryMotorPosition,
                            t.GrayValue
                        })
                ]),
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            var errorMessageStringBuilder = new StringBuilder();
            var detectImageDirectory = ImageFileDirectory;

            var defaultOpticsPolarizationModeEnum = OpticsViewModel.GetPolarizationMode();
            var defaultOpticsCollectorPolarizationModeEnum = OpticsViewModel.GetCollectorPolarizationMode();
            try
            {
                var collectPolarizationGroups = Reviews.GroupBy(t => t.OpticsCollectorPolarizationMode).ToList();

                foreach (var collectPolarizationGroup in collectPolarizationGroups)
                {
                    var dtos = collectPolarizationGroup.Select(t => t).ToList();

                    Cache.OpticsCollectorPolarizationMode = collectPolarizationGroup.Key;
                    Cache.OpticsPolarizationModeEnum = dtos[0].OpticsPolarizationModeEnum;

                    var title = "Polarization" + Cache.OpticsCollectorPolarizationMode;

                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        defaultOpticsPolarizationModeEnum,
                        defaultOpticsCollectorPolarizationModeEnum,
                        Cache.OpticsPolarizationModeEnum,
                        Cache.OpticsCollectorPolarizationMode,
                        CIBConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous()),
                        Cache.LaserLightInformation,
                        Cache.FindBFMachinePosition,
                        Cache.ImageWidth,
                        Cache.Threshold,
                        detectImageDirectory,
                    }), HtmlLogUniqueId.LoggingHtml());

                    SetOpticsConfig(Cache.OpticsPolarizationModeEnum, Cache.OpticsCollectorPolarizationMode);
                    await SetAllChannelCollectionPolarizationMotorValueAsync([.. dtos.Select(t => (t.ChannelId, NDFMotorPosition: t.NDFRotaryMotorPosition))], cancellationToken).ConfigureAwait(false);

                    var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition),
                        Cache.ImageWidth,
                        [.. ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PMTId)],
                        (false, CalChipSiteModelEnum.HazeModel),
                        (true, null),
                        (false, Cache.CIBConfiguration),
                        (false, Cache.LaserLightInformation),
                        false,
                        cancellationToken);

                    foreach (var darkFieldImageDTO in darkFieldImages)
                    {
                        var logTitle = $"{title}_Channel{darkFieldImageDTO.CIBInformation.ChannelId}";

                        var currentCIBItem = Reviews.Single(t => t.ChannelId == darkFieldImageDTO.CIBInformation.ChannelId &&
                                                                 t.OpticsCollectorPolarizationMode == Cache.OpticsCollectorPolarizationMode);
                        var intensity = darkFieldImageDTO.Image.GetIntensity();

                        var imageFilePath = Path.Combine(detectImageDirectory, "Verify", $"Channel {darkFieldImageDTO.CIBInformation}", $"Pos({currentCIBItem.NDFRotaryMotorPosition:0.###})_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImageDTO.Image.SaveImage(imageFilePath);

                        currentCIBItem.GrayValue = intensity.Average;
                        currentCIBItem.IsVerified = currentCIBItem.GrayValue < Cache.Threshold;

                        var htmlBullet = new HtmlBullet(new
                        {
                            currentCIBItem.OpticsCollectorPolarizationMode,
                            currentCIBItem.OpticsPolarizationModeEnum,
                            NDFMotorPosition = currentCIBItem.NDFRotaryMotorPosition,
                            intensity.Average,
                            Image = new HtmlImage(imageFilePath)
                        });

                        if (currentCIBItem.IsVerified)
                            Logger.LogHtmlInformation($"{logTitle} OK", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        else
                        {
                            errorMessageStringBuilder.AppendLine($"{logTitle}: Error");
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        }
                    }
                }
            }
            finally
            {
                SetOpticsConfig(defaultOpticsPolarizationModeEnum, defaultOpticsCollectorPolarizationModeEnum);
            }

            Guard.IsTrue(Save(Reviews, cancellationToken));

            var result = Reviews.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    #endregion

    private async Task<bool> GetResultAsync(CancellationToken cancellationToken)
    {
        CalibratingItems = [];

        var defaultOpticsPolarizationModeEnum = OpticsViewModel.GetPolarizationMode();
        var defaultOpticsCollectorPolarizationModeEnum = OpticsViewModel.GetCollectorPolarizationMode();
        var motorRange = (Min: 0d, Max: 359d);
        try
        {
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.PMTId,
                CIBConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous()),
                defaultOpticsPolarizationModeEnum,
                defaultOpticsCollectorPolarizationModeEnum,
                Cache.OpticsPolarizationModeEnum,
                Cache.OpticsCollectorPolarizationMode,
                Cache.FindBFMachinePosition,
                Cache.ImageWidth,
                Cache.StartNDFRotaryMotorPos,
                Cache.StopNDFRotaryMotorPos,
                Cache.StepNDFRotaryMotorPos,
                Cache.Threshold,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            SetOpticsConfig(Cache.OpticsPolarizationModeEnum, Cache.OpticsCollectorPolarizationMode);

            CIBInformation[] cibInformations = [.. ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PMTId)];

            // 粗找
            Logger.LogHtmlInformation("Roughly", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            await ActionAsync([
                .. ApplicationCookie.CIBInformationChannelIds.Select(t => (t, Generate.LinearRangeContainsEdge(
                    Math.Max(motorRange.Min, Cache.StartNDFRotaryMotorPos),
                    Cache.StepNDFRotaryMotorPos,
                    Math.Min(motorRange.Max, Cache.StopNDFRotaryMotorPos))))
            ]).ConfigureAwait(false);

            // 精找
            Logger.LogHtmlInformation("Refined", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                RangeRefinedNDFMotorPos = Cache.RangeRefinedNDFRotaryMotorPos,
                StepRefinedNDFMotorPos = Cache.StepRefinedNDFRotaryMotorPos
            }), HtmlLogUniqueId.LoggingHtml());

            await ActionAsync([
                .. CalibratingItems.Select(t => (t.ChannelId, Generate.LinearRangeContainsEdge(
                    Math.Max(motorRange.Min, t.NDFRotaryMotorPosition - Cache.RangeRefinedNDFRotaryMotorPos),
                    Cache.StepRefinedNDFRotaryMotorPos,
                    Math.Min(motorRange.Max, t.NDFRotaryMotorPosition + Cache.RangeRefinedNDFRotaryMotorPos))))
            ]).ConfigureAwait(false);

            Logger.LogHtmlInformation("Fit", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            foreach (var calibratingItem in CalibratingItems)
            {
                // 先拟合，后采样
                var (p0, p1, p2, _, _) = PolynomialCurve.Fit2(
                    Vector<double>.Build.Dense([.. calibratingItem.Items.Select(t => t.NDFRotaryMotorPosition)]),
                    Vector<double>.Build.Dense([.. calibratingItem.Items.Select(t => t.GrayValue)]));

                // 抛物线很平缓时（p2 很小但为正），顶点公式 -p1 / (2 * p2) 会对噪声极其敏感，算出来的最小值位置可能大幅跳动，增加校验
                if (HostEnvironment.IsDevelopment() == false)
                    Guard.IsTrue(p2 > 1e-6, "Parabola too flat, minimum is unreliable，Does not conform to physical trends. Please check parameter ranges and hardware status");

                // x 按 0.1 精度取整
                var minX = -p1 / (2 * p2);
                minX = Math.Max(motorRange.Min, Math.Min(motorRange.Max, minX));
                var minPos = Math.Round(minX * 10, MidpointRounding.AwayFromZero) / 10;
                calibratingItem.NDFRotaryMotorPosition = minPos;
                calibratingItem.GrayValue = p2 * minPos * minPos
                                            + p1 * minPos
                                            + p0;

                // 用 0.1 步长在数据范围内生成拟合曲线，并显式加入最小点
                var minXData = calibratingItem.Items.Min(t => t.NDFRotaryMotorPosition);
                var maxXData = calibratingItem.Items.Max(t => t.NDFRotaryMotorPosition);

                // 确保采样点包围minX
                if (HostEnvironment.IsDevelopment() == false)
                    Guard.IsTrue(minX >= minXData && minX <= maxXData, "Estimated minimum is outside sampled range.");

                calibratingItem.FitPoints =
                [
                    .. Generate.LinearRange(minXData, 0.1, maxXData)
                        .Select(x => new Point(x, p2 * x * x + p1 * x + p0))
                        .Append(new Point(minPos, calibratingItem.GrayValue))
                        .OrderBy(p => p.X)
                ];

                calibratingItem.IsCalibrated = true;

                Logger.LogHtmlInformation($"Channel {calibratingItem.ChannelId} Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    calibratingItem.ChannelId,
                    calibratingItem.NDFRotaryMotorPosition,
                    calibratingItem.GrayValue,
                    calibratingItem.IsCalibrated,
                    Plots = new HtmlContainer(calibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts())
                }), HtmlLogUniqueId.LoggingHtml());
            }

            Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Result = new HtmlTable([
                    .. CalibratingItems.Select(t => new
                    {
                        t.ChannelId,
                        t.NDFRotaryMotorPosition,
                        t.GrayValue
                    })
                ]),
            }), HtmlLogUniqueId.LoggingHtml());

            return true;

            async Task ActionAsync(IReadOnlyList<(int channelID, IReadOnlyList<double> motorPoses)> posInfos)
            {
                var currentDetectImageDirectory = Path.Combine(detectImageDirectory, $"{Cache.OpticsCollectorPolarizationMode.ToDescriptionOrString()} Polarization");

                CalibratingItems =
                [
                    .. ApplicationCookie.CIBInformationChannelIds.Select(channelId => new CollectPolarizationDTO
                    {
                        OpticsPolarizationModeEnum = Cache.OpticsPolarizationModeEnum,
                        OpticsCollectorPolarizationMode = Cache.OpticsCollectorPolarizationMode,
                        ChannelId = channelId
                    })
                ];

                for (var i = 0; i < posInfos.Max(t => t.motorPoses.Count); i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 超出索引的忽略
                    var allChannelCurrentPoses = posInfos
                        .Where(info => i < info.motorPoses.Count)
                        .Select(info => (info.channelID, pos: info.motorPoses[i]))
                        .ToList();

                    await SetAllChannelCollectionPolarizationMotorValueAsync(allChannelCurrentPoses, cancellationToken).ConfigureAwait(false);

                    var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition),
                        Cache.ImageWidth,
                        cibInformations,
                        (false, CalChipSiteModelEnum.HazeModel),
                        (true, null),
                        (false, Cache.CIBConfiguration),
                        (false, Cache.LaserLightInformation),
                        false,
                        cancellationToken,
                        isKeepRawImageCIBProfileModeEnum: false);

                    foreach (var darkFieldImageDTO in darkFieldImages.Where(t => allChannelCurrentPoses.Select(tt => tt.channelID).Contains(t.CIBInformation.ChannelId)))
                    {
                        var currentPosInfo = allChannelCurrentPoses.Single(t => t.channelID == darkFieldImageDTO.CIBInformation.ChannelId);
                        var currentCIBItem = CalibratingItems.Single(t => t.ChannelId == darkFieldImageDTO.CIBInformation.ChannelId);
                        var intensity = darkFieldImageDTO.Image.GetIntensity();

                        var imageFilePath = Path.Combine(currentDetectImageDirectory, $"Channel {darkFieldImageDTO.CIBInformation}", $"Pos({currentPosInfo.pos:0.###})_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImageDTO.Image.SaveImage(imageFilePath);

                        currentCIBItem.Items =
                        [
                            .. currentCIBItem.Items,
                            new CollectPolarizationDTOItem
                            {
                                GrayValue = intensity.Average,
                                ImageFilePath = imageFilePath,
                                NDFRotaryMotorPosition = currentPosInfo.pos
                            }
                        ];
                    }
                }

                foreach (var calibratingItem in CalibratingItems)
                {
                    var resultPoint = Point.Origin;
                    if (HostEnvironment.IsDevelopment() == false)
                    {
                        var (_, results) = Extremumor.FindMinima([.. calibratingItem.Items.Select(t => new Point(t.NDFRotaryMotorPosition, t.GrayValue))], isContainsEdge: true);
                        resultPoint = results.Minima(t => t.Y).First();
                    }

                    calibratingItem.NDFRotaryMotorPosition = resultPoint.X;
                    calibratingItem.GrayValue = resultPoint.Y;

                    var posInfo = posInfos.Single(t => t.channelID == calibratingItem.ChannelId);
                    Logger.LogHtmlInformation($"Channel {calibratingItem.ChannelId}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        StartPos = posInfo.motorPoses[0],
                        StopPos = posInfo.motorPoses[^1],
                        Details = new HtmlExpand("Details", new HtmlBullet(calibratingItem.ToFlatnessHtmlAnonymous()))
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Result = new HtmlTable([
                        .. CalibratingItems.Select(t => new
                        {
                            t.ChannelId,
                            t.NDFRotaryMotorPosition,
                            t.GrayValue
                        })
                    ]),
                }), HtmlLogUniqueId.LoggingHtml());
            }
        }
        finally
        {
            SetOpticsConfig(defaultOpticsPolarizationModeEnum, defaultOpticsCollectorPolarizationModeEnum);
        }
    }

    private Task SetAllChannelCollectionPolarizationMotorValueAsync(IReadOnlyList<(int channelID, double motorPos)> posInfos, CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(posInfos, cancellationToken, (info, token) =>
        {
            token.ThrowIfCancellationRequested();

            OpticsViewModel.SetCollectorPolarizationMotorAbsoluteValue(info.channelID, info.motorPos);

            return ValueTask.CompletedTask;
        });
    }

    private void SetOpticsConfig(OpticsPolarizationModeEnum opticsPolarizationModeEnum, OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        OpticsViewModel.SetPolarizationMode(opticsPolarizationModeEnum);
        OpticsViewModel.SetCollectorPolarizationMode(opticsCollectorPolarizationModeEnum);
    }

    private bool Save(IReadOnlyList<CollectPolarizationDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto.Clone(),
                .. Calibrations.Where(t => (t.OpticsCollectorPolarizationMode == dto.OpticsCollectorPolarizationMode && t.ChannelId == dto.ChannelId) == false)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });


    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<CollectPolarizationDTO[]>(calibrations);
        var status = Entry.Status;

        Calibrations = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibrations.All(t => t.IsCalibrated) ? 1 : 0;
        status.VerifiedCount = Calibrations.All(t => t.IsVerified) ? 1 : 0;
        status.Details = [];
    }
}