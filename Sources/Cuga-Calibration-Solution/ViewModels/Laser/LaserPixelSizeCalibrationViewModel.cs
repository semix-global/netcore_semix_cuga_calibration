using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using CugaCalibration.ViewModels.Common.Windows.View;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserPixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPixelSizeCalibrationViewModel(EnableProductiveInformationWindowViewModel enableProductiveInformationWindowViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select Productivity" },
        new() { StepName = "Find a Position", DefaultIsNextEnable = true },
        new() { StepName = "Pixel Size" }
    ];

    private List<(ProductivityInformation productiveInformation, bool isEnbale)> _enableProductiveInformationList = [];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserPixelSizeItemDto> _resultLaserPixelSizeItemDtoList = [];

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

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

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserPixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserPixelSizeItemDto>();

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];

        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
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
                .OrderBy(t => t.ProductivityInformation)
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);

                return await AutomationRecipeInformationAsync(string.Empty);

            case 2:
                if (Cache.Item.FindPosition.ToOriginLength >= Cache.ChuckRadius)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header2, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);
                return true;

            case 3:
                if (ResultLaserPixelSizeItemDtoList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find pixel size!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => t.ProductivityInformation != Cache.ProductivityInformation)];
                    foreach (var (index, laserPixelSizeItemDto) in ResultLaserPixelSizeItemDtoList.Select((dto, i) => (i, dto)))
                    {
                        laserPixelSizeItemDto.IsCalibrated = true;
                        if (Save(laserPixelSizeItemDto, cancellationToken, index == ResultLaserPixelSizeItemDtoList.Count - 1)) continue;

                        laserPixelSizeItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatuses.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).IsCalibrated = true;

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
                IsAutoGain = Cache.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = Cache.CIBConfiguration.Gain,
                IsL0k = Cache.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = Cache.CIBConfiguration.CIBProfileMode
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
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum
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
                Cache.Item.FindPosition
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

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
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
                        ("PMT Y Pixel Size(Y:um,X:PMT ID)", ResultLaserPixelSizeItemDtoList.OrderBy(t => t.PmtId).Select(t => new Point(t.PmtId, t.YPixelSize)).ToArray()),
                    ],
                    "PMT Y Pixel Size"),
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

            if (VerifyCalibration(cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
    }

    private bool VerifyCalibration(CancellationToken cancellationToken)
    {
        ClearCalibrationTemp();
        var detectImageDirectory = ImageFileDirectory;
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
                Cache.CIBConfiguration,
                laserPixelSizeItemDto.ProductivityInformation,
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
        laserPixelSizeItemDto.OriginFilePath = CalibrationConstantsHelper.ImagePathToRawImagePath(laserPixelSizeItemDto.FilePath);

        FileHelper.Save(darkFieldImageDto.Bytes, laserPixelSizeItemDto.OriginFilePath);
        darkFieldImageDto.Image.Save(laserPixelSizeItemDto.FilePath);

        Logger.LogHtmlInformation($"Get Y Pixel Size Success:PMT ID {laserPixelSizeItemDto.PmtId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            darkFieldImageDto.PmtId,
            darkFieldImageDto.ChannelId,
            darkFieldImageDto.Width,
            laserPixelSizeItemDto.ProductivityInformation,
            laserPixelSizeItemDto.FindPosition,
            laserPixelSizeItemDto.YPixelSize,
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
                .Where(t => (t.PmtId == itemDto.PmtId && t.ProductivityInformation == itemDto.ProductivityInformation) == false),
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
                        CalibrationStepIndex = 1;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.ProductivityInformation,
                                Cache.MicroscopeLensInformation.LensName
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
                                SelectReviews = [.. reviewItem];
                                if (VerifyCalibration(cancellationToken) == false)
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
            if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
            CalibrationStepIndex = 3;
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
                    foreach (var reviewItem in Reviews.GroupBy(t => t.ProductivityInformation))
                    {
                        if (_enableProductiveInformationList.Single(t => t.productiveInformation == reviewItem.Key).isEnbale == false)
                            continue;
                        Logger.LogHtmlInformation($"{reviewItem.Key}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                        SelectReviews = [.. reviewItem];
                        Cache.ProductivityInformation = reviewItem.Key;
                        if (await AutomationRecipeInformationAsync(string.Empty) == false) return false;

                        if (VerifyCalibration(cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration Review {reviewItem.Key} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Review Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    #endregion 自动化校准
}