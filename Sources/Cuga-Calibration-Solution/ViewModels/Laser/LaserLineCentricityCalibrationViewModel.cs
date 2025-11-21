using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
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
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.View;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserLineCentricityCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineCentricityCalibrationViewModel(
    IApplicationCookieService applicationCookieService,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    EnableProductiveInformationWindowViewModel enableProductiveInformationWindowViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select Productivity" },
        new() { StepName = "Find a Position" },
        new() { StepName = "Find Template" },
        new() { StepName = "Line Centricity Calibration" }
    ];

    private List<(ProductivityInformation productiveInformation, bool isEnbale)> _enableProductiveInformationList = [];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserLineCentricityItemDto> _resultLaserLineCentricityItemDtoList = [];

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserLineCentricityItemDto> _reviews = [];

    [ObservableProperty]
    private ObservableCollection<LaserLineCentricityItemDto> _selectReviews = [];

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODDelayDto>(out _, out errorMessage) == false)
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

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                .. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];

        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibrationStatuses.Single(tt => tt.ProductivityInformation == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PmtInterval;
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
                .ThenBy(t => t.PmtId)
        ];

        if (Reviews.All(t => t.IsCalibrated == false))
            return false;

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                Cache.Item.FindPosition = Cache.Item.FindPosition.ToOriginLength >= Cache.ChuckRadius
                    ? new Point(0, 0)
                    : Cache.Item.FindPosition;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);
                return true;

            case 1:
                await AutomationRecipeInformationAsync(string.Empty);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);
                return true;

            case 2:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);
                return true;

            case 3:
                return true;

            case 4:
                if (ResultLaserLineCentricityItemDtoList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => (t.ProductivityInformation == Cache.ProductivityInformation) == false)];
                    foreach (var (index, laserLineCentricityItemDto) in ResultLaserLineCentricityItemDtoList.Select((dto, i) => (i, dto)))
                    {
                        laserLineCentricityItemDto.IsCalibrated = true;
                        if (Save(laserLineCentricityItemDto, cancellationToken, index == ResultLaserLineCentricityItemDtoList.Count - 1)) continue;

                        laserLineCentricityItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatuses.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).IsCalibrated = true;
                //DialogWindowProvider.ShowDialog("Find Offset Ok!");

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
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

                Cache.Item.FindPosition = result;

                Cache.Item.BrightTemplateFilePath = $"{TemplateFileDirectory}\\{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.Item.AlgorithmTemplateTypeEnum, Cache.Item.BrightTemplateFilePath, Cache.Item.AlgorithmTemplateSizeEnum);
                if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                else Cache.Item.BrightTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.BrightTemplateFilePath);

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
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);

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
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            result = Step1CalibrateAction();
            return result;
        });
        return result;
    }

    private bool Step1CalibrateAction()
    {
        if (ReviewViewModel.TryGetMatchPosition(Cache.Item.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.Item.FindPosition, Cache.MicroscopeLensInformation, Cache.Item.BrightTemplateFilePath, ImageFileDirectory, null, Name,
                "High Magnification Matching Position", out var resultPosition, out _, out _, out var highResultImageFilePath, out _) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        Cache.Item.FindPosition = resultPosition;
        Cache.Item.BrightTemplateImageFilePath = highResultImageFilePath;
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            Cache.Item.AlgorithmTemplateTypeEnum,
            Cache.MicroscopeLensInformation.LensName,
            Cache.Item.FindPosition,
            Cache.Item.BrightTemplateFilePath,
            Cache.Item.BrightTemplateImageFilePath,
            HtmlTab = new HtmlTab(new
            {
                HighMatchImage = new HtmlImage(highResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                HighTemplateImage = new HtmlImage(Cache.Item.BrightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
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
        var machineStagePosition = StageViewModel.BrightFieldToMachinePosition(Cache.Item.FindPosition);

        if (Cache.Item.FindPosition.ToOriginLength >= Cache.ChuckRadius)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        Cache.Item.FindBrightMachinePosition = machineStagePosition;

        var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
            CalChipSiteModelEnum.ChuckModel,
            Cache.Item.FindPosition,
            (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
            false,
            Cache.CIBConfiguration,
            Cache.ProductivityInformation,
            Cache.Item.XWidthPixel,
            stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
        var detectImageDirectory = ImageFileDirectory;
        using var _ = darkFieldImageDto;

        Cache.Item.TemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
        if (Cache.Item.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
        {
            if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, Cache.Item.TemplateFilePath) == false)
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
            createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.Item.TemplateFilePath;

            var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

            if (showDialog == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
        }

        Cache.Item.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.TemplateFilePath);

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
        {
            Cache.ProductivityInformation,
            Cache.Item.FindPosition,
            HtmlTab = new HtmlTab(new
            {
                TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());
        return true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(async () =>
        {
            result = await Step3CalibrateAsync(cancellationToken);
            return result;
        });
        return result;
    }

    private Task<bool> Step3CalibrateAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            ClearCalibrationTemp();

            var detectImageDirectory = ImageFileDirectory;
            var tempImageDirectory = TemplateFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.PmtInterval,
                Cache.Item.FindPosition,
                Cache.Item.FindBrightMachinePosition,
                ImageFileDirectory = detectImageDirectory,
                TemplateFileDirectory = tempImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var centerPmt = new LaserLineCentricityItemDto
            {
                MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                ProductivityInformation = Cache.ProductivityInformation,
                PmtId = 8,
                FindBrightMachinePosition = Cache.Item.FindBrightMachinePosition,
                FindPosition = Cache.Item.FindPosition,
                FilePath = detectImageDirectory + "Forward",
                TemplateFilePath = tempImageDirectory
            };
            centerPmt.FindDarkMachinePosition = StageViewModel.DarkFieldToMachinePosition(centerPmt.FindPosition);

            // 先从第8个PMT开始，然后调整偏移量 把前7和后7确认好
            var pmtList = new List<LaserLineCentricityItemDto> { centerPmt };

            // 前7倒叙计算
            for (var i = 7; i >= 1; i--)
            {
                var pmt = new LaserLineCentricityItemDto
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                    ProductivityInformation = Cache.ProductivityInformation,
                    PmtId = i,
                    FindPosition = Cache.Item.FindPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i)),
                    FindBrightMachinePosition = Cache.Item.FindBrightMachinePosition,
                    FilePath = detectImageDirectory,
                    TemplateFilePath = tempImageDirectory
                };
                pmt.FindDarkMachinePosition = StageViewModel.DarkFieldToMachinePosition(pmt.FindPosition);
                pmtList.Add(pmt);
            }

            // 后7正序计算
            for (var i = 9; i <= 15; i++)
            {
                var pmt = new LaserLineCentricityItemDto
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                    ProductivityInformation = Cache.ProductivityInformation,
                    PmtId = i,
                    FindPosition = Cache.Item.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8)),
                    FindBrightMachinePosition = Cache.Item.FindBrightMachinePosition,
                    FilePath = detectImageDirectory,
                    TemplateFilePath = tempImageDirectory
                };
                pmt.FindDarkMachinePosition = StageViewModel.DarkFieldToMachinePosition(pmt.FindPosition);
                pmtList.Add(pmt);
            }

            var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;

            if (pmtConfig
                    .Where(t => t.Enabled)
                    .All(t => LaserPixelSizes.Any(dto => dto.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType && dto.PmtId == t.Id && dto.IsOk)) == false)
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

            var calibrationOffsets = applicationCookieService.GetLineCentricityMachineOffsetList([.. ResultLaserLineCentricityItemDtoList], centerPmt.ProductivityInformation);
            LineCentricityOffsetsFit(calibrationOffsets);
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;

        await InvokeVerifyAsync(() =>
        {
            if (SelectReviews.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var productiveGroups = SelectReviews.GroupBy(t => t.ProductivityInformation).ToList();
            if (productiveGroups.Count > 1)
            {
                DialogWindowProvider.ShowDialog("Please select same optics mag items!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var centerLineCentricityItemDto = Reviews.Single(t => t.ProductivityInformation == productiveGroups.First().Key && t.PmtId == 8);

            if (VerifyCalibration(centerLineCentricityItemDto, cancellationToken) == false) result = false;

            return result;
        }).ConfigureAwait(false);
    }

    private bool VerifyCalibration(LaserLineCentricityItemDto centerLineCentricityItemDto, CancellationToken cancellationToken)
    {
        ClearCalibrationTemp();
        var detectImageDirectory = ImageFileDirectory;
        var templateFileDirectory = TemplateFileDirectory;

        var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
        var verifyResultList = new List<bool>();
        var resultLineCentricityItemDtoList = new List<LaserLineCentricityItemDto>();

        Cache.ProductivityInformation = centerLineCentricityItemDto.ProductivityInformation;
        if (ReviewViewModel.TryGetMatchPosition(Cache.Item.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.Item.FindPosition, Cache.MicroscopeLensInformation, Cache.Item.BrightTemplateFilePath, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                out var resultPosition, out _, out _, out _, out _) == false) return false;
        var brightFieldMachinePosition = StageViewModel.BrightFieldToMachinePosition(resultPosition);
        Cache.Item.FindPosition = resultPosition;

        //暗场采图匹配后得到补偿offset后的暗场坐标
        if (LaserViewModel.TryGetMatchPositionByScanImage(
                Cache.AlgorithmTemplateTypeEnum,
                CalChipSiteModelEnum.ChuckModel,
                CalibrationConstantsHelper.MainPmtId,
                resultPosition,
                Cache.Item.TemplateFilePath,
                centerLineCentricityItemDto.FilePath,
                HtmlLogUniqueId,
                string.Empty,
                $"{CalibrationConstantsHelper.MainPmtId}",
                Cache.CIBConfiguration,
                centerLineCentricityItemDto.ProductivityInformation,
                out var position,
                out _,
                out _,
                out _,
                true,
                Cache.Item.XWidthPixel,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Dark,
                CalibrationSetting.SettingCommonParam.MainLaserLightInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        var centerLineFindDarkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(position);

        var calibrationOffsets = applicationCookieService.GetLineCentricityMachineOffsetList(Calibrations, centerLineCentricityItemDto.ProductivityInformation);

        foreach (var selectReviewItemDto in SelectReviews.OrderBy(t => t.PmtId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            selectReviewItemDto.IsVerified = false;

            var laserLineCentricityItemDto = selectReviewItemDto.Clone();
            laserLineCentricityItemDto.FindBrightMachinePosition = brightFieldMachinePosition;
            Cache.Item.TemplateFilePath = laserLineCentricityItemDto.TemplateFilePath;

            var pmtCenterRelativeOffset = calibrationOffsets.Single(t => t.Pmt == laserLineCentricityItemDto.PmtId).Offset;
            var pmtTotalUmDistance = Cache.PmtInterval * (laserLineCentricityItemDto.PmtId - CalibrationConstantsHelper.MainPmtId);

            var findDarkMachinePosition = laserLineCentricityItemDto.FindDarkMachinePosition = centerLineFindDarkFieldMachinePosition
                                                                                               + (Vector)new Point(0, pmtTotalUmDistance * yDirection)
                                                                                               + (Vector)new Point(pmtCenterRelativeOffset.X * xDirection, pmtCenterRelativeOffset.Y * yDirection);

            laserLineCentricityItemDto.FindPosition = Cache.Item.FindPosition + (Vector)new Point(0, pmtTotalUmDistance);
            laserLineCentricityItemDto.FilePath = detectImageDirectory;
            laserLineCentricityItemDto.TemplateFilePath = templateFileDirectory;

            if (GetLineCentricity(laserLineCentricityItemDto) == false) return false;

            var error = selectReviewItemDto.DarkMachineCenterPosition - (Vector)laserLineCentricityItemDto.DarkMachineCenterPosition;
            var result = error.ToOriginLength < Cache.Item.Threshold.ToOriginLength;

            verifyResultList.Add(result);

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                findDarkMachinePosition,
                ResultDarkMachinePosition = laserLineCentricityItemDto.FindDarkMachinePosition,
                newForwardDarkMachineCenterPosition = laserLineCentricityItemDto.DarkMachineCenterPosition,
                oldForwardDarkMachineCenterPosition = selectReviewItemDto.DarkMachineCenterPosition,
                errorForward = error
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

            resultLineCentricityItemDtoList.Add(laserLineCentricityItemDto);
        }

        var selectListAllResult = verifyResultList.All(t => t);

        if (IsAutoCalibrate == false)
            DialogWindowProvider.ShowDialog($"Verify {(selectListAllResult ? "OK" : "Failed")}", DialogButtonsEnum.OK,
                selectListAllResult ? DialogIconEnum.Information : DialogIconEnum.Warning);

        var verifyOffsets = applicationCookieService.GetLineCentricityMachineOffsetList([.. resultLineCentricityItemDtoList], centerLineCentricityItemDto.ProductivityInformation);
        LineCentricityOffsetsFit(verifyOffsets);

        return selectListAllResult;
    }

    private bool GetLineCentricity(LaserLineCentricityItemDto laserLineCentricityItemDto)
    {
        Logger.LogHtmlInformation($"PMT ID :{laserLineCentricityItemDto.PmtId}", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

        //暗场采图匹配后得到补偿offset后的暗场坐标
        if (LaserViewModel.TryGetMatchPositionByScanImage(
                Cache.Item.AlgorithmTemplateTypeEnum,
                CalChipSiteModelEnum.ChuckModel,
                laserLineCentricityItemDto.PmtId,
                laserLineCentricityItemDto.FindDarkMachinePosition,
                Cache.Item.TemplateFilePath,
                laserLineCentricityItemDto.FilePath,
                HtmlLogUniqueId,
                string.Empty,
                $"{laserLineCentricityItemDto.PmtId}",
                Cache.CIBConfiguration,
                laserLineCentricityItemDto.ProductivityInformation,
                out var position,
                out _,
                out _,
                out var resultImageFilePath,
                true,
                Cache.Item.XWidthPixel,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Machine,
                CalibrationSetting.SettingCommonParam.MainLaserLightInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        laserLineCentricityItemDto.FindDarkMachinePosition = position;

        var machineOffset = laserLineCentricityItemDto.FindDarkMachinePosition - laserLineCentricityItemDto.FindBrightMachinePosition; // 明暗场offset(暗-明)
        laserLineCentricityItemDto.DarkMachineCenterPosition = ChuckCenter.NewBFCenterStagePosition + machineOffset;
        laserLineCentricityItemDto.FilePath = resultImageFilePath;

        laserLineCentricityItemDto.TemplateFilePath = Cache.Item.TemplateFilePath;
        laserLineCentricityItemDto.TemplateImageFilePath = Cache.Item.TemplateImageFilePath;

        Logger.LogHtmlInformation($"Success: {laserLineCentricityItemDto.PmtId}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            laserLineCentricityItemDto.PmtId,
            laserLineCentricityItemDto.FindPosition,
            laserLineCentricityItemDto.FindBrightMachinePosition,
            ForwardFindDarkMachinePosition = laserLineCentricityItemDto.FindDarkMachinePosition,
            ForwardDarkMachineCenterPosition = laserLineCentricityItemDto.DarkMachineCenterPosition,
            laserLineCentricityItemDto.TemplateFilePath,
            ForwardFilePath = laserLineCentricityItemDto.FilePath
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
                .Where(t => (t.PmtId == itemDto.PmtId && t.ProductivityInformation == Cache.ProductivityInformation) == false),
            itemDto.Clone()
        ];
        if (isSave == false) return;

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
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

    private void LineCentricityOffsetsFit(IReadOnlyCollection<(int Pmt, Point offsets)> results)
    {
        if (results.Count < 3) return;
        var pmtXErrorCoordinatess = results.OrderBy(t => t.Pmt)
            .Select(t => new Point((t.Pmt - CalibrationConstantsHelper.MainPmtId) * CalibrationSetting.SettingCommonParam.PmtInterval, t.offsets.X)).ToArray();
        var (polynomialX, rSquaredXError, _) = PolynomialLeastSquares.PolynomialFit(
            Vector<double>.Build.DenseOfEnumerable(pmtXErrorCoordinatess.Select(t => t.X)),
            Vector<double>.Build.DenseOfEnumerable(pmtXErrorCoordinatess.Select(t => t.Y)),
            1);
        var interceptXError = polynomialX[0];
        var slopeXError = polynomialX[1];
        var pmtXErrorTitle = $"y ={slopeXError:0.######}x + {interceptXError:0.######} r^2 = {rSquaredXError:0.######} angle = {MathUtils.RadianAngleToDegreeAngle(Math.Atan(slopeXError))}";

        var pmtYErrorCoordinatess = results.OrderBy(t => t.Pmt)
            .Select(t => new Point((t.Pmt - CalibrationConstantsHelper.MainPmtId) * CalibrationSetting.SettingCommonParam.PmtInterval, t.offsets.Y)).ToArray();
        var (polynomialY, rSquaredYError, yPredictedYError) = PolynomialLeastSquares.PolynomialFit(
            Vector<double>.Build.DenseOfEnumerable(pmtYErrorCoordinatess.Select(t => t.X)),
            Vector<double>.Build.DenseOfEnumerable(pmtYErrorCoordinatess.Select(t => t.Y)),
            1);
        var interceptYError = polynomialY[0];
        var slopeYError = polynomialY[1];
        var pmtYErrorTitle = $"y ={slopeYError:0.######}x + {interceptYError:0.######} r^2 = {rSquaredYError:0.######} angle = {MathUtils.RadianAngleToDegreeAngle(Math.Atan(slopeYError))}";

        Logger.LogHtmlInformation("Calibration OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            PmtXErrors = new HtmlPlot2DLinesChart(
                [
                    ("PMT X Errors(Y:um,X:PMT ID(um))", pmtXErrorCoordinatess),
                    (pmtXErrorTitle, pmtXErrorCoordinatess.Select(t => new Point(t.X, slopeXError * t.X + interceptXError)).ToArray())
                ],
                "PMT X Errors"),
            PmtYErrors = new HtmlPlot2DLinesChart(
                [
                    ("PMT Y Errors(Y:um,X:PMT ID(um))", pmtYErrorCoordinatess),
                    (pmtYErrorTitle, pmtYErrorCoordinatess.Select(t => new Point(t.X, slopeYError * t.X + interceptYError)).ToArray())
                ],
                "PMT Y Errors")
        }), HtmlLogUniqueId.LoggingHtml());
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
            WindowManagerService.ShowDialog(enableProductiveInformationWindowViewModel);
            _enableProductiveInformationList = [.. enableProductiveInformationWindowViewModel.ProductiveInformationEnableList.Select(t => (t.ProductivityInformation, t.IsEnable))];
            foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                switch (stepItem.index)
                {
                    case 0:
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        CalibrationStepIndex = 0;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.ProductivityInformation
                            }), HtmlLogUniqueId.LoggingHtml());
                            return true;
                        });
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 1 or 2 or 3:
                        if (await AutoActionStepAsync(ApplicationCookie.ProductivityInformations[stepItem.index - 1], cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog("Auto Calibration Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 4:
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        var result = true;
                        await InvokeCalibrateAsync(() =>
                        {
                            foreach (var reviewItem in Reviews.GroupBy(t => t.ProductivityInformation))
                            {
                                if (_enableProductiveInformationList.Single(t => t.productiveInformation == reviewItem.Key).isEnbale == false)
                                    continue;
                                Logger.LogHtmlInformation($"{reviewItem.Key}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                                Cache.ProductivityInformation = reviewItem.Key;

                                var centerLineCentricityItemDto = SelectReviews.Single(t => t.PmtId == 8);

                                SelectReviews = [.. reviewItem];
                                if (VerifyCalibration(centerLineCentricityItemDto, cancellationToken) == false)
                                {
                                    DialogWindowProvider.ShowDialog($"Auto Calibration Review {Cache.ProductivityInformation} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    result = false;
                                    return result;
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
        Cache.Item.FindPosition = brightFieldMaskPosition;
        Cache.Item.BrightTemplateFilePath = brightFieldMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.Item.BrightTemplateImageFilePath = brightFieldMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        // Dark Field
        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.WaferMaskTypeEnum, null, Cache.ProductivityInformation, out var darkFieldMaskInfo) == false)
            return false;
        Cache.Item.TemplateFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateFilePath;
        Cache.Item.TemplateImageFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateImageFilePath;

        return true;
    }

    private Task<bool> AutoActionStepAsync(ProductivityInformation productivityInformation, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            if (_enableProductiveInformationList.Single(t => t.productiveInformation == productivityInformation).isEnbale)
            {
                Logger.LogHtmlInformation($"{AutoCalibrationStepIndex + 1}. {AutoCalibrationStepList[AutoCalibrationStepIndex + 1].StepName}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                if (await AutomationRecipeInformationAsync(string.Empty) == false) return false;

                Logger.LogHtmlInformation($"{CalibrationStepList[1].StepName}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                if (await Step1CalibrateActionAsync(cancellationToken) == false) return false;

                Logger.LogHtmlInformation($"{CalibrationStepList[2].StepName}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;

                Logger.LogHtmlInformation($"{CalibrationStepList[3].StepName}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                if (await Step3CalibrateAsync(cancellationToken) == false) return false;
                CalibrationStepIndex = 4;
                if (await NextingAsync(cancellationToken) == false) return false;
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
            WindowManagerService.ShowDialog(enableProductiveInformationWindowViewModel);
            _enableProductiveInformationList = [.. enableProductiveInformationWindowViewModel.ProductiveInformationEnableList.Select(t => (t.ProductivityInformation, t.IsEnable))];

            if (await LoadedingAsync(cancellationToken) == false) return false;
            if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false)
            {
                DialogWindowProvider.ShowDialog($"Please Calibration!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var result = false;
            await InvokeVerifyAsync(() =>
            {
                try
                {
                    foreach (var reviewItem in Reviews.GroupBy(t => t.ProductivityInformation))
                    {
                        if (_enableProductiveInformationList.Single(t => t.productiveInformation == reviewItem.Key).isEnbale == false)
                            continue;

                        Logger.LogHtmlInformation($"{reviewItem.Key}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                        Cache.ProductivityInformation = reviewItem.Key;

                        var centerLineCentricityItemDto = SelectReviews.Single(t => t.PmtId == 8);

                        SelectReviews = [.. reviewItem];

                        if (VerifyCalibration(centerLineCentricityItemDto, cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration Review {Cache.ProductivityInformation} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            result = false;
                            return result;
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