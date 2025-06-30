using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeCalChipCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCalChipCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "DSW" },
        new() { StepName = "Undefined" },
        new() { StepName = "Haze" },
        new() { StepName = "Shiny Wafer" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private MicroscopeCalChipDto _resultMicroscopeCalChipDto = new()
    {
        DswPosition = new Point(122000, 128000),
        UndefinedPosition = new Point(-122000, 128000),
        HazePosition = new Point(-122000, -128000),
        ShinyWaferPosition = new Point(122000, -128000)
    };

    [ObservableProperty]
    private ObservableCollection<MicroscopeFocusItemDto> _microscopeFocusItemDtoList = [];

    [ObservableProperty]
    private Point[] _ecsPoints = [];

    [ObservableProperty]
    private MicroscopeFocusItemDto? _selectedMicroscopeFocusItemDto;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private MicroscopeCalChipDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private MicroscopeCalChipCache _cache = new();

    [ObservableProperty]
    private MicroscopeCalChipDto _calibration = new();

    [ObservableProperty]
    private MicroscopeFocusItemDto[] _microscopeFocusItems = [];

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

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<MicroscopeCalChipCache>();
        Calibration = CacheProvider.GetOrDefault<MicroscopeCalChipDto>();

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        AfViewModel.ToggleBrightFieldEnable(false);
        if (MicroscopeViewModel.SwitchMagnificationNotAutoFocus(Cache.MicroscopeMagnificationEnum) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.DswPosition);
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        if (ReviewDto.IsCalibrated == false) return false;

        AfViewModel.ToggleBrightFieldEnable(false);
        if (MicroscopeViewModel.SwitchMagnificationNotAutoFocus(Cache.MicroscopeMagnificationEnum) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        AfViewModel.ToggleBrightFieldEnable(false);
        AfViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);
        if (MicroscopeViewModel.SwitchMagnificationNotAutoFocus(Cache.MicroscopeMagnificationEnum) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Point.Origin);
        return true;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.DswPosition);
                return true;

            case 2:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.UndefinedPosition);
                return true;

            case 3:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.HazePosition);
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
            case 0 or 1 or 2 or 3:
                ClearCalibrationTemp();

                var isCalibrated = CalibrationStepIndex == 3;

                ResultMicroscopeCalChipDto.IsCalibrated = isCalibrated;
                if (Save(ResultMicroscopeCalChipDto, cancellationToken) == false)
                {
                    ResultMicroscopeCalChipDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                IsCalibrated = isCalibrated;

                switch (CalibrationStepIndex)
                {
                    case 0:
                        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.UndefinedPosition);
                        return true;

                    case 1:
                        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.HazePosition);
                        return true;

                    case 2:
                        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.ShinyWaferPosition);
                        return true;

                    case 3:
                        return true;

                    default:
                        return false;
                }

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var resultBright = StageViewModel.GetBrightFieldStagePosition();
                var resultDarkMachine = StageViewModel.DarkFieldToMachinePosition(resultBright);
                var resultBrightMachine = StageViewModel.BrightFieldToMachinePosition(resultBright);

                switch (Cache.CalChipSiteModelEnum)
                {
                    case CalChipSiteModelEnum.DswModel:
                        ResultMicroscopeCalChipDto.DswPosition = resultBright;
                        ResultMicroscopeCalChipDto.DswBrightFieldMachinePosition = resultBrightMachine;
                        ResultMicroscopeCalChipDto.DswDarkFieldMachinePosition = resultDarkMachine;
                        break;

                    case CalChipSiteModelEnum.UndefinedModel:
                        ResultMicroscopeCalChipDto.UndefinedPosition = resultBright;
                        ResultMicroscopeCalChipDto.UndefinedBrightFieldMachinePosition = resultBrightMachine;
                        ResultMicroscopeCalChipDto.UndefinedDarkFieldMachinePosition = resultDarkMachine;
                        break;

                    case CalChipSiteModelEnum.HazeModel:
                        ResultMicroscopeCalChipDto.HazePosition = resultBright;
                        ResultMicroscopeCalChipDto.HazeBrightFieldMachinePosition = resultBrightMachine;
                        ResultMicroscopeCalChipDto.HazeDarkFieldMachinePosition = resultDarkMachine;
                        break;

                    case CalChipSiteModelEnum.ShinyWaferModel:
                        ResultMicroscopeCalChipDto.ShinyWaferPosition = resultBright;
                        ResultMicroscopeCalChipDto.ShinyWaferBrightFieldMachinePosition = resultBrightMachine;
                        ResultMicroscopeCalChipDto.ShinyWaferDarkFieldMachinePosition = resultDarkMachine;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
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
            await Task.Run(() =>
            {
                switch (Cache.CalChipSiteModelEnum)
                {
                    case CalChipSiteModelEnum.DswModel:
                        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.DswPosition);
                        break;

                    case CalChipSiteModelEnum.UndefinedModel:
                        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.UndefinedPosition);
                        break;

                    case CalChipSiteModelEnum.HazeModel:
                        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.HazePosition);
                        break;

                    case CalChipSiteModelEnum.ShinyWaferModel:
                        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(ResultMicroscopeCalChipDto.ShinyWaferPosition);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            var findFocusPosition = ResultMicroscopeCalChipDto.GetFindFocusPosition(Cache.CalChipSiteModelEnum);
            var findFocusMin = Cache.GetFindFocusMin();
            var findFocusMax = Cache.GetFindFocusMax();
            var findFocusInterval = Cache.GetFindFocusInterval();

            if (findFocusMin > findFocusMax || findFocusInterval == 0)
            {
                DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Min <= Focs Max and Focus Interval > 0)", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            AfViewModel.ToggleBrightFieldEnable(false);
            AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
            StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(findFocusPosition);

            if (MicroscopeViewModel.SwitchMagnificationNotAutoFocus(Cache.MicroscopeMagnificationEnum) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var ecsValue = AfViewModel.GetSensorEcsValue();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                MicroscopeMagnification = Cache.MicroscopeMagnificationEnum,
                CurrentEcsValue = ecsValue,
                FindFocusPosition = findFocusPosition,
                FindFocusLimitMin = findFocusMin,
                FindFocusLimitMax = findFocusMax,
                FindFocusInterval = findFocusInterval,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var ecsList = new List<MicroscopeFocusItemDto>();
            foreach (var (index, ecsValueTemp) in Enumerable.Range(0, Convert.ToInt32((findFocusMax - findFocusMin) / findFocusInterval) + 1)
                         .Select(x => Math.Min(findFocusMin + x * findFocusInterval, findFocusMax))
                         .Select((d, i) => (i, d)))
            {
                ecsList.Add(new MicroscopeFocusItemDto
                {
                    Index = index + 1,
                    MicroscopeMagnificationEnum = Cache.MicroscopeMagnificationEnum,
                    FindPosition = findFocusPosition,
                    Quality = 0,
                    EcsValue = ecsValueTemp,
                    FilePath = detectImageDirectory
                });
            }

            var listDownResult = new List<bool>();
            double? downQualityValue = null;
            foreach (var findFocalItem in ecsList.OrderBy(t => t.Index))
            {
                cancellationToken.ThrowIfCancellationRequested();
                GetQuality(findFocalItem);

                if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < findFocalItem.Quality);
                downQualityValue = findFocalItem.Quality;
                if (EnumerableHelper.HasConsecutiveFalse(listDownResult, 30)) break; // 连续30个下降说明已经到了最低点
            }

            var findFocalItemResult = MicroscopeFocusItemDtoList.OrderByDescending(t => t.Quality).ToList();
            var temp = findFocalItemResult[0];
            SelectedMicroscopeFocusItemDto = temp;
            ResultMicroscopeCalChipDto.SetEcsValue(Cache.CalChipSiteModelEnum, SelectedMicroscopeFocusItemDto.EcsValue);
            ResultMicroscopeCalChipDto.SetQuality(Cache.CalChipSiteModelEnum, SelectedMicroscopeFocusItemDto.Quality);
            ResultMicroscopeCalChipDto.SetFilePath(Cache.CalChipSiteModelEnum, SelectedMicroscopeFocusItemDto.FilePath);

            AfViewModel.SetSensorBrightFieldCalChipStandardEcsValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.GetEcsValue(Cache.CalChipSiteModelEnum));
            AfViewModel.SetSensorBrightFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.GetBrightFieldMachinePosition(Cache.CalChipSiteModelEnum));
            AfViewModel.SetSensorDarkFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.GetDarkFieldMachinePosition(Cache.CalChipSiteModelEnum));

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                MicroscopeMagnification = Cache.MicroscopeMagnificationEnum,
                EcsValue = ResultMicroscopeCalChipDto.GetEcsValue(Cache.CalChipSiteModelEnum),
                ImageQuality = ResultMicroscopeCalChipDto.GetQuality(Cache.CalChipSiteModelEnum),
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(ResultMicroscopeCalChipDto.GetFilePath(Cache.CalChipSiteModelEnum), htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;
            var oldQualityList = new List<double>();
            var newQualityList = new List<double>();
            var errorList = new List<double>();
            var resultList = new List<bool>();

            AfViewModel.ToggleBrightFieldEnable(false);

            foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>().Where(t => t != CalChipSiteModelEnum.ChuckModel))
            {
                Cache.CalChipSiteModelEnum = calChipSiteModelEnum;
                Logger.LogHtmlInformation($"{Cache.CalChipSiteModelEnum}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                var findFocusPosition = ReviewDto.GetFindFocusPosition(Cache.CalChipSiteModelEnum);
                var findFocusMin = Cache.GetFindFocusMin();
                var findFocusMax = Cache.GetFindFocusMax();
                var findFocusInterval = Cache.GetFindFocusInterval();

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.CalChipSiteModelEnum,
                    MicroscopeMagnification = Cache.MicroscopeMagnificationEnum,
                    FindFocusPosition = findFocusPosition,
                    FindFocusLimitMin = findFocusMin,
                    FindFocusLimitMax = findFocusMax,
                    FindFocusInterval = findFocusInterval,
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(findFocusPosition);

                if (MicroscopeViewModel.SwitchMagnificationNotAutoFocus(Cache.MicroscopeMagnificationEnum) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Switch Magnification Failed."), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                AfViewModel.SetSensorBrightFieldCalChipStandardEcsValue(Cache.CalChipSiteModelEnum, ReviewDto.GetEcsValue(Cache.CalChipSiteModelEnum));
                AfViewModel.ToggleBrightFieldEnable(true);

                Thread.Sleep(5000);

                var microscopeFocusItemDto = new MicroscopeFocusItemDto
                {
                    Index = 0,
                    MicroscopeMagnificationEnum = Cache.MicroscopeMagnificationEnum,
                    FindPosition = findFocusPosition,
                    Quality = 0,
                    EcsValue = ReviewDto.GetEcsValue(Cache.CalChipSiteModelEnum),
                    FilePath = detectImageDirectory
                };
                GetQuality(microscopeFocusItemDto);

                AfViewModel.ToggleBrightFieldEnable(false);

                var quality = microscopeFocusItemDto.Quality;
                var error = quality - ReviewDto.GetQuality(Cache.CalChipSiteModelEnum);
                var resultTemp = Math.Abs(error) < Cache.Threshold;
                oldQualityList.Add(ReviewDto.GetQuality(Cache.CalChipSiteModelEnum));
                newQualityList.Add(quality);
                errorList.Add(error);
                resultList.Add(resultTemp);
            }

            Cache.VerifyResultQuality = string.Join(", ", newQualityList);
            Cache.VerifyResultError = string.Join(", ", errorList);
            var result = resultList.All(t => t);

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
            {
                MicroscopeMagnification = Cache.MicroscopeMagnificationEnum,
                NewOffset = Cache.VerifyResultQuality,
                OldOffset = string.Join(", ", oldQualityList),
                Error = Cache.VerifyResultError,
                Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            ReviewDto.IsVerified = result;
            if (Save(ReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed."), HtmlLogUniqueId.LoggingHtml());
                ReviewDto.IsVerified = false;
                return false;
            }

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}{Environment.NewLine}" +
                                            $"New Offset: ({Cache.VerifyResultQuality}){Environment.NewLine}" +
                                            $"New Offset: ({string.Join(", ", oldQualityList)}){Environment.NewLine}" +
                                            $"Error: ({Cache.VerifyResultError})", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private void GetQuality(MicroscopeFocusItemDto microscopeFocusItemDto)
    {
        AfViewModel.SetSensorEcsValue(microscopeFocusItemDto.EcsValue);

        if (microscopeFocusItemDto.Index == 1) Thread.Sleep(3000);
        using var image = ReviewViewModel.GetBrightFieldImage();

        var quality = ReviewViewModel.GetQuality(image);
        microscopeFocusItemDto.Quality = quality;
        microscopeFocusItemDto.FilePath =
            $"{microscopeFocusItemDto.FilePath}\\Index({microscopeFocusItemDto.Index})_Ecs({microscopeFocusItemDto.EcsValue:F3})_Quality({microscopeFocusItemDto.Quality:F3})_Guid({HtmlLogUniqueId}).jpg";

        HalconHelper.Save(image, microscopeFocusItemDto.FilePath);
        SynchronizationContextProvider.Send(() =>
        {
            MicroscopeFocusItemDtoList.Add(microscopeFocusItemDto);
            EcsPoints = [.. EcsPoints, new Point(microscopeFocusItemDto.EcsValue, microscopeFocusItemDto.Quality)];
        });

        var htmlBulletList = new HtmlBullet(new
        {
            microscopeFocusItemDto.EcsValue,
            ImageQuality = microscopeFocusItemDto.Quality,
            MicroscopeMagnification = microscopeFocusItemDto.MicroscopeMagnificationEnum,
            HtmlTab = new HtmlTab(new
            {
                Image = new HtmlImage(microscopeFocusItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        });

        Logger.LogHtmlInformation($"Get Quality OK! Time:{microscopeFocusItemDto.Index}", HtmlHeaderLevelEnum.Header3, htmlBulletList, HtmlLogUniqueId.LoggingHtml());
    }

    private bool Save(MicroscopeCalChipDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken) && CacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() =>
        {
            MicroscopeFocusItemDtoList.Clear();
            EcsPoints = [];
        });
        SelectedMicroscopeFocusItemDto = null;
    }

    #endregion 校准
}