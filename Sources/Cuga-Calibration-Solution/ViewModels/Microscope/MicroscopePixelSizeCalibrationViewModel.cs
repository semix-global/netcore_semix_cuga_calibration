using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.AutoFocus;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Helper.Enum;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopePixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopePixelSizeCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a lens" },
        new() { StepName = "Select a location" },
        new() { StepName = "Find Pixel Size" }
    ];


    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeItemDto> _microscopePixelSizeItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeItemDto> _resultMicroscopePixelSizeItemDtoList = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto? _selectMicroscopePixelSizeItemDto;

    [ObservableProperty]
    private MicroscopePixelSizeItemDto? _resultMicroscopePixelSizeItemDto;

    [ObservableProperty]
    private ObservableCollection<MicroscopeMagnificationEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<MicroscopeMagnificationEnum>().Select(t => new MicroscopeMagnificationEnumCalibrationStatus { MicroscopeMagnificationEnum = t, IsCalibrated = false })
    ];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeItemDto> _reviewList = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto? _selectReviewItemDto;

    [ObservableProperty]
    private List<(string Microscope, Size OldPixelSize, Size NewPixelSize, Size OffsetPixelSize)> _reviewResultList = [];

    #endregion Review

    /// <summary>
    /// 标准掩模方块大小
    /// </summary>
    [ObservableProperty]
    private Size _pixelSizeStandardMaskSquareSize;

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private MicroscopePixelSizeCache _cache = new();

    [ObservableProperty]
    private ChuckAutoFocusCache _chuckAutoFocusCache = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _calibrations = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        var isHasCache = false;
        await Task.Run(() =>
        {
            if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
            {
                DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out var errorMessage) == false)
            {
                DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            (isHasCache, ChuckAutoFocusCache) = CacheProvider.TryGetOrDefault<ChuckAutoFocusCache>();
            if (isHasCache == false)
            {
                DialogWindowProvider.ShowDialog("Get Chuck Auto Focus Cache Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            (isHasCache, Cache) = CacheProvider.TryGetOrDefault<MicroscopePixelSizeCache>();
            Calibrations = CacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>();
            foreach (var calibrationStatus in Calibrations)
            {
                CalibrationStatusList
                    .Single(t => t.MicroscopeMagnificationEnum == calibrationStatus.MicroscopeMagnificationEnum)
                    .IsCalibrated = calibrationStatus.IsCalibrated;
            }
        }).ConfigureAwait(false);
        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        Cache.FindPosition = Cache.FindPosition.DistanceToZero() >= Cache.ChuckRadius
            ? new Point(0, 0)
            : Cache.FindPosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
        if (IsRecipeCalibrate && CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
            return false;
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        //if (Cache.FindPosition.DistanceToZero() >= Cache.ChuckRadius)
        //{
        //    DialogWindowProvider.ShowDialog("The Bright Field Cache Position Out Of The Wafer!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        //    return false;
        //}

        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.MicroscopeMagnificationEnum)
        ];

        return ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 0:
                PixelSizeStandardMaskSquareSize = CalibrationConstantsHelper.CalibrationPixelSizeStandardMaskSquareSizeDic[Cache.MicroscopeMagnificationEnum];
                if (IsRecipeCalibrate)
                {
                    if (await AutomationRecipeInformationAsync(((int)Cache.MicroscopeMagnificationEnum).ToString()) == false) return false;
                }

                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return true;

            case 1:
                var result = StageViewModel.GetBrightFieldStagePosition();
                if (Cache.FindPosition.DistanceToZero() >= Cache.ChuckRadius)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header2, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                Cache.FindPosition = result;
                return true;

            case 2:
                if (ResultMicroscopePixelSizeItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find pixel size!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultMicroscopePixelSizeItemDto.IsCalibrated = true;
                    if (Save(ResultMicroscopePixelSizeItemDto, cancellationToken) == false)
                    {
                        ResultMicroscopePixelSizeItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.MicroscopeMagnificationEnum == Cache.MicroscopeMagnificationEnum).IsCalibrated = true;
                //DialogWindowProvider.ShowDialog("Find Pixel Size Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

                return true;

            default:
                return false;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        StageViewModel.SetBrightFieldAbsoluteStageXy(new Point(0, 0));
        return Task.FromResult(true);
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                Cache.FindPosition = result;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoPointAsync()
    {
        try
        {
            await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeMagnificationEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeMagnificationEnum,
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                MicroscopeMagnification = Cache.MicroscopeMagnificationEnum,
                FindPosition = Cache.FindPosition.ToShortString(),
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            if (GetPixelSize(detectImageDirectory, cancellationToken) == false)
                return false;

            var averageWidth = MicroscopePixelSizeItemDtoList.Average(t => t.PixelSize.Width);
            var averageHeight = MicroscopePixelSizeItemDtoList.Average(t => t.PixelSize.Height);
            var averagePixelSize = new Size(averageWidth, averageHeight);

            SelectMicroscopePixelSizeItemDto = MicroscopePixelSizeItemDtoList.OrderBy(t => (t.PixelSize - averagePixelSize).DiagonalDistance).First();
            ResultMicroscopePixelSizeItemDto = SelectMicroscopePixelSizeItemDto.Clone();
            ResultMicroscopePixelSizeItemDto.PixelSize = averagePixelSize;
            SynchronizationContextProvider.Send(() => ResultMicroscopePixelSizeItemDtoList.Add(ResultMicroscopePixelSizeItemDto));

            Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                MicroscopeMagnification = ResultMicroscopePixelSizeItemDto.MicroscopeMagnificationEnum,
                FindPosition = Cache.FindPosition.ToShortString(),
                PixelSize = ResultMicroscopePixelSizeItemDto.PixelSize.ToShortString(),
                HtmlTab = new HtmlTab(new
                {
                    OriginImage = new HtmlImage(ResultMicroscopePixelSizeItemDto.OriginFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    DrawingImage = new HtmlImage(ResultMicroscopePixelSizeItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                Plot2DWidthHeight = new HtmlPlot2DLinesChart(
                [
                    ("Width", MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Width).ToPoints()),
                    ("Height", MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Height).ToPoints())
                ], "PlotWidthHeight")
            }), HtmlLogUniqueId.LoggingHtml());
            result = true;
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        await InvokeVerifyAsync(async () =>
        {
            if (await VerifyCaibrationAsync(SelectReviewItemDto, cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
        return result;
    }

    private async Task<bool> VerifyCaibrationAsync(MicroscopePixelSizeItemDto selectReviewItemDto, CancellationToken cancellationToken)
    {
        var result = true;
        await Task.Run(() =>
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            if (selectReviewItemDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return;
            }
            else
            {
                selectReviewItemDto.IsVerified = false;
                Cache.FindPosition = selectReviewItemDto.FindPosition;
                Cache.MicroscopeMagnificationEnum = selectReviewItemDto.MicroscopeMagnificationEnum;
                PixelSizeStandardMaskSquareSize = CalibrationConstantsHelper.CalibrationPixelSizeStandardMaskSquareSizeDic[Cache.MicroscopeMagnificationEnum];

                StageViewModel.SetBrightFieldAbsoluteStageXy(selectReviewItemDto.FindPosition);
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                Logger.LogHtmlInformation($"{Cache.MicroscopeMagnificationEnum}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    MicroscopeMagnification = Cache.MicroscopeMagnificationEnum,
                    FindPosition = Cache.FindPosition.ToShortString(),
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                if (GetPixelSize(detectImageDirectory, cancellationToken, 1) == false) return;

                var averageWidth = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Width).Average();
                var averageHeight = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Height).Average();
                var averagePixelSize = new Size(averageWidth, averageHeight);
                var error = averagePixelSize - selectReviewItemDto.PixelSize;
                ReviewResultList.Add((Cache.MicroscopeMagnificationEnum.ToString(), selectReviewItemDto.PixelSize, averagePixelSize, error));
                result = error.DiagonalDistance < Cache.Threshold.DiagonalDistance;

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    NowOffset = averagePixelSize.ToShortString(),
                    OldOffset = selectReviewItemDto.PixelSize.ToShortString(),
                    Error = error.ToShortString()
                }), HtmlLogUniqueId.LoggingHtml());

                selectReviewItemDto.IsVerified = result;
                if (Save(selectReviewItemDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    selectReviewItemDto.IsVerified = false;
                    result = false;
                    return;
                }

                if (!result || !IsAutoCalibrate)
                {
                    DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({averagePixelSize.ToShortString()}) Old Offset: ({SelectReviewItemDto!.PixelSize.ToShortString()}) Error: ({error.ToShortString()})", DialogButtonsEnum.OK,
                        result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                }
            }
        }, cancellationToken);
        return result;
    }

    private bool GetPixelSize(string detectImageDirectory, CancellationToken cancellationToken, int repeatCount = 10)
    {
        try
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
            foreach (var times in Enumerable.Range(1, repeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var image = ReviewViewModel.GetBrightFieldImage();

                var pixelSize = CalibrationAlgorithmService.GetPixelSize(image, PixelSizeStandardMaskSquareSize, out var drawingImage, out var angle);
                if (Math.Abs(angle) > Cache.AngleThreshold)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"The current horizontal angle exceeds the limit! Angle:{angle}"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                using var _2 = drawingImage;
                var microscopePixelSizeItemDto = new MicroscopePixelSizeItemDto
                {
                    MicroscopeMagnificationEnum = Cache.MicroscopeMagnificationEnum,
                    FindPosition = Cache.FindPosition,
                    OriginFilePath = $"{detectImageDirectory}\\PixelSize({pixelSize.ToShortString()})_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    FilePath = $"{detectImageDirectory}\\PixelSize({pixelSize.ToShortString()})_Drawing_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    PixelSize = pixelSize
                };

                HalconHelper.Save(image, microscopePixelSizeItemDto.OriginFilePath);
                HalconHelper.Save(drawingImage, microscopePixelSizeItemDto.FilePath);

                var htmlBulletList = new HtmlBullet(new
                {
                    MicroscopeMagnification = microscopePixelSizeItemDto.MicroscopeMagnificationEnum,
                    FindPosition = Cache.FindPosition.ToShortString(),
                    PixelSize = microscopePixelSizeItemDto.PixelSize.ToShortString(),
                    HtmlTab = new HtmlTab(new
                    {
                        OriginImage = new HtmlImage(microscopePixelSizeItemDto.OriginFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        DrawingImage = new HtmlImage(microscopePixelSizeItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                });

                Logger.LogHtmlInformation($"Get Pixel Size OK! Time:{times}", HtmlHeaderLevelEnum.Header5, htmlBulletList, HtmlLogUniqueId.LoggingHtml());

                SynchronizationContextProvider.Send(() => MicroscopePixelSizeItemDtoList.Add(microscopePixelSizeItemDto));
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Pixel Size Failed", Name);
            return false;
        }
    }

    private bool Save(MicroscopePixelSizeItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.MicroscopeMagnificationEnum != itemDto.MicroscopeMagnificationEnum),
            itemDto.Clone(),
        ];

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && CacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependMicroscopePixelSizeCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(MicroscopePixelSizeItemDtoList.Clear);
        SelectMicroscopePixelSizeItemDto = null;
        ResultMicroscopePixelSizeItemDto = null;
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "loading" },
            new() { StepName = "5X" },
            new() { StepName = "10X" },
            new() { StepName = "50X" },
            new() { StepName = "100X" },
            new() { StepName = "150X" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            SynchronizationContextProvider.Send(ResultMicroscopePixelSizeItemDtoList.Clear);
            var result = true;
            foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                switch (stepItem.index)
                {
                    case 0:
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.MicroscopeMagnificationEnum,
                                Cache.AlgorithmTemplateTypeEnum,
                                Cache.FindPosition
                            }), HtmlLogUniqueId.LoggingHtml());
                            return result;
                        });
                        AutoCalibrationStepIndex++;
                        CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex].StepName.ToString();
                        break;

                    case 1:
                        if (await AutoActionStepAsync((int)MicroscopeMagnificationEnum.Magnification5X, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Magnification5X Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;
                    case 2:
                        if (await AutoActionStepAsync((int)MicroscopeMagnificationEnum.Magnification10X, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Magnification10X Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 3:
                        if (await AutoActionStepAsync((int)MicroscopeMagnificationEnum.Magnification50X, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Magnification50X Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 4:
                        if (await AutoActionStepAsync((int)MicroscopeMagnificationEnum.Magnification100X, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Magnification100X Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 5:
                        if (await AutoActionStepAsync((int)MicroscopeMagnificationEnum.Magnification150X, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Magnification150X Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        Logger.LogHtmlInformation("ResultPixelSize", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                        {
                            ResultPixelSize = new HtmlTable([.. ResultMicroscopePixelSizeItemDtoList.Select(t => new { t.MicroscopeMagnificationEnum, t.PixelSize }).Cast<object>()])
                        }), HtmlLogUniqueId.LoggingHtml());
                        break;

                    case 6:
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        if (await InvokeCalibrateAsync(async () =>
                            {
                                ReviewResultList.Clear();
                                foreach (var itemReview in ReviewList)
                                {
                                    SelectReviewItemDto = itemReview;
                                    if (await VerifyCaibrationAsync(SelectReviewItemDto, cancellationToken) == false)
                                    {
                                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                        return false;
                                    }
                                }

                                Logger.LogHtmlInformation("ReviewResultPixelSize", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                                {
                                    Cache.Threshold,
                                    ReviewResultPixelSize = new HtmlTable([.. ReviewResultList.Select(t => new { t.Microscope, t.OldPixelSize, t.NewPixelSize, t.OffsetPixelSize }).Cast<object>()])
                                }), HtmlLogUniqueId.LoggingHtml());
                                return result;
                            }) == false) return false;
                        AutoCalibrationStepIndex++;
                        break;

                    default:
                        break;
                }

                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Auto Calibration Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string microscopeName)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (IsAutoCalibrate == false)
        {
            if (CalibrationRecipeService.GetCorrectWaferMapByOffset(false) == false)
                return false;
        }

        OriginReticleDieDto = CalibrationRecipeDto.WaferDto.WaferMapDto.OriginReticleDto;
        switch (microscopeName)
        {
            case "0":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.Grid_100um, Cache.MicroscopeMagnificationEnum, null, out var maskInfo) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(OriginReticleDieDto, maskInfo, out var position);
                Cache.FindPosition = position;
                break;

            case "1":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification10X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.Grid_50um, Cache.MicroscopeMagnificationEnum, null, out maskInfo) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(OriginReticleDieDto, maskInfo, out position);
                Cache.FindPosition = position;
                break;

            case "2":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.Grid_25um, Cache.MicroscopeMagnificationEnum, null, out maskInfo) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(OriginReticleDieDto, maskInfo, out position);
                Cache.FindPosition = position;
                break;

            case "3":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification100X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.Grid_10um, Cache.MicroscopeMagnificationEnum, null, out maskInfo) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(OriginReticleDieDto, maskInfo, out position);
                Cache.FindPosition = position;
                break;

            case "4":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification150X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.Grid_10um, Cache.MicroscopeMagnificationEnum, null, out maskInfo) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(OriginReticleDieDto, maskInfo, out position);
                Cache.FindPosition = position;
                break;

            default:
                break;
        }

        PixelSizeStandardMaskSquareSize = CalibrationConstantsHelper.CalibrationPixelSizeStandardMaskSquareSizeDic[Cache.MicroscopeMagnificationEnum];
        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum, true);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
        return true;
    }

    private async Task<bool> AutoActionStepAsync(int magnification, CancellationToken cancellationToken)
    {
        if (await AutomationRecipeInformationAsync(magnification.ToString()) == false) return false;
        if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
        await Task.Delay(2000, cancellationToken);
        CalibrationStepIndex = 2;
        if (await NextingAsync(cancellationToken) == false) return false;
        if (await AutoNextingAsync(cancellationToken) == false) return false;
        return true;
    }

    private async Task<bool> AutoNextingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            AutoCalibrationStepIndex++;
            CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex].StepName.ToString();
        }, cancellationToken);
        return true;
    }

    public override async Task<bool> AutomationReviewActionAsync(CancellationToken cancellationToken)
    {
        GetAutoCalibrationStep();
        await base.AutomationReviewActionAsync(cancellationToken);
        if (await LoadedingAsync(cancellationToken) == false) return false;
        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false)
        {
            DialogWindowProvider.ShowDialog($"Please Calibration!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var result = false;
        await InvokeVerifyAsync(async () =>
        {
            try
            {
                foreach (var (index, itemReview) in ReviewList.Select((t, i) => (index: i, itemReview: t)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectReviewItemDto = itemReview;
                    if (await AutomationRecipeInformationAsync(((int)SelectReviewItemDto.MicroscopeMagnificationEnum).ToString()) == false) return false;
                    if (await VerifyCaibrationAsync(SelectReviewItemDto, cancellationToken) == false)
                    {
                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        return false;
                    }
                }

                result = true;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Review Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        });
        AutoCalibrationProgress = (AutoCalibrationStepIndex + 1) / (double)AutoCalibrationStepList.Count * 100;
        return result;
    }

    #endregion 自动化校准
}