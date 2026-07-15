using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.BestFocusAndAstigmatism;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using CugaCalibration.ViewModels.Common.Windows.Tools.Optics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODBestFocusAndAstigmatismViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODBestFocusAndAstigmatismViewModel : CalibrationViewModelBase<AODBestFocusAndAstigmatismCache>
{
    #region 属性

    private string ChirpFileDirectory => Path.Combine(
        AppHomeDirectory,
        "Chirp",
        nameof(AODBestFocusAndAstigmatismViewModel),
        DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName),
        DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Select Optics Apodization Mode" },
        new() { StepName = "CIB Config" },
        new() { StepName = "Alignment" },
        new() { StepName = "Chirp AOD Waveform Config" },
        new() { StepName = "Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial AODBestFocusAndAstigmatismDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationAndApodizationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial ObservableCollection<AODBestFocusAndAstigmatismDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<AODBestFocusAndAstigmatismDTO> SelectedReviewItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OpticsBestFocusResult> BestFocusResults { get; set; } = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial AODBestFocusAndAstigmatismCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial AODBestFocusAndAstigmatismDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial AODDelayDTO[] AODDelays { get; set; } = [];

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    [ObservableProperty]
    public partial MicroscopeCalChipCache MicroscopeCalChipCache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        AODDelays = ApplicationCookieService.GetCalibrations<AODDelayDTO>(cancellationToken);
        MicroscopeCalChipCache = ApplicationCookieService.GetCache<MicroscopeCalChipCache>(cancellationToken);
        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<AODBestFocusAndAstigmatismCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<AODBestFocusAndAstigmatismDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PMTInterval;

        if (Cache.Item.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
            StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);

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
                .ThenBy(t => t.ApodizationModeEnum)
        ];

        if (Reviews.All(t => t.IsCalibrated == false))
            return false;

        BestFocusResults = [];
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                return true;

            case 2:
                DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                    out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                Cache.Item.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;
                return true;

            case 3:
                return true;
            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.StartPosition,
                    Cache.Item.CalChipSiteModelEnum);
                return true;

            case 5:
                DialogWindowProvider.ShowDialog($"{Cache.ProductivityInformation}-{Cache.ApodizationModeEnum.ToHexString()} best focus and astigmatism calibration ok!");

                return true;
            default:
                return true;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var chirpCache = ApplicationCookieService.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>(false, cancellationToken);
            var chirpResult = chirpCache.Results.SingleOrDefault(t => t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                      && t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType);
            if (chirpResult is null)
            {
                const string comment = "Warning: Chirp AOD Waveform Param No matched found for current Productivity Information!";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog(comment, DialogButtonsEnum.OK, DialogIconEnum.Error);

                return false;
            }

            Cache.Item.DefaultGenerateChirpAODWaveformParam = chirpResult.GenerateChirpAODWaveformParam;
            Cache.Item.DefaultGenerateChirpAODWaveformParam.ProductivityInformation =
                Cache.ProductivityInformation.Clone();
            // 有AOD Delay结果时，默认chirp波形使用该delay值
            var laserAodDelayItem = AODDelays.SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation);
            if (laserAodDelayItem is not null && laserAodDelayItem.IsOk)
            {
                var delayTime = Convert.ToInt32(laserAodDelayItem.ChirpAODDelay);
                Cache.Item.DefaultGenerateChirpAODWaveformParam.ZeroSampleCount = delayTime;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ApodizationModeEnum,
                Cache.Item.CalChipSiteModelEnum
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.Item.OpticsConfiguration.OpticsApodizationModeEnum = Cache.ApodizationModeEnum;
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                AstigmatisPMTId = Cache.Item.CIBInformation.PMTId,
                AstigmatismChannelId = Cache.Item.CIBInformation.ChannelId,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.Item.CalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.Item.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.Item.CalChipSiteModelEnum,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                DefaultGenerateChirpAODWaveformParam =
                    new HtmlQuote(Cache.Item.DefaultGenerateChirpAODWaveformParam.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;

                ClearCalibrationTemp();

                // 下发默认波形
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Coefficient);
                LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.StartPosition);

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageFileDirectory = detectImageDirectory,
                    Cache.Item.AlgorithmImageQualityTypeEnum,
                    Cache.Item.StartPosition,
                    Cache.Item.ScanLength,
                    StartDFMachinePosition = StageViewModel.DarkFieldToMachinePosition(Cache.Item.StartPosition),
                    EndDFMachinePosition =
                        StageViewModel.DarkFieldToMachinePosition(Cache.Item.StartPosition +
                                                                  (Vector)new Point(Cache.Item.ScanLength, 0)),
                    Cache.Item.CenterECS,
                    Cache.Item.RangeECS,
                    Cache.Item.StartSpectralDensity,
                    Cache.Item.SpectralDensityStepCount,
                    SpectralDensityStep = Cache.Item.StepSpectralDensity,
                    DefaultGenerateChirpAODWaveformParam =
                        new HtmlQuote(Cache.Item.DefaultGenerateChirpAODWaveformParam.ToHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Astigmatism", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var spectralDensity in Enumerable.Range(0, Cache.Item.SpectralDensityStepCount)
                             .Select(t => Cache.Item.StartSpectralDensity + t * Cache.Item.StepSpectralDensity)
                             .ToList())

                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await GetBestFocusAndAstigmatismResultAsync(spectralDensity, cancellationToken);
                }

                var listRow = CalibratingItem.Items.Select(t => t.BestFocus.BestYStrehlRatioECS).ToList();
                var listCol = CalibratingItem.Items.Select(t => 1 / t.SpectralDensity).ToList();

                var (k, b, rSquared, yPredicted) = PolynomialCurve.Fit1(
                    Vector<double>.Build.DenseOfEnumerable(listRow),
                    Vector<double>.Build.DenseOfEnumerable(listCol));
                CalibratingItem.Slope = k;
                CalibratingItem.Intercept = b;
                CalibratingItem.RSquared = rSquared;
                CalibratingItem.FitPoints = [.. listRow.Index().Select(t => new Point(t.Item, yPredicted[t.Index]))];

                var astigmatismItem = CalibratingItem.Items.Minima(t => Math.Abs(t.XYBestFocusOffsetEcs)).First();

                CalibratingItem.ResultDTO = astigmatismItem;

                var calibrationResult = HostEnvironment.IsDevelopment() ||
                                        Math.Abs(astigmatismItem.XYBestFocusOffsetEcs) <=
                                        Cache.XYBestFocusEcsOffsetThreshold;

                Logger.LogHtmlInformation($"Calibration {(calibrationResult ? "Success" : "Failed")}",
                    HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        Result = new HtmlQuote(CalibratingItem.ToFlatnessHtmlAnonymous())
                    }), HtmlLogUniqueId.LoggingHtml());

                //迭代

                if (calibrationResult == false)
                {
                    Logger.LogHtmlInformation("Iteration", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    ClearCalibrationTemp();
                    var iterationAstigmatismItem = await GetBestSpectralDensityAsync(0, astigmatismItem.BestFocus.BestXStrehlRatioECS);

                    calibrationResult = iterationAstigmatismItem.XYBestFocusOffsetEcs <
                                        Cache.XYBestFocusEcsOffsetThreshold;

                    CalibratingItem.ResultDTO = iterationAstigmatismItem;

                    Logger.LogHtmlInformation($"Iteration {(calibrationResult ? "Success" : "Failed")}",
                        HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                        {
                            Cache.XYBestFocusEcsOffsetThreshold,
                            Result = new HtmlQuote(CalibratingItem.ToFlatnessHtmlAnonymous())
                        }), HtmlLogUniqueId.LoggingHtml());
                }

                CalibratingItem.IsCalibrated = calibrationResult;

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                Logger.LogHtmlInformation($"Calibration {(calibrationResult ? "Success" : "Failed")}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                return calibrationResult;

                //迭代，根据拟合一次函数，首次输入ecsX，得到F0下发，后续用XY ECS Offset作为增量迭代
                async Task<AODBestFocusAndAstigmatismDTOItem> GetBestSpectralDensityAsync(double spectralDensity,
                    double deltaEcs, int time = 0)
                {
                    if (time > Cache.Times)
                        ThrowHelper.ThrowArgumentOutOfRangeException("The circle time is out of range!");

                    var resultSpectralDensity = spectralDensity == 0
                        ? Math.Round(1.0 / (k * deltaEcs + b), 2) // 首次：绝对目标Ecs对应的频率变化率
                        : Math.Round(1.0 / (1.0 / spectralDensity + k * deltaEcs), 2); // 迭代：用实测残差做增量修正  Δ(1/f) = k · ΔECS  →  1/f_new = 1/f_current + k · deltaEcs

                    if (double.IsNaN(resultSpectralDensity) || resultSpectralDensity == 0)
                    {
                        DialogWindowProvider.ShowDialog(
                            "Get Rate Change By Relational function Failed! The Points is not enough!",
                            DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(resultSpectralDensity));
                    }

                    await GetBestFocusAndAstigmatismResultAsync(resultSpectralDensity, cancellationToken)
                        .ConfigureAwait(false);

                    var iterationAstigmatismDTOItem = CalibratingItem.Items.Last();
                    time++;

                    var targetECS = astigmatismItem.BestFocus.BestXStrehlRatioECS;
                    var deltaECS = targetECS - iterationAstigmatismDTOItem.BestFocus.BestYStrehlRatioECS;

                    if (Math.Abs(deltaECS) > Cache.XYBestFocusEcsOffsetThreshold)
                        return await GetBestSpectralDensityAsync(resultSpectralDensity, deltaECS, time).ConfigureAwait(false);

                    return iterationAstigmatismDTOItem;
                }
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3,
                    new HtmlComment($"Calibrate Failed.Error: {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation,
                    Cache.Item.LaserLightInformation.Coefficient);
                LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK,
                DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var title = selectedReviewItem.ProductivityInformation.ToString();

                    if (selectedReviewItem.IsCalibrated == false)
                    {
                        errorMessageStringBuilder.AppendLine($"{title}: Error");
                        continue;
                    }

                    Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;
                    Cache.ApodizationModeEnum = selectedReviewItem.ApodizationModeEnum;

                    AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.Item.CalChipSiteModelEnum;
                    AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;
                    AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.Item.IsDarkFieldAlignment;
                    await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

                    selectedReviewItem.Items = [];

                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        Cache.ProductivityInformation,
                        Cache.ApodizationModeEnum,
                        Cache.Item.MicroscopeLensInformation,
                        Cache.Item.StartPosition,
                        Cache.Item.ScanLength,
                        Cache.Item.CenterECS,
                        Cache.Item.RangeECS,
                        AstigmatismCIBInformation = Cache.Item.CIBInformation,
                        Cache.Item.CIBConfiguration,
                        Cache.Item.LaserLightInformation,
                        IsKeepRawImageCIBProfileModeEnum =
                            Cache.Item.CIBConfiguration.CIBProfileMode == CIBProfileModeEnum.PMTVoltage
                    }), HtmlLogUniqueId.LoggingHtml());

                    // 下发默认Prescan波形
                    LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation,
                        Cache.Item.LaserLightInformation.Coefficient);

                    // 下发校准的Chirp波形
                    LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum,
                        selectedReviewItem.ResultDTO.ChirpAODWaveformProfiles);

                    // 全光斑BestFocus诊断
                    var bestFocusResults = await GetPMTImagesByXZSyncAsync(
                            HostEnvironment.IsDevelopment()
                                ? [.. ApplicationCookie.CIBInformations.Where(t => t.PMTId is 7 or 8 or 9)]
                                : ApplicationCookie.CIBInformations
                            , cancellationToken)
                        .ConfigureAwait(false);

                    // 散光对象验证结果
                    var verifyAstigmatismBestFocusDTO = bestFocusResults.Single(t =>
                        t.cibInformation == Cache.Item.CIBInformation);

                    selectedReviewItem.ResultDTO.BestFocus = verifyAstigmatismBestFocusDTO.bestFocus;

                    if (selectedReviewItem.IsCalibrated)
                        selectedReviewItem.IsVerified =
                            selectedReviewItem.ResultDTO.XYBestFocusOffsetEcs <= Cache.XYBestFocusEcsOffsetThreshold
                            && selectedReviewItem.ResultDTO.BestFocus.BestXStrehlRatioPoint.Y >= Cache.XQualityThreshold
                            && selectedReviewItem.ResultDTO.BestFocus.BestYStrehlRatioPoint.Y >=
                            Cache.YQualityThreshold;

                    var htmlBullet = new HtmlBullet(new
                    {
                        Cache.XQualityThreshold,
                        Cache.YQualityThreshold,
                        AllPMTBestFocusResults = new HtmlContainer([
                            ..bestFocusResults
                                .OrderBy(t => t.cibInformation)
                                .Select(t => new HtmlExpand(t.cibInformation.ToString(),
                                    new HtmlQuote(new
                                    {
                                        t.bestFocus.RawImageFilePath,
                                        XStrehlRatioScatterPlot =
                                            new HtmlContainer([
                                                .. t.bestFocus.XStrehlRatioScatterPlotControl
                                                    .GetAllHtmlPlot2DLinesCharts()
                                            ]),
                                        YStrehlRatioScatterPlot =
                                            new HtmlContainer([
                                                ..t.bestFocus.YStrehlRatioScatterPlotControl
                                                    .GetAllHtmlPlot2DLinesCharts()
                                            ])
                                    })))
                        ]),
                        AstigmastimVerifyResult =
                            new HtmlQuote(selectedReviewItem.ResultDTO.ToFlatnessHtmlAnonymous())
                    });

                    if (selectedReviewItem.IsOk)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet,
                            HtmlLogUniqueId.LoggingHtml());
                    else
                    {
                        errorMessageStringBuilder.AppendLine($"{title}: Error");
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet,
                            HtmlLogUniqueId.LoggingHtml());
                    }
                }
                finally
                {
                    LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation,
                        Cache.Item.LaserLightInformation.Coefficient);
                    LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
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


    private void ClearCalibrationTemp()
    {
        CalibratingItem = new AODBestFocusAndAstigmatismDTO
        {
            ProductivityInformation = Cache.ProductivityInformation.Clone(),
            ApodizationModeEnum = Cache.ApodizationModeEnum
        };
        SynchronizationContextProvider.Send(CalibratingItem.Items.Clear);
    }


    private bool Save(IReadOnlyList<AODBestFocusAndAstigmatismDTO> dtos, CancellationToken cancellationToken) =>
        InvokeSave(update =>
        {
            update(Cache);

            foreach (var dto in dtos)
            {
                update(dto);
                Calibrations =
                [
                    dto,
                    .. Calibrations.Where(t => (t.ProductivityInformation == dto.ProductivityInformation && t.ApodizationModeEnum == dto.ApodizationModeEnum) == false)
                ];
            }

            ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
            ApplicationCookieService.SetCache(Cache, cancellationToken);
        });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<AODBestFocusAndAstigmatismDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationAndApodizationStatus
            {
                SelectedItem = t,
                Items = [.. ApplicationCookie.OpticsApodizationModeEnums.Select(tt => new OpticsApodizationModeStatus { SelectedItem = tt, IsCalibrated = false })]
            })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                            && ApplicationCookie.OpticsApodizationModeEnums.Contains(t.ApodizationModeEnum))
                .DistinctBy(t => (t.ProductivityInformation, t.ApodizationModeEnum))
                .Select(t =>
                {
                    CalibratingStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .Items
                        .Single(tt => tt.SelectedItem == t.ApodizationModeEnum)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.OpticsMagTypeProductivityInformations.Count * ApplicationCookie.OpticsApodizationModeEnums.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.SelectMany(productivityInformation =>
                ApplicationCookie.OpticsApodizationModeEnums.Select(opticsApodizationModeEnum =>
                {
                    var item = Calibrations.SingleOrDefault(tt => tt.ProductivityInformation == productivityInformation
                                                                  && tt.ApodizationModeEnum == opticsApodizationModeEnum);

                    return new CalibrationViewModelStatus.Detail(
                        $"{productivityInformation}/ {opticsApodizationModeEnum}",
                        item?.IsCalibrated,
                        item?.IsVerified);
                }))
        ];
    }

    #endregion 校准

    #region 算法

    private async Task GetBestFocusAndAstigmatismResultAsync(
        double spectralDensity,
        CancellationToken cancellationToken)
    {
        // 下发波形
        var bandWidth = Cache.Item.DefaultGenerateChirpAODWaveformParam.SoundPacketLength * spectralDensity;
        var generateChirpAODWaveformParam = Cache.Item.DefaultGenerateChirpAODWaveformParam.Clone();
        generateChirpAODWaveformParam.BandWidth = bandWidth;
        generateChirpAODWaveformParam.DirectoryPath = ChirpFileDirectory;

        var aodWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(generateChirpAODWaveformParam.AdaptTo(), cancellationToken);

        var chirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);

        LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum,
            chirpAODWaveformProfiles);

        var astigmatismDTOItem = new AODBestFocusAndAstigmatismDTOItem
        {
            SpectralDensity = spectralDensity,
            GenerateChirpAODWaveformParam = generateChirpAODWaveformParam,
            ChirpAODWaveformProfiles = chirpAODWaveformProfiles
        };

        // BestFocus
        var bestFocusResults = await GetPMTImagesByXZSyncAsync([Cache.Item.CIBInformation], cancellationToken)
            .ConfigureAwait(false);

        astigmatismDTOItem.BestFocus = bestFocusResults[0].bestFocus;

        CalibratingItem.Items = [.. CalibratingItem.Items, astigmatismDTOItem];

        Logger.LogHtmlInformation(
            $"spectralDensity: {spectralDensity}",
            HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Result = new HtmlQuote(astigmatismDTOItem.ToFlatnessHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
    }

    private async Task<(CIBInformation cibInformation, BestFocus bestFocus)[]> GetPMTImagesByXZSyncAsync(
        IReadOnlyList<CIBInformation> cibInformations,
        CancellationToken cancellationToken)
    {
        (CIBInformation, BestFocus)[] bestFocusResults = [];

        var startECS = Cache.Item.CenterECS - Cache.Item.RangeECS;
        var stopECS = Cache.Item.CenterECS + Cache.Item.RangeECS;
        foreach (var grabInformations in cibInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStartPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Bright,
                Cache.ProductivityInformation,
                grabInformations[0],
                Cache.Item.StartPosition,
                Cache.Item.MicroscopeLensInformation);

            var currentStopPosition = currentStartPosition + new Vector(Cache.Item.ScanLength, 0);

            var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                currentStartPosition,
                currentStopPosition,
                startECS,
                stopECS,
                grabInformations,
                (false, Cache.Item.CalChipSiteModelEnum),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                true,
                cancellationToken,
                isKeepRawImageCIBProfileModeEnum: Cache.Item.CIBConfiguration.CIBProfileMode ==
                                                  CIBProfileModeEnum.PMTVoltage).ConfigureAwait(false);

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _ = darkFieldImage;
            }

            foreach (var darkFieldImage in darkFieldImages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bestFocusResults = [.. bestFocusResults, GetBestFocusResult(darkFieldImage)];
            }
        }

        return bestFocusResults;

        (CIBInformation, BestFocus) GetBestFocusResult(DarkFieldRawScanImageDTO darkFieldRawScanImage)
        {
            var item = new BestFocus { RawImageFilePath = darkFieldRawScanImage.RawImageFilePath };

            try
            {
                var temp = darkFieldRawScanImage.Clone();
                temp.IsKeepRawImageCIBProfileModeEnum = false;
                using var image = temp.GetImage();

                item = CalibrationAlgorithmService.GetBestFocus(image, startECS, stopECS);
                item.RawImageFilePath = darkFieldRawScanImage.RawImageFilePath;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlError("Best Focus Error", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                {
                    darkFieldRawScanImage.RawImageFilePath,
                    Exception = ex
                }), HtmlLogUniqueId.LoggingHtml());
            }

            return (darkFieldRawScanImage.CIBInformation, item);
        }
    }

    #endregion 算法
}