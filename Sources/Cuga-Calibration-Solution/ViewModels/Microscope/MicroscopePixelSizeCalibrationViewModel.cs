using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities.SourceGenerators.Attributes;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
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

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

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
    private ObservableCollection<MicroscopeLensInfoCalibrationStatus> _calibrationStatusList = [];

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

    [RecipeCache]
    [ObservableProperty]
    private MicroscopePixelSizeCache _cache = new();

    [ObservableProperty]
    private MicroscopePixelSizeCacheItem _selectMicroscopePixelSizeCacheItem = new();

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    [DefaultCache]
    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _calibrations = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDto>();

        (_, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopePixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>();

        Calibrations = [.. Calibrations.Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.LensInformation))]; // 过滤掉变更静态配置后原来的缓存
        SynchronizationContextProvider.Send(() =>
            CalibrationStatusList =
            [
                .. ApplicationCookie.MicroscopeLensInformations
                    .Select(t => new MicroscopeLensInfoCalibrationStatus { MicroscopeLensInformation = t, IsCalibrated = false })
            ]
        );
        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.MicroscopeLensInformation == calibrationStatus.LensInformation)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
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
                .OrderBy(t => t.LensInformation.ObjectiveMagnification)
                .ThenBy(t => t.LensInformation.LensCode)
        ];

        return ReviewList.Count != 0 && ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(SelectMicroscopePixelSizeCacheItem.FindPosition, Cache.CalChipSiteModelEnum);
                if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
                {
                    StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.DSWBrightFieldMachineAffinePosition), Cache.CalChipSiteModelEnum);
                }
                else
                {
                    Logger.LogHtmlError("The Cal Chip Model is not supported!", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                return true;

            case 1:
                var result = StageViewModel.GetBrightFieldStagePosition();

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

                CalibrationStatusList.Single(t => t.MicroscopeLensInformation == SelectMicroscopePixelSizeCacheItem.LensInformation).IsCalibrated = true;
                //DialogWindowProvider.ShowDialog("Find Pixel Size Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            SelectMicroscopePixelSizeCacheItem = Cache.CurrentCalibrationCacheItem;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.CalChipSiteModelEnum
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

            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.SetFindFocusPosition(result);

            if (IsRecipeCalibrate)
            {
                if (await AutomationRecipeInformationAsync(Cache.MicroscopeLensInformation.LensName) == false) return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
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
                Cache.MicroscopeLensInformation.LensName,
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
                ResultMicroscopePixelSizeItemDto.LensInformation.LensName,
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
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return await InvokeVerifyAsync(async () => await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(MicroscopePixelSizeItemDto? selectReviewItemDto, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            if (selectReviewItemDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
                StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);

            selectReviewItemDto.IsVerified = false;
            Cache.MicroscopeLensInformation = SelectReviewItemDto!.LensInformation;
            SelectMicroscopePixelSizeCacheItem = Cache.CurrentCalibrationCacheItem;
            SelectMicroscopePixelSizeCacheItem.FindPosition = selectReviewItemDto.FindPosition;

            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
            Logger.LogHtmlInformation($"{Cache.MicroscopeLensInformation.LensName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                SelectMicroscopePixelSizeCacheItem.FindPosition,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            if (GetPixelSize(detectImageDirectory, cancellationToken, 1) == false) return false;

            var averageWidth = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Width).Average();
            var averageHeight = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Height).Average();
            var averagePixelSize = new Size(averageWidth, averageHeight);
            var error = (Size)((Vector)averagePixelSize - (Vector)selectReviewItemDto.PixelSize);
            ReviewResultList.Add((Cache.MicroscopeLensInformation.LensName, selectReviewItemDto.PixelSize, averagePixelSize, error));
            var result = error.DiagonalLength < Cache.Threshold.DiagonalLength;

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
                return false;
            }

            if (!result || !IsAutoCalibrate)
            {
                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({averagePixelSize}) Old Offset: ({SelectReviewItemDto!.PixelSize}) Error: ({error})", DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            }

            return result;
        }, cancellationToken);
    }

    private bool GetPixelSize(string detectImageDirectory, CancellationToken cancellationToken, int repeatCount = 10)
    {
        try
        {
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(SelectMicroscopePixelSizeCacheItem.FindPosition, Cache.CalChipSiteModelEnum);
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
                    LensInformation = Cache.MicroscopeLensInformation,
                    FindPosition = SelectMicroscopePixelSizeCacheItem.FindPosition,
                    OriginFilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    FilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Drawing_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    PixelSize = pixelSize
                };

                image.Save(microscopePixelSizeItemDto.OriginFilePath);
                drawingImage.Save(microscopePixelSizeItemDto.FilePath);

                var htmlBulletList = new HtmlBullet(new
                {
                    microscopePixelSizeItemDto.LensInformation.LensName,
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
                .Where(t => t.LensInformation != itemDto.LensInformation),
            itemDto.Clone()
        ];

        foreach (var microscopeFocusCacheItem in Cache.MicroscopePixelSizeCacheItemDic)
        {
            if (ApplicationCookie.MicroscopeLensInformations.SingleOrDefault(t => t.LensName == microscopeFocusCacheItem.Key) is null)
                Cache.MicroscopePixelSizeCacheItemDic.TryRemove(microscopeFocusCacheItem.Key, out _);
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

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
                new CalibrationItemStep { StepName = "loading" },
                .. ApplicationCookie.MicroscopeLensInformations.Select(info => new CalibrationItemStep { StepName = info.LensName }),
                new CalibrationItemStep { StepName = "Review" }
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
                                Cache.AlgorithmTemplateTypeEnum
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
                                    DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.LensInformation.LensName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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

                        if (ResultMicroscopePixelSizeItemDtoList.Count(t => t.IsOk) == ApplicationCookie.MicroscopeLensInformations.Count)
                        {
                            Logger.LogHtmlInformation("ResultPixelSize", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                            {
                                ResultPixelSize = new HtmlTable([.. ResultMicroscopePixelSizeItemDtoList.Select(t => new { t.LensInformation.LensName, t.PixelSize })])
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

        Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformations.Single(t => t.LensName == microscopeName);
        SelectMicroscopePixelSizeCacheItem = Cache.CurrentCalibrationCacheItem;
        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });

        if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(SelectMicroscopePixelSizeCacheItem.WaferMaskTypeEnum, Cache.MicroscopeLensInformation, null, out var maskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo, out var position);
        Cache.SetFindFocusPosition(position);

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation, true);
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
                    if (await AutomationRecipeInformationAsync(SelectReviewItemDto.LensInformation.LensName) == false) return false;
                    if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false)
                    {
                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.LensInformation.LensName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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