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

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "DSW Left Top Position", StepIndex = 1 },
        new() { StepName = "DSW Right Bottom Position", StepIndex = 2 },
        new() { StepName = "DSW", StepIndex = 3 },
        new() { StepName = "Undefined Left Top Position", StepIndex = 4 },
        new() { StepName = "Undefined Right Bottom Position", StepIndex = 5 },
        new() { StepName = "Undefined", StepIndex = 6 },
        new() { StepName = "Haze Left Top Position", StepIndex = 7 },
        new() { StepName = "Haze Right Bottom Position", StepIndex = 8 },
        new() { StepName = "Haze", StepIndex = 9 },
        new() { StepName = "Shiny Wafer Left Top Position", StepIndex = 10 },
        new() { StepName = "Shiny Wafer Right Bottom Position", StepIndex = 11 },
        new() { StepName = "Shiny Wafer", StepIndex = 12 },
        new() { StepName = "Chuck RTFC", StepIndex = 13 }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private MicroscopeCalChipDto _resultMicroscopeCalChipDto = new();

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
        if (Cache.MicroscopeLensInformation.LensCode == -1)
            Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];

        StageViewModel.SetAbsoluteStageTheta(0);

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        AfViewModel.ToggleBrightFieldEnable(false);
        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.MicroscopeLensInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.DswLeftTopPosition);
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        if (ReviewDto.IsCalibrated == false) return false;

        AfViewModel.ToggleBrightFieldEnable(false);
        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.MicroscopeLensInformation) == false)
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
        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.MicroscopeLensInformation) == false)
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
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.DswLeftTopPosition);
                return true;

            case 2:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.DswRightBottomPosition);
                return true;

            case 3:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.DswPosition);
                return true;

            case 4:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.UndefinedLeftTopPosition);
                return true;

            case 5:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.UndefinedRightBottomPosition);
                return true;

            case 6:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.UndefinedPosition);
                return true;

            case 7:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.HazeLeftTopPosition);
                return true;

            case 8:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.HazeRightBottomPosition);
                return true;

            case 9:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.HazePosition);
                return true;

            case 10:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.ShinyWaferLeftTopPosition);
                return true;

            case 11:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.ShinyWaferRightBottomPosition);
                return true;

            case 12:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.ShinyWaferPosition);
                return true;

            default:
                return true;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.DswRightBottomPosition);
                break;

            case 1:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.DswPosition);
                break;

            case 2:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.UndefinedLeftTopPosition);
                break;

            case 3:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.UndefinedRightBottomPosition);
                break;

            case 4:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.UndefinedPosition);
                break;

            case 5:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.HazeLeftTopPosition);
                break;

            case 6:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.HazeRightBottomPosition);
                break;

            case 7:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.HazePosition);
                break;

            case 8:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.ShinyWaferLeftTopPosition);
                break;

            case 9:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.ShinyWaferRightBottomPosition);
                break;

            case 10:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.ShinyWaferPosition);
                break;

            case 11:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.ChuckPosition);
                break;

            default:
                break;
        }

        switch (CalibrationStepIndex)
        {
            case 2 or 5 or 8 or 11 or 12:
                ClearCalibrationTemp();

                var isCalibrated = CalibrationStepIndex == CalibrationStepList.Count - 1;

                ResultMicroscopeCalChipDto.IsCalibrated = isCalibrated;
                if (Save(ResultMicroscopeCalChipDto, cancellationToken) == false)
                {
                    ResultMicroscopeCalChipDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                IsCalibrated = isCalibrated;

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GotoBrightFieldPositionAsync(string name)
    {
        try
        {
            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            await Task.Run(() =>
            {
                switch (name)
                {
                    case "DSW":
                        var brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(ReviewDto.DswBrightFieldMachinePosition);
                        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(brightFieldPosition);
                        break;

                    case "Undefined":
                        brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(ReviewDto.UndefinedBrightFieldMachinePosition);
                        StageViewModel.SetCalChipUndefinedBrightFieldAbsoluteStageXy(brightFieldPosition);
                        break;

                    case "Haze":
                        brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(ReviewDto.HazeBrightFieldMachinePosition);
                        StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(brightFieldPosition);
                        break;

                    case "ShinyWafer":
                        brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(ReviewDto.ShinyWaferBrightFieldMachinePosition);
                        StageViewModel.SetCalChipShinyWaferBrightFieldAbsoluteStageXy(brightFieldPosition);
                        break;

                    default:
                        break;
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoDarkFieldPositionAsync(string name)
    {
        try
        {
            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            await Task.Run(() =>
            {
                switch (name)
                {
                    case "DSW":
                        var darkFieldPosition = StageViewModel.MachineToDarkFieldPosition(ReviewDto.DswDarkFieldMachinePosition);
                        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(darkFieldPosition);
                        break;

                    case "Undefined":
                        darkFieldPosition = StageViewModel.MachineToDarkFieldPosition(ReviewDto.UndefinedDarkFieldMachinePosition);
                        StageViewModel.SetCalChipUndefinedBrightFieldAbsoluteStageXy(darkFieldPosition);
                        break;

                    case "Haze":
                        darkFieldPosition = StageViewModel.MachineToDarkFieldPosition(ReviewDto.HazeDarkFieldMachinePosition);
                        StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(darkFieldPosition);
                        break;

                    case "ShinyWafer":
                        darkFieldPosition = StageViewModel.MachineToDarkFieldPosition(ReviewDto.ShinyWaferDarkFieldMachinePosition);
                        StageViewModel.SetCalChipShinyWaferBrightFieldAbsoluteStageXy(darkFieldPosition);
                        break;

                    default:
                        break;
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(string name, CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var resultMachine = StageViewModel.GetMachineStagePosition();

            switch (Cache.CalChipSiteModelEnum)
            {
                case CalChipSiteModelEnum.DswModel:
                    switch (name)
                    {
                        case "LeftTop":
                            Cache.DswLeftTopPosition = resultMachine;
                            break;

                        case "RightBottom":
                            Cache.DswRightBottomPosition = resultMachine;
                            break;
                    }

                    ResultMicroscopeCalChipDto.DswBrightFieldMachinePosition = Cache.DswPosition;
                    var darkFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.DswPosition);
                    ResultMicroscopeCalChipDto.DswDarkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldPosition);
                    break;

                case CalChipSiteModelEnum.UndefinedModel:
                    switch (name)
                    {
                        case "LeftTop":
                            Cache.UndefinedLeftTopPosition = resultMachine;
                            break;

                        case "RightBottom":
                            Cache.UndefinedRightBottomPosition = resultMachine;
                            break;
                    }

                    ResultMicroscopeCalChipDto.UndefinedBrightFieldMachinePosition = Cache.UndefinedPosition;
                    darkFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.UndefinedPosition);
                    ResultMicroscopeCalChipDto.UndefinedDarkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldPosition);
                    break;

                case CalChipSiteModelEnum.HazeModel:
                    switch (name)
                    {
                        case "LeftTop":
                            Cache.HazeLeftTopPosition = resultMachine;
                            break;

                        case "RightBottom":
                            Cache.HazeRightBottomPosition = resultMachine;
                            break;
                    }

                    ResultMicroscopeCalChipDto.HazeBrightFieldMachinePosition = Cache.HazePosition;
                    darkFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazePosition);
                    ResultMicroscopeCalChipDto.HazeDarkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldPosition);
                    break;

                case CalChipSiteModelEnum.ShinyWaferModel:
                    switch (name)
                    {
                        case "LeftTop":
                            Cache.ShinyWaferLeftTopPosition = resultMachine;
                            break;

                        case "RightBottom":
                            Cache.ShinyWaferRightBottomPosition = resultMachine;
                            break;
                    }

                    ResultMicroscopeCalChipDto.ShinyWaferBrightFieldMachinePosition = Cache.ShinyWaferPosition;
                    darkFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.ShinyWaferPosition);
                    ResultMicroscopeCalChipDto.ShinyWaferDarkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldPosition);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Direction = name,
                Cache.CalChipSiteModelEnum,
                MachinePosition = resultMachine
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.ChuckPosition = StageViewModel.GetMachineStagePosition();
            var (ecs, afMotor) = LaserViewModel.RuntimeAfCalibration(Cache.ChuckPosition);
            ResultMicroscopeCalChipDto.ChuckAfEcsValue = ecs;
            ResultMicroscopeCalChipDto.ChuckAfMotorValue = afMotor;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                ChuckAfEcsValue = ecs,
                ChuckAfMotorValue = afMotor,
                ResultMicroscopeCalChipDto.DswAfEcsValue,
                ResultMicroscopeCalChipDto.DswAfMotorValue,
                ResultMicroscopeCalChipDto.HazeAfEcsValue,
                ResultMicroscopeCalChipDto.HazeAfMotorValue,
                ResultMicroscopeCalChipDto.DswToChuckAfEcsValue,
                ResultMicroscopeCalChipDto.DswToChuckAfMotorValue,
                ResultMicroscopeCalChipDto.HazeToChuckAfEcsValue,
                ResultMicroscopeCalChipDto.HazeToChuckAfMotorValue,
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
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

            var findFocusPosition = Cache.GetFindPosition();
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
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(findFocusPosition);

            if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.MicroscopeLensInformation) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var ecsValue = AfViewModel.GetSensorEcsValue();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.MicroscopeLensInformation.LensName,
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
                    LensInformation = Cache.MicroscopeLensInformation,
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

            double ecs = 0d;
            double afMotor = 0d;
            if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel or CalChipSiteModelEnum.HazeModel)
            {
                (ecs, afMotor) = LaserViewModel.RuntimeAfCalibration(calChipSiteModelEnum: Cache.CalChipSiteModelEnum);
                ResultMicroscopeCalChipDto.SetAfEcsValue(Cache.CalChipSiteModelEnum, ecs);
                ResultMicroscopeCalChipDto.SetAfMotorValue(Cache.CalChipSiteModelEnum, afMotor);
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                RtfcAfEcs = ecs,
                RtfcAfMotor = afMotor,
                Cache.MicroscopeLensInformation.LensName,
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

            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

            foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>().Where(t => t != CalChipSiteModelEnum.ChuckModel))
            {
                Cache.CalChipSiteModelEnum = calChipSiteModelEnum;
                Logger.LogHtmlInformation($"{Cache.CalChipSiteModelEnum}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                var findFocusPosition = Cache.GetFindPosition();
                var findFocusMin = Cache.GetFindFocusMin();
                var findFocusMax = Cache.GetFindFocusMax();
                var findFocusInterval = Cache.GetFindFocusInterval();

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.CalChipSiteModelEnum,
                    Cache.MicroscopeLensInformation,
                    FindFocusPosition = findFocusPosition,
                    FindFocusLimitMin = findFocusMin,
                    FindFocusLimitMax = findFocusMax,
                    FindFocusInterval = findFocusInterval,
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(findFocusPosition);

                if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.MicroscopeLensInformation) == false)
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
                    LensInformation = Cache.MicroscopeLensInformation,
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

            // Rtfc
            var calchipVerifyItemDto = new MicroscopeCalChipDto();
            (calchipVerifyItemDto.ChuckAfEcsValue, calchipVerifyItemDto.ChuckAfMotorValue) = LaserViewModel.RuntimeAfCalibration(Cache.ChuckPosition);
            (calchipVerifyItemDto.DswAfEcsValue, calchipVerifyItemDto.DswAfMotorValue) = LaserViewModel.RuntimeAfCalibration(calChipSiteModelEnum: CalChipSiteModelEnum.DswModel);
            (calchipVerifyItemDto.HazeAfEcsValue, calchipVerifyItemDto.HazeAfMotorValue) = LaserViewModel.RuntimeAfCalibration(calChipSiteModelEnum: CalChipSiteModelEnum.HazeModel);

            var dswToChuckAfEcsOffset = calchipVerifyItemDto.DswToChuckAfEcsValue - ReviewDto.DswToChuckAfEcsValue;
            var dswToChuckAfMotorOffset = calchipVerifyItemDto.DswToChuckAfMotorValue - ReviewDto.DswToChuckAfMotorValue;
            var hazeToChuckAfEcsOffset = calchipVerifyItemDto.HazeToChuckAfEcsValue - ReviewDto.HazeToChuckAfEcsValue;
            var hazeToChuckAfMotorOffset = calchipVerifyItemDto.HazeToChuckAfMotorValue - ReviewDto.HazeToChuckAfMotorValue;
            var rtfcResult = Math.Abs(dswToChuckAfEcsOffset) < Cache.AfEcsErrorThreshold
                             && Math.Abs(dswToChuckAfMotorOffset) < Cache.AfMotorErrorThreshold
                             && Math.Abs(hazeToChuckAfEcsOffset) < Cache.AfEcsErrorThreshold
                             && Math.Abs(hazeToChuckAfMotorOffset) < Cache.AfMotorErrorThreshold;

            var result = resultList.All(t => t) && rtfcResult;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                NewOffset = Cache.VerifyResultQuality,
                OldOffset = string.Join(", ", oldQualityList),
                Error = Cache.VerifyResultError,
                QualityThreshold = Cache.Threshold,
                Cache.AfEcsErrorThreshold,
                Cache.AfMotorErrorThreshold,
                calchipVerifyItemDto.ChuckAfEcsValue,
                calchipVerifyItemDto.ChuckAfMotorValue,
                calchipVerifyItemDto.DswAfEcsValue,
                calchipVerifyItemDto.DswAfMotorValue,
                calchipVerifyItemDto.HazeAfEcsValue,
                calchipVerifyItemDto.HazeAfMotorValue,
                calchipVerifyItemDto.DswToChuckAfEcsValue,
                calchipVerifyItemDto.DswToChuckAfMotorValue,
                calchipVerifyItemDto.HazeToChuckAfEcsValue,
                calchipVerifyItemDto.HazeToChuckAfMotorValue,
                dswToChuckAfEcsOffset,
                dswToChuckAfMotorOffset,
                hazeToChuckAfEcsOffset,
                hazeToChuckAfMotorOffset
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
                                            $"Error: ({Cache.VerifyResultError})" +
                                            $"DswToChuckAfEcsOffset:({dswToChuckAfEcsOffset})" +
                                            $"DswToChuckAfMotorOffset: ({dswToChuckAfMotorOffset})" +
                                            $"HazeToChuckAfEcsOffset: ({hazeToChuckAfEcsOffset})" +
                                            $"HazeToChuckAfMotorOffset: ({hazeToChuckAfMotorOffset})", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

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
            microscopeFocusItemDto.LensInformation.LensName,
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