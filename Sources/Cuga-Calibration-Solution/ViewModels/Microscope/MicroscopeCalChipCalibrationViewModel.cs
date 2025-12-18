using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Setting;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeCalChipCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCalChipCalibrationViewModel(
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie) : CalibrationViewModelBase
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

    private bool _isSkipRtfc = false;

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private MicroscopeCalChipCache _cache = new();

    [ObservableProperty]
    private MicroscopeCalChipDto _calibration = new();

    [ObservableProperty]
    private MicroscopeFocusItemDto[] _microscopeFocusItems = [];

    #endregion 缓存

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GotoBrightFieldPositionCommand))]
    [NotifyCanExecuteChangedFor(nameof(GotoDarkFieldPositionCommand))]
    private bool _isWindowEnable = true;

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
        if (applicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation) == false)
            Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        StageViewModel.SetAbsoluteStageTheta(0);

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
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

        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);

        DialogWindowProvider.TryShowDialog("Do you want to skip dark field rtfc step?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
        _isSkipRtfc = dialogResult == DialogResultEnum.Yes;
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        ResultMicroscopeCalChipDto = Calibration.Clone();
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
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                return true;

            case 2:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                return true;

            case 3:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                return true;

            case 4:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                return true;

            case 5:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                return true;

            case 6:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                return true;

            case 7:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                return true;

            case 8:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                return true;

            case 9:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                return true;

            case 10:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                return true;

            case 11:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                return true;

            case 12:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
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
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                break;

            case 1:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                break;

            case 2:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                break;

            case 3:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                break;

            case 4:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                break;

            case 5:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                break;

            case 6:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                break;

            case 7:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                break;

            case 8:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                break;

            case 9:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                break;

            case 10:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
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

    [RelayCommand(CanExecute = nameof(IsWindowEnable))]
    private async Task GotoBrightFieldPositionAsync(CalChipSiteModelEnum? calChipSiteModelEnum)
    {
        try
        {
            if (calChipSiteModelEnum is null)
                return;
            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            await Task.Run(() =>
            {
                ReviewDto.CalChipSiteModelEnum = calChipSiteModelEnum.Value;

                var brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(ReviewDto.CurrentItem.BrightFieldMachinePosition);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(brightFieldPosition);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand(CanExecute = nameof(IsWindowEnable))]
    private async Task GotoDarkFieldPositionAsync(CalChipSiteModelEnum? calChipSiteModelEnum)
    {
        try
        {
            if (calChipSiteModelEnum is null)
                return;

            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            await Task.Run(() =>
            {
                ReviewDto.CalChipSiteModelEnum = calChipSiteModelEnum.Value;

                var darkFieldPosition = StageViewModel.MachineToDarkFieldPosition(ReviewDto.CurrentItem.DarkFieldMachinePosition);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(darkFieldPosition);
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
            switch (name)
            {
                case "LeftTop":
                    Cache.Item.LeftTopPosition = resultMachine;
                    break;

                case "RightBottom":
                    Cache.Item.RightBottomPosition = resultMachine;
                    break;
            }

            ResultMicroscopeCalChipDto.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            var darkFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.CenterPosition);
            ResultMicroscopeCalChipDto.CurrentItem.BrightFieldMachinePosition = Cache.Item.CenterPosition;
            ResultMicroscopeCalChipDto.CurrentItem.DarkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldPosition);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Direction = name,
                Cache.CalChipSiteModelEnum,
                MachinePosition = resultMachine,
                darkFieldPosition,
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
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

            #region BF

            var findFocusPosition = Cache.Item.CenterPosition;
            var findFocusMin = Cache.Item.FindFocusMin;
            var findFocusMax = Cache.Item.FindFocusMax;
            var findFocusInterval = Cache.Item.FindFocusInterval;

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
                if (listDownResult.HasConsecutiveEqual(30, false)) break; // 连续30个下降说明已经到了最低点
            }

            var findFocalItemResult = MicroscopeFocusItemDtoList.OrderByDescending(t => t.Quality).ToList();
            var temp = findFocalItemResult[0];
            SelectedMicroscopeFocusItemDto = temp;
            ResultMicroscopeCalChipDto.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            ResultMicroscopeCalChipDto.CurrentItem.EcsValue = SelectedMicroscopeFocusItemDto.EcsValue;
            ResultMicroscopeCalChipDto.CurrentItem.BrightFieldQuality = SelectedMicroscopeFocusItemDto.Quality;
            ResultMicroscopeCalChipDto.CurrentItem.BrightFieldFilePath = SelectedMicroscopeFocusItemDto.FilePath;

            AfViewModel.SetSensorBrightFieldCalChipStandardEcsValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.CurrentItem.EcsValue);
            AfViewModel.SetSensorBrightFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.CurrentItem.BrightFieldMachinePosition);
            AfViewModel.SetSensorDarkFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.CurrentItem.DarkFieldMachinePosition);

            #endregion BF

            #region DF

            if (!_isSkipRtfc && Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel or CalChipSiteModelEnum.HazeModel)
            {
                RuntimeAfCalibration(ResultMicroscopeCalChipDto, Cache.CalChipSiteModelEnum);
            }

            #endregion DF

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                IsSkipRtfc = _isSkipRtfc,
                Cache.CalChipSiteModelEnum,
                Cache.MicroscopeLensInformation.LensName,
                ResultMicroscopeCalChipDto.CurrentItem.EcsValue,
                ResultMicroscopeCalChipDto.CurrentItem.AfEcsValue,
                ResultMicroscopeCalChipDto.CurrentItem.AfMotorValue,
                ResultMicroscopeCalChipDto.CurrentItem.BrightFieldQuality,
                ResultMicroscopeCalChipDto.CurrentItem.DarkFieldQuality
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.ChuckPosition = StageViewModel.GetBrightFieldStagePosition();
            if (_isSkipRtfc) return true;

            RuntimeAfCalibration(ResultMicroscopeCalChipDto, CalChipSiteModelEnum.ChuckModel);

            var afMotorResults = new[]
            {
                GuardUtils.IsNotNullAndReturn(ResultMicroscopeCalChipDto.ChuckItem).AfMotorValue,
                GuardUtils.IsNotNullAndReturn(ResultMicroscopeCalChipDto.DswItem).AfMotorValue,
                GuardUtils.IsNotNullAndReturn(ResultMicroscopeCalChipDto.HazeItem).AfMotorValue
            };
            var afOffsetAbs = Math.Abs(afMotorResults.Max() - afMotorResults.Min());
            var result = afOffsetAbs < Cache.AfMotorOffsetThreshold;

            Logger.LogHtmlInformation($"Calibration {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AfMotorOffsetThreshold,
                Cache.CalChipSiteModelEnum,
                ResultMicroscopeCalChipDto.DswToChuckAfEcsValue,
                ResultMicroscopeCalChipDto.DswToChuckAfMotorValue,
                ResultMicroscopeCalChipDto.HazeToChuckAfEcsValue,
                ResultMicroscopeCalChipDto.HazeToChuckAfMotorValue,
                AfOffsetAbs = afOffsetAbs,
                CalibrationResult = new HtmlTable([
                    .. ResultMicroscopeCalChipDto.Items.OrderBy(t => t.Key)
                        .Select(t => new { t.Key, t.Value.BrightFieldMachinePosition, t.Value.EcsValue, t.Value.BrightFieldQuality, t.Value.DarkFieldMachinePosition, t.Value.AfEcsValue, t.Value.AfMotorValue, t.Value.DarkFieldQuality })
                ])
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() => { IsWindowEnable = false; });

        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        try
        {
            await InvokeVerifyAsync(() =>
            {
                ClearCalibrationTemp();
                var detectImageDirectory = ImageFileDirectory;

                Cache.VerifyBrightFieldResultError = string.Empty;
                Cache.VerifyDarkFieldResultError = string.Empty;

                AfViewModel.ToggleBrightFieldEnable(false);

                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

                ResultMicroscopeCalChipDto = ReviewDto.Clone();

                foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>().Where(t => t != CalChipSiteModelEnum.ChuckModel))
                {
                    ResultMicroscopeCalChipDto.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = calChipSiteModelEnum;
                    Logger.LogHtmlInformation($"{Cache.CalChipSiteModelEnum}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                    var findFocusPosition = Cache.Item.CenterPosition;
                    var findFocusMin = Cache.Item.FindFocusMin;
                    var findFocusMax = Cache.Item.FindFocusMax;
                    var findFocusInterval = Cache.Item.FindFocusInterval;

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

                    AfViewModel.SetSensorBrightFieldCalChipStandardEcsValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.CurrentItem.EcsValue);
                    AfViewModel.ToggleBrightFieldEnable(true);

                    Thread.Sleep(5000);

                    var microscopeFocusItemDto = new MicroscopeFocusItemDto
                    {
                        Index = 0,
                        LensInformation = Cache.MicroscopeLensInformation,
                        FindPosition = findFocusPosition,
                        Quality = 0,
                        EcsValue = ResultMicroscopeCalChipDto.CurrentItem.EcsValue,
                        FilePath = detectImageDirectory
                    };
                    GetQuality(microscopeFocusItemDto);

                    AfViewModel.ToggleBrightFieldEnable(false);

                    ResultMicroscopeCalChipDto.CurrentItem.BrightFieldQuality = microscopeFocusItemDto.Quality;
                    ResultMicroscopeCalChipDto.CurrentItem.BrightFieldFilePath = microscopeFocusItemDto.FilePath;

                    if (!_isSkipRtfc && calChipSiteModelEnum is not CalChipSiteModelEnum.UndefinedModel && calChipSiteModelEnum is not CalChipSiteModelEnum.ShinyWaferModel)
                    {
                        AfViewModel.SetDarkField(calChipSiteModelEnum,
                            calChipSiteModelEnum switch
                            {
                                CalChipSiteModelEnum.ChuckModel => GuardUtils.IsNotNullAndReturn(ReviewDto.ChuckItem).AfEcsValue,
                                CalChipSiteModelEnum.DswModel => GuardUtils.IsNotNullAndReturn(ReviewDto.DswItem).AfEcsValue,
                                CalChipSiteModelEnum.HazeModel => GuardUtils.IsNotNullAndReturn(ReviewDto.HazeItem).AfEcsValue,
                                _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(calChipSiteModelEnum))
                            },
                            calChipSiteModelEnum switch
                            {
                                CalChipSiteModelEnum.ChuckModel => GuardUtils.IsNotNullAndReturn(ReviewDto.ChuckItem).AfMotorValue,
                                CalChipSiteModelEnum.DswModel => GuardUtils.IsNotNullAndReturn(ReviewDto.DswItem).AfMotorValue,
                                CalChipSiteModelEnum.HazeModel => GuardUtils.IsNotNullAndReturn(ReviewDto.HazeItem).AfMotorValue,
                                _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(calChipSiteModelEnum))
                            });
                        using var darkFieldImageDto =
                            LaserViewModel.GetDarkFieldLineScanImage(
                                calChipSiteModelEnum,
                                findFocusPosition,
                                (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                                false,
                                GuardUtils.IsNotNullAndReturn(applicationCookie.CalibrationRecipeDto).CalibrationRecipeInfoDto.CIBConfiguration,
                                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Machine
                            );

                        ResultMicroscopeCalChipDto.CurrentItem.DarkFieldFilePath = $"{ImageFileDirectory}\\Verify_({calChipSiteModelEnum.ToDescriptionOrString()})_Guid({HtmlLogUniqueId}).jpg";
                        darkFieldImageDto.Image.Save(ResultMicroscopeCalChipDto.CurrentItem.DarkFieldFilePath);

                        ResultMicroscopeCalChipDto.CurrentItem.DarkFieldQuality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImageDto.Image);

                        Logger.LogHtmlInformation("RTFC Review", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                        {
                            ResultMicroscopeCalChipDto.CurrentItem.CalChipSiteModelEnum,
                            ResultMicroscopeCalChipDto.CurrentItem.AfEcsValue,
                            ResultMicroscopeCalChipDto.CurrentItem.AfMotorValue,
                            ResultMicroscopeCalChipDto.CurrentItem.DarkFieldQuality,
                            HtmlTab = new HtmlTab(new
                            {
                                Image = new HtmlImage(ResultMicroscopeCalChipDto.CurrentItem.DarkFieldFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }

                var calibrationQualitys = ReviewDto.Items.OrderBy(t => t.Key)
                    .Select(t => (t.Key, t.Value.BrightFieldQuality, t.Value.DarkFieldQuality))
                    .ToArray();
                var qualitys = ResultMicroscopeCalChipDto.Items.OrderBy(t => t.Key)
                    .Select(t => (t.Key, t.Value.BrightFieldQuality, t.Value.DarkFieldQuality))
                    .ToArray();

                var brightFieldQualityErrors = qualitys
                    .Select((t, i) => (t.Key, Value: t.BrightFieldQuality - calibrationQualitys[i].BrightFieldQuality))
                    .ToArray();
                var darkFieldQualityErrors = qualitys
                    .Select((t, i) => (t.Key, Value: t.DarkFieldQuality - calibrationQualitys[i].DarkFieldQuality))
                    .Where(t => t.Key is CalChipSiteModelEnum.ChuckModel or CalChipSiteModelEnum.DswModel or CalChipSiteModelEnum.HazeModel)
                    .ToArray();

                Cache.VerifyBrightFieldResultError = string.Join("; ", brightFieldQualityErrors.Select(t => $"{t.Key.ToDescriptionOrString()}: {t.Value}"));
                Cache.VerifyDarkFieldResultError = string.Join("; ", darkFieldQualityErrors.Select(t => $"{t.Key.ToDescriptionOrString()}: {t.Value}"));

                var result = brightFieldQualityErrors.All(t => t.Value < Cache.BfQualityThreshold)
                             && darkFieldQualityErrors.All(t => t.Value < Cache.DfQualityThreshold);

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}{Environment.NewLine}" +
                                                $"Bright Field Quality Error: ({Cache.VerifyBrightFieldResultError}){Environment.NewLine}" +
                                                $"Dark Field Quality Error: ({Cache.VerifyDarkFieldResultError})", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                {
                    Cache.MicroscopeLensInformation,
                    Cache.VerifyBrightFieldResultError,
                    Cache.VerifyDarkFieldResultError,
                    QualityThreshold = Cache.BfQualityThreshold,
                    Cache.BfQualityThreshold,
                    Cache.DfQualityThreshold,
                    CalibrationResult = new HtmlTable([
                        .. ReviewDto.Items.OrderBy(t => t.Key)
                            .Select(t => new { t.Key, t.Value.BrightFieldMachinePosition, t.Value.EcsValue, t.Value.BrightFieldQuality, t.Value.DarkFieldMachinePosition, t.Value.AfEcsValue, t.Value.AfMotorValue, t.Value.DarkFieldQuality })
                    ]),
                    VerifyResult = new HtmlTable([
                        .. ResultMicroscopeCalChipDto.Items.OrderBy(t => t.Key)
                            .Select(t => new { t.Key, t.Value.BrightFieldMachinePosition, t.Value.EcsValue, t.Value.BrightFieldQuality, t.Value.DarkFieldMachinePosition, t.Value.AfEcsValue, t.Value.AfMotorValue, t.Value.DarkFieldQuality })
                    ])
                }), HtmlLogUniqueId.LoggingHtml());

                ReviewDto.IsVerified = result;
                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed."), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                return result;
            }).ConfigureAwait(false);
        }
        finally
        {
            SynchronizationContextProvider.Send(() => { IsWindowEnable = true; });
        }
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

        image.Save(microscopeFocusItemDto.FilePath);
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

    private void RuntimeAfCalibration(MicroscopeCalChipDto microscopeCalChipDto, CalChipSiteModelEnum calChipSiteModelEnum)
    {
        microscopeCalChipDto.CalChipSiteModelEnum = calChipSiteModelEnum;

        var position = StageViewModel.MachineToBrightFieldPosition(
            GuardUtils.IsNotNullAndReturn(Cache.Items.Get(calChipSiteModelEnum)).CenterPosition);

        var (afEcs, afMotor) = LaserViewModel.RuntimeAfCalibration(
            GuardUtils.IsNotNullAndReturn(applicationCookie.CalibrationRecipeDto).CalibrationRecipeInfoDto.CIBConfiguration,
            position,
            calibrationSetting.SettingCommonParam.MainLaserLightInformation,
            out var rtfcFilePath,
            calChipSiteModelEnum: calChipSiteModelEnum,
            stageCoordinateSystemEnum: StageCoordinateSystemEnum.Dark,
            saveImageFileDirectory: ImageFileDirectory,
            logGuid: HtmlLogUniqueId,
            logName: calChipSiteModelEnum.ToDescriptionOrString());

        using var rtfcImage = HalconFactory.CreateImage(rtfcFilePath);

        microscopeCalChipDto.CurrentItem.AfEcsValue = afEcs;
        microscopeCalChipDto.CurrentItem.AfMotorValue = afMotor;
        microscopeCalChipDto.CurrentItem.DarkFieldFilePath = rtfcFilePath;
        microscopeCalChipDto.CurrentItem.DarkFieldQuality = CalibrationAlgorithmService.GetDarkFieldQuality(rtfcImage);
    }

    private bool Save(MicroscopeCalChipDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;
        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
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