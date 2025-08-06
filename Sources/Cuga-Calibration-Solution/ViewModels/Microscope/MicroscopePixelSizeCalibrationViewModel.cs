using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Extensions;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopePixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopePixelSizeCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName);

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
    private ObservableCollection<MicroscopeMagnificationInfoCalibrationStatus> _calibrationStatusList = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeItemDto> _reviewList = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto? _selectReviewItemDto;

    [ObservableProperty]
    private List<(string Microscope, Size OldPixelSize, Size NewPixelSize, Size OffsetPixelSize)> _reviewResultList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private MicroscopePixelSizeCache _cache = new();

    [ObservableProperty]
    private MicroscopePixelSizeCacheItem _selectMicroscopePixelSizeCacheItem = new();

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

            (isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopePixelSizeCache>();
            Calibrations = CacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>();

            Calibrations = [.. Calibrations.Where(t => ApplicationCookie.MicroscopeMagnificationInfoList.Contains(t.MagnificationInfo))]; // 过滤掉变更静态配置后原来的缓存
            SynchronizationContextProvider.Send(() =>
                CalibrationStatusList =
                [
                    .. ApplicationCookie.MicroscopeMagnificationInfoList
                        .Select(t => new MicroscopeMagnificationInfoCalibrationStatus { MicroscopeMagnificationInfo = t, IsCalibrated = false })
                ]
            );
            foreach (var calibrationStatus in Calibrations)
            {
                CalibrationStatusList
                    .Single(t => t.MicroscopeMagnificationInfo == calibrationStatus.MagnificationInfo)
                    .IsCalibrated = calibrationStatus.IsCalibrated;
            }
        }).ConfigureAwait(false);
        return (isHasCache && Cache.InitializeCacheList(ApplicationCookie.MicroscopeMagnificationInfoList)) || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return !IsRecipeCalibrate || CalibrationRecipeService.GetCorrectWaferMapByOffset(true);
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Where(t => t.IsCalibrated)
                .Select(t => t.Clone())
                .OrderBy(t => t.MagnificationInfo.MagnificationCode)
        ];

        return ReviewList.Count != 0 && ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationInfo);
                StageViewModel.SetBrightFieldAbsoluteStageXy(SelectMicroscopePixelSizeCacheItem.FindPosition);

                return true;

            case 1:
                var result = StageViewModel.GetBrightFieldStagePosition();
                if (SelectMicroscopePixelSizeCacheItem.FindPosition.ToOriginLength >= Cache.ChuckRadius)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header2, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                Cache.SetFindFocusPosition(result);
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

                CalibrationStatusList.Single(t => t.MicroscopeMagnificationInfo == SelectMicroscopePixelSizeCacheItem.MagnificationInfo).IsCalibrated = true;
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
        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
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

                Cache.SetFindFocusPosition(result);
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
            await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(SelectMicroscopePixelSizeCacheItem.FindPosition)).ConfigureAwait(false);
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
            SelectMicroscopePixelSizeCacheItem = Cache.GetSelectedCacheItem();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (IsRecipeCalibrate)
            {
                if (await AutomationRecipeInformationAsync(Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName) == false) return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName,
                SelectMicroscopePixelSizeCacheItem.FindPosition
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
                MicroscopeMagnification = Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName,
                SelectMicroscopePixelSizeCacheItem.FindPosition,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            if (GetPixelSize(detectImageDirectory, cancellationToken) == false)
                return false;

            var averageWidth = MicroscopePixelSizeItemDtoList.Average(t => t.PixelSize.Width);
            var averageHeight = MicroscopePixelSizeItemDtoList.Average(t => t.PixelSize.Height);
            var averagePixelSize = new Size(averageWidth, averageHeight);

            SelectMicroscopePixelSizeItemDto = MicroscopePixelSizeItemDtoList.OrderBy(t => ((Point)t.PixelSize - (Vector)averagePixelSize).ToOriginLength).First();
            ResultMicroscopePixelSizeItemDto = SelectMicroscopePixelSizeItemDto.Clone();
            ResultMicroscopePixelSizeItemDto.PixelSize = averagePixelSize;
            SynchronizationContextProvider.Send(() => ResultMicroscopePixelSizeItemDtoList.Add(ResultMicroscopePixelSizeItemDto));

            Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                MicroscopeMagnification = ResultMicroscopePixelSizeItemDto.MagnificationInfo.MicroscopeMagnificationName,
                SelectMicroscopePixelSizeCacheItem.FindPosition,
                ResultMicroscopePixelSizeItemDto.PixelSize,
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
            if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
        return result;
    }

    private async Task<bool> VerifyCalibrationAsync(MicroscopePixelSizeItemDto selectReviewItemDto, CancellationToken cancellationToken)
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
            }
            else
            {
                selectReviewItemDto.IsVerified = false;
                Cache.MicroscopeMagnificationInfo = SelectReviewItemDto!.MagnificationInfo;
                SelectMicroscopePixelSizeCacheItem = Cache.GetSelectedCacheItem();
                SelectMicroscopePixelSizeCacheItem.FindPosition = selectReviewItemDto.FindPosition;

                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationInfo);
                Logger.LogHtmlInformation($"{Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    MicroscopeMagnification = Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName,
                    SelectMicroscopePixelSizeCacheItem.FindPosition,
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                if (GetPixelSize(detectImageDirectory, cancellationToken, 1) == false) return;

                var averageWidth = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Width).Average();
                var averageHeight = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Height).Average();
                var averagePixelSize = new Size(averageWidth, averageHeight);
                var error = (Size)((Vector)averagePixelSize - (Vector)selectReviewItemDto.PixelSize);
                ReviewResultList.Add((Cache.MicroscopeMagnificationInfo.MicroscopeMagnificationName, selectReviewItemDto.PixelSize, averagePixelSize, error));
                result = error.DiagonalLength < Cache.Threshold.DiagonalLength;

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    NowOffset = averagePixelSize,
                    OldOffset = selectReviewItemDto.PixelSize,
                    Error = error
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
                    DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({averagePixelSize}) Old Offset: ({SelectReviewItemDto!.PixelSize}) Error: ({error})", DialogButtonsEnum.OK,
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
            StageViewModel.SetBrightFieldAbsoluteStageXy(SelectMicroscopePixelSizeCacheItem.FindPosition);
            foreach (var times in Enumerable.Range(1, repeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var image = ReviewViewModel.GetBrightFieldImage();

                var pixelSize = CalibrationAlgorithmService.GetPixelSize(image, SelectMicroscopePixelSizeCacheItem.AlgorithmStandardMaskSquareSizeEnum.ToSize(), out var drawingImage, out var angle);
                if (Math.Abs(angle) > Cache.AngleThreshold)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"The current horizontal angle exceeds the limit! Angle:{angle}"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                using var _2 = drawingImage;
                var microscopePixelSizeItemDto = new MicroscopePixelSizeItemDto
                {
                    MagnificationInfo = Cache.MicroscopeMagnificationInfo,
                    FindPosition = SelectMicroscopePixelSizeCacheItem.FindPosition,
                    OriginFilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    FilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Drawing_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    PixelSize = pixelSize
                };

                HalconHelper.Save(image, microscopePixelSizeItemDto.OriginFilePath);
                HalconHelper.Save(drawingImage, microscopePixelSizeItemDto.FilePath);

                var htmlBulletList = new HtmlBullet(new
                {
                    MicroscopeMagnification = microscopePixelSizeItemDto.MagnificationInfo.MicroscopeMagnificationName,
                    SelectMicroscopePixelSizeCacheItem.FindPosition,
                    microscopePixelSizeItemDto.PixelSize,
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
                .Where(t => t.MagnificationInfo != itemDto.MagnificationInfo),
            itemDto.Clone(),
        ];

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
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
        SynchronizationContextProvider.Send(() =>
        {
            AutoCalibrationStepList.Clear();
            AutoCalibrationStepList.AddRange([
                new() { StepName = "loading" },
                .. ApplicationCookie.MicroscopeMagnificationInfoList.Select(info => new CalibrationItemStep { StepName = info.MicroscopeMagnificationName }),
                new() { StepName = "Review" }
            ]);
        });
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            SynchronizationContextProvider.Send(ResultMicroscopePixelSizeItemDtoList.Clear);
            foreach (var (calibrationItemStep, stepIndex) in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                Func<Task<bool>> autoStepAction = stepIndex switch
                {
                    0 => async () =>
                    {
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.AlgorithmTemplateTypeEnum,
                            }), HtmlLogUniqueId.LoggingHtml());
                            return true;
                        });
                        if (await AutoStepAsync().ConfigureAwait(false) == false) return false;
                        return await AutoNextingAsync(cancellationToken).ConfigureAwait(false);
                    }
                    ,
                    var index when index == AutoCalibrationStepList.Count - 1 => async () =>
                    {
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        var result = await InvokeCalibrateAsync(async () =>
                        {
                            foreach (var itemReview in ReviewList)
                            {
                                SelectReviewItemDto = itemReview;
                                if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken).ConfigureAwait(false) == false)
                                {
                                    DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.MagnificationInfo.MicroscopeMagnificationName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    return false;
                                }
                            }

                            return true;
                        }).ConfigureAwait(false);
                        if (result == false) return false;
                        AutoCalibrationStepIndex++;
                        return true;
                    }
                    ,
                    _ => async () =>
                    {
                        if (await AutoActionStepAsync(calibrationItemStep.StepName, cancellationToken).ConfigureAwait(false) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration {calibrationItemStep.StepName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        if (ResultMicroscopePixelSizeItemDtoList.Count(t => t.IsOk) == ApplicationCookie.MicroscopeMagnificationInfoList.Count)
                        {
                            Logger.LogHtmlInformation("ResultPixelSize", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                            {
                                ResultPixelSize = new HtmlTable([.. ResultMicroscopePixelSizeItemDtoList.Select(t => new { t.MagnificationInfo.MicroscopeMagnificationName, t.PixelSize }).Cast<object>()])
                            }), HtmlLogUniqueId.LoggingHtml());
                        }

                        return true;
                    }
                };

                if (await autoStepAction().ConfigureAwait(false) == false) return false;
                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }

            return true;
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

        Cache.MicroscopeMagnificationInfo = ApplicationCookie.MicroscopeMagnificationInfoList.Single(t => t.MicroscopeMagnificationName == microscopeName);
        SelectMicroscopePixelSizeCacheItem = Cache.GetSelectedCacheItem();
        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });

        if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(SelectMicroscopePixelSizeCacheItem.WaferMaskTypeEnum, Cache.MicroscopeMagnificationInfo, null, out var maskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo, out var position);
        Cache.SetFindFocusPosition(position);

        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationInfo, true);
        StageViewModel.SetBrightFieldAbsoluteStageXy(position);
        return true;
    }

    private async Task<bool> AutoActionStepAsync(string magnification, CancellationToken cancellationToken)
    {
        if (await AutomationRecipeInformationAsync(magnification) == false) return false;
        if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
        await Task.Delay(2000, cancellationToken);
        CalibrationStepIndex = 2;
        if (await NextingAsync(cancellationToken) == false) return false;
        return await AutoNextingAsync(cancellationToken);
    }

    private async Task<bool> AutoNextingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            AutoCalibrationStepIndex++;
            CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex].StepName;
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
                foreach (var (_, itemReview) in ReviewList.Select((t, i) => (index: i, itemReview: t)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectReviewItemDto = itemReview;
                    if (await AutomationRecipeInformationAsync(SelectReviewItemDto.MagnificationInfo.MicroscopeMagnificationName) == false) return false;
                    if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false)
                    {
                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.MagnificationInfo.MicroscopeMagnificationName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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