using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using CugaCalibration.ViewModels.Common.Windows.View;
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

[IOCAppService(ServiceType = typeof(LaserPixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPixelSizeCalibrationViewModel(
    AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel,
    EnableProductiveInformationWindowViewModel enableProductiveInformationWindowViewModel,
    EnableOpticsIlluminationModeWindowViewModel enableOpticsIlluminationModeWindowViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{Cache.OpticsIlluminationModeEnum.ToDescriptionOrString()}_{Cache.ProductivityInformation.ToString()}";

    public override string CalibrateFileName => $"{Cache.OpticsIlluminationModeEnum.ToDescriptionOrString()}_{Cache.ProductivityInformation.ToString()}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Select Productivity" },
        new() { StepName = "Config" },
        new() { StepName = "P5" },
        new() { StepName = "Find a Position", DefaultIsNextEnable = true },
        new() { StepName = "Pixel Size" }
    ];

    private List<(OpticsIlluminationModeEnum OpticsIlluminationModeEnum, bool isEnbale)> _enableOpticsIlluminationModelList = [];

    private List<(ProductivityInformation productiveInformation, bool isEnbale)> _enableProductiveInformationList = [];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserPixelSizeItemDto> _resultLaserPixelSizeItemDtoList = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeAndProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserPixelSizeItemDto> _reviews = [];

    [ObservableProperty]
    private ObservableCollection<LaserPixelSizeItemDto> _selectReviews = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserPixelSizeCache _cache = new();

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _calibrations = [];

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField[] _alignmentCacheDarkFields = [];

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

        AlignmentCacheDarkFields = RecipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserPixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserPixelSizeItemDto>();

        CalibrationStatuses =
        [
            ..EnumHelper.Enums<OpticsIlluminationModeEnum>()
                .Select(t => new OpticsIlluminationModeAndProductivityInformationCalibrationStatus()
                {
                    SelectedItem = t,
                    ProductivityInformationCalibrationStatusList = [.. ProductivityInformationCalibrationStatus.CreateList(t is OpticsIlluminationModeEnum.OI? ApplicationCookie.OIOpticsMagTypeProductivityInformations: ApplicationCookie.NIOpticsMagTypeProductivityInformations)]
                })
        ];

        foreach (var calibrationStatus in Calibrations)
        {
            var opticsIlluminationModeStatus = CalibrationStatuses.Single(t => t.SelectedItem == calibrationStatus.OpticsIlluminationMode);
            var status = opticsIlluminationModeStatus
                .ProductivityInformationCalibrationStatusList
                .SingleOrDefault(t => t.SelectedItem == calibrationStatus.ProductivityInformation);
            if (status is not null) status.IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PMTInterval;
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (Cache.Item.FindPosition.ToOriginLength >= Cache.ChuckRadius)
        {
            DialogWindowProvider.ShowDialog("The Bright Field Cache Position Out Of The Wafer!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;
        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .Where(t => pmtConfig.Count <= 0 || pmtConfig[t.PmtId - 1].Enabled)
                .OrderBy(t => t.OpticsIlluminationMode)
                .ThenBy(t => t.ProductivityInformation)
                .ThenBy(t => t.PmtId)
        ];

        return Reviews.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                Cache.Item.FindPosition = Cache.Item.FindPosition.ToOriginLength >= Cache.ChuckRadius
                    ? new Point(0, 0)
                    : Cache.Item.FindPosition;
                return true;

            case 2:
                DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                Cache.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

                return true;

            case 3:
                await AutomationRecipeInformationAsync(string.Empty);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);

                return true;

            case 4:
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);

                return true;

            case 5:
                if (ResultLaserPixelSizeItemDtoList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find pixel size!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations =
                    [
                        .. Calibrations
                            .Where(t => t.OpticsIlluminationMode != Cache.OpticsIlluminationModeEnum
                                        || t.ProductivityInformation != Cache.ProductivityInformation)
                    ];
                    foreach (var (index, laserPixelSizeItemDto) in ResultLaserPixelSizeItemDtoList.Select((dto, i) => (i, dto)))
                    {
                        laserPixelSizeItemDto.IsCalibrated = true;
                        if (Save(laserPixelSizeItemDto, cancellationToken, index == ResultLaserPixelSizeItemDtoList.Count - 1)) continue;

                        laserPixelSizeItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum)
                    .ProductivityInformationCalibrationStatusList
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation).IsCalibrated = true;

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
    private async Task GetPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                Cache.Item.FindPosition = result;
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
            await Task.Run(() => StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum)).ConfigureAwait(false);
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
                IsAutoGain = Cache.Item.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = Cache.Item.CIBConfiguration.Gain,
                IsL0k = Cache.Item.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = Cache.Item.CIBConfiguration.CIBProfileMode
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand]
    private Task Step0CalibrateActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand]
    private Task Step1CalibrateActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t =>
                                          t.OpticsIlluminationModeEnum == Cache.OpticsIlluminationModeEnum &&
                                          t.ProductivityInformation == Cache.ProductivityInformation)
                                      ?? new();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum
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

            if (Cache.IsDarkFieldAlignment)
            {
                if (AlignmentCacheDarkField.IsOk)
                {
                    alignmentResultDto = StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        Cache.ProductivityInformation,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                        opticsIlluminationModeEnum: Cache.OpticsIlluminationModeEnum);
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
                Cache.IsDarkFieldAlignment,
                Cache.P5Angle
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.PmtInterval,
                Cache.Item.FindPosition,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            // 先从第8个PMT开始，然后调整偏移量 把前7和后7确认好
            List<LaserPixelSizeItemDto> pmtList =
            [
                new()
                {
                    OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum,
                    ProductivityInformation = Cache.ProductivityInformation,
                    PmtId = 8,
                    FindPosition = Cache.Item.FindPosition,
                    FilePath = detectImageDirectory,
                    OriginFilePath = detectImageDirectory
                }
            ];

            // 前7倒叙计算
            for (var i = 7; i >= 1; i--)
            {
                var pmt = new LaserPixelSizeItemDto
                {
                    OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum,
                    ProductivityInformation = Cache.ProductivityInformation,
                    PmtId = i,
                    FindPosition = Cache.Item.FindPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i)),
                    FilePath = detectImageDirectory,
                    OriginFilePath = detectImageDirectory
                };
                pmtList.Add(pmt);
            }

            // 后7正序计算
            for (var i = 9; i <= 15; i++)
            {
                var pmt = new LaserPixelSizeItemDto
                {
                    OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum,
                    ProductivityInformation = Cache.ProductivityInformation,
                    PmtId = i,
                    FindPosition = Cache.Item.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8)),
                    FilePath = detectImageDirectory,
                    OriginFilePath = detectImageDirectory
                };
                pmtList.Add(pmt);
            }

            Logger.LogHtmlInformation("Get Y Pixel Size", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;
            foreach (var laserPixelSizeItemDto in pmtList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (pmtConfig.Count == 0 || pmtConfig[laserPixelSizeItemDto.PmtId - 1].Enabled)
                {
                    if (GetPixelSize(laserPixelSizeItemDto) == false)
                    {
                        Logger.LogError("Error:Get Pixel Size Failed!");
                        return false;
                    }
                }
            }

            result = true;

            Logger.LogHtmlInformation($"Calibration {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                PmtYPixelSize = new HtmlPlot2DLinesChart(
                    [
                        ("PMT Y Pixel Size(Y:um,X:PMT ID)", ResultLaserPixelSizeItemDtoList.OrderBy(t => t.PmtId).Select(t => new Point(t.PmtId, t.YPixelSize)).ToArray())
                    ],
                    "PMT Y Pixel Size")
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
        return result;
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

            var productiveGroups = SelectReviews.GroupBy(t => t.OpticsIlluminationMode).ToList();
            if (productiveGroups.Count > 1)
            {
                DialogWindowProvider.ShowDialog("Please select same optics illumination mode items!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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

        if (IsAutoCalibrate == false)
        {
            if (Cache.IsDarkFieldAlignment == false)
                StageViewModel.Alignment(
                    AlignmentCacheBrightField.LowSite1,
                    AlignmentCacheBrightField.LowSite2,
                    AlignmentCacheBrightField.HighSite1,
                    AlignmentCacheBrightField.HighSite2,
                    AlignmentCacheBrightField.LowMag,
                    AlignmentCacheBrightField.HighMag,
                    AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
            else
            {
                StageViewModel.AlignmentDarkField(
                    AlignmentCacheDarkField.LowSite1,
                    AlignmentCacheDarkField.LowSite2,
                    AlignmentCacheDarkField.HighSite1,
                    AlignmentCacheDarkField.HighSite2,
                    Cache.ProductivityInformation,
                    AlignmentCacheDarkField.LowMag,
                    AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                    opticsIlluminationModeEnum: Cache.OpticsIlluminationModeEnum);
            }
        }

        Cache.ProductivityInformation = SelectReviews[0].ProductivityInformation;

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            Cache.Item.FindPosition,
            ImageFileDirectory = detectImageDirectory
        }), HtmlLogUniqueId.LoggingHtml());

        var verifyResultList = new List<bool>();
        foreach (var selectReviewItemDto in SelectReviews)
        {
            cancellationToken.ThrowIfCancellationRequested();
            selectReviewItemDto.IsVerified = false;
            var laserPixelSizeItemDto = selectReviewItemDto.Clone();
            laserPixelSizeItemDto.FilePath = detectImageDirectory;
            laserPixelSizeItemDto.OriginFilePath = detectImageDirectory;

            if (GetPixelSize(laserPixelSizeItemDto) == false)
            {
                Logger.LogError("Error:Get Pixel Size Failed!");
                return false;
            }

            var yPixelSize = laserPixelSizeItemDto.YPixelSize;
            var error = selectReviewItemDto.YPixelSize - yPixelSize;
            var result = Math.Abs(error) < Cache.Item.Threshold;
            Cache.Item.VerifyResultYPixelSize = yPixelSize;
            verifyResultList.Add(result);

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                selectReviewItemDto.OpticsIlluminationMode,
                selectReviewItemDto.ProductivityInformation,
                NewOffset = yPixelSize,
                OldOffset = selectReviewItemDto.YPixelSize,
                Error = error,
                Cache.Item.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            selectReviewItemDto.IsVerified = result;

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

    private bool GetPixelSize(LaserPixelSizeItemDto laserPixelSizeItemDto)
    {
        var algoRet = true;
        using var darkFieldImageDto =
            LaserViewModel.GetDarkFieldLineScanImage(
                CalChipSiteModelEnum.ChuckModel,
                laserPixelSizeItemDto.FindPosition,
                (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                false,
                Cache.Item.CIBConfiguration,
                laserPixelSizeItemDto.ProductivityInformation,
                Cache.OpticsIlluminationModeEnum,
                Cache.Item.XWidthPixel,
                laserPixelSizeItemDto.PmtId);
        try
        {
            var yPixelSize = CalibrationAlgorithmService.GetYPixelSize(darkFieldImageDto, 10);
            laserPixelSizeItemDto.YPixelSize = yPixelSize;
        }
        catch (Exception ex)
        {
            algoRet = false;
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Get Y Pixel Size Failed: {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
        }

        laserPixelSizeItemDto.FilePath = $"{laserPixelSizeItemDto.OriginFilePath}{(algoRet ? string.Empty : "\\Error")}\\PmtId({laserPixelSizeItemDto.PmtId})_YPixelSize({laserPixelSizeItemDto.YPixelSize:f3})_Guid({HtmlLogUniqueId}).jpg";
        laserPixelSizeItemDto.OriginFilePath = darkFieldImageDto.RawImageFilePath;

        darkFieldImageDto.Image.Save(laserPixelSizeItemDto.FilePath);

        Logger.LogHtmlInformation($"Get Y Pixel Size Success:PMT ID {laserPixelSizeItemDto.PmtId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            darkFieldImageDto.PmtId,
            darkFieldImageDto.ChannelId,
            darkFieldImageDto.Width,
            laserPixelSizeItemDto.ProductivityInformation,
            laserPixelSizeItemDto.FindPosition,
            laserPixelSizeItemDto.YPixelSize,
            laserPixelSizeItemDto.OriginFilePath,
            HtmlTab = new HtmlTab(new
            {
                Image = new HtmlImage(laserPixelSizeItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        SynchronizationContextProvider.Send(() => ResultLaserPixelSizeItemDtoList.Add(laserPixelSizeItemDto));

        return algoRet;
    }

    private bool Save(LaserPixelSizeItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;
        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.PmtId == itemDto.PmtId
                             && t.ProductivityInformation == itemDto.ProductivityInformation
                             && t.OpticsIlluminationMode == itemDto.OpticsIlluminationMode) == false),
            itemDto.Clone()
        ];

        if (isSave == false) return;

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserPixelSizeCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultLaserPixelSizeItemDtoList.Clear);
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "loading" },
            ..CalibrationStatuses.SelectMany(
                calibrationStatus => calibrationStatus.ProductivityInformationCalibrationStatusList,
                (calibrationStatus, productivityInformations) => new CalibrationItemStep()
                {
                    StepName = $"{calibrationStatus.SelectedItem.ToDescriptionOrString()} {productivityInformations.SelectedItem}"
                }),
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            WindowManagerService.ShowDialog(enableOpticsIlluminationModeWindowViewModel);
            _enableOpticsIlluminationModelList = [.. enableOpticsIlluminationModeWindowViewModel.OpticsIlluminationModeEnableList.Select(t => (t.OpticsIlluminationModeEnum, t.IsEnable))];

            WindowManagerService.ShowDialog(enableProductiveInformationWindowViewModel);
            _enableProductiveInformationList = [.. enableProductiveInformationWindowViewModel.ProductiveInformationEnableList.Select(t => (t.ProductivityInformation, t.IsEnable))];

            var reviewStepIndex = AutoCalibrationStepList.Count - 1;
            foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                if (stepItem.index == 0)
                {
                    if (await LoadedingAsync(cancellationToken) == false) return false;
                    CalibrationStepIndex = 4;
                    if (await NextingAsync(cancellationToken) == false) return false;
                    await InvokeCalibrateAsync(() =>
                    {
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                        {
                            Cache.OpticsIlluminationModeEnum,
                            Cache.ProductivityInformation,
                            Cache.MicroscopeLensInformation.LensName
                        }), HtmlLogUniqueId.LoggingHtml());
                        return true;
                    });
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                }
                else if (stepItem.index == reviewStepIndex)
                {
                    AutoReviewCalibrationStepIndex = reviewStepIndex;
                    if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                    var result = true;
                    await InvokeCalibrateAsync(() =>
                    {
                        foreach (var opticsIlluminationModeEnumReviews in Reviews.GroupBy(t => t.OpticsIlluminationMode))
                        {
                            if (_enableOpticsIlluminationModelList.Single(t => t.OpticsIlluminationModeEnum == opticsIlluminationModeEnumReviews.Key).isEnbale == false)
                                continue;
                            Cache.OpticsIlluminationModeEnum = opticsIlluminationModeEnumReviews.Key;
                            foreach (var reviewItem in opticsIlluminationModeEnumReviews.GroupBy(t => t.ProductivityInformation))
                            {
                                if (_enableProductiveInformationList.Single(t => t.productiveInformation == reviewItem.Key).isEnbale == false)
                                    continue;
                                Logger.LogHtmlInformation($"{Cache.OpticsIlluminationModeEnum.ToDescriptionOrString()} {reviewItem.Key}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                                Cache.ProductivityInformation = reviewItem.Key;
                                SelectReviews = [.. reviewItem];
                                if (VerifyCalibration(cancellationToken) == false)
                                {
                                    DialogWindowProvider.ShowDialog($"Auto Calibration Review {Cache.OpticsIlluminationModeEnum.ToDescriptionOrString()} {Cache.ProductivityInformation} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    result = false;
                                    return result;
                                }
                            }
                        }

                        return true;
                    });
                    if (result == false) return false;
                }
                else
                {
                    foreach (var status in CalibrationStatuses)
                    {
                        Cache.OpticsIlluminationModeEnum = status.SelectedItem;

                        foreach (var productivity in status.ProductivityInformationCalibrationStatusList)
                        {
                            if (await AutoActionStepAsync(productivity.SelectedItem, cancellationToken) == false)
                            {
                                DialogWindowProvider.ShowDialog("Auto Calibration Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                return false;
                            }

                            RefreshAutoStepProgress();
                        }
                    }

                    continue;
                }

                RefreshAutoStepProgress();
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

        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.Item.WaferMaskTypeEnum, Cache.MicroscopeLensInformation, Cache.ProductivityInformation, out var maskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo, out var maskPosition);

        Cache.Item.FindPosition = maskPosition;

        return true;
    }

    private async Task<bool> AutoActionStepAsync(ProductivityInformation productivityInformation, CancellationToken cancellationToken)
    {
        Cache.ProductivityInformation = productivityInformation;
        if (_enableProductiveInformationList.Single(t => t.productiveInformation == productivityInformation).isEnbale)
        {
            if (await Step4CalibrateActionAsync(cancellationToken) == false) return false;
            CalibrationStepIndex = 5;
            if (await NextingAsync(cancellationToken) == false) return false;
        }

        if (await AutoNextingAsync(cancellationToken) == false) return false;
        return true;
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
            await InvokeVerifyAsync(async () =>
            {
                try
                {
                    foreach (var opticsIlluminationModeReviews in Reviews.GroupBy(t => t.OpticsIlluminationMode))
                    {
                        if (_enableOpticsIlluminationModelList.Single(t => t.OpticsIlluminationModeEnum == opticsIlluminationModeReviews.Key).isEnbale == false)
                            continue;
                        Cache.OpticsIlluminationModeEnum = opticsIlluminationModeReviews.Key;
                        foreach (var reviewItem in opticsIlluminationModeReviews.GroupBy(t => t.ProductivityInformation))
                        {
                            if (_enableProductiveInformationList.Single(t => t.productiveInformation == reviewItem.Key).isEnbale == false)
                                continue;
                            Logger.LogHtmlInformation($"{Cache.OpticsIlluminationModeEnum.ToDescriptionOrString()} {reviewItem.Key}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                            Cache.ProductivityInformation = reviewItem.Key;

                            if (await AutomationRecipeInformationAsync(string.Empty) == false) return false;

                            SelectReviews = [.. reviewItem];
                            if (VerifyCalibration(cancellationToken) == false)
                            {
                                DialogWindowProvider.ShowDialog($"Auto Calibration Review {Cache.OpticsIlluminationModeEnum.ToDescriptionOrString()} {Cache.ProductivityInformation} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                result = false;
                                return result;
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

            RefreshAutoStepProgress();
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