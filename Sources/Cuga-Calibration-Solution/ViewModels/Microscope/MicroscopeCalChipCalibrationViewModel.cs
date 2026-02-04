using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
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
using Net.Utilities.WPF.Helper;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeCalChipCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCalChipCalibrationViewModel(
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "DSW Left Top Position" },
        new() { StepName = "DSW Right Bottom Position" },
        new() { StepName = "DSW" },
        new() { StepName = "DSW Alignment Low MarkSite1" },
        new() { StepName = "DSW Alignment Low MarkSite2" },
        new() { StepName = "DSW Alignment High MarkSite1" },
        new() { StepName = "DSW Alignment High MarkSite2" },
        new() { StepName = "DSW Alignment" },
        new() { StepName = "Undefined Left Top Position" },
        new() { StepName = "Undefined Right Bottom Position" },
        new() { StepName = "Undefined" },
        new() { StepName = "Haze Left Top Position" },
        new() { StepName = "Haze Right Bottom Position" },
        new() { StepName = "Haze" },
        new() { StepName = "Shiny Wafer Left Top Position" },
        new() { StepName = "Shiny Wafer Right Bottom Position" },
        new() { StepName = "Shiny Wafer" },
        new() { StepName = "Chuck RTFC" }
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

    [RecipeCache]
    [ObservableProperty]
    private MicroscopeCalChipCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private MicroscopeCalChipDto _calibration = new();

    [ObservableProperty]
    private MicroscopeFocusItemDto[] _microscopeFocusItems = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

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

        MicroscopePixelSizeItems = CalibrationStatusService.GetCalibrations<MicroscopePixelSizeItemDto>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopeCalChipCache>();
        Calibration = CacheProvider.GetOrDefault<MicroscopeCalChipDto>();
        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        AfViewModel.ToggleBrightFieldEnable(false);
        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.LowMicroscopeLensInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);

        StageViewModel.SetAbsoluteStageTheta(0);
        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);

        DialogWindowProvider.TryShowDialog("Do you want to skip dark field rtfc step?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
        Cache.IsSkipRtfc = dialogResult == DialogResultEnum.Yes;
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        ResultMicroscopeCalChipDto = Calibration.Clone();
        if (ReviewDto.IsCalibrated == false) return false;

        AfViewModel.ToggleBrightFieldEnable(false);
        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.LowMicroscopeLensInformation) == false)
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
        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.LowMicroscopeLensInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        StageViewModel.SetAbsoluteStageTheta(0);
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                return true;

            case 4:
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.LowSite1.Location, Cache.CalChipSiteModelEnum);
                return true;

            case 5:
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Cache.LowSite2.Location);

                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.LowSite2.Location, Cache.CalChipSiteModelEnum);
                return true;

            case 6:
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.HighSite1.Location, Cache.CalChipSiteModelEnum);
                return true;

            case 7:
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Cache.HighSite2.Location);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.HighSite2.Location, Cache.CalChipSiteModelEnum);

                return true;

            case 8:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
                return true;

            case 9:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                return true;

            case 10:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                return true;

            case 11:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                return true;

            case 12:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                return true;

            case 13:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                return true;

            case 14:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                return true;

            case 15:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                return true;

            case 16:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                return true;

            case 17:
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.LowSite1.Location, Cache.CalChipSiteModelEnum);
                break;

            case 3:
                Cache.LowSite2.Location = Cache.LowSite1.Location;
                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);

                Cache.HighSite1.Location = Cache.LowSite1.Location;
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.HighSite1.Location, Cache.CalChipSiteModelEnum);
                return true;

            case 5:
                Cache.HighSite2.Location = Cache.LowSite2.Location;
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.HighSite2.Location, Cache.CalChipSiteModelEnum);
                return true;

            case 7:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                break;

            case 8:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                break;

            case 9:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                break;

            case 10:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                break;

            case 11:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                break;

            case 12:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                break;

            case 13:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopPosition);
                break;

            case 14:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomPosition);
                break;

            case 15:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterPosition);
                break;

            case 16:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
                StageViewModel.SetMachineAbsoluteStageXy(Cache.ChuckPosition);
                break;

            default:
                break;
        }

        switch (CalibrationStepIndex)
        {
            case 7 or 10 or 13 or 16 or 17:
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
                darkFieldPosition
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

            if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.LowMicroscopeLensInformation) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var ecsValue = AfViewModel.GetSensorEcsValue();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.LowMicroscopeLensInformation.LensName,
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
                    LensInformation = Cache.HighMicroscopeLensInformation,
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

            if (!Cache.IsSkipRtfc && Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel or CalChipSiteModelEnum.HazeModel)
            {
                RuntimeAfCalibration(ResultMicroscopeCalChipDto, Cache.CalChipSiteModelEnum);
            }

            #endregion DF

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.IsSkipRtfc,
                Cache.CalChipSiteModelEnum,
                Cache.HighMicroscopeLensInformation.LensName,
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
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            Cache.LowSite1.Location = position;
            var lowTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            Cache.LowSiteTemplateFilePath = lowTemplateFilePath;

            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var lowTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(lowTemplateFilePath);

            var resultLowSite1 = StageViewModel.MarkAlignSite1(Cache.LowSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
            if (resultLowSite1.Template is null) return false;

            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultLowSite1.Template.Thumb), lowTemplateImageFilePath);
            Cache.LowSite1 = resultLowSite1;
            Cache.LowSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.LowSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowMicroscopeLensInformation.LensName,
                Cache.LowSite1.Location,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplate = new HtmlImage(lowTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            if (ReviewViewModel.TryGetMatchPosition(
                    Cache.AlgorithmTemplateTypeEnum,
                    MicroscopePixelSizeItems,
                    position,
                    Cache.LowMicroscopeLensInformation,
                    Cache.LowSiteTemplateFilePath,
                    null, null, null, null,
                    out var lowPositionResult,
                    out _, out _, out _, out _,
                    Cache.CalChipSiteModelEnum) == false) return false;

            Cache.LowSite2.Location = lowPositionResult;
            var resultLowSite2 = StageViewModel.MarkAlignSite2(Cache.LowSite1, Cache.AlgorithmWaferTypeEnum);
            Cache.LowSite2 = resultLowSite2;
            Cache.LowSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.LowSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowMicroscopeLensInformation.LensName,
                Cache.LowSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            if (Cache.HighMicroscopeLensInformation.LensCode <= Cache.LowMicroscopeLensInformation.LensCode)
            {
                DialogWindowProvider.ShowDialog("The high magnification less than or equal low magnification! Please select correct magnification!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var position = StageViewModel.GetBrightFieldStagePosition();
            Cache.HighSite1.Location = position;

            var highTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            Cache.HighSiteTemplateFilePath = highTemplateFilePath;

            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var highTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(highTemplateFilePath);

            var resultHighSite1 = StageViewModel.MarkAlignSite1(Cache.HighSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
            if (resultHighSite1.Template is null) return false;

            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultHighSite1.Template.Thumb), highTemplateImageFilePath);
            Cache.HighSite1 = resultHighSite1;
            Cache.HighSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.HighSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeLensInformation.LensName,
                Cache.HighSite1.Location,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplate = new HtmlImage(highTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();

            if (ReviewViewModel.TryGetMatchPosition(
                    Cache.AlgorithmTemplateTypeEnum,
                    MicroscopePixelSizeItems,
                    position,
                    Cache.HighMicroscopeLensInformation,
                    Cache.HighSiteTemplateFilePath,
                    null, null, null, null,
                    out var highPositionResult,
                    out _, out _, out _, out _,
                    Cache.CalChipSiteModelEnum) == false) return false;

            Cache.HighSite2.Location = highPositionResult;
            var resultHighSite2 = StageViewModel.MarkAlignSite2(Cache.HighSite1, Cache.AlgorithmWaferTypeEnum);
            Cache.HighSite2 = resultHighSite2;
            Cache.HighSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.HighSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeLensInformation.LensName,
                Cache.HighSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step6CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.Alignment(
                Cache.LowSite1,
                Cache.LowSite2,
                Cache.HighSite1,
                Cache.HighSite2,
                Cache.LowMicroscopeLensInformation,
                Cache.HighMicroscopeLensInformation,
                Cache.AlgorithmWaferTypeEnum,
                Cache.CalChipSiteModelEnum);

            var degree = StageViewModel.GetMachineStageTheta();

            var originBrightFieldPosition = StageViewModel.MachineToBrightFieldPosition(ResultMicroscopeCalChipDto.DswItem.BrightFieldMachinePosition);
            var originDarkFieldPosition = StageViewModel.MachineToDarkFieldPosition(ResultMicroscopeCalChipDto.DswItem.DarkFieldMachinePosition);
            ResultMicroscopeCalChipDto.DSWBrightFieldMachineAffinePosition = StageViewModel.BrightFieldToMachinePosition(originBrightFieldPosition.DegreeAngleByXy(degree));
            ResultMicroscopeCalChipDto.DSWDarkFieldMachineAffinePosition = StageViewModel.DarkFieldToMachinePosition(originDarkFieldPosition.DegreeAngleByXy(degree));
            ResultMicroscopeCalChipDto.DSWAlignmentDegree = degree;

            AfViewModel.SetSensorBrightFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.DSWBrightFieldMachineAffinePosition);
            AfViewModel.SetSensorDarkFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.DSWDarkFieldMachineAffinePosition);

            Logger.LogHtmlInformation("P5 OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AlgorithmWaferTypeEnum,
                ResultMicroscopeCalChipDto.DSWAlignmentDegree,
                ResultMicroscopeCalChipDto.DswItem.BrightFieldMachinePosition,
                ResultMicroscopeCalChipDto.DswItem.DarkFieldMachinePosition,
                ResultMicroscopeCalChipDto.DSWBrightFieldMachineAffinePosition,
                ResultMicroscopeCalChipDto.DSWDarkFieldMachineAffinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step7CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.ChuckPosition = StageViewModel.GetBrightFieldStagePosition();
            if (Cache.IsSkipRtfc) return true;

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
                ResultMicroscopeCalChipDto = ReviewDto.Clone();

                Cache.VerifyBrightFieldResultError = string.Empty;
                Cache.VerifyDarkFieldResultError = string.Empty;

                AfViewModel.ToggleBrightFieldEnable(false);

                var dswAlignmentDegree = ResultMicroscopeCalChipDto.DSWAlignmentDegree;
                StageViewModel.SetAbsoluteStageTheta(dswAlignmentDegree);

                var alignmentResultDto = StageViewModel.AlignmentVerify(
                    Cache.LowSite1.DegreeAngleByXy(dswAlignmentDegree),
                    Cache.LowSite2.DegreeAngleByXy(dswAlignmentDegree),
                    Cache.HighSite1.DegreeAngleByXy(dswAlignmentDegree),
                    Cache.HighSite2.DegreeAngleByXy(dswAlignmentDegree),
                    Cache.LowMicroscopeLensInformation,
                    Cache.HighMicroscopeLensInformation,
                    Cache.AlgorithmWaferTypeEnum,
                    CalChipSiteModelEnum.DswModel);

                if (Math.Abs(alignmentResultDto.Degrees) > Cache.DSWAlignmentVerifyThreshold)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error:DSW alignment verify is failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>().Where(t => t != CalChipSiteModelEnum.ChuckModel))
                {
                    StageViewModel.SetAbsoluteStageTheta(0);

                    ResultMicroscopeCalChipDto.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = calChipSiteModelEnum;
                    Logger.LogHtmlInformation($"{Cache.CalChipSiteModelEnum}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                    var findFocusPosition = ResultMicroscopeCalChipDto.CurrentItem.BrightFieldMachinePosition;
                    if (calChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
                    {
                        StageViewModel.SetAbsoluteStageTheta(dswAlignmentDegree);
                        findFocusPosition = ResultMicroscopeCalChipDto.DSWBrightFieldMachineAffinePosition;
                    }

                    Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                    {
                        Cache.CalChipSiteModelEnum,
                        Cache.HighMicroscopeLensInformation,
                        FindFocusPosition = findFocusPosition,
                        ImageFileDirectory = detectImageDirectory
                    }), HtmlLogUniqueId.LoggingHtml());

                    if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.LowMicroscopeLensInformation) == false)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Switch Magnification Failed."), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }

                    AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
                    StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(findFocusPosition);

                    AfViewModel.SetSensorBrightFieldCalChipStandardEcsValue(Cache.CalChipSiteModelEnum, ResultMicroscopeCalChipDto.CurrentItem.EcsValue);
                    AfViewModel.ToggleBrightFieldEnable(true);

                    Thread.Sleep(5000);
                    var ecs = AfViewModel.GetSensorEcsValue();

                    var microscopeFocusItemDto = new MicroscopeFocusItemDto
                    {
                        Index = 0,
                        LensInformation = Cache.LowMicroscopeLensInformation,
                        FindPosition = findFocusPosition,
                        EcsValue = ecs,
                        Quality = 0,
                        FilePath = detectImageDirectory
                    };
                    GetQuality(microscopeFocusItemDto, true);

                    AfViewModel.ToggleBrightFieldEnable(false);

                    ResultMicroscopeCalChipDto.CurrentItem.EcsValue = ecs;
                    ResultMicroscopeCalChipDto.CurrentItem.BrightFieldQuality = microscopeFocusItemDto.Quality;
                    ResultMicroscopeCalChipDto.CurrentItem.BrightFieldFilePath = microscopeFocusItemDto.FilePath;

                    if (!Cache.IsSkipRtfc && calChipSiteModelEnum is not CalChipSiteModelEnum.UndefinedModel && calChipSiteModelEnum is not CalChipSiteModelEnum.ShinyWaferModel)
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

                var result = brightFieldQualityErrors.All(t => Math.Abs(t.Value) < Cache.BfQualityThreshold)
                             && darkFieldQualityErrors.All(t => Math.Abs(t.Value) < Cache.DfQualityThreshold);

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}{Environment.NewLine}" +
                                                $"Bright Field Quality Error: ({Cache.VerifyBrightFieldResultError}){Environment.NewLine}" +
                                                $"Dark Field Quality Error: ({Cache.VerifyDarkFieldResultError})", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                {
                    Cache.DSWAlignmentVerifyThreshold,
                    Cache.BfQualityThreshold,
                    Cache.DfQualityThreshold,
                    VerifyDSWAlignmentDegree = dswAlignmentDegree,
                    Cache.VerifyBrightFieldResultError,
                    Cache.VerifyDarkFieldResultError,

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

    private void GetQuality(MicroscopeFocusItemDto microscopeFocusItemDto, bool isReview = false)
    {
        if (isReview == false) AfViewModel.SetSensorEcsValue(microscopeFocusItemDto.EcsValue);

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

        dto.MicroscopeLensInformation = Cache.HighMicroscopeLensInformation;
        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
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