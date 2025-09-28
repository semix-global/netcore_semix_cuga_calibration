using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Services.Interfaces;
using Core.Utilities;
using HalconDotNet;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserXYAstigmatismCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXYAstigmatismCalibrationViewModel(ICalibrationLaserService calibrationLaserService) : CalibrationViewModelBase
{
    #region 属性

    private string ChirpFileDirectory => Path.Combine(AppHomeDirectory, "Chirp", nameof(LaserXYAstigmatismCalibrationViewModel), DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select Mag", DefaultIsNextEnable = true },
        new() { StepName = "Select a lens and a location" },
        new() { StepName = "Find Best EcsX With Chirp AOD Default Wave", DefaultIsNextEnable = true },
        new() { StepName = "Get Optimum RateRange" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserXYAstigmatismCalibrationItemDto> _laserXyAstigmatismItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<Point> _frequencyEcsList = [];

    [ObservableProperty]
    private ObservableCollection<Point> _ecsQualityList = [];

    /// <summary>
    /// 校准界面显示当前的ChirpAod波形
    /// </summary>
    [ObservableProperty]
    private GenerateChirpAodWaveParamDto _chirpAodFindEcsYDto = new();

    [ObservableProperty]
    private LaserXYAstigmatismCalibrationItemDto? _selectedLaserXyAstigmatismItemDto;

    [ObservableProperty]
    private LaserXYAstigmatismCalibrationItemDto? _resultLaserXyAstigmatismItemDto;

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    /// <summary>
    ///EcsY Zlimit中心位置
    /// </summary>
    [ObservableProperty]
    private double _sliderCenterEcsY;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserXYAstigmatismCalibrationItemDto> _reviewList = [];

    [ObservableProperty]
    private LaserXYAstigmatismCalibrationItemDto? _selectReviewItemDto;

    /// <summary>
    /// review左侧文本框binding显示review结果用
    /// </summary>
    [ObservableProperty]
    private LaserXYAstigmatismCalibrationItemDto? _resultReviewItemDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserXYAstigmatismCalibrationCache _cache = new();

    [ObservableProperty]
    private LaserXYAstigmatismCalibrationItemDto[] _calibrations = [];

    public LaserAodDelayItemDto[] LaserAodDelayItemList { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserAodDelayItemDto>(out var laserAodDelayItemDtos, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserAodDelayItemList = laserAodDelayItemDtos;

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPrescanChirpAodAlignmentDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserXYAstigmatismCalibrationCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserXYAstigmatismCalibrationItemDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation.LensCode == -1) Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.FindPosition, Cache.CalChipSiteModelEnum);
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
        ];

        return ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                return true;

            case 2:
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.FindPosition, Cache.CalChipSiteModelEnum);
                return true;

            case 3:
                // 无校准记录时，find ecsY界面参数继承上一步设置find ecsX的参数
                var chirpAodDefaultDto = Cache.GetDefaultChirpAodProfile();
                var temp = Calibrations.Where(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).ToList();
                if (temp.Count == 0 && SelectedLaserXyAstigmatismItemDto is null)
                {
                    Cache.SetStartFrequencyChangeRate(chirpAodDefaultDto.FrequencyChangeRate);
                    Cache.SetEcsYParams();
                }

                return true;

            case 4:
                // 缓存
                if (ResultLaserXyAstigmatismItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find XY Astigmatism Cib!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultLaserXyAstigmatismItemDto.IsCalibrated = true;
                    if (Save(ResultLaserXyAstigmatismItemDto, cancellationToken) == false)
                    {
                        ResultLaserXyAstigmatismItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog("Find XY Astigmatism Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

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

    [RelayCommand]
    private Task Step0CalibrateActionAsync()
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
            Cache.MicroscopeLensInformation = MicroscopeViewModel.GetCurrentMicroscopeLensInformation();

            var resultBright = StageViewModel.GetBrightFieldStagePosition();

            Cache.FindPosition = resultBright;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            try
            {
                var (ecsUpperLimitX, ecsLowerLimitX, ecsLimitIntervalX, ecsXInitial) = Cache.GetEcsXParams();
                if (ecsLowerLimitX < 0 || ecsUpperLimitX < 0 || ecsLimitIntervalX <= 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Upper>0 And Focs Lower>0 and Focus Interval > 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                var detectImageDirectory = ImageFileDirectory;
                ClearCalibrationTemp();
                var chirpAodDefaultDto = Cache.GetDefaultChirpAodProfile();
                OnPropertyChanged(nameof(Cache.HighChirpAodDefaultDto));
                OnPropertyChanged(nameof(Cache.HighChirpAodDefaultDto.FrequencyChangeRate));
                chirpAodDefaultDto.IsHeaderAndFooter = false;
                // 下发默认波形
                if (LaserViewModel.TrySendAodFile(Cache.OpticsMagTypeEnum, (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation), false, out var errorMessage) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Send Default Aod Wave Failed.Error: " + errorMessage), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                var defaultChirpAodWaveProfileLst = ConfigureViewModel.GetChirpAODWaveProfileList(Cache.OpticsMagTypeEnum);
                chirpAodDefaultDto.ZeroSampleCount = defaultChirpAodWaveProfileLst[0].ZeroSampleCount;
                // 有AOD Delay结果时，默认chirp波形使用该delay值
                var laserAodDelayItem = LaserAodDelayItemList.SingleOrDefault(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum);
                if (laserAodDelayItem is not null && laserAodDelayItem.IsOk)
                {
                    var delayTime = Convert.ToInt32(laserAodDelayItem.RefinedChirpAodDelayTime);
                    chirpAodDefaultDto.ZeroSampleCount = delayTime;

                    IReadOnlyList<ChirpAODWaveformProfile> customZeroAodWaveProfileList = defaultChirpAodWaveProfileLst.Select(t =>
                    {
                        t.ZeroSampleCount = delayTime;
                        return t;
                    }).ToList();
                    LaserViewModel.SetChirpAODWaveProfileList(customZeroAodWaveProfileList);
                }

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.FindPosition, Cache.CalChipSiteModelEnum);

                var (currentResultList, isSuccess) = GetBestEcsItemByCurrentChirpAodWaveProfile(chirpAodDefaultDto, cancellationToken, true, false);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Best Ecs X Failed."), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                Cache.SetEcsXParams(currentResultList[0].EcsX);
                Logger.LogHtmlInformation("Find EcsX OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    chirpAodDefaultDto.SoundPackageLength,
                    chirpAodDefaultDto.BandWidth,
                    chirpAodDefaultDto.SampleRate,
                    chirpAodDefaultDto.FrequencyChangeRate,
                    chirpAodDefaultDto.ZeroSampleCount,
                    chirpAodDefaultDto.CenterFrequency,
                    chirpAodDefaultDto.FunctionMonotonicTypeEnum,
                    InitialFindEcsX = ecsXInitial,
                    EcsXUpperLimit = ecsUpperLimitX,
                    EcsXLowerLimit = ecsLowerLimitX,
                    EcsXLimitInterval = ecsLimitIntervalX,
                    FindXEcsResult = currentResultList[0].EcsX,
                    ImageFileDirectory = detectImageDirectory,
                    HtmlTab = new HtmlTab(new
                    {
                        Image = new HtmlImage(currentResultList[0].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error: Find Optimum Ecs X Failed.");
                return false;
            }
            finally
            {
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.FindPosition, Cache.CalChipSiteModelEnum);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                ClearCalibrationTemp();

                var (ecsUpperLimitY, ecsLowerLimitY, ecsLimitIntervalY, ecsYInitial) = Cache.GetEcsYParams();
                var (frequencyIncreaseCount, frequencyIncreaseInterval) = Cache.GetFrequencyParams();
                var setErrorThreshold = Cache.GetThresholdParams();
                var startFrequencyChangeRate = Cache.GetStartFrequencyChangeRate();
                var chirpAodDefaultDto = Cache.GetDefaultChirpAodProfile();
                if (ecsLowerLimitY < 0 || ecsUpperLimitY < 0 || ecsLimitIntervalY <= 0 || frequencyIncreaseCount < 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Upper>0 And Focs Lower>0 and Focus Interval > 0 and  FrequencyIncreaseCount> 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                Logger.LogHtmlInformation("Initial Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    chirpAodDefaultDto.SoundPackageLength,
                    chirpAodDefaultDto.ZeroSampleCount,
                    chirpAodDefaultDto.BandWidth,
                    chirpAodDefaultDto.HeaderFrequency,
                    chirpAodDefaultDto.FooterFrequency,
                    startFrequencyChangeRate,
                    InitialFindEcsY = ecsYInitial,
                    EcsYUpperLimit = ecsUpperLimitY,
                    EcsYLowerLimit = ecsLowerLimitY,
                    EcsYLimitInterval = ecsLimitIntervalY,
                    FrequenceIncreaseCount = frequencyIncreaseCount,
                    FrequenceIncreaseInterval = frequencyIncreaseInterval,
                    ErrorThreshold = setErrorThreshold,
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                ChirpAodFindEcsYDto = chirpAodDefaultDto.Clone();

                var result = GetOptimumFrequencyChangeRate();
                if (!result)
                {
                    DialogWindowProvider.ShowDialog("Calibration Result Over The Threshold!Calibrate Fail!", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }
                else
                    DialogWindowProvider.ShowDialog("XY Astigmatism Calibration Finished!");

                return true;

                bool GetOptimumFrequencyChangeRate()
                {
                    foreach (var (index, frequencyRateChange) in Enumerable.Range(0, frequencyIncreaseCount)
                                 .Select(t => startFrequencyChangeRate + t * frequencyIncreaseInterval)
                                 .Select((d, i) => (i, d)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var (isSuccess, chirpAodChangeDto, chirpAodWaveResultList) = GenerateAndSendChirpAodWave(frequencyRateChange);
                        if (isSuccess == false) return false;

                        (var currentResultList, isSuccess) = GetBestEcsItemByCurrentChirpAodWaveProfile(chirpAodChangeDto, cancellationToken);
                        if (isSuccess == false) return false;

                        var resultItem = currentResultList[0].Clone();
                        resultItem.Index = index;
                        resultItem.ChirpAodWaveResultList = chirpAodWaveResultList;
                        SynchronizationContextProvider.Send(() =>
                        {
                            LaserXyAstigmatismItemDtoList.Add(resultItem);
                            FrequencyEcsList = [.. FrequencyEcsList, new Point(resultItem.FrequencyChangeRate, resultItem.EcsErrorValue)];
                        });
                    }

                    // 从结果集合中截掉方向判断的item，用来生成ecsError-changeRate曲线,获得resultItem
                    var startIndex = LaserXyAstigmatismItemDtoList.Select((dto, index) => (dto, index))
                        .Where(t => t.dto.Index == 0)
                        .Select(t => t.index)
                        .ToArray();
                    var plotList = startIndex.Length > 1
                        ? LaserXyAstigmatismItemDtoList.Skip(startIndex.Last()).ToList()
                        : [.. LaserXyAstigmatismItemDtoList.Select(t => t)];

                    var csvPath = $@"{CsvFileDirectory}\Calibration\SoundPackage{chirpAodDefaultDto.SoundPackageLength}mm_BandWith{ChirpAodFindEcsYDto.BandWidth}_CenterFrequency{startFrequencyChangeRate}_ZeroNum{ChirpAodFindEcsYDto.ZeroSampleCount}\Error_Guid({HtmlLogUniqueId}).csv";
                    SaveIdealCsv(plotList, csvPath);

                    // 拟合
                    var findItemByNotFit = plotList.OrderBy(t => Math.Abs(t.EcsErrorValue)).First();
                    var listRow = plotList.Select(t => t.EcsY).ToList();
                    var listCol = plotList.Select(t => 1 / t.FrequencyChangeRate).ToList();
                    var (k, b, _, _) = PolynomialLeastSquares.Polynomial1Fit(Vector<double>.Build.DenseOfEnumerable(listRow), Vector<double>.Build.DenseOfEnumerable(listCol));
                    var calibrationResult = Math.Abs(findItemByNotFit.EcsErrorValue) < setErrorThreshold;

                    var circleCount = 0;
                    var isIterationEcsErrorLessThanPreviousList = new List<bool>();
                    var iterationDtoItems = new List<LaserXYAstigmatismCalibrationItemDto>();
                    if (calibrationResult)
                        SelectedLaserXyAstigmatismItemDto = findItemByNotFit.Clone();
                    else
                    {
                        Logger.LogHtmlInformation("Iteration", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                        if (FrequencyRateChangeIteration(Math.Abs(findItemByNotFit.EcsErrorValue))) return false; // 结果迭代
                        // 结果dto赋值，ecsXY error<阈值时校准成功
                        SelectedLaserXyAstigmatismItemDto = iterationDtoItems.Minima(t => Math.Abs(t.EcsErrorValue)).First();
                        calibrationResult = Math.Abs(SelectedLaserXyAstigmatismItemDto!.EcsErrorValue) < setErrorThreshold;
                    }

                    SelectedLaserXyAstigmatismItemDto.IsCalibrated = calibrationResult;
                    ResultLaserXyAstigmatismItemDto = SelectedLaserXyAstigmatismItemDto.Clone();

                    // log
                    csvPath = $"{CsvFileDirectory}\\Calibration_Iteration\\SoundPackage{chirpAodDefaultDto.SoundPackageLength}mm_BandWith{ChirpAodFindEcsYDto.BandWidth}_CenterFrequency{startFrequencyChangeRate}_ZeroNum{ChirpAodFindEcsYDto.ZeroSampleCount}\\Error_Guid({HtmlLogUniqueId}).csv";
                    SaveIdealCsv(iterationDtoItems, csvPath);

                    Logger.LogHtmlInformation("Find EcsY OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                    {
                        OpticsMagType = ResultLaserXyAstigmatismItemDto.OpticsMagTypeEnum,
                        OptimumEcsXByDefaultWave = Cache.GetInitialEcsX(),
                        FrequenceRateChangeIdea = findItemByNotFit.FrequencyChangeRate,
                        FrequenceRateChangeResult = ResultLaserXyAstigmatismItemDto.FrequencyChangeRate,
                        OriEcsX = findItemByNotFit.EcsX,
                        OriEcsY = findItemByNotFit.EcsY,
                        ResultEcsX = ResultLaserXyAstigmatismItemDto.EcsX,
                        ResultEcsY = ResultLaserXyAstigmatismItemDto.EcsY,
                        OriEcsError = findItemByNotFit.EcsErrorValue,
                        ResultError = ResultLaserXyAstigmatismItemDto.EcsErrorValue,
                        FrequencyChangeRate_EcsError_List = new HtmlPlot2DLinesChart([
                            ("FrequencyChangeRate-EcsError", [..plotList.OrderBy(s => s.FrequencyChangeRate).Select(s => new Point(s.FrequencyChangeRate, s.EcsErrorValue))])
                        ], "FrequencyChangeRate-EcsError"),
                        Result_List = new HtmlPlot2DLinesChart([
                            ("EcsY-FrequencyChangeRateReciprocal", [.. plotList.OrderBy(s => s.FrequencyChangeRate).Select(s => new Point(s.EcsY, 1 / s.FrequencyChangeRate))]),
                            ("EcsX-FrequencyChangeRateReciprocal", [.. plotList.OrderBy(s => s.FrequencyChangeRate).Select(s => new Point(s.EcsX, 1 / s.FrequencyChangeRate))]),
                            ("PlyFit1Function", [..listRow.Select(t => new Point(t, t * k + b))])
                        ], "Result"),
                        ChirpWaveResultList = new HtmlTable([.. ResultLaserXyAstigmatismItemDto.ChirpAodWaveResultList.Select(t => new { t.OpticsAODElectrodeEnum, t.FilePath })]),
                        HtmlTab = new HtmlTab(new
                        {
                            Image = new HtmlImage(ResultLaserXyAstigmatismItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());

                    return calibrationResult;

                    //迭代，根据拟合一次函数，首次输入ecsX，得到F0下发，后续迭代代入deltaEcs，频率变化率根据斜率改变deltaRateChange，得到新的F下发
                    bool FrequencyRateChangeIteration(double previousEcsError, double previousRateChange = 0, int repeatNum = 5)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var bestEcsX = Cache.GetInitialEcsX();
                            // 首次输入ecsX，后续迭代输入xyEcsError
                            var deltaEcs = previousRateChange == 0 ? bestEcsX : previousEcsError;
                            var currentRateChange = Math.Round(1.0 / (k * deltaEcs + b), 2) + previousRateChange;
                            if (double.IsNaN(currentRateChange) || currentRateChange == 0)
                            {
                                DialogWindowProvider.ShowDialog("Get Rate Change By Relational function Failed! The Points is not enough!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(currentRateChange));
                            }

                            // 下发f0
                            var (isSendSuccess, chirpAodF0Dto, chirpAodWaveResultList) = GenerateAndSendChirpAodWave(currentRateChange);
                            if (isSendSuccess == false)
                                return false;

                            var (findItemResultF0, isGetBestItemSuccess) = GetBestEcsItemByCurrentChirpAodWaveProfile(chirpAodF0Dto, cancellationToken);
                            if (!isGetBestItemSuccess)
                            {
                                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Get Best Ecs Item By Chirp Aod Wave Failed."), HtmlLogUniqueId.LoggingHtml());
                                return false;
                            }

                            var findItemResult = findItemResultF0[0].Clone();
                            findItemResult.ChirpAodWaveResultList = chirpAodWaveResultList;

                            iterationDtoItems.Add(findItemResult);
                            circleCount++;
                            var currentEcsError = Math.Abs(findItemResult.EcsY - bestEcsX);
                            if (currentEcsError <= setErrorThreshold)
                                return true;

                            isIterationEcsErrorLessThanPreviousList.Add(currentEcsError < previousEcsError);

                            Logger.LogHtmlInformation("Iteration Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                            {
                                optinumEcsX = bestEcsX,
                                deltaEcs,
                                rateChange = currentRateChange,
                                newDelta = currentEcsError,
                                previousEcsError
                            }), HtmlLogUniqueId.LoggingHtml());

                            if (EnumerableHelper.HasConsecutiveFalse(isIterationEcsErrorLessThanPreviousList, 3) || circleCount > repeatNum)
                                return false;

                            return FrequencyRateChangeIteration(currentEcsError, currentRateChange);
                        }
                        catch (Exception e)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Iteration Failed.{e.Message}"), HtmlLogUniqueId.LoggingHtml());
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error: Find Optimum Ecs Y Failed.{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.FindPosition, Cache.CalChipSiteModelEnum);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InvokeVerifyAsync(() =>
            {
                if (SelectReviewItemDto is null)
                {
                    DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                Cache.OpticsMagTypeEnum = SelectReviewItemDto.OpticsMagTypeEnum;
                Cache.MicroscopeLensInformation = SelectReviewItemDto.MicroscopeLensInformation;

                ClearCalibrationTemp();
                var detectImageDirectory = ImageFileDirectory;
                SelectReviewItemDto.IsVerified = false;

                var selectItemFrequencyChangeRate = SelectReviewItemDto.FrequencyChangeRate;
                var (ecsLimitUpperY, ecsLimitLowerY, ecsLimitIntervalY, ecsYInitial) = Cache.GetEcsYParams();
                var setErrorThreshold = Cache.GetThresholdParams();

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.FindPosition, Cache.CalChipSiteModelEnum);

                // 下发默认波形
                if (LaserViewModel.TrySendAodFile(Cache.OpticsMagTypeEnum, (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation), false, out var errorMessage) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Send Default Aod Wave Failed.Error: " + errorMessage), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                var chirpAodDefaultDto = Cache.GetDefaultChirpAodProfile();

                var reviewDto = SelectReviewItemDto.Clone();

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    chirpAodDefaultDto.SoundPackageLength,
                    chirpAodDefaultDto.ZeroSampleCount,
                    chirpAodDefaultDto.BandWidth,
                    chirpAodDefaultDto.HeaderFrequency,
                    chirpAodDefaultDto.FooterFrequency,
                    FrequencyChangeRate = selectItemFrequencyChangeRate,
                    FindEcsYRangeAxis = ecsYInitial,
                    EcsYUpperLimit = ecsLimitUpperY,
                    EcsYLowerLimit = ecsLimitLowerY,
                    EcsYLimitInterval = ecsLimitIntervalY,
                    ErrorThreshold = setErrorThreshold,
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                Task.Delay(1000, cancellationToken).Wait(cancellationToken);

                var reviewChirpAodWaveList = AODWaveformProfileFactory.CreateChirpList(reviewDto.ChirpAodWaveResultList);
                LaserViewModel.SetChirpAODWaveProfileList(reviewChirpAodWaveList);

                var bandWidth = chirpAodDefaultDto.SoundPackageLength * reviewDto.FrequencyChangeRate;
                var chirpAodWaveProfileDto = chirpAodDefaultDto.Clone();
                chirpAodWaveProfileDto.BandWidth = bandWidth;

                var (currentResultList, isSuccess) = GetBestEcsItemByCurrentChirpAodWaveProfile(chirpAodWaveProfileDto, cancellationToken);
                if (isSuccess == false)
                    return false;

                var ecsError = currentResultList[0].EcsErrorValue;
                var result = Math.Abs(ecsError) < setErrorThreshold;
                ResultReviewItemDto = currentResultList[0].Clone();

                Logger.LogHtmlInformation($"Verify {(isSuccess ? "Success" : "Error")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    CurrentFrequenceRateChange = ResultReviewItemDto.FrequencyChangeRate,
                    OldEcsError = SelectReviewItemDto.EcsErrorValue,
                    NewEcsError = ecsError,
                    ReviewThreshold = setErrorThreshold,
                    NewEcsX = ResultReviewItemDto.EcsX,
                    NewEcsY = ResultReviewItemDto.EcsY,
                    NewQualityX = ResultReviewItemDto.QualityX,
                    NewQualityY = ResultReviewItemDto.QualityY,
                    HtmlTab = new HtmlTab(new
                    {
                        Image = new HtmlImage(ResultReviewItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                SelectReviewItemDto.IsVerified = result;
                if (Save(SelectReviewItemDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    SelectReviewItemDto.IsVerified = false;
                    return false;
                }

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New EcsError: ({ecsError:f3}) Old EcsError: ({SelectReviewItemDto.EcsErrorValue:f3}) FrequenceIncrement: ({selectItemFrequencyChangeRate:f3})", DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }).ConfigureAwait(false);
        }
        finally
        {
            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.FindPosition, Cache.CalChipSiteModelEnum);
        }
    }

    private bool Save(LaserXYAstigmatismCalibrationItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.OpticsMagTypeEnum != itemDto.OpticsMagTypeEnum),
            itemDto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserXYAstigmatismCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => { LaserXyAstigmatismItemDtoList.Clear(); });
        FrequencyEcsList = [];
        SelectedLaserXyAstigmatismItemDto = null;
    }

    #endregion 校准

    #region 算法

    /// <summary>
    /// 找一段高度范围内的Ecs作为新的轴心，范围上下限以新的轴心滑动
    /// </summary>
    /// <param name="chirpAodWaveDto"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="isFindEcsX"></param>
    /// <param name="isAutoSlider"></param>
    /// <returns>返回x/y最清晰的一组结果</returns>
    private (List<LaserXYAstigmatismCalibrationItemDto>, bool) GetBestEcsItemByCurrentChirpAodWaveProfile(GenerateChirpAodWaveParamDto chirpAodWaveDto, CancellationToken cancellationToken, bool isFindEcsX = false, bool isAutoSlider = false)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            SynchronizationContextProvider.Send(() => EcsQualityList = []);
            var (ecsLimitUpper, ecsLimitLower, ecsLimitInterval, ecsInitial) = isFindEcsX ? Cache.GetEcsXParams() : Cache.GetEcsYParams();
            Logger.LogHtmlInformation($"Get Result With RateChange: {chirpAodWaveDto.FrequencyChangeRate}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                ecsLimitUpper,
                ecsLimitLower,
                ecsLimitInterval,
                ecsInitial,
                chirpAodWaveDto.FrequencyChangeRate,
                chirpAodWaveDto.SoundPackageLength,
                chirpAodWaveDto.CenterFrequency,
                chirpAodWaveDto.HeaderFrequency,
                chirpAodWaveDto.FooterFrequency,
                chirpAodWaveDto.ZeroSampleCount,
                chirpAodWaveDto.SampleRate,
                chirpAodWaveDto.AodWaveDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var count = Convert.ToInt32((ecsLimitLower + ecsLimitUpper) / ecsLimitInterval) + 1;
            // 初始化循环测试的列表
            var dtoTempList = new List<LaserXYAstigmatismCalibrationItemDto>();
            var rangeList = Enumerable.Range(0, count)
                .Select(x => Math.Min(ecsInitial - ecsLimitUpper + x * ecsLimitInterval, ecsInitial + ecsLimitLower));

            foreach (var (index, ecsValueTemp) in rangeList.Select((d, i) => (i, d)))
            {
                dtoTempList.Add(new LaserXYAstigmatismCalibrationItemDto
                {
                    Index = index,
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    FrequencyChangeRate = chirpAodWaveDto.FrequencyChangeRate,
                    EcsX = ecsValueTemp,
                    EcsY = ecsValueTemp,
                    EcsErrorValue = 0,
                    QualityX = 0,
                    QualityY = 0,
                    FilePath = ImageFileDirectory,
                    OriginFilePath = ImageFileDirectory
                });
            }

            var getQualityResultList = new List<LaserXYAstigmatismCalibrationItemDto>();
            // ECS执行一轮采图
            foreach (var itemDto in dtoTempList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var temp = itemDto.Clone();
                if (GetXyQuality(ref temp) == false)
                    return ([], false);

                getQualityResultList.Add(temp);
                SynchronizationContextProvider.Send(() => EcsQualityList = [.. EcsQualityList, new Point(isFindEcsX ? temp.EcsX : temp.EcsY, isFindEcsX ? temp.QualityX : temp.QualityY)]);
            }

            var (isSuccess, resultItems) = GetXyQualityBestResultItem();
            if (isSuccess == false) return ([], false);

            var currentEcs = isFindEcsX ? resultItems[0].EcsX : resultItems[0].EcsY;
            var currentQuality = isFindEcsX ? resultItems[0].QualityX : resultItems[0].QualityY;
            var ecsError = Math.Abs(currentEcs - ecsInitial);

            Logger.LogHtmlInformation($"Get Result OK: Frequency Change Rate {chirpAodWaveDto.FrequencyChangeRate}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Mode = "Extremum",
                OpticsMagType = Cache.OpticsMagTypeEnum,
                FrequencyChangedRate = chirpAodWaveDto.FrequencyChangeRate,
                chirpAodWaveDto.AodWaveDirectory,
                CurrentCircleInitialEcs = ecsInitial,
                FindInitialEcs = currentEcs,
                resultItems[0].EcsErrorValue,
                EcsX_Extremum = resultItems[0].EcsX,
                EcsY_Extremum = resultItems[0].EcsY,
                EcsX_Average = resultItems[1].EcsX,
                EcsY_Average = resultItems[1].EcsY,
                Ecs_Quality_List = new HtmlPlot2DLinesChart([
                    ("Ecs-QualityX", getQualityResultList.OrderBy(o => o.Index).Select(s => new Point(s.EcsX, s.QualityX)).ToArray()),
                    ("Ecs-QualityY", getQualityResultList.OrderBy(o => o.Index).Select(s => new Point(s.EcsX, s.QualityY)).ToArray())
                ], "Ecs-QualityX/Y"),
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(resultItems[0].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());


            // 执行一轮找ecs之后，判断最佳Ecs距离initialEcs是否小于量程的一半，大于时继续迭代
            if (isAutoSlider == false || ecsError <= (ecsLimitLower + ecsLimitUpper) / 2) return (resultItems, true);

            if (isFindEcsX)
                Cache.SetEcsXParams(currentEcs);
            else
                Cache.SetEcsYParams(currentEcs);

            DialogWindowProvider.TryShowDialog($"Find Ecs {(isFindEcsX ? "X" : "Y")} value far from initial axis more, EcsError: ({ecsError})," +
                                               $"Do you want to repeat once use the current ecs value as the new axis?"
                , out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
            if (dialogButtonsEnum != DialogResultEnum.Retry)
                return (resultItems, true);

            return GetBestEcsItemByCurrentChirpAodWaveProfile(chirpAodWaveDto, cancellationToken, isFindEcsX, isAutoSlider);

            (bool, List<LaserXYAstigmatismCalibrationItemDto>) GetXyQualityBestResultItem()
            {
                var listDto = new List<LaserXYAstigmatismCalibrationItemDto>();
                try
                {
                    // xy得分大于100的对象参与结果运算（防止超出景深极端值的干扰）
                    var temp = getQualityResultList.Select(s => s.Clone()).ToList();

                    // 筛选X、Y得分最低的对象，找出ecs差值最小的两个对象，把y得分最高的ecs值和得分值赋值给x，输出ecsError（根据验证，梯度算法的出来的趋势，分数越小越清晰）
                    var qualityXMinList = temp.Minima(s => s.QualityX).ToList();
                    var qualityYMaxList = temp.Maxima(s => s.QualityY).ToList();
                    var res = 1000d;
                    (int XIndex, int YIndex) index = (0, 0);
                    foreach (var itemY in qualityYMaxList)
                    {
                        foreach (var itemX in qualityXMinList)
                        {
                            var r = Math.Abs(itemX.EcsX - itemY.EcsX);
                            if (r > res) continue;

                            res = r;
                            index = (itemX.Index, itemY.Index);
                        }
                    }

                    if (isFindEcsX) index.XIndex = qualityXMinList.First().Index;

                    var resultQualityMinX = qualityXMinList.Single(s => s.Index == index.XIndex);
                    var resultQualityMaxY = qualityYMaxList.Single(s => s.Index == index.YIndex);

                    var averageEcsX = resultQualityMinX.Index == 0 || temp.Count < 3
                        ? temp.Skip(0).Take(temp.Count < 3 ? temp.Count : 3).Average(t => t.EcsX)
                        : temp.Count - resultQualityMinX.Index == 1
                            ? temp.Last().EcsX
                            : temp.Skip(resultQualityMinX.Index - 1).Take(3).Average(t => t.EcsX);

                    var averageEcsY = resultQualityMaxY.Index == 0 || temp.Count < 3
                        ? temp.Skip(0).Take(temp.Count < 3 ? temp.Count : 3).Average(t => t.EcsY)
                        : temp.Count - resultQualityMaxY.Index == 1
                            ? temp.Last().EcsY
                            : temp.Skip(resultQualityMaxY.Index - 1).Take(3).Average(t => t.EcsY);

                    resultQualityMinX.EcsY = resultQualityMaxY.EcsY;
                    resultQualityMinX.QualityY = resultQualityMaxY.QualityY;
                    resultQualityMinX.EcsErrorValue = resultQualityMinX.EcsX - resultQualityMinX.EcsY;

                    resultQualityMaxY.EcsX = resultQualityMinX.EcsX;
                    resultQualityMaxY.QualityX = resultQualityMinX.QualityX;
                    resultQualityMaxY.EcsErrorValue = resultQualityMaxY.EcsX - resultQualityMaxY.EcsY;

                    var resultQualityAverage = isFindEcsX ? resultQualityMinX.Clone() : resultQualityMaxY.Clone();
                    resultQualityAverage.EcsX = averageEcsX;
                    resultQualityAverage.EcsY = averageEcsY;
                    resultQualityAverage.EcsErrorValue = resultQualityAverage.EcsX - resultQualityAverage.EcsY;

                    listDto.Add(isFindEcsX ? resultQualityMinX : resultQualityMaxY); // 取最小值
                    listDto.Add(resultQualityAverage); // 取均值

                    return (true, listDto);
                }
                catch (Exception e)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Get Quality Best Result Error! {e.Message}"), HtmlLogUniqueId.LoggingHtml());
                    return (false, listDto);
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Get Best Ecs Item Failed! {e.Message}"), HtmlLogUniqueId.LoggingHtml());
            return ([], false);
        }
    }

    private (bool isSuccess, GenerateChirpAodWaveParamDto chirpAodWaveParamDto, IReadOnlyList<ChirpAODWaveformResult> ChirpAodWaveResultList) GenerateAndSendChirpAodWave(double rateRange)
    {
        try
        {
            var bandWidth = ChirpAodFindEcsYDto.SoundPackageLength * rateRange;
            var chirpAodWaveProfileDto = ChirpAodFindEcsYDto.Clone();
            chirpAodWaveProfileDto.BandWidth = bandWidth;

            var ret = calibrationLaserService.GenerateChirpAodWaveList(Cache.OpticsMagTypeEnum, chirpAodWaveProfileDto);
            if (ret.IsSuccess == false)
                throw new CugaException(ret.ErrorMsg);

            var chirpAodWaveList = ret.Anything;
            LaserViewModel.SetChirpAODWaveProfileList(chirpAodWaveList);

            var chirpAodWaveResultList = AODWaveformResultFactory.CreateChirpList(chirpAodWaveList);
            return (true, chirpAodWaveProfileDto, chirpAodWaveResultList);
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Send Chirp Aod Wave Error! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return (false, new(), []);
        }
    }

    private bool GetXyQuality(ref LaserXYAstigmatismCalibrationItemDto xyAstigmatismItemDto)
    {
        try
        {
            // 防止移动后明场af模式打开
            AfViewModel.ToggleBrightFieldEnable(false);
            AfViewModel.SetSensorEcsValue(xyAstigmatismItemDto.EcsX);

            if (xyAstigmatismItemDto.Index == 0) Thread.Sleep(1000);

            var list = LaserViewModel.GetDarkFieldLineScanImageList(
                Cache.CalChipSiteModelEnum,
                Cache.FindPosition,
                800,
                Cache.OpticsMagTypeEnum,
                StageSpeedEnum.Low,
                8,
                StageCoordinateSystemEnum.Dark,
                Cache.CIBConfiguration,
                (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                true,
                isAutoFocus: false,
                isRtfc: false);

            var channel1DarkFieldImageDto = list.Single(t => t.ChannelId == 1);
            var channel2DarkFieldImageDto = list.Single(t => t.ChannelId == 2);
            var channel3DarkFieldImageDto = list.Single(t => t.ChannelId == 3);


            var size = channel3DarkFieldImageDto.Image.GetSize();
            var roi = new Rect(0, 0, size.Width, size.Height);

            var (ch3XQuality, _) = CalibrationAlgorithmService.GetXyQuality(channel3DarkFieldImageDto.Image);
            var (_, ch3YQuality) = CalibrationAlgorithmService.ModulationTransferFunction(channel3DarkFieldImageDto.Image, roi);

            var qualityX = ch3XQuality;
            var qualityY = ch3YQuality;

            xyAstigmatismItemDto.FilePath =
                $"{ImageFileDirectory}\\ECS({xyAstigmatismItemDto.EcsX})_FrequencyChangeRate({xyAstigmatismItemDto.FrequencyChangeRate})_Guid({HtmlLogUniqueId}).jpg";
            xyAstigmatismItemDto.OriginFilePath = CalibrationConstantsHelper.ImagePathToRawImagePath(xyAstigmatismItemDto.FilePath);
            xyAstigmatismItemDto.QualityX = qualityX;
            xyAstigmatismItemDto.QualityY = qualityY;

            FileHelper.Save(channel3DarkFieldImageDto.Bytes, xyAstigmatismItemDto.OriginFilePath);
            channel3DarkFieldImageDto.Image.Save(xyAstigmatismItemDto.FilePath);

            var ch1FilePath =
                $"{ImageFileDirectory}\\Ch1_ECS({xyAstigmatismItemDto.EcsX})__FrequencyChangeRate({xyAstigmatismItemDto.FrequencyChangeRate})_Guid({HtmlLogUniqueId}).jpg";
            var ch2FilePath =
                $"{ImageFileDirectory}\\Ch2_ECS({xyAstigmatismItemDto.EcsX})__FrequencyChangeRate({xyAstigmatismItemDto.FrequencyChangeRate})_Guid({HtmlLogUniqueId}).jpg";
            channel1DarkFieldImageDto.Image.Save(ch1FilePath);
            channel2DarkFieldImageDto.Image.Save(ch2FilePath);

            HOperatorSet.WriteObject(channel1DarkFieldImageDto.Image, ch1FilePath.Replace(".jpg", ".hobj"));
            HOperatorSet.WriteObject(channel2DarkFieldImageDto.Image, ch2FilePath.Replace(".jpg", ".hobj"));

            Logger.LogHtmlInformation($"Get Quality OK, Time: {xyAstigmatismItemDto.Index}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                FrequenceIncrement = xyAstigmatismItemDto.FrequencyChangeRate,
                EcsValue = xyAstigmatismItemDto.EcsX,
                ImageQualityX = xyAstigmatismItemDto.QualityX,
                ImageQualityY = xyAstigmatismItemDto.QualityY,
                HtmlTab = new HtmlTab(new
                {
                    ImageCh3 = new HtmlImage(xyAstigmatismItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    ImageCh2 = new HtmlImage(ch2FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    ImageCh1 = new HtmlImage(ch1FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }
        catch (Exception e)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Get Xy Quality failed! {e.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    #endregion 算法

    #region 日志

    /// <summary>
    /// 保存结果csv文件
    /// </summary>
    /// <param name="resultList"></param>
    /// <param name="filepath">保存路径</param>
    private static void SaveIdealCsv(List<LaserXYAstigmatismCalibrationItemDto> resultList, string filepath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filepath);
        FileHelper.DeleteFileIfExists(filepath);

        var sb = new StringBuilder();
        sb.Append("RateChange,RateChange-Reciprocal,EcsX,EcsY,EcsErrorValue,QualityY");
        sb.AppendLine();
        foreach (var item in resultList)
        {
            sb.Append($"{item.FrequencyChangeRate},{1 / item.FrequencyChangeRate},{item.EcsX},{item.EcsY},{item.EcsErrorValue},{item.QualityY}");
            sb.AppendLine();
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    #endregion 日志
}