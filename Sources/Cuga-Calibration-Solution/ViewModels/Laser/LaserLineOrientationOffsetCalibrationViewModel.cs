using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineOrientationOffset;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserLineOrientationOffsetCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineOrientationOffsetCalibrationViewModel(
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum)}-{EnumHelper.ToDescriptionString(Cache.StageSpeedEnum)}";

    public override string CalibrateFileName => $"{EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum)}-{EnumHelper.ToDescriptionString(Cache.StageSpeedEnum)}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Select a Speed" },
        new() { StepName = "P5" },
        new() { StepName = "Find a Position" },
        new() { StepName = "Find Template" },
        new() { StepName = "Calibration" }
    ];

    private List<(OpticsMagTypeEnum mag, bool isEnbale)> _enableOpticsMagList = [];

    private List<(StageSpeedEnum stageSpeed, bool isEnbale)> _enableStageSpeedList = [];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LineOrientationOffsetItemDto> _resultLaserLineOrientationOffsetDtoList = [];

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumAndStageSpeedEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumAndStageSpeedEnumCalibrationStatus
        {
            OpticsMagTypeEnum = t,
            StageSpeedEnumCalibrationStatusList = [..EnumHelper.Enums<StageSpeedEnum>().Select(tt => new StageSpeedEnumCalibrationStatus { StageSpeedEnum = tt, IsCalibrated = false })]
        })
    ];

    [ObservableProperty]
    private ObservableCollection<StageSpeedEnumCalibrationStatus> _calibrationStatusListItem =
    [
        .. EnumHelper.Enums<StageSpeedEnum>().Select(t => new StageSpeedEnumCalibrationStatus { StageSpeedEnum = t, IsCalibrated = false })
    ];

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LineOrientationOffsetItemDto> _reviewList = [];

    [ObservableProperty]
    private ObservableCollection<LineOrientationOffsetItemDto> _selectReviewList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LineOrientationOffsetCache _cache = new();

    [ObservableProperty]
    private LineOrientationOffsetItemDto[] _calibrations = [];

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _laserPixelSizes = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out _, out errorMessage) == false)
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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<ChuckGlobalScaleErrorDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserAutoFocusDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserBeamStabilizerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODDelayDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODAlignmentDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserXYAstigmatismCalibrationItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserIlluminationProfileItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserXTCCalibrationItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPixelSizeItemDto>(out var laserPixelSizes, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserPixelSizes = laserPixelSizes;

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LineOrientationOffsetCache>();

        AlignmentCacheDarkField = RecipeCacheProvider.GetOrDefault<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        Calibrations = CacheProvider.GetOrDefaultArray<LineOrientationOffsetItemDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .StageSpeedEnumCalibrationStatusList.Single(t => t.StageSpeedEnum == calibrationStatus.StageSpeedEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PmtInterval;

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.FindPosition = Cache.FindPosition.ToOriginLength >= Cache.WaferDiameter / 2
            ? new Point(0, 0)
            : Cache.FindPosition;
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsMagTypeEnum)
                .ThenBy(t => t.StageSpeedEnum)
                .ThenBy(t => t.PmtId)
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                foreach (var temp in CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).StageSpeedEnumCalibrationStatusList)
                {
                    CalibrationStatusListItem.Single(t => t.StageSpeedEnum == temp.StageSpeedEnum).IsCalibrated = temp.IsCalibrated;
                }

                return true;
            case 2:
                DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;
                return true;
            case 3:
                await AutomationRecipeInformationAsync(string.Empty);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;
            case 4:
                return true;
            case 5:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;

            case 6:
                if (ResultLaserLineOrientationOffsetDtoList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => (t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum && t.StageSpeedEnum == Cache.StageSpeedEnum) == false)];
                    foreach (var (index, lineOrientationOffsetItemDto) in ResultLaserLineOrientationOffsetDtoList.Select((dto, i) => (i, dto)))
                    {
                        lineOrientationOffsetItemDto.IsCalibrated = true;
                        if (Save(lineOrientationOffsetItemDto, cancellationToken, index == ResultLaserLineOrientationOffsetDtoList.Count - 1)) continue;

                        lineOrientationOffsetItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum)
                    .StageSpeedEnumCalibrationStatusList.Single(t => t.StageSpeedEnum == Cache.StageSpeedEnum).IsCalibrated = true;
                //DialogWindowProvider.ShowDialog("Find Offset Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync(string parameter)
    {
        try
        {
            Logger.LogInformation("{@Name}: Get Point Image Start", Name);
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                Cache.FindPosition = result;

                Cache.BrightTemplateFilePath = $"{TemplateFileDirectory}\\{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.BrightTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                else Cache.BrightTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.BrightTemplateFilePath);

                Logger.LogInformation("{@Name}: Get Point Image OK!", Name);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Image Failed", Name);
        }
    }

    [RelayCommand]
    private async Task<bool> GotoPointAsync(object parameter)
    {
        try
        {
            Logger.LogInformation("{@Name}: Move Point Start", Name);
            return await Task.Run(() =>
            {
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);

                Logger.LogInformation("{@Name}: Move Point OK!", Name);
                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
            return false;
        }
    }

    [RelayCommand]
    private Task ConfigStepActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                IsAutoGain = Cache.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = Cache.CIBConfiguration.Gain,
                IsL0k = Cache.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = Cache.CIBConfiguration.CIBProfileMode
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsMagTypeEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.StageSpeedEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            AlignmentResultDto alignmentResultDto = new();

            if (IsDarkFieldAlignment)
            {
                if (AlignmentCacheDarkField.IsOk)
                {
                    alignmentResultDto = StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        AlignmentCacheDarkField.HighDarkFieldOpticsMagTypeEnum,
                        AlignmentCacheDarkField.HighDarkFieldStageSpeedEnum,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum);
                    return true;
                }

                var showDialog = WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                AlignmentCacheDarkField = alignmentWindowDarkFieldViewModel.Cache;
                StageViewModel.SetBrightFieldAbsoluteStageXy(AlignmentCacheDarkField.LowSite1.Location);
            }
            else
            {
                if (AlignmentCacheBrightField.IsOk)
                {
                    alignmentResultDto = StageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
                    return true;
                }

                var showDialog = WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
                StageViewModel.SetBrightFieldAbsoluteStageXy(AlignmentCacheBrightField.LowSite1.Location);
            }

            Cache.P5Angle = alignmentResultDto.Degrees;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.P5Angle,
                LensName = Cache.MicroscopeLensInformation.LensName
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            result = Step3CalibrateAction();
            return result;
        });
        return result;
    }

    private bool Step3CalibrateAction()
    {
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.FindPosition, Cache.MicroscopeLensInformation, Cache.BrightTemplateFilePath, ImageFileDirectory, null, Name,
                "High Magnification Matching Position", out var resultPosition, out _, out _, out var highResultImageFilePath, out _) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        Cache.FindPosition = resultPosition;
        Cache.BrightTemplateImageFilePath = highResultImageFilePath;

        (var isSuccess, Cache.StartPosition, Cache.EndPosition) = GetIdeaBrightFieldPosition(resultPosition);
        if (isSuccess == false) return false;

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            Cache.AlgorithmTemplateTypeEnum,
            Cache.MicroscopeLensInformation.LensName,
            Cache.WaferDiameter,
            Cache.ColumnCellWidth,
            Cache.FindPosition,
            Cache.StartPosition,
            Cache.EndPosition,
            Cache.BrightTemplateFilePath,
            Cache.BrightTemplateImageFilePath,
            HtmlTab = new HtmlTab(new
            {
                HighMatchImage = new HtmlImage(highResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                HighTemplateImage = new HtmlImage(Cache.BrightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        return true;

        (bool isSuccess, Point negativePosition, Point positivePosition) GetIdeaBrightFieldPosition(Point findPosition)
        {
            try
            {
                // 靠近边缘位置的理想位置可能拍不全，总长度截去一个die
                var actualWaferRadius = (Cache.WaferDiameter - Cache.ColumnCellWidth) / 2d;
                var interval = Cache.ColumnCellWidth;
                var baseValue = findPosition.X;

                var leftError = (int)((actualWaferRadius - baseValue) / interval) * interval;
                var rightError = (int)((actualWaferRadius + baseValue) / interval) * interval;

                var leftPoint = new Point(baseValue - leftError, findPosition.Y);
                var rightPoint = new Point(baseValue + rightError, findPosition.Y);

                return (true, leftPoint, rightPoint);
            }
            catch (Exception ex)
            {
                Logger.LogError("Get idea bright field position failed:{ex}", ex);
                return (false, default, default);
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            result = Step4CalibrateAction();
            return result;
        });
        return result;
    }

    private bool Step4CalibrateAction()
    {
        if (Cache.FindPosition.ToOriginLength >= Cache.WaferDiameter / 2)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
            CalChipSiteModelEnum.ChuckModel,
            Cache.FindPosition,
            (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
            false,
            Cache.CIBConfiguration,
            Cache.XWidthPixel,
            Cache.OpticsMagTypeEnum,
            Cache.StageSpeedEnum,
            stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
        var detectImageDirectory = ImageFileDirectory;
        using var _ = darkFieldImageDto;

        Cache.TemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
        if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
        {
            if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, Cache.TemplateFilePath) == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
        }
        else
        {
            var filePath = $"{detectImageDirectory}\\Guid({HtmlLogUniqueId}_{Guid.NewGuid()}).jpg";
            darkFieldImageDto.Image.Save(filePath);
            createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
            createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.TemplateFilePath;

            var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

            if (showDialog == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
        }

        Cache.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath);

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            Cache.OpticsMagTypeEnum,
            Cache.StageSpeedEnum,
            Cache.FindPosition,
            HtmlTab = new HtmlTab(new
            {
                TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());
        return true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(async () =>
        {
            result = await Step5CalibrateAsync(cancellationToken);
            return result;
        });
        return result;
    }

    private Task<bool> Step5CalibrateAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            ClearCalibrationTemp();

            var detectImageDirectory = ImageFileDirectory;
            var tempImageDirectory = TemplateFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.PmtId,
                Cache.PmtInterval,
                Cache.FindPosition,
                Cache.StartPosition,
                Cache.EndPosition,
                ImageFileDirectory = detectImageDirectory,
                TemplateFileDirectory = tempImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var centerPmt = new LineOrientationOffsetItemDto
            {
                MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                StageSpeedEnum = Cache.StageSpeedEnum,
                PmtId = 8,
                FindPosition = Cache.FindPosition,
                StartPosition = Cache.StartPosition,
                EndPosition = Cache.EndPosition,
                ForwardFilePath = detectImageDirectory + "Forward",
                ReverseFilePath = detectImageDirectory + "Reverse",
                TemplateFilePath = tempImageDirectory
            };

            // 先从第8个PMT开始，然后调整偏移量 把前7和后7确认好
            var pmtList = new List<LineOrientationOffsetItemDto> { centerPmt };

            // 前7倒叙计算
            for (var i = 7; i >= 1; i--)
            {
                var pmt = new LineOrientationOffsetItemDto
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    StageSpeedEnum = Cache.StageSpeedEnum,
                    PmtId = i,
                    FindPosition = Cache.FindPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i)),
                    StartPosition = Cache.StartPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i)),
                    EndPosition = Cache.EndPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i)),
                    ForwardFilePath = detectImageDirectory + "Forward",
                    ReverseFilePath = detectImageDirectory + "Reverse",
                    TemplateFilePath = tempImageDirectory
                };
                pmtList.Add(pmt);
            }

            // 后7正序计算
            for (var i = 9; i <= 15; i++)
            {
                var pmt = new LineOrientationOffsetItemDto
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    StageSpeedEnum = Cache.StageSpeedEnum,
                    PmtId = i,
                    FindPosition = Cache.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8)),
                    StartPosition = Cache.StartPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8)),
                    EndPosition = Cache.EndPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8)),
                    ForwardFilePath = detectImageDirectory + "Forward",
                    ReverseFilePath = detectImageDirectory + "Reverse",
                    TemplateFilePath = tempImageDirectory
                };
                pmtList.Add(pmt);
            }

            var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;

            if (pmtConfig
                    .Where(t => t.Enabled)
                    .All(t => LaserPixelSizes.Any(dto => dto.ProductivityInformation == Cache.ProductivityInformation && dto.PmtId == t.Id && dto.IsOk)) == false)
            {
                DialogWindowProvider.ShowDialog("Missing pixel size for PMT configuration! Please check the pixel size calibration!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            foreach (var lineOrientationOffsetItemDto in pmtList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (pmtConfig.Count == 0 || pmtConfig.Single(t => t.Id == lineOrientationOffsetItemDto.PmtId).Enabled)
                {
                    if (GetLineOrientationOffset(lineOrientationOffsetItemDto) == false) return false;
                }
            }

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;

        await InvokeVerifyAsync(() =>
        {
            if (SelectReviewList.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (VerifyCalibration(cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
    }

    private bool VerifyCalibration(CancellationToken cancellationToken)
    {
        ClearCalibrationTemp();
        var detectImageDirectory = ImageFileDirectory;
        var templateFileDirectory = TemplateFileDirectory;

        var verifyResultList = new List<bool>();
        if (IsAutoCalibrate)
        {
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.FindPosition, Cache.MicroscopeLensInformation, Cache.BrightTemplateFilePath, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                    out var resultPosition, out _, out _, out _, out _) == false) return false;
            Cache.FindPosition = resultPosition;
        }

        foreach (var selectReviewItemDto in SelectReviewList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            selectReviewItemDto.IsVerified = false;
            var lineOrientationOffsetItemDto = selectReviewItemDto.Clone();

            if (IsAutoCalibrate)
            {
                lineOrientationOffsetItemDto.FindPosition = Cache.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (lineOrientationOffsetItemDto.PmtId - 8));
                Cache.TemplateFilePath = lineOrientationOffsetItemDto.TemplateFilePath;
            }

            lineOrientationOffsetItemDto.ForwardFilePath = detectImageDirectory + "Forward";
            lineOrientationOffsetItemDto.ReverseFilePath = detectImageDirectory + "Reverse";
            lineOrientationOffsetItemDto.TemplateFilePath = templateFileDirectory;

            if (GetLineOrientationOffset(lineOrientationOffsetItemDto, true) == false) return false;

            var result = lineOrientationOffsetItemDto.Offset.ToOriginLength < Cache.Threshold.ToOriginLength;
            verifyResultList.Add(result);

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                offset = lineOrientationOffsetItemDto.Offset
            }), HtmlLogUniqueId.LoggingHtml());

            selectReviewItemDto.IsVerified = result;
            if (Save(selectReviewItemDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                selectReviewItemDto.IsVerified = false;
                return false;
            }

            if (EnableDependedCalibrationItems(cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Enable Depended Calibration Items Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        }

        var selectListAllResult = verifyResultList.All(t => t);

        if (IsAutoCalibrate == false)
            DialogWindowProvider.ShowDialog($"Verify {(selectListAllResult ? "OK" : "Failed")}", DialogButtonsEnum.OK,
                selectListAllResult ? DialogIconEnum.Information : DialogIconEnum.Warning);

        return selectListAllResult;
    }

    private bool GetLineOrientationOffset(LineOrientationOffsetItemDto lineOrientationOffsetItemDto, bool isVerify = false)
    {
        Logger.LogHtmlInformation($"PMT ID :{lineOrientationOffsetItemDto.PmtId}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

        var pegTriggerPoint = Enumerable.Range(0, Convert.ToInt32(Math.Abs((Cache.EndPosition - Cache.StartPosition).X) / Cache.ColumnCellWidth))
            .Select(t => new Point(Cache.StartPosition.X + t * Cache.ColumnCellWidth, lineOrientationOffsetItemDto.StartPosition.Y))
            .ToList();

        if (GetMatchResult(lineOrientationOffsetItemDto, true) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Try Template Match To Offset Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        if (isVerify)
        {
            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
            lineOrientationOffsetItemDto.StartPosition += (Vector)new Point(lineOrientationOffsetItemDto.Offset.X * xDirection, lineOrientationOffsetItemDto.Offset.Y * yDirection);
            lineOrientationOffsetItemDto.EndPosition += (Vector)new Point(lineOrientationOffsetItemDto.Offset.X * xDirection, lineOrientationOffsetItemDto.Offset.Y * yDirection);
        }

        if (GetMatchResult(lineOrientationOffsetItemDto, false) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Try Template Match To Offset Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        Logger.LogHtmlInformation($"Success: {lineOrientationOffsetItemDto.PmtId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            lineOrientationOffsetItemDto.PmtId,
            lineOrientationOffsetItemDto.FindPosition,
            lineOrientationOffsetItemDto.StartPosition,
            lineOrientationOffsetItemDto.EndPosition,
            lineOrientationOffsetItemDto.ForwardFindDarkMachinePosition,
            lineOrientationOffsetItemDto.ReverseFindDarkMachinePosition,
            lineOrientationOffsetItemDto.Offset,
            lineOrientationOffsetItemDto.TemplateFilePath,
            lineOrientationOffsetItemDto.ForwardFilePath,
            lineOrientationOffsetItemDto.ReverseFilePath
        }), HtmlLogUniqueId.LoggingHtml());

        SynchronizationContextProvider.Send(() => ResultLaserLineOrientationOffsetDtoList.Add(lineOrientationOffsetItemDto));
        return true;

        bool GetMatchResult(LineOrientationOffsetItemDto lineOrientationOffsetDto, bool isForward)
        {
            var points = pegTriggerPoint.Select(t => t).ToArray();
            if (isForward == false) points = [.. points.Reverse()];
            var machinePoints = points.Select(t => StageViewModel.DarkFieldToMachinePosition(t)).ToList();

            Logger.LogHtmlInformation($"{(isForward ? "Forward" : "Reverse")} Param", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                lineOrientationOffsetItemDto.StartPosition,
                lineOrientationOffsetItemDto.EndPosition,
                lineOrientationOffsetItemDto.Offset,
                Point = new HtmlTable([.. points.Select((t, i) => new { i, t })]),
                MachinePoint = new HtmlTable([.. points.Select((t, i) => new { i, t })])
            }), HtmlLogUniqueId.LoggingHtml());

            var rowDarkFieldImageDtoList = LaserViewModel.GetChuckDarkFieldRowLineScanImage(
                machinePoints,
                (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                false,
                Cache.CIBConfiguration,
                Cache.XWidthPixel,
                Cache.OpticsMagTypeEnum,
                Cache.StageSpeedEnum,
                CalibrationConstantsHelper.MainPmtId,
                CalibrationConstantsHelper.MainChannelId,
                StageCoordinateSystemEnum.Machine);

            var findPosition = points.Select((t, i) => (Index: i, Point: t))
                .Minima(t => Math.Abs((t.Point.X - lineOrientationOffsetDto.FindPosition.X)))
                .First();
            var darkFieldImageDto = isForward ? rowDarkFieldImageDtoList[findPosition.Index] : rowDarkFieldImageDtoList[^findPosition.Index];

            if (LaserViewModel.TryGetMatchPosition(
                    Cache.AlgorithmTemplateTypeEnum,
                    darkFieldImageDto,
                    lineOrientationOffsetDto.PmtId,
                    findPosition.Point,
                    Cache.TemplateFilePath,
                    isForward ? lineOrientationOffsetDto.ForwardFilePath : lineOrientationOffsetDto.ReverseFilePath,
                    HtmlLogUniqueId,
                    string.Empty,
                    $"{lineOrientationOffsetDto.PmtId} {(isForward ? "Forward" : "Reverse")}",
                    out var position,
                    out _,
                    out _,
                    out var resultImageFilePath,
                    xWidthPixel: Cache.XWidthPixel,
                    yOpticsMagTypeEnum: lineOrientationOffsetDto.OpticsMagTypeEnum,
                    xStageSpeedEnum: Cache.StageSpeedEnum,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Dark) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var resultMachinePosition = StageViewModel.DarkFieldToMachinePosition(position);
            if (isForward)
            {
                lineOrientationOffsetDto.ForwardFilePath = resultImageFilePath;
                lineOrientationOffsetDto.ForwardFindDarkMachinePosition = resultMachinePosition;
            }
            else
            {
                lineOrientationOffsetDto.ReverseFilePath = resultImageFilePath;
                lineOrientationOffsetDto.ReverseFindDarkMachinePosition = resultMachinePosition;
            }

            return true;
        }
    }

    private bool Save(LineOrientationOffsetItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.PmtId == itemDto.PmtId && t.OpticsMagTypeEnum == itemDto.OpticsMagTypeEnum && t.StageSpeedEnum == itemDto.StageSpeedEnum) == false),
            itemDto.Clone()
        ];
        if (isSave == false) return;

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultLaserLineOrientationOffsetDtoList.Clear);
    }

    #endregion 校准
}