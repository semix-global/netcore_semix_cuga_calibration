using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.Helper;
using System.IO;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeCalChipViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCalChipViewModel : CalibrationViewModelBase
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
        new() { StepName = "Shiny Wafer" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private MicroscopeCalChipDTO _calibratingItem = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private MicroscopeCalChipDTO _review = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO? _selectReviewItem;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private MicroscopeCalChipCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private MicroscopeCalChipDTO _calibration = new();

    [ObservableProperty]
    private MicroscopeFocusItemDto[] _microscopeFocusItems = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizes = [];

    #endregion 缓存

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GotoBrightFieldPositionCommand))]
    private bool _isWindowEnable = true;

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopePixelSizes = CalibrationStatusService.GetCalibrations<MicroscopePixelSizeItemDto>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopeCalChipCache>();
        Calibration = CacheProvider.GetOrDefault<MicroscopeCalChipDTO>();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        AfViewModel.ToggleBrightFieldEnable(false);

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);

        StageViewModel.SetAbsoluteStageTheta(0);
        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration.Clone();
        SelectReviewItem = null;

        if (Review.IsCalibrated == false) return false;

        AfViewModel.ToggleBrightFieldEnable(false);
        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.LowMicroscopeLensInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Review.DswItem.BrightFieldMachinePosition, CalChipSiteModelEnum.DswModel);

        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetAbsoluteStageTheta(0);

        return true;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 2:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterMachinePosition, Cache.CalChipSiteModelEnum);
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
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 10:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 11:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 12:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 13:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 14:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 15:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 16:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
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
            case 0:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 1:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.LowSite1.Location, Cache.CalChipSiteModelEnum);
                return true;

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

            case 6:
                return true;

            case 7:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 8:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 9:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 10:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 11:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 12:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 13:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.LeftTopMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 14:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.RightBottomMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 15:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.CenterMachinePosition, Cache.CalChipSiteModelEnum);
                return true;

            case 16:
                IsCalibrated = true;
                return true;

            default:
                return false;
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

            await Task.Run(() =>
            {
                Review.CalChipSiteModelEnum = calChipSiteModelEnum.Value;
                StageViewModel.SetMachineAbsoluteStageXy(Review.CurrentItem.BrightFieldMachinePosition);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(StageDirectionTypeEnum name, CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var resultMachine = StageViewModel.GetMachineStagePosition();
            switch (name)
            {
                case StageDirectionTypeEnum.UpLeft:
                    Cache.Item.LeftTopMachinePosition = resultMachine;
                    break;

                case StageDirectionTypeEnum.DownRight:
                    Cache.Item.RightBottomMachinePosition = resultMachine;
                    break;
            }

            CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;

            CalibratingItem.CurrentItem.BrightFieldMachinePosition = Cache.Item.CenterMachinePosition;

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
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Validate Failed:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var detectImageDirectory = ImageFileDirectory;

            AfViewModel.ToggleBrightFieldEnable(false);
            AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
            if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.LowMicroscopeLensInformation) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(CalibratingItem.CurrentItem.BrightFieldMachinePosition, Cache.CalChipSiteModelEnum);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.LowMicroscopeLensInformation,
                FindPosition = StageViewModel.GetMachineStagePosition(),
                Cache.Item.StartECS,
                Cache.Item.StepECS,
                Cache.Item.StopECS,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.Items = [];

            var ecses = Generate.LinearRange(Cache.Item.StartECS, Cache.Item.StepECS, Cache.Item.StopECS);
            Guard.IsNotEmpty(ecses);

            var currentDetectImageDirectory = Path.Combine(detectImageDirectory, $"[{ecses[0]:0.###}ECS, {ecses[^1]:0.###}ECS]_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat)}");

            AfViewModel.SetSensorEcsValue(Cache.Item.StartECS);
            Thread.Sleep(3000);

            var listDownResult = new List<bool>();
            double? downQualityValue = null;
            foreach (var ecs in ecses)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AfViewModel.SetSensorEcsValue(ecs);
                var dtoItem = new MicroscopeCalChipDTOItem
                {
                    EcsValue = ecs,
                    FilePath = currentDetectImageDirectory
                };
                GetQuality(dtoItem);

                CalibratingItem.Items = [.. CalibratingItem.Items, dtoItem.Clone()];

                if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < dtoItem.Quality);
                downQualityValue = dtoItem.Quality;
                if (listDownResult.HasConsecutiveEqual(30, false)) break; // 连续30个下降说明已经到了最低点
            }

            var bestFocusItem = CalibratingItem.Items.Maxima(t => t.Quality).First();
            CalibratingItem.CurrentItem.EcsValue = bestFocusItem.EcsValue;
            CalibratingItem.CurrentItem.Quality = bestFocusItem.Quality;
            CalibratingItem.CurrentItem.FilePath = bestFocusItem.FilePath;

            AfViewModel.SetSensorBrightFieldCalChipStandardEcsValue(Cache.CalChipSiteModelEnum, CalibratingItem.CurrentItem.EcsValue);
            AfViewModel.SetSensorBrightFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, CalibratingItem.CurrentItem.BrightFieldMachinePosition);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.CurrentItem.EcsValue,
                CalibratingItem.CurrentItem.Quality,
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(CalibratingItem.CurrentItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.IsCalibrated = Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.ShinyWaferModel;

            Guard.IsTrue(Save(CalibratingItem, cancellationToken));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            Cache.LowSite1.Location = position;
            var lowTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.LowMicroscopeLensInformation}_{Guid.NewGuid()}";
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
                Cache.LowMicroscopeLensInformation,
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
    private async Task<bool> Step3Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            if (ReviewViewModel.TryGetMatchPosition(
                    Cache.AlgorithmTemplateTypeEnum,
                    MicroscopePixelSizes,
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
                Cache.LowMicroscopeLensInformation,
                Cache.LowSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.HighMicroscopeLensInformation.LensCode, Cache.LowMicroscopeLensInformation.LensCode, "The high magnification less than or equal low magnification! Please select correct magnification!");

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
                Cache.HighMicroscopeLensInformation,
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
    private async Task<bool> Step5Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();

            if (ReviewViewModel.TryGetMatchPosition(
                    Cache.AlgorithmTemplateTypeEnum,
                    MicroscopePixelSizes,
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
                Cache.HighMicroscopeLensInformation,
                Cache.HighSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step6Async(CancellationToken cancellationToken)
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

            var originBrightFieldPosition = StageViewModel.MachineToBrightFieldPosition(CalibratingItem.DswItem.BrightFieldMachinePosition);
            CalibratingItem.DSWBrightFieldMachineAffinePosition = StageViewModel.BrightFieldToMachinePosition(originBrightFieldPosition.DegreeAngleByXy(degree));
            CalibratingItem.DSWAlignmentDegree = degree;

            AfViewModel.SetSensorBrightFieldCalChipCenterMachinePositionValue(Cache.CalChipSiteModelEnum, CalibratingItem.DSWBrightFieldMachineAffinePosition);

            Logger.LogHtmlInformation("Alignment OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AlgorithmWaferTypeEnum,
                CalibratingItem.DSWAlignmentDegree,
                CalibratingItem.DswItem.BrightFieldMachinePosition,
                CalibratingItem.DSWBrightFieldMachineAffinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() => { IsWindowEnable = false; });
        try
        {
            await InvokeVerifyAsync(() =>
            {
                var detectImageDirectory = ImageFileDirectory;
                SelectReviewItem = Review.Clone();

                Cache.VerifyQualityError = string.Empty;

                AfViewModel.ToggleBrightFieldEnable(false);

                var dswAlignmentDegree = SelectReviewItem.DSWAlignmentDegree;
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

                    SelectReviewItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = calChipSiteModelEnum;
                    Logger.LogHtmlInformation($"{Cache.CalChipSiteModelEnum}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                    var findFocusPosition = SelectReviewItem.CurrentItem.BrightFieldMachinePosition;
                    if (calChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
                    {
                        StageViewModel.SetAbsoluteStageTheta(dswAlignmentDegree);
                        findFocusPosition = SelectReviewItem.DSWBrightFieldMachineAffinePosition;
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

                    StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(findFocusPosition, Cache.CalChipSiteModelEnum);

                    AfViewModel.SetSensorBrightFieldCalChipStandardEcsValue(Cache.CalChipSiteModelEnum, SelectReviewItem.CurrentItem.EcsValue);
                    AfViewModel.ToggleBrightFieldEnable(true);

                    Thread.Sleep(5000);
                    var ecs = AfViewModel.GetSensorEcsValue();

                    SelectReviewItem.CurrentItem.FilePath = detectImageDirectory;
                    GetQuality(SelectReviewItem.CurrentItem);

                    AfViewModel.ToggleBrightFieldEnable(false);
                }

                var calibrationQualitys = Review.Results.OrderBy(t => t.Key)
                    .Select(t => (t.Key, BrightFieldQuality: t.Value.Quality))
                    .ToArray();
                var qualitys = SelectReviewItem.Results.OrderBy(t => t.Key)
                    .Select(t => (t.Key, BrightFieldQuality: t.Value.Quality))
                    .ToArray();

                var brightFieldQualityErrors = qualitys
                    .Select((t, i) => (t.Key, Value: t.BrightFieldQuality - calibrationQualitys[i].BrightFieldQuality))
                    .ToArray();

                Cache.VerifyQualityError = string.Join("; ", brightFieldQualityErrors.Select(t => $"{t.Key.ToDescriptionOrString()}: {t.Value}"));

                var result = brightFieldQualityErrors.All(t => Math.Abs(t.Value) < Cache.QualityThreshold);

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}{Environment.NewLine}" +
                                                $"Quality Error: ({Cache.VerifyQualityError}){Environment.NewLine}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                {
                    Cache.DSWAlignmentVerifyThreshold,
                    Cache.QualityThreshold,
                    VerifyDSWAlignmentDegree = dswAlignmentDegree,
                    VerifyBrightFieldResultError = Cache.VerifyQualityError,
                    CalibrationResult = new HtmlTable([
                        .. Review.Results.OrderBy(t => t.Key)
                            .Select(t => new { t.Key, t.Value.BrightFieldMachinePosition, t.Value.EcsValue, BrightFieldQuality = t.Value.Quality })
                    ]),
                    VerifyResult = new HtmlTable([
                        .. SelectReviewItem.Results.OrderBy(t => t.Key)
                            .Select(t => new { t.Key, t.Value.BrightFieldMachinePosition, t.Value.EcsValue, BrightFieldQuality = t.Value.Quality })
                    ])
                }), HtmlLogUniqueId.LoggingHtml());

                Review.IsVerified = result;
                Guard.IsTrue(Save(Review, cancellationToken));

                return result;
            }).ConfigureAwait(false);
        }
        finally
        {
            SynchronizationContextProvider.Send(() => { IsWindowEnable = true; });
        }
    }

    private void GetQuality(MicroscopeCalChipDTOItem microscopeCalChipDTOItem)
    {
        using var image = ReviewViewModel.GetBrightFieldImage();

        var quality = ReviewViewModel.GetQuality(image);
        microscopeCalChipDTOItem.Quality = quality;
        microscopeCalChipDTOItem.FilePath =
            $"{microscopeCalChipDTOItem.FilePath}\\Ecs({microscopeCalChipDTOItem.EcsValue:F3})_Quality({microscopeCalChipDTOItem.Quality:F3})_Guid({HtmlLogUniqueId}).jpg";

        image.Save(microscopeCalChipDTOItem.FilePath);

        var htmlBulletList = new HtmlBullet(new
        {
            microscopeCalChipDTOItem.EcsValue,
            ImageQuality = microscopeCalChipDTOItem.Quality,
            HtmlTab = new HtmlTab(new
            {
                Image = new HtmlImage(microscopeCalChipDTOItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        });

        Logger.LogHtmlInformation($"ECS:{microscopeCalChipDTOItem.EcsValue}", HtmlHeaderLevelEnum.Header3, htmlBulletList, HtmlLogUniqueId.LoggingHtml());
    }

    private bool Save(MicroscopeCalChipDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.MicroscopeLensInformation = Cache.LowMicroscopeLensInformation;
        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}