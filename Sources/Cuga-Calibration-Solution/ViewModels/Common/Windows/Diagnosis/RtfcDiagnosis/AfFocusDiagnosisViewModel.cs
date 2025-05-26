using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.Rtfc;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Algorithm.MathNet.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis.RtfcDiagonosis;

[IOCAppService(ServiceType = typeof(AfFocusDiagnosisViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AfFocusDiagnosisViewModel(CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel) : RtfcDiagnosisViewModelBase
{
    #region 属性

    /// <summary>
    /// 校准Html日志文件路径名称
    /// </summary>
    public override string LogHtmlFileName => "RtfcDiagnosis_AfFocus";

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
    private RtfcDto _resultRtfcDto = new();

    /// <summary>
    /// 诊断缓存
    /// </summary>
    [ObservableProperty]
    private RtfcCache _cache = new();

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

            if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterObjDto>(out _, out errorMessage) == false)
            {
                DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckPrealignerObjDto>(out _, out errorMessage) == false)
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

            Cache = CacheProvider.GetOrDefault<RtfcCache>();
            StageViewModel.SetAbsoluteStageTheta(0);

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
                            Cache.LowSiteFindPosition = result;
                            Cache.HighSiteFindPosition = result;

                            Cache.LowSiteTemplateFilePath = $"{TemplateFileDirectory}\\{Cache.LowMicroscopeMagnificationEnum}_{Guid.NewGuid()}";
                            var generateTemplateLow = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                            if (generateTemplateLow == false) DialogWindowProvider.ShowDialog("Generate Low Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            else Cache.LowSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowSiteTemplateFilePath);

                            break;

                        case "HighFindPosition":
                            {
                                Logger.LogHtmlInformation("Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                                IsEnableWindow = false;

                                Cache.HighSiteFindPosition = result;
                                Cache.HighSiteTemplateFilePath = $"{TemplateFileDirectory}\\{Cache.HighMicroscopeMagnificationEnum}_{Guid.NewGuid()}";
                                var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                                if (generateTemplateHigh == false)
                                {
                                    DialogWindowProvider.ShowDialog("Generate High Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    return;
                                }
                                else Cache.HighSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighSiteTemplateFilePath);

                                var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                                    CalChipSiteModelEnum.ChuckModel,
                                    Cache.HighSiteFindPosition,
                                    (false, 0.85),
                                    false,
                                    Cache.SettingDarkFieldAutoFocusParam,
                                    800,
                                    Cache.OpticsMagTypeEnum,
                                    Cache.StageSpeedEnum,
                                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
                                var detectImageDirectory = ImageFileDirectory;
                                using var image = darkFieldImageDto;

                                Cache.DarkFiledTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.OpticsMagTypeEnum}_{Guid.NewGuid()}";
                                if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
                                {
                                    if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, Cache.DarkFiledImageFilePath) == false)
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
                                    createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.DarkFiledTemplateFilePath;

                                    var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                                    if (showDialog == false)
                                    {
                                        DialogWindowProvider.ShowDialog("Generate Dark Field Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                        return;
                                    }
                                }

                                Cache.DarkFiledTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.DarkFiledTemplateFilePath);

                                var originImagePath = $"{detectImageDirectory}\\DarkFieldMatchOriginImage_{Guid.NewGuid()}).jpg";
                                if (LaserViewModel.TryGetMatchPosition(
                                        Cache.AlgorithmTemplateTypeEnum,
                                        CalChipSiteModelEnum.ChuckModel,
                                        LaserPixelSizeItems,
                                        8,
                                        Cache.HighSiteFindPosition,
                                        Cache.DarkFiledTemplateFilePath,
                                        originImagePath,
                                        HtmlLogUniqueId,
                                        string.Empty,
                                        string.Empty,
                                        Cache.SettingDarkFieldAutoFocusParam,
                                        out var darkFieldResultPosition,
                                        out _,
                                        out _,
                                        out var resultImageFilePath,
                                        true,
                                        800,
                                        Cache.OpticsMagTypeEnum,
                                        Cache.StageSpeedEnum) == false)
                                {
                                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                                    return;
                                }

                                Cache.MachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldResultPosition);
                                Cache.DarkFiledImageFilePath = resultImageFilePath;
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

    public override async Task<bool> DiagnosisActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            PlotList.Clear();
            return await DiagnosisAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
        }
    }

    private async Task<bool> DiagnosisAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (Cache.FindFocusMin <= 0 || Cache.FindFocusMax <= 0 || Cache.FindFocusInterval == 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Min > 0 and Focs Max > 0 and Focus Interval > 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                IsEnableWindow = false;

                Logger.LogHtmlInformation("1. Param", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    Cache.AlgorithmTemplateSizeEnum,
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.LowMicroscopeMagnificationEnum,
                    Cache.HighMicroscopeMagnificationEnum,
                    CalChipSiteModelEnum,
                    Cache.OpticsMagTypeEnum,
                    Cache.StageSpeedEnum,
                    Pmt = 8,
                    xPixelWidth = 800,
                    Cache.SettingDarkFieldAutoFocusParam.ChuckEcsValue,
                    Cache.SettingDarkFieldAutoFocusParam.ChuckMotorValue,
                    Cache.SettingDarkFieldAutoFocusParam.IsEnableChuck,
                    Cache.LowSiteFindPosition,
                    Cache.HighSiteFindPosition,
                    Cache.MachinePosition,
                    Cache.FindFocusMin,
                    Cache.FindFocusMax,
                    Cache.FindFocusInterval,
                    Cache.ObliqueAngle,
                    Cache.OffsetThreshold,
                    Cache.QualityThreshold,
                    HtmlTab = new HtmlTab(new
                    {
                        LowSiteTemplateImage = new HtmlImage(Cache.LowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighSiteTemplateImage = new HtmlImage(Cache.HighSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        DarkFieldTemplateImage = new HtmlImage(Cache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        DarkFieldResultImage = new HtmlImage(Cache.DarkFiledImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                // Bright Field Match
                Logger.LogHtmlInformation($"2. Bright Field Match Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowSiteFindPosition, Cache.LowMicroscopeMagnificationEnum, Cache.LowSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "Low Magnification", out var lowResultPosition, out _, out _, out var lowResultImageFilePath, out _) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
                }

                if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowResultPosition, Cache.HighMicroscopeMagnificationEnum, Cache.HighSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "High Magnification", out var highResultPosition, out _, out _, out var highResultImageFilePath, out _) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
                }

                Cache.HighSiteFindPosition = highResultPosition;

                // 修正照明焦点
                Logger.LogHtmlInformation($"3. Revise Illumination Focus", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                // 获得NSC模式下的ECS
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.HighSiteFindPosition);
                var isAutoFocus = AfViewModel.SetDarkFieldAutoFocus(Cache.SettingDarkFieldAutoFocusParam, Cache.OpticsMagTypeEnum, CalChipSiteModelEnum);
                if (isAutoFocus)
                    AfViewModel.ToggleDarkFieldEnable(true);
                Cache.AutoFocusEcs = AfViewModel.GetSensorAverageEcsValue();
                AfViewModel.ToggleDarkFieldEnable(false);

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    darkFieldFindPosition = Cache.HighSiteFindPosition,
                    Cache.SettingDarkFieldAutoFocusParam.ChuckEcsValue,
                    Cache.SettingDarkFieldAutoFocusParam.ChuckMotorValue,
                    Cache.AutoFocusEcs
                }), HtmlLogUniqueId.LoggingHtml());
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
                    if (ReviseIlluminationFocus(cancellationToken))
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
                    TemplateDarkMachinePosition = Cache.MachinePosition,
                    ResultRtfcDto.DarkFieldMatchOffset,
                    ResultRtfcDto.DeltaEcs,
                    ResultRtfcDto.EcsValue,
                    ResultRtfcDto.Quality,
                    ReviseEcs = ResultRtfcDto.EcsValue,
                    ResultRtfcDto.IlluminationFocusOffset,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(ResultRtfcDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(Cache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    }),
                    IterationCurve = new HtmlPlot2DLinesChart([
                        ("Times-Ecs", RtfcDtoIterationItems.Select(t => t.EcsValue).ToList().ToPoints()),
                        ("Times-DeltaEcs", RtfcDtoIterationItems.Select(t => t.DeltaEcs).ToList().ToPoints()),
                        ("Times-IlluminationFocusOffset", RtfcDtoIterationItems.Select(t => t.IlluminationFocusOffset).ToList().ToPoints()),
                    ], "IterationCurve"),
                }), HtmlLogUniqueId.LoggingHtml());

                // 修正af(方向未定、公式未定)
                Logger.LogHtmlInformation($"4. Revise Af", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                //ResultRtfcDto.DeltaNsc = (ResultRtfcDto.EcsValue - Cache.AutoFocusEcs) / Cache.AfEcsRelation;
                //ResultRtfcDto.AfMotor += ResultRtfcDto.DeltaNsc;
                //AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(ResultRtfcDto.AfMotor);
                //var verifyReviseAfResult = Verify(ResultRtfcDto, true);
                Logger.LogHtmlInformation($"Revise Af {(true ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    ResultRtfcDto.EcsValue,
                    ResultRtfcDto.AfMotor,
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Diagnosis Failed! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog("Diagnosis Failed! Please Check Config And Try Again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
            finally
            {
                Messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(true));
                IsEnableWindow = true;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private bool GetQuality(RtfcDto rtfcItemDto)
    {
        try
        {
            Logger.LogHtmlInformation($"Get Quality,Ecs:{rtfcItemDto.EcsValue}", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
            // 防止移动后明场af模式打开
            AfViewModel.ToggleBrightFieldEnable(false);
            AfViewModel.SetSensorEcsValue(rtfcItemDto.EcsValue);

            if (rtfcItemDto.Index == 0) Thread.Sleep(1000);
            using var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImageByNotAutoFocus(
                rtfcItemDto.BrightFieldFindPosition,
                (true, null),
                true,
                800,
                Cache.OpticsMagTypeEnum,
                StageSpeedEnum.Low,
                8,
                3,
                StageCoordinateSystemEnum.Dark);

            using var scaleImage = HalconHelper.ScaleImageTo8Bit(darkFieldImageDto.Image);
            var xQuality = CalibrationAlgorithmService.GetDarkFieldQuality(scaleImage);

            var qualityX = xQuality;
            rtfcItemDto.DarkFieldImageFilePath =
                $"{ImageFileDirectory}\\ECS({rtfcItemDto.EcsValue})_Guid({HtmlLogUniqueId}).jpg";
            rtfcItemDto.DarkFieldOriginImageFilePath = CalibrationConstantsHelper.ImagePathToRawImagePath(rtfcItemDto.DarkFieldImageFilePath);
            rtfcItemDto.Quality = qualityX;
            SynchronizationContextProvider.Send(() =>
            {
                RtfcDtoItems.Add(rtfcItemDto);
                EcsPoints = [.. EcsPoints, new Point(rtfcItemDto.EcsValue, rtfcItemDto.Quality)];
            });

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

    private bool ReviseIlluminationFocus(CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() =>
        {
            EcsPoints = [];
            RtfcDtoItems.Clear();
        });

        var rtfcDtoList = new List<RtfcDto>();
        var minEcs = Cache.AutoFocusEcs - Cache.FindFocusMin;
        var maxEcs = Cache.AutoFocusEcs + Cache.FindFocusMax;
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            FindFocusMin = minEcs,
            FindFocusMax = maxEcs,
            Cache.FindFocusInterval,
            FindPosition = Cache.HighSiteFindPosition,
            ImageFileDirectory
        }), HtmlLogUniqueId.LoggingHtml());
        // 初始化
        foreach (var (index, ecsValueTemp) in Enumerable.Range(0, Convert.ToInt32((Cache.FindFocusMax + Cache.FindFocusMin) / Cache.FindFocusInterval) + 1)
                     .Select(x => Math.Min(minEcs + x * Cache.FindFocusInterval, maxEcs))
                     .Select((d, i) => (i, d)))
        {
            rtfcDtoList.Add(new RtfcDto
            {
                Index = index,
                BrightFieldFindPosition = Cache.HighSiteFindPosition,
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
            if (GetQuality(rtfcDtoItem) == false) return false;

            if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < rtfcDtoItem.Quality);
            downQualityValue = rtfcDtoItem.Quality;
            if (EnumerableHelper.HasConsecutiveFalse(listDownResult, 10)) break; // 连续30个下降说明已经到了最低点
        }

        // xy得分大于100的对象参与结果运算（防止超出景深极端值的干扰）
        ResultRtfcDto = RtfcDtoItems.Select(s => s.Clone()).Where(t => t.Quality > 100).Maxima(s => s.Quality).Single();
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
        AfViewModel.SetSensorEcsValue(ResultRtfcDto.EcsValue);
        if (LaserViewModel.TryGetMatchPositionByNotAutoFocus(
                Cache.AlgorithmTemplateTypeEnum,
                LaserPixelSizeItems,
                8,
                ResultRtfcDto.BrightFieldFindPosition,
                Cache.DarkFiledTemplateFilePath,
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
                Cache.OpticsMagTypeEnum,
                Cache.StageSpeedEnum) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Dark Field Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        var machinePosition = StageViewModel.DarkFieldToMachinePosition(resultPosition);
        ResultRtfcDto.DarkFieldImageFilePath = resultImageFilePath;
        ResultRtfcDto.DarkFieldMatchOffset = machinePosition - Cache.MachinePosition;
        // 照明修正值(方向未定)
        ResultRtfcDto.IlluminationFocusOffset = ResultRtfcDto.DarkFieldMatchOffset.X / Math.Sin(Cache.ObliqueAngle);
        ResultRtfcDto.DeltaEcs = ResultRtfcDto.DarkFieldMatchOffset.X / Math.Tan(Cache.ObliqueAngle);
        ResultRtfcDto.EcsValue += ResultRtfcDto.DeltaEcs;
        // todo:修正照明轴
        //AfViewModel.SetSensorEcsValue(ResultRtfcDto.EcsValue);

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            Cache.ObliqueAngle,
            TemplateDarkMachinePosition = Cache.MachinePosition,
            MatchDarkMachinePosition = machinePosition,
            ResultRtfcDto.DarkFieldMatchOffset,
            ResultRtfcDto.DeltaEcs,
            ReviseEcs = ResultRtfcDto.EcsValue,
            ResultRtfcDto.IlluminationFocusOffset,
            HtmlTab = new HtmlTab(new
            {
                ResultImage = new HtmlImage(ResultRtfcDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                TemplateImage = new HtmlImage(Cache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
            })
        }), HtmlLogUniqueId.LoggingHtml());


        //Logger.LogHtmlInformation("Verify Illumination Focus", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
        //var verifyResult = Verify(ResultRtfcDto, false);
        SynchronizationContextProvider.Send(() => { RtfcDtoIterationItems.Add(ResultRtfcDto); });
        return true;
    }

    private bool Verify(RtfcDto rtfcDto, bool isAutoFocus)
    {
        if (GetQuality(rtfcDto) == false) return false;
        Point resultPosition;
        string verifyResultImageFilePath;
        if (isAutoFocus == false)
        {
            if (LaserViewModel.TryGetMatchPositionByNotAutoFocus(
                    Cache.AlgorithmTemplateTypeEnum,
                    LaserPixelSizeItems,
                    8,
                    rtfcDto.BrightFieldFindPosition,
                    Cache.DarkFiledTemplateFilePath,
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
                    Cache.OpticsMagTypeEnum,
                    Cache.StageSpeedEnum) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Dark Field Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        }
        else
        {
            var autoFocusParam = new SettingDarkFieldAutoFocusParam()
            {
                ChuckEcsValue = rtfcDto.EcsValue,
                ChuckMotorValue = rtfcDto.AfMotor
            };
            if (LaserViewModel.TryGetMatchPosition(
                    Cache.AlgorithmTemplateTypeEnum,
                    CalChipSiteModelEnum.ChuckModel,
                    LaserPixelSizeItems,
                    8,
                    rtfcDto.BrightFieldFindPosition,
                    Cache.DarkFiledTemplateFilePath,
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
                    Cache.OpticsMagTypeEnum,
                    Cache.StageSpeedEnum) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        }

        var machinePosition = StageViewModel.DarkFieldToMachinePosition(resultPosition);
        var verifyMatchOffset = machinePosition.X - Cache.MachinePosition.X;
        var verifyResult = Math.Abs(verifyMatchOffset) < Cache.OffsetThreshold && rtfcDto.Quality < Cache.QualityThreshold;
        Logger.LogHtmlInformation($"Verify {(verifyResult ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            Cache.OffsetThreshold,
            Cache.QualityThreshold,
            rtfcDto.EcsValue,
            rtfcDto.Quality,
            TemplateMachinePosition = Cache.MachinePosition,
            MatchMachinePosition = machinePosition,
            resultPosition,
            verifyMatchOffset,
            HtmlTab = new HtmlTab(new
            {
                ResultImage = new HtmlImage(verifyResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                TemplateImage = new HtmlImage(Cache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
            })
        }), HtmlLogUniqueId.LoggingHtml());
        return verifyResult;
    }

    public override async Task<bool> SavingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (CalibrationCacheProvider.TrySet(ResultRtfcDto, CancellationToken.None) == false) return false;
        if (CalibrationCacheProvider.TrySet(Cache, CancellationToken.None) == false) return false;
        return true;
    }

    #endregion 诊断业务
}