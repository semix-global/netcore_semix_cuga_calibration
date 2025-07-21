using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Laser.FocusShift;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.Rtfc;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Core.Utilities;
using CugaCalibration.ViewModels.Common.Windows.Diagnosis.RtfcDiagonosis;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis.RtfcDiagnosis;

[IOCAppService(ServiceType = typeof(AfFocusDiagnosisViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AfFocusDiagnosisViewModel(CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel) : RtfcDiagnosisViewModelBase
{
    #region 属性

    /// <summary>
    /// 校准Html日志文件路径名称
    /// </summary>
    public override string LogHtmlFileName => "RtfcDiagnosis_AfFocus";

    /// <summary>
    /// 焦点位移对象集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FocusShiftDto> _focusShiftDtoItems = [];

    /// <summary>
    /// 诊断对象集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RtfcDto> _rtfcDtoItems = [];

    /// <summary>
    /// 诊断对象迭代结果集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RtfcDto> _rtfcDtoIterationItems = [];

    [ObservableProperty]
    private FocusShiftDto _resultFocusShiftDto = new();

    [ObservableProperty]
    private RtfcDto _resultRtfcDto = new();

    /// <summary>
    /// 焦点偏移缓存
    /// </summary>
    [ObservableProperty]
    private FocusShiftCache _focusShiftCache = new();

    /// <summary>
    /// 诊断缓存
    /// </summary>
    [ObservableProperty]
    private RtfcCache _cache = new();

    public double NscDiagnosisK = 1;

    #region 界面

    [ObservableProperty]
    private RtfcDto? _selectedItemDto;

    /// <summary>
    /// 界面图标显示
    /// </summary>
    [ObservableProperty]
    private List<WpfPlotModel> _plotList = [];

    [ObservableProperty]
    private Point[] _ecsPoints = [];

    #endregion 界面

    #region 缓存

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _laserPixelSizeItems = [];

    [ObservableProperty]
    private LaserLineCentricityItemDto[] _laserLineCentricityItems = [];

    /// <summary>
    /// chuck ecs
    /// </summary>
    [ObservableProperty]
    private double _chuckAfEcs;

    /// <summary>
    /// chuck温度
    /// </summary>
    [ObservableProperty]
    private double _xAxisTemperature;

    /// <summary>
    /// chuck温度
    /// </summary>
    [ObservableProperty]
    private double _yAxisTemperature;

    /// <summary>
    /// cib温度
    /// </summary>
    [ObservableProperty]
    private double _cibTemperature;

    /// <summary>
    /// review相机温度
    /// </summary>
    [ObservableProperty]
    private double _reviewCamTemperature;

    #endregion

    #endregion 属性

    [RelayCommand]
    private async Task<bool> LoadAsync()
    {
        try
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

            if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckGantryDto>(out _, out errorMessage) == false)
            {
                DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopePixelSizeItemDto>(out var microscopePixelSizeItems, out errorMessage) == false)
            {
                DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            MicroscopePixelSizeItems = microscopePixelSizeItems;

            if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeCentricityItemDto>(out _, out errorMessage) == false)
            {
                DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPixelSizeItemDto>(out var laserPixelSizeItems, out errorMessage) == false)
            {
                DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            LaserPixelSizeItems = laserPixelSizeItems;

            if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserLineCentricityItemDto>(out var laserLineCentricityItems, out errorMessage) == false)
            {
                DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            LaserLineCentricityItems = laserLineCentricityItems;

            FocusShiftCache = CacheProvider.GetOrDefault<FocusShiftCache>();
            Cache = CacheProvider.GetOrDefault<RtfcCache>();
            ResultFocusShiftDto = CacheProvider.GetOrDefault<FocusShiftDto>();
            StageViewModel.SetAbsoluteStageTheta(0);
            AfViewModel.ToggleCalChipSiteModelEnum(FocusShiftCache.CalChipSiteModelEnum);
            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.HighSiteFindPosition);
            FocusShiftCache.OpticsMagTypeEnum = OpticsMagTypeEnum.High;
            Cache.OpticsMagTypeEnum = OpticsMagTypeEnum.High;

            LaserViewModel.ToggleEnableAutoGain(true);
            LaserViewModel.ToggleEnableL0K(false);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Loaded Failed", nameof(AfFocusDiagnosisViewModel));
            return false;
        }
    }

    [RelayCommand]
    private async Task GetPointAsync(string parameter)
    {
        try
        {
            Logger.LogInformation("{@Name}: Get Point Image Start", parameter);
            await Task.Run(() =>
            {
                try
                {
                    var result = StageViewModel.GetBrightFieldStagePosition();

                    switch (parameter)
                    {
                        case "LowFindPosition":
                            FocusShiftCache.LowSiteFindPosition = result;
                            FocusShiftCache.HighSiteFindPosition = result;

                            FocusShiftCache.LowSiteTemplateFilePath = $"{TemplateFileDirectory}\\{FocusShiftCache.LowMicroscopeMagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                            var generateTemplateLow = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, FocusShiftCache.LowSiteTemplateFilePath, FocusShiftCache.AlgorithmTemplateSizeEnum);
                            if (generateTemplateLow == false) DialogWindowProvider.ShowDialog("Generate Low Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            else FocusShiftCache.LowSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(FocusShiftCache.LowSiteTemplateFilePath);

                            break;

                        case "HighFindPosition":
                            {
                                Logger.LogHtmlInformation("Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                                IsEnableWindow = false;

                                FocusShiftCache.HighSiteFindPosition = result;
                                FocusShiftCache.HighSiteTemplateFilePath = $"{TemplateFileDirectory}\\{FocusShiftCache.HighMicroscopeMagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                                var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(FocusShiftCache.AlgorithmTemplateTypeEnum, FocusShiftCache.HighSiteTemplateFilePath, FocusShiftCache.AlgorithmTemplateSizeEnum);
                                if (generateTemplateHigh == false)
                                {
                                    DialogWindowProvider.ShowDialog("Generate High Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    return;
                                }
                                else FocusShiftCache.HighSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(FocusShiftCache.HighSiteTemplateFilePath);

                                var settingDarkFieldAutoFocusParam = FocusShiftCache.GetDarkFieldAutoFocusParam();
                                var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                                    FocusShiftCache.CalChipSiteModelEnum,
                                    FocusShiftCache.HighSiteFindPosition,
                                    (false, 0.26),
                                    false,
                                    settingDarkFieldAutoFocusParam,
                                    800,
                                    FocusShiftCache.OpticsMagTypeEnum,
                                    FocusShiftCache.StageSpeedEnum,
                                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
                                var detectImageDirectory = ImageFileDirectory;
                                using var image = darkFieldImageDto;

                                FocusShiftCache.DarkFiledTemplateFilePath = $"{TemplateFileDirectory}\\1_{FocusShiftCache.OpticsMagTypeEnum}_{Guid.NewGuid()}";
                                if (FocusShiftCache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
                                {
                                    if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, FocusShiftCache.DarkFiledTemplateFilePath) == false)
                                    {
                                        DialogWindowProvider.ShowDialog("Generate Dark Field Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                        return;
                                    }
                                }
                                else
                                {
                                    var filePath = $"{detectImageDirectory}\\DarkFieldTemplateOriginImage_{Guid.NewGuid()}).jpg";
                                    HalconHelper.Save(darkFieldImageDto.Image, filePath);
                                    createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                                    createDarkImageTemplateWindowViewModel.TemplateFilePath = FocusShiftCache.DarkFiledTemplateFilePath;

                                    var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                                    if (showDialog == false)
                                    {
                                        DialogWindowProvider.ShowDialog("Generate Dark Field Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                        return;
                                    }
                                }

                                FocusShiftCache.DarkFiledTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(FocusShiftCache.DarkFiledTemplateFilePath);
                            }
                            break;

                        default:
                            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(parameter));
                            break;
                    }

                    Logger.LogInformation("{@Name}: Get Point Image OK!", parameter);
                }
                catch (Exception ex)
                {
                    Logger.LogInformation("{@Name}: Get Point Image OK!{@Exception}", parameter, ex.Message);
                }
                finally
                {
                    Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(true));
                    IsEnableWindow = true;
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Image Failed", parameter);
        }
    }

    #region 诊断业务

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task FocusShiftCalibrationAsync(CancellationToken cancellationToken)
    {
        var result = await Task.Run(async () =>
        {
            try
            {
                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                IsEnableWindow = false;
                ResultFocusShiftDto.IsCalibrated = false;
                HtmlLogUniqueId = Guid.NewGuid();
                Logger.LogHtmlInformation($"{LogHtmlFileName}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());

                var (findFocusMin, findFocusMax, findFocusInterval) = FocusShiftCache.GetSteppingRangeParam();
                if (findFocusMin <= 0 || findFocusMax <= 0 || findFocusInterval == 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Min > 0 and Focs Max > 0 and Focus Interval > 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                Logger.LogHtmlInformation("1. Param", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    FocusShiftCache.CalChipSiteModelEnum,
                    FocusShiftCache.AlgorithmTemplateSizeEnum,
                    FocusShiftCache.AlgorithmTemplateTypeEnum,
                    LowMagnification = FocusShiftCache.LowMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                    HighMagnification = FocusShiftCache.HighMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                    FocusShiftCache.OpticsMagTypeEnum,
                    FocusShiftCache.StageSpeedEnum,
                    Pmt = 8,
                    xPixelWidth = 800,
                    FocusShiftCache.LowSiteFindPosition,
                    FocusShiftCache.HighSiteFindPosition,
                    findFocusMin,
                    findFocusMax,
                    findFocusInterval,
                    HtmlTab = new HtmlTab(new
                    {
                        LowSiteTemplateImage = new HtmlImage(FocusShiftCache.LowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighSiteTemplateImage = new HtmlImage(FocusShiftCache.HighSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        DarkFieldTemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                // Bright Field Match
                Logger.LogHtmlInformation($"2. Bright Field Match Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                if (ReviewViewModel.TryGetMatchPosition(FocusShiftCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, FocusShiftCache.LowSiteFindPosition, FocusShiftCache.LowMicroscopeMagnificationInfo, FocusShiftCache.LowSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "Low Magnification", out var lowResultPosition, out _, out _, out _, out _, FocusShiftCache.CalChipSiteModelEnum) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
                }

                if (ReviewViewModel.TryGetMatchPosition(FocusShiftCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowResultPosition, FocusShiftCache.HighMicroscopeMagnificationInfo, FocusShiftCache.HighSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "High Magnification", out var highResultPosition, out _, out _, out _, out _, FocusShiftCache.CalChipSiteModelEnum) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
                }

                //获得明场Af模式下的ECS值
                var brightFieldMachinePosition = StageViewModel.BrightFieldToMachinePosition(highResultPosition);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(highResultPosition, FocusShiftCache.CalChipSiteModelEnum);
                var afEcs = AfViewModel.GetSensorAverageEcsValue();
                Logger.LogHtmlInformation("Bright Field Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    afEcs,
                    highSiteFindPosition = highResultPosition,
                    brightFieldMachinePosition
                }), HtmlLogUniqueId.LoggingHtml());
                var darkFieldAutoFocusParam = FocusShiftCache.GetDarkFieldAutoFocusParam();
                // Dark Field Match
                Logger.LogHtmlInformation($"3. Dark Field Match Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                var detectImageDirectory = ImageFileDirectory;
                var originImagePath = $"{detectImageDirectory}\\DarkFieldMatchOriginImage_{Guid.NewGuid()}).jpg";
                if (LaserViewModel.TryGetMatchPosition(
                        FocusShiftCache.AlgorithmTemplateTypeEnum,
                        FocusShiftCache.CalChipSiteModelEnum,
                        8,
                        highResultPosition,
                        FocusShiftCache.DarkFiledTemplateFilePath,
                        originImagePath,
                        HtmlLogUniqueId,
                        string.Empty,
                        string.Empty,
                        darkFieldAutoFocusParam,
                        out var darkFieldResultPosition,
                        out _,
                        out _,
                        out _,
                        true,
                        800,
                        FocusShiftCache.OpticsMagTypeEnum,
                        FocusShiftCache.StageSpeedEnum,
                        CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
                        FocusShiftCache.LightCoefficient) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                var darkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldResultPosition);

                SynchronizationContextProvider.Send(() =>
                {
                    EcsPoints = [];
                    FocusShiftDtoItems.Clear();
                });
                // 获得NSC模式下的当前的ECS、NSC值
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(darkFieldMachinePosition);
                var isAutoFocus = AfViewModel.SetDarkFieldAutoFocus(darkFieldAutoFocusParam, FocusShiftCache.OpticsMagTypeEnum, FocusShiftCache.CalChipSiteModelEnum);
                if (isAutoFocus)
                    AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(1500, cancellationToken);
                var nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                var autoFocusNsc = nscBuffers.Average();
                var autoFocusEcs = AfViewModel.GetSensorAverageEcsValue();

                (var nscDiagnosis, NscDiagnosisK) = AfViewModel.NscDiagnosis(autoFocusEcs, FocusShiftCache.CalChipSiteModelEnum);
                Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    darkFieldAutoFocusParam.DswEcsValue,
                    darkFieldAutoFocusParam.DswMotorValue,
                    darkFieldAutoFocusParam.IsEnableDsw,
                    DarkFieldFindPosition = darkFieldResultPosition,
                    darkFieldMachinePosition,
                    autoFocusEcs,
                    autoFocusNsc,
                    NscDiagnosisK,
                    NscDiagnosisTraceBuffers = new HtmlPlot2DLinesChart([
                        ("Ecs-Nsc", nscDiagnosis),
                    ], "NscDiagnosisTraceBuffers"),
                    NscTraceBuffers = new HtmlPlot2DLinesChart([
                        ("Time-nm", nscBuffers.Select(t => t * NscDiagnosisK).ToPoints()),
                    ], "Nsc(nm)TraceBuffers"),
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation($"4. Get Quality", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                // 初始化
                var focusShift = new List<FocusShiftDto>();
                var minEcs = autoFocusEcs - findFocusMin;
                var maxEcs = autoFocusEcs + findFocusMax;
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    autoFocusEcs,
                    FindFocusMin = minEcs,
                    FindFocusMax = maxEcs,
                    findFocusInterval,
                    FindPosition = FocusShiftCache.HighSiteFindPosition,
                    ImageFileDirectory
                }), HtmlLogUniqueId.LoggingHtml());
                foreach (var (index, ecsValueTemp) in Enumerable.Range(0, Convert.ToInt32((findFocusMax + findFocusMin) / findFocusInterval) + 1)
                             .Select(x => Math.Min(minEcs + x * findFocusInterval, maxEcs))
                             .Select((d, i) => (i, d)))
                {
                    focusShift.Add(new FocusShiftDto
                    {
                        Index = index,
                        BrightFieldFindPosition = brightFieldMachinePosition,
                        DarkFieldFindPosition = darkFieldMachinePosition,
                        BrightFieldEcsValue = afEcs,
                        DarkFieldEcsValue = ecsValueTemp,
                        NscValue = 0,
                        DarkFieldQuality = 0,
                        DarkFieldImageFilePath = ImageFileDirectory
                    });
                }

                // Quality
                var listDownResult = new List<bool>();
                double? downQualityValue = null;
                Logger.LogHtmlInformation("Get Optimal Quality", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var focusShiftDtoItem in focusShift.OrderBy(t => t.Index))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rtfcDto = new RtfcDto
                    {
                        Index = focusShiftDtoItem.Index,
                        EcsValue = focusShiftDtoItem.DarkFieldEcsValue,
                        BrightFieldFindPosition = darkFieldResultPosition
                    };
                    if (GetQuality(ref rtfcDto) == false) return false;

                    focusShiftDtoItem.DarkFieldQuality = rtfcDto.Quality;
                    focusShiftDtoItem.DarkFieldImageFilePath = rtfcDto.DarkFieldImageFilePath;
                    focusShiftDtoItem.DarkFieldOriginImageFilePath = rtfcDto.DarkFieldOriginImageFilePath;
                    SynchronizationContextProvider.Send(() =>
                    {
                        FocusShiftDtoItems.Add(focusShiftDtoItem);
                        EcsPoints = [.. EcsPoints, new Point(focusShiftDtoItem.DarkFieldEcsValue, focusShiftDtoItem.DarkFieldQuality)];
                    });

                    if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < focusShiftDtoItem.DarkFieldQuality);
                    downQualityValue = focusShiftDtoItem.DarkFieldQuality;
                    if (EnumerableHelper.HasConsecutiveFalse(listDownResult, 10)) break; // 连续10个下降说明已经到了最低点
                }

                ResultFocusShiftDto = FocusShiftDtoItems.Select(s => s.Clone())
                    .Maxima(s => s.DarkFieldQuality)
                    .Single();
                ResultFocusShiftDto.EcsOffset = ResultFocusShiftDto.DarkFieldEcsValue - afEcs;
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultFocusShiftDto.DarkFieldEcsValue,
                    ResultFocusShiftDto.BrightFieldEcsValue,
                    ResultFocusShiftDto.DarkFieldQuality,
                    ResultFocusShiftDto.EcsOffset,
                    EcsQuality = new HtmlPlot2DLinesChart([
                        ("Ecs-Quality", FocusShiftDtoItems.Select(t => new Point(t.DarkFieldEcsValue, t.DarkFieldQuality)).ToArray())
                    ], "EcsQuality")
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("BF To DF Machine Offset Calibration", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                // 监控NSC值
                var resultMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldResultPosition);
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(resultMachinePosition);
                AfViewModel.ToggleBrightFieldEnable(false);
                //await Task.Delay(2000, cancellationToken);
                AfViewModel.SetSensorEcsValue(ResultFocusShiftDto.DarkFieldEcsValue);
                await Task.Delay(3000, cancellationToken);
                nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                ResultFocusShiftDto.NscValue = nscBuffers.Average();
                ResultFocusShiftDto.AutoFocusEcs = autoFocusEcs;
                ResultFocusShiftDto.AutoFocusNsc = autoFocusNsc;
                // 获得照明焦点偏移量
                if (LaserViewModel.TryGetMatchPositionByNotAutoFocus(
                        FocusShiftCache.AlgorithmTemplateTypeEnum,
                        8,
                        darkFieldResultPosition,
                        FocusShiftCache.DarkFiledTemplateFilePath,
                        ResultFocusShiftDto.DarkFieldImageFilePath,
                        HtmlLogUniqueId,
                        string.Empty,
                        string.Empty,
                        out var resultPosition,
                        out _,
                        out _,
                        out _,
                        true,
                        800,
                        FocusShiftCache.OpticsMagTypeEnum,
                        FocusShiftCache.StageSpeedEnum,
                        CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
                        FocusShiftCache.LightCoefficient) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Dark Field Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                ResultFocusShiftDto.DarkFieldFindPosition = StageViewModel.DarkFieldToMachinePosition(resultPosition);
                ResultFocusShiftDto.SettingDarkFieldAutoFocusParam = darkFieldAutoFocusParam.Clone();
                ResultFocusShiftDto.IsCalibrated = ResultFocusShiftDto.FocusShiftOffset < FocusShiftCache.AfMotorReviseThreshold;
                ResultFocusShiftDto.AfMotorOffset = ResultFocusShiftDto.FocusShiftOffset / FocusShiftCache.AfEcsRelation;
                if (await SavingAsync().ConfigureAwait(true) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Saving Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
                Logger.LogHtmlInformation($"Calibration {(ResultFocusShiftDto.IsCalibrated ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    darkFieldAutoFocusParam.DswEcsValue,
                    darkFieldAutoFocusParam.DswMotorValue,
                    darkFieldAutoFocusParam.IsEnableDsw,
                    ResultFocusShiftDto.BrightFieldFindPosition,
                    ResultFocusShiftDto.DarkFieldFindPosition,
                    AfEcs = ResultFocusShiftDto.BrightFieldEcsValue,
                    ResultFocusShiftDto.AutoFocusEcs,
                    ResultFocusShiftDto.AutoFocusNsc,
                    ResultEcs = ResultFocusShiftDto.DarkFieldEcsValue,
                    ResultEcsNsc = ResultFocusShiftDto.NscValue,
                    BrightFiedlToDarkFieldOffset = ResultFocusShiftDto.BrightFieldToDarkFieldOffset,
                    ResultFocusShiftDto.EcsOffset,
                    ResultFocusShiftDto.FocusShiftOffset,
                    ResultFocusShiftDto.AfMotorOffset,
                    ResultNscTraceBuffers = new HtmlPlot2DLinesChart([
                        ("Time-Nsc", nscBuffers.Select(t => t * NscDiagnosisK).ToPoints()),
                    ], "NscTraceBuffers"),
                }), HtmlLogUniqueId.LoggingHtml());

                return ResultFocusShiftDto.IsCalibrated;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error: Focus Shift Calibration Failed! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(true));
                IsEnableWindow = true;
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.HighSiteFindPosition);
            }
        }, cancellationToken).ConfigureAwait(false);
        Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"FocusShiftCalibration_{(result ? "OK" : "Failed")}"));
        Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
    }

    public override async Task<bool> DiagnosisActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = true;
            DialogWindowProvider.TryShowDialog("Do you want to circle test?", out DialogResultEnum resultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    HtmlLogUniqueId = Guid.NewGuid();
                    Logger.LogHtmlInformation($"{LogHtmlFileName}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
                    PlotList.Clear();
                    result = await DiagnosisAsync(cancellationToken).ConfigureAwait(false);
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{DiagnosisHtmlLogFileName}_{(result ? "OK" : "Failed")}"));
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
                    if (resultEnum is DialogResultEnum.No)
                        return result;
                }
                catch (OperationCanceledException)
                {
                    // 正常退出
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Circle Test Error!");
                }

                // 等待1分钟
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            return result;
            //return await DiagnosisAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.HighSiteFindPosition);
        }
    }

    private async Task<bool> DiagnosisAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var (findFocusMin, findFocusMax, findFocusInterval) = FocusShiftCache.GetSteppingRangeParam();

                if (findFocusMin <= 0 || findFocusMax <= 0 || findFocusInterval == 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Min > 0 and Focs Max > 0 and Focus Interval > 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                if (ResultFocusShiftDto.IsCalibrated == false)
                {
                    DialogWindowProvider.ShowDialog("Please calibration offset first!", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                IsEnableWindow = false;
                var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
                Logger.LogHtmlInformation("1. Param", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    FocusShiftCache.AlgorithmTemplateSizeEnum,
                    FocusShiftCache.AlgorithmTemplateTypeEnum,
                    LowMagnification = FocusShiftCache.LowMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                    HighMagnification = FocusShiftCache.HighMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                    FocusShiftCache.CalChipSiteModelEnum,
                    FocusShiftCache.OpticsMagTypeEnum,
                    FocusShiftCache.StageSpeedEnum,
                    Pmt = 8,
                    xPixelWidth = 800,
                    ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswEcsValue,
                    ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswMotorValue,
                    ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.IsEnableDsw,
                    FocusShiftCache.LowSiteFindPosition,
                    FocusShiftCache.HighSiteFindPosition,
                    findFocusMin,
                    findFocusMax,
                    findFocusInterval,
                    Cache.ObliqueAngle,
                    Cache.OffsetThreshold,
                    HtmlTab = new HtmlTab(new
                    {
                        LowSiteTemplateImage = new HtmlImage(FocusShiftCache.LowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighSiteTemplateImage = new HtmlImage(FocusShiftCache.HighSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        DarkFieldTemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                // Bright Field Match
                Logger.LogHtmlInformation($"2. Bright Field Match Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                if (ReviewViewModel.TryGetMatchPosition(FocusShiftCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, FocusShiftCache.LowSiteFindPosition, FocusShiftCache.LowMicroscopeMagnificationInfo, FocusShiftCache.LowSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "Low Magnification", out var lowResultPosition, out _, out _, out _, out _, FocusShiftCache.CalChipSiteModelEnum) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
                }

                if (ReviewViewModel.TryGetMatchPosition(FocusShiftCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowResultPosition, FocusShiftCache.HighMicroscopeMagnificationInfo, FocusShiftCache.HighSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "High Magnification", out var highResultPosition, out _, out _, out _, out _, FocusShiftCache.CalChipSiteModelEnum) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
                }

                //获得明场Af模式下的ECS值
                var brightFieldMachinePosition = StageViewModel.BrightFieldToMachinePosition(highResultPosition);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(highResultPosition, FocusShiftCache.CalChipSiteModelEnum);
                var afEcs = AfViewModel.GetSensorAverageEcsValue();
                Logger.LogHtmlInformation("Bright Field Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    afEcs,
                    highSiteFindPosition = highResultPosition,
                    brightFieldMachinePosition
                }), HtmlLogUniqueId.LoggingHtml());

                // 获得NSC模式下的ECS、NSC值
                Cache.IdeaDarkFieldMachinePosition = brightFieldMachinePosition + (Vector)ResultFocusShiftDto.BrightFieldToDarkFieldOffset;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.IdeaDarkFieldMachinePosition);

                var isAutoFocus = AfViewModel.SetDarkFieldAutoFocus(ResultFocusShiftDto.SettingDarkFieldAutoFocusParam, FocusShiftCache.OpticsMagTypeEnum, FocusShiftCache.CalChipSiteModelEnum);
                if (isAutoFocus)
                    AfViewModel.ToggleDarkFieldEnable(true);
                var nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                var autoFocusNsc = nscBuffers.Average();
                var autoFocusEcs = AfViewModel.GetSensorAverageEcsValue();
                (var nscDiagnosis, NscDiagnosisK) = AfViewModel.NscDiagnosis(autoFocusEcs, FocusShiftCache.CalChipSiteModelEnum);

                var idealEcs = afEcs + ResultFocusShiftDto.EcsOffset;
                AfViewModel.ToggleBrightFieldEnable(false);
                AfViewModel.SetSensorEcsValue(idealEcs);
                await Task.Delay(3000, cancellationToken);
                var notAutoFocusNscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                var idealEcsNsc = notAutoFocusNscBuffers.Average();

                // 自动聚焦下ECS高度下的图像
                AfViewModel.ToggleBrightFieldEnable(false);
                AfViewModel.SetSensorEcsValue(autoFocusEcs);
                await Task.Delay(2000, cancellationToken);
                using var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImageByNotAutoFocus(
                    Cache.IdeaDarkFieldMachinePosition,
                    (false, FocusShiftCache.LightCoefficient),
                    true,
                    800,
                    FocusShiftCache.OpticsMagTypeEnum,
                    FocusShiftCache.StageSpeedEnum,
                    8,
                    3,
                    StageCoordinateSystemEnum.Machine);
                var nscDarkFieldImageFilePath =
                    $"{ImageFileDirectory}\\ECS({autoFocusEcs})_AutoFocus_Guid({HtmlLogUniqueId}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, nscDarkFieldImageFilePath);

                Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswEcsValue,
                    ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswMotorValue,
                    ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.IsEnableDsw,
                    Cache.IdeaDarkFieldMachinePosition,
                    idealEcs,
                    idealEcsNsc,
                    autoFocusEcs,
                    autoFocusNsc,
                    NscDiagnosisK,
                    NscDiagnosisTraceBuffers = new HtmlPlot2DLinesChart([
                        ("Ecs-Nsc", nscDiagnosis),
                    ], "NscDiagnosisTraceBuffers"),
                    IdeaEcsNscTraceBuffer = new HtmlPlot2DLinesChart([
                        ("Time-nm", notAutoFocusNscBuffers.Select(t => t * NscDiagnosisK).ToPoints()),
                    ], "IdeaEcsNscTraceBuffer"),
                    AutoFocusNscTraceBuffer = new HtmlPlot2DLinesChart([
                        ("Time-nm", nscBuffers.Select(t => t * NscDiagnosisK).ToPoints()),
                    ], "AutoFocusNscTraceBuffer"),
                    HtmlTab = new HtmlTab(new
                    {
                        nscDarkFieldImageFilePath = new HtmlImage(nscDarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    }),
                }), HtmlLogUniqueId.LoggingHtml());

                // 修正照明焦点
                Logger.LogHtmlInformation($"3. Revise Illumination Focus", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                // 迭代修正
                SynchronizationContextProvider.Send(() =>
                {
                    EcsPoints = [];
                    RtfcDtoIterationItems.Clear();
                });
                var iterationResult = false;
                for (var i = 0; i < 5; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Logger.LogHtmlInformation($"Iteration Times: {(i + 1)}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                    if (ReviseIlluminationFocus(idealEcs, cancellationToken))
                    {
                        iterationResult = true;
                        break;
                    }
                }

                if (iterationResult == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Get Optimal Quality Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                Logger.LogHtmlInformation("Iteration Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.ObliqueAngle,
                    ResultRtfcDto.DarkFieldMatchOffset,
                    ResultRtfcDto.DeltaEcs,
                    ResultRtfcDto.EcsValue,
                    ResultRtfcDto.Quality,
                    ReviseEcs = ResultRtfcDto.EcsValue,
                    ResultRtfcDto.LightAxisOffset,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(ResultRtfcDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    }),
                    IterationCurve = new HtmlPlot2DLinesChart([
                        ("Times-Ecs", RtfcDtoIterationItems.Select(t => t.EcsValue).ToList().ToPoints()),
                        ("Times-DeltaEcs", RtfcDtoIterationItems.Select(t => t.DeltaEcs).ToList().ToPoints()),
                        ("Times-IlluminationFocusOffset", RtfcDtoIterationItems.Select(t => t.LightAxisOffset).ToList().ToPoints()),
                    ], "IterationCurve"),
                }), HtmlLogUniqueId.LoggingHtml());

                // 修正af(方向未定、公式未定)
                Logger.LogHtmlInformation($"4. Revise Af", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                //ResultRtfcDto.DeltaNsc = (ResultRtfcDto.EcsValue - Cache.AutoFocusEcs) / Cache.AfEcsRelation;
                //ResultRtfcDto.AfMotor += ResultRtfcDto.DeltaNsc;
                //AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(ResultRtfcDto.AfMotor);
                //var verifyReviseAfResult = Verify(ResultRtfcDto, true);
                (ReviewCamTemperature, CibTemperature, XAxisTemperature, YAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
                //StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
                ChuckAfEcs = 0;
                ResultRtfcDto.IdealEcs = idealEcs;
                ResultRtfcDto.IdealEcsNsc = idealEcsNsc;
                ResultRtfcDto.AutoFocusEcs = autoFocusEcs;
                ResultRtfcDto.AutoFocusNsc = autoFocusNsc;
                Logger.LogHtmlInformation("Ok", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    XAxisTemperature,
                    yAxisTemperature,
                    ReviewCamTemperature,
                    CibTemperature,
                    ChuckAfEcs,
                    ResultFocusShiftDto.EcsOffset,
                    afEcs,
                    idealEcs,
                    idealEcsNsc,
                    autoFocusEcs,
                    autoFocusNsc,
                    RealEcs = ResultRtfcDto.EcsValue,
                    RealNsc = ResultRtfcDto.NscValue,
                    ResultRtfcDto.Quality,
                    ResultRtfcDto.DarkFieldMatchOffset,
                }), HtmlLogUniqueId.LoggingHtml());
                var csvFilePath = Path.Combine(CsvFileDirectory, "RtfcDiagnosis.csv");
                SaveCsv(csvFilePath);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Diagnosis Failed! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                //DialogWindowProvider.ShowDialog("Diagnosis Failed! Please Check Config And Try Again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
            finally
            {
                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(true));
                IsEnableWindow = true;
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.HighSiteFindPosition);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private bool GetQuality(ref RtfcDto rtfcItemDto)
    {
        try
        {
            Logger.LogHtmlInformation($"Get Quality,Ecs:{rtfcItemDto.EcsValue}", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
            // 防止移动后明场af模式打开
            AfViewModel.ToggleBrightFieldEnable(false);

            if (rtfcItemDto.Index == 0) Thread.Sleep(5000);
            AfViewModel.SetSensorEcsValue(rtfcItemDto.EcsValue);
            Thread.Sleep(1000);
            using var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImageByNotAutoFocus(
                rtfcItemDto.BrightFieldFindPosition,
                (false, FocusShiftCache.LightCoefficient),
                true,
                800,
                FocusShiftCache.OpticsMagTypeEnum,
                StageSpeedEnum.Low,
                8,
                3,
                StageCoordinateSystemEnum.Dark);

            using var scaleImage = HalconHelper.ScaleImageTo8Bit(darkFieldImageDto.Image);
            var xQuality = CalibrationAlgorithmService.GetDarkFieldQuality(scaleImage);
            //HOperatorSet.WriteObject(scaleImage, path);
            var qualityX = xQuality;
            rtfcItemDto.DarkFieldImageFilePath =
                $"{ImageFileDirectory}\\ECS({rtfcItemDto.EcsValue})_Guid({HtmlLogUniqueId}).jpg";
            rtfcItemDto.DarkFieldOriginImageFilePath = CalibrationConstantsHelper.ImagePathToRawImagePath(rtfcItemDto.DarkFieldImageFilePath);
            rtfcItemDto.Quality = qualityX;

            FileHelper.Save(darkFieldImageDto.Bytes, rtfcItemDto.DarkFieldOriginImageFilePath);
            HalconHelper.Save(darkFieldImageDto.Image, rtfcItemDto.DarkFieldImageFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                rtfcItemDto.EcsValue,
                ImageQuality = rtfcItemDto.Quality,
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(rtfcItemDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error: Get Quality Failed! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    private bool ReviseIlluminationFocus(double idealEcs, CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() =>
        {
            EcsPoints = [];
            RtfcDtoItems.Clear();
        });

        var rtfcDtoList = new List<RtfcDto>();
        var (findFocusMin, findFocusMax, findFocusInterval) = FocusShiftCache.GetSteppingRangeParam();
        var minEcs = idealEcs - findFocusMin;
        var maxEcs = idealEcs + findFocusMax;
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            idealEcs,
            FindFocusMin = minEcs,
            FindFocusMax = maxEcs,
            findFocusInterval,
            ImageFileDirectory
        }), HtmlLogUniqueId.LoggingHtml());
        // 初始化

        var findPosition = StageViewModel.MachineToDarkFieldPosition(Cache.IdeaDarkFieldMachinePosition);
        foreach (var (index, ecsValueTemp) in Enumerable.Range(0, Convert.ToInt32((findFocusMax + findFocusMin) / findFocusInterval) + 1)
                     .Select(x => Math.Min(minEcs + x * findFocusInterval, maxEcs))
                     .Select((d, i) => (i, d)))
        {
            rtfcDtoList.Add(new RtfcDto
            {
                Index = index,
                BrightFieldFindPosition = findPosition,
                Quality = 0,
                EcsValue = ecsValueTemp,
                DarkFieldImageFilePath = ImageFileDirectory
            });
        }

        // Quality
        var listDownResult = new List<bool>();
        double? downQualityValue = null;
        Logger.LogHtmlInformation("Get Optimal Quality", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
        foreach (var rtfcDtoItem in rtfcDtoList.OrderBy(t => t.Index))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tempItemDto = rtfcDtoItem.Clone();
            if (GetQuality(ref tempItemDto) == false) return false;
            SynchronizationContextProvider.Send(() =>
            {
                RtfcDtoItems.Add(tempItemDto);
                EcsPoints = [.. EcsPoints, new Point(tempItemDto.EcsValue, tempItemDto.Quality)];
            });

            if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < tempItemDto.Quality);
            downQualityValue = tempItemDto.Quality;
            if (EnumerableHelper.HasConsecutiveFalse(listDownResult, 10)) break; // 连续30个下降说明已经到了最低点
        }

        // xy得分大于100的对象参与结果运算（防止超出景深极端值的干扰）
        ResultRtfcDto = RtfcDtoItems.Select(s => s.Clone()).Maxima(s => s.Quality).Single();
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            ResultRtfcDto.EcsValue,
            ResultRtfcDto.Quality,
            EcsQuality = new HtmlPlot2DLinesChart([
                ("Ecs-Quality", RtfcDtoItems.Select(t => new Point(t.EcsValue, t.Quality)).ToArray())
            ], "EcsQuality")
        }), HtmlLogUniqueId.LoggingHtml());

        Logger.LogHtmlInformation("Calibration Illumination Focus", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
        // 获得照明焦点偏移量
        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.IdeaDarkFieldMachinePosition);
        AfViewModel.ToggleBrightFieldEnable(false);
        AfViewModel.SetSensorEcsValue(ResultRtfcDto.EcsValue);
        Thread.Sleep(5000);
        var nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
        var notAutoFocusNsc = nscBuffers.Average();
        if (LaserViewModel.TryGetMatchPositionByNotAutoFocus(
                FocusShiftCache.AlgorithmTemplateTypeEnum,
                8,
                ResultRtfcDto.BrightFieldFindPosition,
                FocusShiftCache.DarkFiledTemplateFilePath,
                ResultRtfcDto.DarkFieldImageFilePath,
                HtmlLogUniqueId,
                string.Empty,
                string.Empty,
                out var resultPosition,
                out _,
                out _,
                out var resultImageFilePath,
                true,
                800,
                FocusShiftCache.OpticsMagTypeEnum,
                FocusShiftCache.StageSpeedEnum,
                CalibrationConstantsHelper.MainStageCoordinateSystemEnum,
                FocusShiftCache.LightCoefficient) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Dark Field Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        var matchMachinePosition = StageViewModel.DarkFieldToMachinePosition(resultPosition);
        ResultRtfcDto.DarkFieldFindPosition = resultPosition;
        ResultRtfcDto.DarkFieldImageFilePath = resultImageFilePath;
        ResultRtfcDto.DarkFieldMatchOffset = matchMachinePosition - (Vector)Cache.IdeaDarkFieldMachinePosition;
        // 照明修正值(方向未定)
        ResultRtfcDto.LightAxisOffset = ResultRtfcDto.DarkFieldMatchOffset.X / Math.Sin(Cache.ObliqueAngle);
        ResultRtfcDto.DeltaEcs = ResultRtfcDto.DarkFieldMatchOffset.X / Math.Tan(Cache.ObliqueAngle);
        //ResultRtfcDto.EcsValue += ResultRtfcDto.DeltaEcs;
        ResultRtfcDto.NscValue = notAutoFocusNsc;
        // todo:修正照明轴
        //AfViewModel.SetSensorEcsValue(ResultRtfcDto.EcsValue);

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            Cache.ObliqueAngle,
            MatchDarkMachinePosition = matchMachinePosition,
            ResultRtfcDto.DarkFieldMatchOffset,
            ResultRtfcDto.DeltaEcs,
            ReviseEcs = ResultRtfcDto.EcsValue,
            ResultRtfcDto.LightAxisOffset,
            ResultRtfcDto.EcsValue,
            ResultRtfcDto.NscValue,
            NscTraceBuffers = new HtmlPlot2DLinesChart([
                ("Time-nm", nscBuffers.Select(t => t * NscDiagnosisK).ToPoints()),
            ], "NscTraceBuffers"),
            HtmlTab = new HtmlTab(new
            {
                ResultImage = new HtmlImage(ResultRtfcDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                TemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
            })
        }), HtmlLogUniqueId.LoggingHtml());

        //Logger.LogHtmlInformation("Verify Illumination Focus", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
        //var verifyResult = Verify(ResultRtfcDto, false);
        SynchronizationContextProvider.Send(() => { RtfcDtoIterationItems.Add(ResultRtfcDto); });
        return true;
    }

    private bool Verify(RtfcDto rtfcDto, bool isAutoFocus)
    {
        var tempDto = rtfcDto.Clone();
        if (GetQuality(ref tempDto) == false) return false;
        Point resultPosition;
        string verifyResultImageFilePath;
        if (isAutoFocus == false)
        {
            if (LaserViewModel.TryGetMatchPositionByNotAutoFocus(
                    FocusShiftCache.AlgorithmTemplateTypeEnum,
                    8,
                    rtfcDto.BrightFieldFindPosition,
                    FocusShiftCache.DarkFiledTemplateFilePath,
                    ImageFileDirectory,
                    HtmlLogUniqueId,
                    string.Empty,
                    string.Empty,
                    out resultPosition,
                    out _,
                    out _,
                    out verifyResultImageFilePath,
                    true,
                    800,
                    FocusShiftCache.OpticsMagTypeEnum,
                    FocusShiftCache.StageSpeedEnum) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Dark Field Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        }
        else
        {
            var cacheAutoFocusParam = FocusShiftCache.GetDarkFieldAutoFocusParam();
            var autoFocusParam = new SettingDarkFieldAutoFocusParam()
            {
                DswEcsValue = rtfcDto.EcsValue,
                DswMotorValue = cacheAutoFocusParam.DswMotorValue + rtfcDto.AfMotorOffset
            };
            if (LaserViewModel.TryGetMatchPosition(
                    FocusShiftCache.AlgorithmTemplateTypeEnum,
                    FocusShiftCache.CalChipSiteModelEnum,
                    8,
                    rtfcDto.BrightFieldFindPosition,
                    FocusShiftCache.DarkFiledTemplateFilePath,
                    ImageFileDirectory,
                    HtmlLogUniqueId,
                    string.Empty,
                    string.Empty,
                    autoFocusParam,
                    out resultPosition,
                    out _,
                    out _,
                    out verifyResultImageFilePath,
                    true,
                    800,
                    FocusShiftCache.OpticsMagTypeEnum,
                    FocusShiftCache.StageSpeedEnum) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        }

        var machinePosition = StageViewModel.DarkFieldToMachinePosition(resultPosition);
        var verifyMatchOffset = machinePosition.X - ResultFocusShiftDto.DarkFieldFindPosition.X;
        var verifyResult = Math.Abs(verifyMatchOffset) < Cache.OffsetThreshold;
        Logger.LogHtmlInformation($"Verify {(verifyResult ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            Cache.OffsetThreshold,
            rtfcDto.EcsValue,
            rtfcDto.Quality,
            TemplateMachinePosition = ResultFocusShiftDto.DarkFieldFindPosition,
            MatchMachinePosition = machinePosition,
            resultPosition,
            verifyMatchOffset,
            HtmlTab = new HtmlTab(new
            {
                ResultImage = new HtmlImage(verifyResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                TemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
            })
        }), HtmlLogUniqueId.LoggingHtml());
        return verifyResult;
    }

    public override async Task<bool> SavingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (CacheProvider.Set(ResultRtfcDto, CancellationToken.None) == false) return false;
        if (CacheProvider.Set(ResultFocusShiftDto, CancellationToken.None) == false) return false;
        if (CacheProvider.Set(Cache, CancellationToken.None) == false) return false;
        if (CacheProvider.Set(FocusShiftCache, CancellationToken.None) == false) return false;
        return true;
    }

    #endregion 诊断业务

    #region 文件读写

    public bool SaveCsv(string filePath)
    {
        try
        {
            DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);

            var fileExists = System.IO.File.Exists(filePath);
            var sb = new StringBuilder();

            // 如果文件不存在，写入表头
            if (!fileExists)
            {
                sb.AppendLine("DataTime,SettingEcs,SettingMotor,EcsOffset,BfToDfOffsetX," +
                              "ErrorOffsetX,DF-AutoFocusEcs,DF-AutoFocusNsc,DF-IdealEcs,DF-IdealEcsNsc,DF-CalChipRealEcs,DF-CalChipRealNsc,BF-ChuckAfEcs,BF-CalChipAfEcs," +
                              "AfOffset,DfOffset,Af-dF,ReviewCamTemperature,CibTemperature,XAxisTemperature,YAxisTemperature");
            }
            else
            {
                // 读取原有内容
                var oldContent = System.IO.File.ReadAllText(filePath);
                sb.Append(oldContent);

                // 如果旧内容不是以换行结尾，补一个换行
                if (!oldContent.EndsWith("\n") && !oldContent.EndsWith("\r"))
                {
                    sb.AppendLine();
                }
            }

            var calChipAfEcs = ResultRtfcDto.IdealEcs - ResultFocusShiftDto.EcsOffset;
            var afOffset = ResultRtfcDto.AutoFocusEcs - ResultRtfcDto.IdealEcs;
            var dfOffset = ResultRtfcDto.EcsValue - ResultRtfcDto.IdealEcs;
            var afDfError = afOffset - dfOffset;
            var settingDarkFieldAutoFocusParam = FocusShiftCache.GetDarkFieldAutoFocusParam();
            sb.AppendLine(
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}," +
                $"{settingDarkFieldAutoFocusParam.DswEcsValue}," +
                $"{settingDarkFieldAutoFocusParam.DswMotorValue}," +
                $"{ResultFocusShiftDto.EcsOffset}," +
                $"{ResultFocusShiftDto.BrightFieldToDarkFieldOffset.X}," +
                $"{ResultRtfcDto.DarkFieldMatchOffset.X.ToString(CultureInfo.InvariantCulture)}," +
                $"{ResultRtfcDto.AutoFocusEcs}," +
                $"{ResultRtfcDto.AutoFocusNsc}," +
                $"{ResultRtfcDto.IdealEcs}," +
                $"{ResultRtfcDto.IdealEcsNsc}," +
                $"{ResultRtfcDto.EcsValue}," + // calchip NSC ECS
                $"{ResultRtfcDto.NscValue}," +
                $"{ChuckAfEcs}," + // chuckAf ECS
                $"{calChipAfEcs}," + // calchipAf ECS
                $"{afOffset}," +
                $"{dfOffset}," +
                $"{afDfError}," +
                $"{ReviewCamTemperature}," +
                $"{CibTemperature}," +
                $"{XAxisTemperature}," +
                $"{YAxisTemperature},");
            // 重新写入文件
            System.IO.File.WriteAllText(filePath, sb.ToString());
            return true;
        }
        catch (Exception ex)
        {
            DialogWindowProvider.ShowDialog("Save Csv Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            Logger.LogError(ex, "Save Csv Failed!");
            return false;
        }
    }

    #endregion 文件读写
}