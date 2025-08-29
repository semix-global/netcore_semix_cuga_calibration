using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.View;
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

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserLineCentricityCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineCentricityCalibrationViewModel(
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    EnableOpticsMagWindowViewModel enableOpticsMagWindowViewModel,
    EnableStageSpeedWindowViewModel enableStageSpeedWindowViewModel,
    CalibrationSetting calibrationSetting) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum)}-{EnumHelper.ToDescriptionString(Cache.StageSpeedEnum)}";

    public override string CalibrateFileName => $"{EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum)}-{EnumHelper.ToDescriptionString(Cache.StageSpeedEnum)}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Select a Speed" },
        new() { StepName = "Find a Position" },
        new() { StepName = "Find Template" },
        new() { StepName = "Line Centricity Calibration" }
    ];

    private List<(OpticsMagTypeEnum mag, bool isEnbale)> _enableOpticsMagList = [];

    private List<(StageSpeedEnum stageSpeed, bool isEnbale)> _enableStageSpeedList = [];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserLineCentricityItemDto> _resultLaserLineCentricityItemDtoList = [];

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

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserLineCentricityItemDto> _reviewList = [];

    [ObservableProperty]
    private ObservableCollection<LaserLineCentricityItemDto> _selectReviewList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserLineCentricityCache _cache = new();

    [ObservableProperty]
    private LaserLineCentricityItemDto[] _calibrations = [];

    [ObservableProperty]
    private ChuckCenterObjDto _chuckCenter = new();

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _laserPixelSizes = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterObjDto>(out var chuckCenter, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        ChuckCenter = chuckCenter;

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserAodDelayItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPrescanChirpAodAlignmentDto>(out _, out errorMessage) == false)
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

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserLineCentricityCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserLineCentricityItemDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .StageSpeedEnumCalibrationStatusList.Single(t => t.StageSpeedEnum == calibrationStatus.StageSpeedEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation.LensCode == -1)
            Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList.Count <= 2
                ? ApplicationCookie.MicroscopeLensInformationList[^1]
                : ApplicationCookie.MicroscopeLensInformationList[2];

        Cache.PmtInterval = calibrationSetting.SettingCommonParam.PmtInterval;
        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.FindPosition = Cache.FindPosition.ToOriginLength >= Cache.ChuckRadius
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
                await AutomationRecipeInformationAsync(string.Empty);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;

            case 3:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;

            case 4:
                return true;

            case 5:
                if (ResultLaserLineCentricityItemDtoList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => (t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum && t.StageSpeedEnum == Cache.StageSpeedEnum) == false)];
                    foreach (var (index, laserLineCentricityItemDto) in ResultLaserLineCentricityItemDtoList.Select((dto, i) => (i, dto)))
                    {
                        laserLineCentricityItemDto.IsCalibrated = true;
                        if (Save(laserLineCentricityItemDto, cancellationToken, index == ResultLaserLineCentricityItemDtoList.Count - 1)) continue;

                        laserLineCentricityItemDto.IsCalibrated = false;
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
    private async Task MagnificationSelectedAsync(object obj)
    {
        try
        {
            if (obj is not MicroscopeLensInformation)
            {
                Logger.LogError("{@Name}: Select magnification illegal!", Name);
                return;
            }

            await Task.Run(() => MicroscopeViewModel.SwitchMicroscopeLensInformation(ApplicationCookie.MicroscopeLensInformationList.Single(t => t == (MicroscopeLensInformation)obj))
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
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
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            result = Step2CalibrateAction();
            return result;
        });
        return result;
    }

    private bool Step2CalibrateAction()
    {
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.FindPosition, Cache.MicroscopeLensInformation, Cache.BrightTemplateFilePath, ImageFileDirectory, null, Name,
                "High Magnification Matching Position", out var resultPosition, out _, out _, out var highResultImageFilePath, out _) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        Cache.FindPosition = resultPosition;
        Cache.BrightTemplateImageFilePath = highResultImageFilePath;
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            Cache.AlgorithmTemplateTypeEnum,
            Cache.MicroscopeLensInformation.LensName,
            Cache.FindPosition,
            Cache.BrightTemplateFilePath,
            Cache.BrightTemplateImageFilePath,
            HtmlTab = new HtmlTab(new
            {
                HighMatchImage = new HtmlImage(highResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                HighTemplateImage = new HtmlImage(Cache.BrightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
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
        var machineStagePosition = StageViewModel.BrightFieldToMachinePosition(Cache.FindPosition);

        if (Cache.FindPosition.ToOriginLength >= Cache.ChuckRadius)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        Cache.FindBrightMachinePosition = machineStagePosition;

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
            HalconHelper.Save(darkFieldImageDto.Image, filePath);
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
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(async () =>
        {
            result = await Step4CalibrateAsync(cancellationToken);
            return result;
        });
        return result;
    }

    private Task<bool> Step4CalibrateAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            ClearCalibrationTemp();

            var detectImageDirectory = ImageFileDirectory;
            var tempImageDirectory = TemplateFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
            {
                Cache.OpticsMagTypeEnum,
                Cache.PmtInterval,
                Cache.FindPosition,
                Cache.FindBrightMachinePosition,
                ImageFileDirectory = detectImageDirectory,
                TemplateFileDirectory = tempImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            // 先从第8个PMT开始，然后调整偏移量 把前7和后7确认好
            List<LaserLineCentricityItemDto> pmtList =
            [
                new()
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    StageSpeedEnum = Cache.StageSpeedEnum,
                    PmtId = 8,
                    FindBrightMachinePosition = Cache.FindBrightMachinePosition,
                    FindPosition = Cache.FindPosition,
                    ForwardFilePath = detectImageDirectory + "Forward",
                    ReverseFilePath = detectImageDirectory + "Reverse",
                    TemplateFilePath = tempImageDirectory
                }
            ];

            // 前7倒叙计算
            for (var i = 7; i >= 1; i--)
            {
                var pmt = new LaserLineCentricityItemDto
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    StageSpeedEnum = Cache.StageSpeedEnum,
                    PmtId = i,
                    FindPosition = Cache.FindPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i)),
                    FindBrightMachinePosition = Cache.FindBrightMachinePosition,
                    ForwardFilePath = detectImageDirectory + "Forward",
                    ReverseFilePath = detectImageDirectory + "Reverse",
                    TemplateFilePath = tempImageDirectory
                };
                pmtList.Add(pmt);
            }

            // 后7正序计算
            for (var i = 9; i <= 15; i++)
            {
                var pmt = new LaserLineCentricityItemDto
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    StageSpeedEnum = Cache.StageSpeedEnum,
                    PmtId = i,
                    FindPosition = Cache.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8)),
                    FindBrightMachinePosition = Cache.FindBrightMachinePosition,
                    ForwardFilePath = detectImageDirectory + "Forward",
                    ReverseFilePath = detectImageDirectory + "Reverse",
                    TemplateFilePath = tempImageDirectory
                };
                pmtList.Add(pmt);
            }

            var pmtConfig = calibrationSetting.SettingPmtConfigParam.PmtConfigList;

            if (pmtConfig
                    .Where(t => t.Enabled)
                    .All(t => LaserPixelSizes.Any(dto => dto.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum && dto.PmtId == t.Id && dto.IsOk)) == false)
            {
                DialogWindowProvider.ShowDialog("Missing pixel size for PMT configuration! Please check the pixel size calibration!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            foreach (var laserLineCentricityItemDto in pmtList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (pmtConfig.Count == 0 || pmtConfig[laserLineCentricityItemDto.PmtId - 1].Enabled)
                {
                    if (GetLineCentricity(laserLineCentricityItemDto) == false) return false;
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
        var machineStagePosition = Point.Origin;
        if (IsAutoCalibrate)
        {
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.FindPosition, Cache.MicroscopeLensInformation, Cache.BrightTemplateFilePath, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                    out var resultPosition, out _, out _, out _, out _) == false) return false;
            machineStagePosition = StageViewModel.BrightFieldToMachinePosition(resultPosition);
            Cache.FindPosition = resultPosition;
        }

        foreach (var selectReviewItemDto in SelectReviewList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            selectReviewItemDto.IsVerified = false;
            var laserLineCentricityItemDto = selectReviewItemDto.Clone();

            if (IsAutoCalibrate)
            {
                laserLineCentricityItemDto.FindPosition = Cache.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (laserLineCentricityItemDto.PmtId - 8));
                laserLineCentricityItemDto.FindBrightMachinePosition = machineStagePosition;
                Cache.TemplateFilePath = laserLineCentricityItemDto.TemplateFilePath;
            }

            laserLineCentricityItemDto.ForwardFilePath = detectImageDirectory + "Forward";
            laserLineCentricityItemDto.ReverseFilePath = detectImageDirectory + "Reverse";
            laserLineCentricityItemDto.TemplateFilePath = templateFileDirectory;

            if (GetLineCentricity(laserLineCentricityItemDto) == false) return false;

            var newForwardDarkMachineCenterPosition = laserLineCentricityItemDto.ForwardDarkMachineCenterPosition;
            var oldForwardDarkMachineCenterPosition = selectReviewItemDto.ForwardDarkMachineCenterPosition;
            var errorForward = newForwardDarkMachineCenterPosition - (Vector)oldForwardDarkMachineCenterPosition;
            var resultForward = errorForward.ToOriginLength < Cache.Threshold.ToOriginLength;
            var oldReverseDarkMachineCenterPosition = selectReviewItemDto.ReverseDarkMachineCenterPosition;
            var newReverseDarkMachineCenterPosition = laserLineCentricityItemDto.ReverseDarkMachineCenterPosition;
            var errorReverse = newReverseDarkMachineCenterPosition - (Vector)oldReverseDarkMachineCenterPosition;
            var resultReverse = errorReverse.ToOriginLength < Cache.Threshold.ToOriginLength;

            verifyResultList.Add(resultForward);
            verifyResultList.Add(resultReverse);

            Logger.LogHtmlInformation(resultForward && resultReverse ? "OK" : "Failed", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                newForwardDarkMachineCenterPosition,
                oldForwardDarkMachineCenterPosition,
                errorForward,
                newReverseDarkMachineCenterPosition,
                oldReverseDarkMachineCenterPosition,
                errorReverse
            }), HtmlLogUniqueId.LoggingHtml());

            selectReviewItemDto.IsVerified = resultForward && resultReverse;
            if (Save(selectReviewItemDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                selectReviewItemDto.IsVerified = false;
                return false;
            }
        }

        var selectListAllResult = verifyResultList.All(t => t);

        if (IsAutoCalibrate == false)
            DialogWindowProvider.ShowDialog($"Verify {(selectListAllResult ? "OK" : "Failed")}", DialogButtonsEnum.OK,
                selectListAllResult ? DialogIconEnum.Information : DialogIconEnum.Warning);

        return selectListAllResult;
    }

    private bool GetLineCentricity(LaserLineCentricityItemDto laserLineCentricityItemDto)
    {
        Logger.LogHtmlInformation($"PMT ID :{laserLineCentricityItemDto.PmtId}", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

        //暗场采图匹配后得到补偿offset后的暗场坐标
        if (LaserViewModel.TryGetMatchPosition(
                Cache.AlgorithmTemplateTypeEnum,
                CalChipSiteModelEnum.ChuckModel,
                laserLineCentricityItemDto.PmtId,
                laserLineCentricityItemDto.FindPosition,
                Cache.TemplateFilePath,
                laserLineCentricityItemDto.ForwardFilePath,
                HtmlLogUniqueId,
                string.Empty,
                $"{laserLineCentricityItemDto.PmtId} Forward",
                Cache.CIBConfiguration,
                out var position,
                out _,
                out _,
                out var resultImageFilePath,
                true,
                Cache.XWidthPixel,
                laserLineCentricityItemDto.OpticsMagTypeEnum,
                Cache.StageSpeedEnum) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        var result = StageViewModel.DarkFieldToMachinePosition(position);
        laserLineCentricityItemDto.ForwardFindDarkMachinePosition = result;

        var machineOffset = laserLineCentricityItemDto.ForwardFindDarkMachinePosition - laserLineCentricityItemDto.FindBrightMachinePosition; // 明暗场offset(暗-明)
        laserLineCentricityItemDto.ForwardDarkMachineCenterPosition = ChuckCenter.NewBFCenterStagePosition + machineOffset;
        laserLineCentricityItemDto.ForwardFilePath = resultImageFilePath;

        if (LaserViewModel.TryGetMatchPosition(
                Cache.AlgorithmTemplateTypeEnum,
                CalChipSiteModelEnum.ChuckModel,
                laserLineCentricityItemDto.PmtId,
                laserLineCentricityItemDto.FindPosition,
                Cache.TemplateFilePath,
                laserLineCentricityItemDto.ForwardFilePath,
                HtmlLogUniqueId,
                string.Empty,
                $"{laserLineCentricityItemDto.PmtId} Reverse",
                Cache.CIBConfiguration,
                out position,
                out _,
                out _,
                out resultImageFilePath,
                false,
                Cache.XWidthPixel,
                laserLineCentricityItemDto.OpticsMagTypeEnum,
                Cache.StageSpeedEnum) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        result = StageViewModel.DarkFieldToMachinePosition(position);

        laserLineCentricityItemDto.ReverseFindDarkMachinePosition = result;

        machineOffset = laserLineCentricityItemDto.ReverseFindDarkMachinePosition - laserLineCentricityItemDto.FindBrightMachinePosition; // 明暗场offset(暗-明)
        laserLineCentricityItemDto.ReverseDarkMachineCenterPosition = ChuckCenter.NewBFCenterStagePosition + machineOffset;
        laserLineCentricityItemDto.ReverseFilePath = resultImageFilePath;

        if (laserLineCentricityItemDto.PmtId == CalibrationConstantsHelper.MainPmtId)
        {
            StageViewModel.SetDarkFieldCenterMachinePositionValue(laserLineCentricityItemDto.ForwardDarkMachineCenterPosition);
        }

        laserLineCentricityItemDto.TemplateFilePath = Cache.TemplateFilePath;
        laserLineCentricityItemDto.TemplateImageFilePath = Cache.TemplateImageFilePath;

        Logger.LogHtmlInformation($"Success: {laserLineCentricityItemDto.PmtId}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            laserLineCentricityItemDto.PmtId,
            laserLineCentricityItemDto.FindPosition,
            laserLineCentricityItemDto.FindBrightMachinePosition,
            laserLineCentricityItemDto.ForwardFindDarkMachinePosition,
            laserLineCentricityItemDto.ForwardDarkMachineCenterPosition,
            laserLineCentricityItemDto.ReverseFindDarkMachinePosition,
            laserLineCentricityItemDto.ReverseDarkMachineCenterPosition,
            laserLineCentricityItemDto.TemplateFilePath,
            laserLineCentricityItemDto.ForwardFilePath,
            laserLineCentricityItemDto.ReverseFilePath
        }), HtmlLogUniqueId.LoggingHtml());
        SynchronizationContextProvider.Send(() => ResultLaserLineCentricityItemDtoList.Add(laserLineCentricityItemDto));
        return true;
    }

    private bool Save(LaserLineCentricityItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.PmtId == itemDto.PmtId && t.OpticsMagTypeEnum == itemDto.OpticsMagTypeEnum && t.StageSpeedEnum == itemDto.StageSpeedEnum) == false),
            itemDto.Clone()
        ];
        if (isSave == false) return true;

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserLineCentricityCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultLaserLineCentricityItemDtoList.Clear);
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "loading" },
            new() { StepName = "Low Mag" },
            new() { StepName = "Middle Mag" },
            new() { StepName = "High Mag" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            WindowManagerService.ShowDialog(enableOpticsMagWindowViewModel);
            WindowManagerService.ShowDialog(enableStageSpeedWindowViewModel);
            _enableOpticsMagList = [.. enableOpticsMagWindowViewModel.OpticsMagEnableList.Select(t => (t.OpticsMagTypeEnum, t.IsEnable))];
            _enableStageSpeedList = [.. enableStageSpeedWindowViewModel.StageSpeedEnableList.Select(t => (t.StageSpeedEnum, t.IsEnable))];
            foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                switch (stepItem.index)
                {
                    case 0:
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        CalibrationStepIndex = 1;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.MicroscopeLensInformation.LensName
                            }), HtmlLogUniqueId.LoggingHtml());
                            return true;
                        });
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 1:
                        if (await AutoActionStepAsync((int)OpticsMagTypeEnum.Low, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Low Magnification Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 2:
                        if (await AutoActionStepAsync((int)OpticsMagTypeEnum.Middle, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Middle Magnification Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 3:
                        if (await AutoActionStepAsync((int)OpticsMagTypeEnum.High, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration High Magnification Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 4:
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        var result = true;
                        await InvokeCalibrateAsync(() =>
                        {
                            foreach (var opticsMagGroup in ReviewList.GroupBy(t => t.OpticsMagTypeEnum))
                            {
                                if (_enableOpticsMagList.Single(t => t.mag == opticsMagGroup.Key).isEnbale == false)
                                    continue;
                                Cache.OpticsMagTypeEnum = opticsMagGroup.Key;
                                Logger.LogHtmlInformation($"{opticsMagGroup.Key} Mag", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                                var opticsMagGroupValue = opticsMagGroup.Select(t => t).ToList();
                                foreach (var speedGroup in opticsMagGroupValue.GroupBy(t => t.StageSpeedEnum))
                                {
                                    if (_enableStageSpeedList.Single(t => t.stageSpeed == speedGroup.Key).isEnbale == false)
                                        continue;
                                    Cache.StageSpeedEnum = speedGroup.Key;
                                    Logger.LogHtmlInformation($"{speedGroup.Key} Speed", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                                    SelectReviewList = [.. speedGroup];
                                    if (VerifyCalibration(cancellationToken) == false)
                                    {
                                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {opticsMagGroup.Key} Mag-{speedGroup.Key} Speed Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                        result = false;
                                        return result;
                                    }
                                }
                            }

                            return true;
                        });
                        if (result == false) return false;
                        break;
                }

                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Calibration Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string stepName)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeService.GetCorrectWaferMapByOffset(!IsAutoCalibrate) == false)
            return false;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });
        // Bright Field
        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.WaferMaskTypeEnum, Cache.MicroscopeLensInformation, null, null, out var brightFieldMaskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, brightFieldMaskInfo, out var brightFieldMaskPosition);
        Cache.FindPosition = brightFieldMaskPosition;
        Cache.BrightTemplateFilePath = brightFieldMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.BrightTemplateImageFilePath = brightFieldMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        // Dark Field
        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.WaferMaskTypeEnum, null, Cache.OpticsMagTypeEnum, Cache.StageSpeedEnum, out var darkFieldMaskInfo) == false)
            return false;
        Cache.TemplateFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateFilePath;
        Cache.TemplateImageFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateImageFilePath;

        return true;
    }

    private Task<bool> AutoActionStepAsync(int magnification, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            Cache.OpticsMagTypeEnum = (OpticsMagTypeEnum)magnification;
            if (_enableOpticsMagList.Single(t => t.mag == Cache.OpticsMagTypeEnum).isEnbale)
            {
                Logger.LogHtmlInformation($"{AutoCalibrationStepIndex + 1}. {AutoCalibrationStepList[magnification + 1].StepName}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                foreach (var speedEnum in EnumHelper.Enums<StageSpeedEnum>())
                {
                    Cache.StageSpeedEnum = speedEnum;
                    if (_enableStageSpeedList.Single(t => t.stageSpeed == speedEnum).isEnbale == false)
                        continue;

                    Logger.LogHtmlInformation($"{speedEnum} Speed", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    if (await AutomationRecipeInformationAsync(string.Empty) == false) return false;

                    Logger.LogHtmlInformation($"{CalibrationStepList[2].StepName}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                    if (Step2CalibrateAction() == false) return false;

                    Logger.LogHtmlInformation($"{CalibrationStepList[3].StepName}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                    if (Step3CalibrateAction() == false) return false;

                    Logger.LogHtmlInformation($"{CalibrationStepList[4].StepName}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                    if (await Step4CalibrateAsync(cancellationToken) == false) return false;
                    CalibrationStepIndex = 5;
                    if (await NextingAsync(cancellationToken) == false) return false;
                }
            }

            if (await AutoNextingAsync(cancellationToken) == false) return false;
            return true;
        });
    }

    private async Task<bool> AutoNextingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex + 1].StepName;
            AutoCalibrationStepIndex++;
        }, cancellationToken);
        return true;
    }

    public override async Task<bool> AutomationReviewActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationReviewActionAsync(cancellationToken);
            WindowManagerService.ShowDialog(enableOpticsMagWindowViewModel);
            WindowManagerService.ShowDialog(enableStageSpeedWindowViewModel);
            _enableOpticsMagList = [.. enableOpticsMagWindowViewModel.OpticsMagEnableList.Select(t => (t.OpticsMagTypeEnum, t.IsEnable))];
            _enableStageSpeedList = [.. enableStageSpeedWindowViewModel.StageSpeedEnableList.Select(t => (t.StageSpeedEnum, t.IsEnable))];

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
                    foreach (var opticsMagGroup in ReviewList.GroupBy(t => t.OpticsMagTypeEnum))
                    {
                        if (_enableOpticsMagList.Single(t => t.mag == opticsMagGroup.Key).isEnbale == false)
                            continue;
                        Cache.OpticsMagTypeEnum = opticsMagGroup.Key;
                        Logger.LogHtmlInformation($"{opticsMagGroup.Key} Mag", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                        var opticsMagGroupValue = opticsMagGroup.Select(t => t).ToList();
                        foreach (var speedGroup in opticsMagGroupValue.GroupBy(t => t.StageSpeedEnum))
                        {
                            if (_enableStageSpeedList.Single(t => t.stageSpeed == speedGroup.Key).isEnbale == false)
                                continue;
                            if (await AutomationRecipeInformationAsync(string.Empty) == false) return false;
                            Cache.StageSpeedEnum = speedGroup.Key;
                            Logger.LogHtmlInformation($"{speedGroup.Key} Speed", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                            SelectReviewList = [.. speedGroup];
                            if (VerifyCalibration(cancellationToken) == false)
                            {
                                DialogWindowProvider.ShowDialog($"Auto Calibration Review {opticsMagGroup.Key} Mag-{speedGroup.Key} Speed Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                return false;
                            }
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
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Review Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    #endregion 自动化校准
}