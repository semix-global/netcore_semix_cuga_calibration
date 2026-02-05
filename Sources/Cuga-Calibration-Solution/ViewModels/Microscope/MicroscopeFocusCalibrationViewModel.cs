using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities.SourceGenerators.Attributes;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeFocusCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeFocusCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a lens" },
        new() { StepName = "Select a location" },
        new() { StepName = "Find Focus" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<MicroscopeFocusItemDto> _microscopeFocusItemDtoList = [];

    [ObservableProperty]
    private Point[] _ecsPoints = [];

    [ObservableProperty]
    private MicroscopeFocusItemDto? _selectedMicroscopeFocusItemDto;

    [ObservableProperty]
    private MicroscopeFocusItemDto? _resultMicroscopeFocusItemDto;

    [ObservableProperty]
    private ObservableCollection<MicroscopeLensInfoCalibrationStatus> _calibrationStatusList = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<MicroscopeFocusItemDto> _reviewList = [];

    [ObservableProperty]
    private MicroscopeFocusItemDto? _selectReviewItemDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private MicroscopeFocusCache _cache = new();

    [ObservableProperty]
    private MicroscopeFocusCacheItem _selectMicroscopeFocusCacheItem = new();

    [DefaultCache]
    [ObservableProperty]
    private MicroscopeFocusItemDto[] _calibrations = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        (_, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopeFocusCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<MicroscopeFocusItemDto>();

        Calibrations = [.. Calibrations.Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.LensInformation))]; // 过滤掉变更静态配置后原来的缓存

        SynchronizationContextProvider.Send(() =>
            CalibrationStatusList =
            [
                .. ApplicationCookie.MicroscopeLensInformations
                    .Select(t => new MicroscopeLensInfoCalibrationStatus { MicroscopeLensInformation = t, IsCalibrated = false })
            ]
        );
        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.MicroscopeLensInformation == calibrationStatus.LensInformation)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        AfViewModel.ToggleBrightFieldEnable(false);
        AfViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);
        return !IsRecipeCalibrate || CalibrationRecipeService.GetCorrectWaferMapByOffset(true);
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Where(t => t.IsCalibrated)
                .Select(t => t.Clone())
                .OrderBy(t => t.LensInformation.ObjectiveMagnification)
                .ThenBy(t => t.LensInformation.LensCode)
        ];

        if (ReviewList.Count == 0)
            return false;

        AfViewModel.ToggleBrightFieldEnable(false);
        AfViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.MicroscopeLensInformation);

            case 1:
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(SelectMicroscopeFocusCacheItem.FindFocusPosition);
                return true;

            case 2:
                if (ResultMicroscopeFocusItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find focus!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultMicroscopeFocusItemDto.IsCalibrated = true;
                    if (Save(ResultMicroscopeFocusItemDto, cancellationToken) == false)
                    {
                        ResultMicroscopeFocusItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.MicroscopeLensInformation == SelectMicroscopeFocusCacheItem.LensInformation).IsCalibrated = true;
                //DialogWindowProvider.ShowDialog("Find Focus Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        AfViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);

        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(Cache.MicroscopeLensInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Switch Magnification Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Point.Origin);
        return true;
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            SelectMicroscopeFocusCacheItem = Cache.CurrentCalibrationCacheItem;
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.SetFindFocusPosition(result);

            if (await AutomationRecipeInformationAsync(Cache.MicroscopeLensInformation.LensName) == false)
                return false;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LensName = SelectMicroscopeFocusCacheItem.LensInformation.LensName,
                SelectMicroscopeFocusCacheItem.FindFocusPosition
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
            var (isSuccessVerify, errorMessage) = Cache.CalibrationVerify();
            if (isSuccessVerify == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var detectImageDirectory = ImageFileDirectory;

            var findFocusPosition = SelectMicroscopeFocusCacheItem.FindFocusPosition;
            var findFocusMin = SelectMicroscopeFocusCacheItem.FindFocusMin;
            var findFocusMax = SelectMicroscopeFocusCacheItem.FindFocusMax;
            var findFocusInterval = SelectMicroscopeFocusCacheItem.FindFocusInterval;
            var setVoltageAfErrorThreshold = SelectMicroscopeFocusCacheItem.SetVoltageAfErrorThreshold;

            if (findFocusMin > findFocusMax || findFocusInterval == 0 || setVoltageAfErrorThreshold == 0)
            {
                DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Min <= Focs Max and Focus Interval > 0 and Voltage Af Error Threshold > 0)", DialogButtonsEnum.OK,
                    DialogIconEnum.Warning);
                return false;
            }

            AfViewModel.ToggleBrightFieldEnable(false);
            if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(SelectMicroscopeFocusCacheItem.LensInformation) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Switch Magnification Failed."), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(findFocusPosition);
            var ecsValue = AfViewModel.GetSensorEcsValue();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CurrentEcsValue = ecsValue,
                SelectMicroscopeFocusCacheItem.LensInformation.LensName,
                FindFocusPosition = findFocusPosition,
                FindFocusLimitMin = findFocusMin,
                FindFocusLimitMax = findFocusMax,
                FindFocusInterval = findFocusInterval,
                SetVoltageAfErrorThreshold = setVoltageAfErrorThreshold,
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
                    LensInformation = SelectMicroscopeFocusCacheItem.LensInformation,
                    FindPosition = findFocusPosition,
                    Quality = 0,
                    EcsValue = ecsValueTemp,
                    FilePath = detectImageDirectory
                });
            }

            Logger.LogHtmlInformation("Get Quality", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

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

            var findFocalItemResult = MicroscopeFocusItemDtoList.Maxima(t => t.Quality).First();
            var temp = findFocalItemResult;
            SelectedMicroscopeFocusItemDto = temp;
            AfViewModel.SetSensorEcsValue(SelectedMicroscopeFocusItemDto.EcsValue);

            Logger.LogHtmlInformation("Ecs-Quality Scatter", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ImageQuality = findFocalItemResult.Quality,
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(findFocalItemResult.FilePath)
                }),
                Quality = new HtmlPlot2DLinesChart([("Ecs-Quality", EcsPoints)], "Ecs-Quality")
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Set Voltage To AfError Zero", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            var (isSuccess, voltage, afErrorAverage) = MicroscopeViewModel.SetVoltageToAfErrorZeroFast(setVoltageAfErrorThreshold, 20, HtmlLogUniqueId, Name, cancellationToken);
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Set Voltage To AfError Zero Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            SelectedMicroscopeFocusItemDto.TransBufferAfErrorValue = afErrorAverage;
            SelectedMicroscopeFocusItemDto.MicroscopeVoltage = voltage;
            ResultMicroscopeFocusItemDto = SelectedMicroscopeFocusItemDto.Clone();
            AfViewModel.SetSensorBrightFieldChuckStandardEcsValue(ResultMicroscopeFocusItemDto.LensInformation, ResultMicroscopeFocusItemDto.EcsValue);
            MicroscopeViewModel.SetVoltage(ResultMicroscopeFocusItemDto.MicroscopeVoltage);

            Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                TransBufferValueArray = afErrorAverage,
                ResultMicroscopeFocusItemDto.EcsValue,
                ResultMicroscopeFocusItemDto.MicroscopeVoltage,
                ImageQuality = ResultMicroscopeFocusItemDto.Quality,
                ResultMicroscopeFocusItemDto.LensInformation.LensName,
                ResultMicroscopeFocusItemDto.TransBufferAfErrorValue,
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(ResultMicroscopeFocusItemDto.FilePath)
                })
            }), HtmlLogUniqueId.LoggingHtml());

            result = true;
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        await InvokeVerifyAsync(() =>
        {
            if (VerifyCalibration(SelectReviewItemDto, cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
        return result;
    }

    private bool VerifyCalibration(MicroscopeFocusItemDto selectReviewItemDto, CancellationToken cancellationToken)
    {
        ClearCalibrationTemp();
        var detectImageDirectory = ImageFileDirectory;

        selectReviewItemDto.IsVerified = false;
        Cache.MicroscopeLensInformation = SelectReviewItemDto!.LensInformation;

        SelectMicroscopeFocusCacheItem = Cache.CurrentCalibrationCacheItem;

        var findFocusPosition = SelectMicroscopeFocusCacheItem.FindFocusPosition;
        var findFocusMin = SelectMicroscopeFocusCacheItem.FindFocusMin;
        var findFocusMax = SelectMicroscopeFocusCacheItem.FindFocusMax;
        var findFocusInterval = SelectMicroscopeFocusCacheItem.FindFocusInterval;

        Logger.LogHtmlInformation($"{SelectMicroscopeFocusCacheItem.LensInformation.LensName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation($"Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            ResultEcsValue = selectReviewItemDto.EcsValue,
            ResultVoltage = selectReviewItemDto.MicroscopeVoltage,
            SelectMicroscopeFocusCacheItem.LensInformation.LensName,
            FindFocusPosition = findFocusPosition,
            FindFocusLimitMin = findFocusMin,
            FindFocusLimitMax = findFocusMax,
            FindFocusInterval = findFocusInterval,
            ImageFileDirectory = detectImageDirectory
        }), HtmlLogUniqueId.LoggingHtml());

        AfViewModel.ToggleBrightFieldEnable(false);

        if (MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocus(SelectMicroscopeFocusCacheItem.LensInformation) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Switch Magnification Failed."), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(findFocusPosition);
        AfViewModel.SetSensorBrightFieldChuckStandardEcsValue(selectReviewItemDto.LensInformation, selectReviewItemDto.EcsValue);
        MicroscopeViewModel.SetVoltage(selectReviewItemDto.MicroscopeVoltage);
        AfViewModel.ToggleBrightFieldEnable(true);

        Thread.Sleep(5000);

        var microscopeFocusItemDto = new MicroscopeFocusItemDto
        {
            Index = 0,
            LensInformation = SelectMicroscopeFocusCacheItem.LensInformation,
            FindPosition = findFocusPosition,
            Quality = 0,
            EcsValue = selectReviewItemDto.EcsValue,
            FilePath = detectImageDirectory
        };

        GetQuality(microscopeFocusItemDto);

        AfViewModel.ToggleBrightFieldEnable(false);

        var quality = microscopeFocusItemDto.Quality;
        var error = quality - selectReviewItemDto.Quality;
        var result = Math.Abs(error) < Cache.Threshold;
        Cache.VerifyResultQuality = quality;
        Cache.VerifyResultError = error;

        selectReviewItemDto.IsVerified = result;
        if (Save(selectReviewItemDto, cancellationToken) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
            selectReviewItemDto.IsVerified = false;
            return false;
        }

        Logger.LogHtmlInformation($"Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            NewOffset = quality,
            OldOffset = selectReviewItemDto.Quality,
            Error = error,
            Cache.Threshold,
            microscopeFocusItemDto.EcsValue,
            ImageQuality = microscopeFocusItemDto.Quality,
            microscopeFocusItemDto.LensInformation.LensName
        }), HtmlLogUniqueId.LoggingHtml());

        if (!IsAutoCalibrate)
        {
            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({quality:f3}) Old Offset: ({selectReviewItemDto.Quality:f3}) Error: ({error:f3})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }

        if (result == false)
            return false;

        // 同焦验证
        var varifiedDtoList = Calibrations.Where(t => t.IsOk).ToList();
        if (varifiedDtoList.Count >= 2)
        {
            var parfocalOffset = Calibrations.Max(t => t.EcsValue) - Calibrations.Min(t => t.EcsValue);
            result = Math.Abs(parfocalOffset) <= Cache.ParfocalThreshold;
            if (result == false)
            {
                DialogWindowProvider.ShowDialog($"Parfocal {(result ? "OK" : "Failed")}, Offset: {parfocalOffset}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                selectReviewItemDto.IsVerified = false;
                if (Save(selectReviewItemDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }
            }

            Logger.LogHtmlInformation($"Parfocal {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Cache.ParfocalThreshold,
                parfocalOffset,
                MinEcsMicroscopeType = Calibrations.OrderBy(t => t.EcsValue).First().LensInformation.LensName,
                MaxEcsMicroscopeType = Calibrations.OrderBy(t => t.EcsValue).Last().LensInformation.LensName,
                EcsResult = new HtmlTable([.. Calibrations.Select(t => new { t.IsVerified, LensName = t.LensInformation.LensName, t.EcsValue })])
            }), HtmlLogUniqueId.LoggingHtml());
        }

        return result;
    }

    private void GetQuality(MicroscopeFocusItemDto microscopeFocusItemDto)
    {
        AfViewModel.SetSensorEcsValue(microscopeFocusItemDto.EcsValue);

        if (microscopeFocusItemDto.Index == 1) Thread.Sleep(3000);
        using var image = ReviewViewModel.GetBrightFieldImage();

        microscopeFocusItemDto.FilePath =
            $"{microscopeFocusItemDto.FilePath}\\Index({microscopeFocusItemDto.Index})_Ecs({microscopeFocusItemDto.EcsValue:F3})_Quality({microscopeFocusItemDto.Quality:F3})_Guid({HtmlLogUniqueId}).jpg";

        image.Save(microscopeFocusItemDto.FilePath);
        using var localImage = HalconFactory.CreateImage(microscopeFocusItemDto.FilePath);
        var quality = ReviewViewModel.GetQuality(image);
        microscopeFocusItemDto.Quality = quality;
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

        Logger.LogHtmlInformation($"Get Quality, Ecs:{microscopeFocusItemDto.EcsValue}", HtmlHeaderLevelEnum.Header4, htmlBulletList, HtmlLogUniqueId.LoggingHtml());
    }

    private bool Save(MicroscopeFocusItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.LensInformation != itemDto.LensInformation),
            itemDto.Clone()
        ];

        foreach (var microscopeFocusCacheItem in Cache.MicroscopeFocusCacheItemDic)
        {
            if (ApplicationCookie.MicroscopeLensInformations.SingleOrDefault(t => t.LensName == microscopeFocusCacheItem.Key) is null)
                Cache.MicroscopeFocusCacheItemDic.TryRemove(microscopeFocusCacheItem.Key, out _);
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
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
        ResultMicroscopeFocusItemDto = null;
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        SynchronizationContextProvider.Send(() =>
        {
            AutoCalibrationStepList.Clear();
            AutoCalibrationStepList.AddRange([
                new CalibrationItemStep { StepName = "loading" },
                .. ApplicationCookie.MicroscopeLensInformations.Select(info => new CalibrationItemStep { StepName = info.LensName }),
                new CalibrationItemStep { StepName = "Review" }
            ]);
        });
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        GetAutoCalibrationStep();
        await base.AutomationActionAsync(cancellationToken).ConfigureAwait(false);
        SynchronizationContextProvider.Send(MicroscopeFocusItemDtoList.Clear);
        foreach (var (calibrationItemStep, stepIndex) in AutoCalibrationStepList.Select((step, index) => (step, index)))
        {
            Func<Task<bool>> autoStepAction = stepIndex switch
            {
                0 => async () =>
                {
                    if (await LoadedingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                    await InvokeCalibrateAsync(() =>
                    {
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                        {
                            Cache.AlgorithmTemplateTypeEnum
                        }), HtmlLogUniqueId.LoggingHtml());
                        return true;
                    });
                    if (await AutoStepAsync().ConfigureAwait(false) == false) return false;
                    return await AutoNextingAsync(cancellationToken).ConfigureAwait(false);
                }
                ,
                var index when index == AutoCalibrationStepList.Count - 1 => async () =>
                {
                    AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                    if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                    var result = await InvokeCalibrateAsync(() =>
                    {
                        foreach (var itemReview in ReviewList)
                        {
                            SelectReviewItemDto = itemReview;
                            if (VerifyCalibration(SelectReviewItemDto, cancellationToken) == false)
                            {
                                DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.LensInformation.LensName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                return false;
                            }
                        }

                        return true;
                    }).ConfigureAwait(false);
                    if (result == false) return false;
                    AutoCalibrationStepIndex++;
                    return true;
                }
                ,
                _ => async () =>
                {
                    if (await AutoActionStepAsync(calibrationItemStep.StepName, cancellationToken).ConfigureAwait(false) == false)
                    {
                        DialogWindowProvider.ShowDialog($"Auto Calibration {calibrationItemStep.StepName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        return false;
                    }

                    return true;
                }
            };

            if (await autoStepAction().ConfigureAwait(false) == false) return false;
            AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
        }

        return true;
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string microscopeName)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformations.Single(t => t.LensName == microscopeName);
        SelectMicroscopeFocusCacheItem = Cache.CurrentCalibrationCacheItem;
        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });

        if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(SelectMicroscopeFocusCacheItem.WaferMaskTypeEnum, Cache.MicroscopeLensInformation, null, out var maskInfo) == false)
            return false;

        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo, out var position);
        Cache.SetFindFocusPosition(position);

        StageViewModel.SetBrightFieldAbsoluteStageXy(position);
        return true;
    }

    private async Task<bool> AutoActionStepAsync(string lensName, CancellationToken cancellationToken)
    {
        if (await AutomationRecipeInformationAsync(lensName) == false) return false;
        if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
        CalibrationStepIndex = 2;
        if (await NextingAsync(cancellationToken) == false) return false;
        return await AutoNextingAsync(cancellationToken);
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
        GetAutoCalibrationStep();
        await base.AutomationReviewActionAsync(cancellationToken);
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
                foreach (var (_, itemReview) in ReviewList.Select((t, i) => (index: i, itemReview: t)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectReviewItemDto = itemReview;
                    if (await AutomationRecipeInformationAsync(SelectReviewItemDto.LensInformation.LensName) == false) return false;
                    if (VerifyCalibration(SelectReviewItemDto, cancellationToken) == false)
                    {
                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.LensInformation.LensName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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

    #endregion 自动化校准
}