using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
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
using Core.Utilities;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithms.Halcon;
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
public sealed partial class LaserXYAstigmatismCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public string ChirpFileDirectory => Path.Combine(AppHomeDirectory, "Chirp", nameof(LaserXYAstigmatismCalibrationViewModel), DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select Mag", DefaultIsNextEnable = true },
        new() { StepName = "Select a lens and a location" },
        new() { StepName = "Find EcsX With Chirp AOD Default Wave", DefaultIsNextEnable = true },
        new() { StepName = "Set Params before action And Get Optimum RateRange" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserXYAstigmatismCalibrationItemDto> _laserXyAstigmatismItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<Point> _ecsList = [];

    /// <summary>
    /// 校准界面显示当前的ChirpAod波形
    /// </summary>
    [ObservableProperty]
    private DarkFieldChirpAodWaveDto _chirpAodDefaultDto = new();

    [ObservableProperty]
    private DarkFieldChirpAodWaveDto _chirpAodFindEcsYDto = new();

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

    [ObservableProperty]
    private DarkFieldChirpAodWaveDto _chirpAodReviewSelectDto = new();

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserXYAstigmatismCalibrationCache _cache = new();

    [ObservableProperty]
    private LaserXYAstigmatismCalibrationItemDto[] _calibrations = [];

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

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserXYAstigmatismCalibrationCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserXYAstigmatismCalibrationItemDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation.LensCode == -1) Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];

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

        if (Cache.FindPosition.ToOriginLength >= Cache.ChuckRadius)
        {
            DialogWindowProvider.ShowDialog("The Bright Field Cache Position Out Of The Wafer!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

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
                if (Cache.FindPosition.ToOriginLength >= Cache.ChuckRadius)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header2, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                // 更新界面用
                try
                {
                    ChirpAodDefaultDto = LaserViewModel.ReadChirpAodByCustomFile(Cache.GetChirpAodFilePath());
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Read default chirp aod failed!");
                }

                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;

            case 3:
                // 无校准记录时，find ecsY界面参数继承上一步设置find ecsX的参数
                var temp = Calibrations.Where(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).ToList();
                if (temp.Count == 0 && SelectedLaserXyAstigmatismItemDto is null)
                {
                    Cache.SetInitialChirpAodWaveParams(ChirpAodDefaultDto.CenterFrequency, ChirpAodDefaultDto.SoundPackageLength);
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
    private void ChangeChirpAodFile()
    {
        var dialog = DialogWindowProvider.TryShowSelectFilePathDialog(".txt", out var filePath);
        if (dialog == false) return;
        Cache.SetChirpAodFilePath(filePath);
        ChirpAodDefaultDto = LaserViewModel.ReadChirpAodByCustomFile(filePath);
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
            Cache.SetBrightFieldPosition();

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
                var detectImageDirectory = ImageFileDirectory;
                ClearCalibrationTemp();

                var (ecsUpperLimitX, ecsLowerLimitX, ecsLimitIntervalX, ecsXInitial) = Cache.GetEcsXParams();
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);

                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.OpticsMagTypeEnum, 0.8);

                // 默认波形
                var defaultChirpAodWaveFilePath = Cache.GetChirpAodFilePath();
                var copyFilePath = FileHelper.GetEnsureLongPathSupport(defaultChirpAodWaveFilePath.Replace(Directory.GetParent(defaultChirpAodWaveFilePath).FullName, ChirpFileDirectory));
                DirectoryHelper.CreateDirectoryIfNotExists(ChirpFileDirectory);
                if (File.Exists(copyFilePath) == false)
                    File.Copy(defaultChirpAodWaveFilePath, copyFilePath);

                ChirpAodDefaultDto = LaserViewModel.ReadChirpAodByCustomFile(copyFilePath);
                ChirpAodDefaultDto.ZeroNum = Cache.GetChirpAodDefaultWaveZeroNum();

                if (ecsLowerLimitX < 0 || ecsUpperLimitX < 0 || ecsLimitIntervalX <= 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Upper>0 And Focs Lower>0 and Focus Interval > 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                ChirpAodDefaultDto.ZeroNum = Cache.GetChirpAodDefaultWaveZeroNum();
                var (_, isSuccess) = SendChirpAodWave(ChirpAodDefaultDto, ChirpAodDefaultDto.RateChange);
                (var currentResultList, isSuccess) = GetResultDtoByCurrentChirpAodRateChange(ChirpAodDefaultDto, isFindEcsX: true, isAutoSlider: true, cancellationToken: cancellationToken);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Optinum Ecs X Failed."), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                Logger.LogHtmlInformation("Find EcsX Param OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    ChirpAodDefaultDto.SoundPackageLength,
                    DefaultRateChange = ChirpAodDefaultDto.RateChange,
                    ChirpAodDefaultDto.ZeroNum,
                    RegisterNum = ChirpAodDefaultDto.RegNum,
                    CenterFrequence = ChirpAodDefaultDto.CenterFrequency,
                    Cache.SampleRate,
                    InitialFindEcsX = ecsXInitial,
                    EcsXUpperLimit = ecsUpperLimitX,
                    EcsXLowerLimit = ecsLowerLimitX,
                    EcsXLimitInterval = ecsLimitIntervalX,
                    ImageFileDirectory = detectImageDirectory,
                    HtmlTab = new HtmlTab(new
                    {
                        Image = new HtmlImage(currentResultList[1].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog("XY Astigmatism Calibration Finished!", DialogButtonsEnum.OK,
                    isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error: Find Optimum Ecs X Failed.");
                return false;
            }
            finally
            {
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
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
                var (frequenceIncreaseCount, frequenceIncreaseInterval) = Cache.GetFrequenceParams();
                var setErrorThreshold = Cache.GetThresholdParams();
                var (initialCenterFrequence, initialSoundPackageLength, initialZeroNum) = Cache.GetInitialChirpAodWaveParams();

                if (ecsLowerLimitY < 0 || ecsUpperLimitY < 0 || ecsLimitIntervalY <= 0 || frequenceIncreaseCount < 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Upper>0 And Focs Lower>0 and Focus Interval > 0 and  FrequenceIncreaseCount> 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                Logger.LogHtmlInformation("Initial Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    InitialSoundPackageLength = initialSoundPackageLength,
                    InitialZeroNum = initialZeroNum,
                    InitialCenterFrequence = initialCenterFrequence,
                    InitialFindEcsY = ecsYInitial,
                    EcsYUpperLimit = ecsUpperLimitY,
                    EcsYLowerLimit = ecsLowerLimitY,
                    EcsYLimitInterval = ecsLimitIntervalY,
                    FrequenceIncreaseCount = frequenceIncreaseCount,
                    FrequenceIncreaseInterval = frequenceIncreaseInterval,
                    ErrorThreshold = setErrorThreshold,
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                ChirpAodFindEcsYDto = ChirpAodDefaultDto.Clone();
                ChirpAodFindEcsYDto.SampleRate = Cache.SampleRate;
                ChirpAodFindEcsYDto.CenterFrequency = initialCenterFrequence;
                ChirpAodFindEcsYDto.SoundPackageLength = initialSoundPackageLength;
                ChirpAodFindEcsYDto.ZeroNum = initialZeroNum;

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
            }
            catch
            {
                return false;
            }
            finally
            {
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
            }
        });

        bool GetOptimumFrequencyChangeRate(bool isPositive = true)
        {
            var (initialCenterFrequence, initialSoundPackageLength, initialZeroNum) = Cache.GetInitialChirpAodWaveParams();
            var (frequenceIncreaseCount, frequenceIncreaseInterval) = Cache.GetFrequenceParams();
            var setErrorThreshold = Cache.GetThresholdParams();
            var startRateChange = Cache.GetDefaultChirpAodFrequence();
            var isSuccess = true;

            foreach (var (index, frequence) in Enumerable.Range(0, frequenceIncreaseCount)
                         .Select(t => isPositive ? startRateChange + t * frequenceIncreaseInterval : startRateChange - t * frequenceIncreaseInterval)
                         .Select((d, i) => (i, d)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                (var chirpAodChangeDto, isSuccess) = SendChirpAodWave(ChirpAodFindEcsYDto, frequence);
                (var currentResultList, isSuccess) = GetResultDtoByCurrentChirpAodRateChange(chirpAodChangeDto, cancellationToken: cancellationToken);
                if (!isSuccess) continue; // 文件缺失跳过一次
                var resultList = currentResultList.Select(t =>
                {
                    var temp = t.Clone();
                    temp.FrequenceIncrease = frequence;
                    temp.Index = index;

                    return temp;
                }).ToList();
                SynchronizationContextProvider.Send(() => { LaserXyAstigmatismItemDtoList.Add(resultList[0]); });
            }

            // 从结果集合中截掉方向判断的item，用来生成ecsError-changeRate曲线,获得resultItem
            var startIndex = LaserXyAstigmatismItemDtoList.Select((dto, index) => (dto, index))
                .Where(t => t.dto.Index == 0)
                .Select(t => t.index)
                .ToArray();
            var plotList = startIndex.Length > 1
                ? LaserXyAstigmatismItemDtoList.Skip(startIndex.Last()).ToList()
                : [.. LaserXyAstigmatismItemDtoList.Select(t => t)];
            // 更新控件曲线
            foreach (var item in plotList.OrderBy(s => s.FrequenceIncrease))
            {
                SynchronizationContextProvider.Send(() => { EcsList = [.. EcsList, new Point(item.FrequenceIncrease, item.EcsErrorValue)]; });
            }

            var csvPath = $"{CsvFileDirectory}\\Calibration\\SoundPackage{initialSoundPackageLength}mm_BandWith{ChirpAodFindEcsYDto.BandWidth}_CenterFrequence{initialCenterFrequence}_ZeroNum{initialZeroNum}\\Error_Guid({HtmlLogUniqueId}).csv";
            SaveIdealCsv(plotList, csvPath);

            // result
            var findItemByNotFit = plotList.OrderBy(t => Math.Abs(t.EcsErrorValue)).First();
            var (fitFunction, coefficient) = GetFitRelationalfunction(plotList);
            var result = Math.Abs(findItemByNotFit.EcsErrorValue) < setErrorThreshold;

            var circleCount = 0;
            var list_ecsValue = new List<bool>();
            var list_iterationResult = new List<LaserXYAstigmatismCalibrationItemDto>();
            if (result)
                SelectedLaserXyAstigmatismItemDto = findItemByNotFit;
            else
            {
                Logger.LogHtmlInformation("Iteration", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                GetOptinumRateChange(Math.Abs(findItemByNotFit.EcsErrorValue)); // 结果迭代
                // 结果dto赋值，ecsXY error<阈值时校准成功
                SelectedLaserXyAstigmatismItemDto = list_iterationResult.Minima(t => Math.Abs(t.EcsErrorValue)).First();
                result = Math.Abs(SelectedLaserXyAstigmatismItemDto!.EcsErrorValue) < setErrorThreshold;
            }

            SelectedLaserXyAstigmatismItemDto.IsCalibrated = result;
            ResultLaserXyAstigmatismItemDto = SelectedLaserXyAstigmatismItemDto.Clone();

            // log
            csvPath = $"{CsvFileDirectory}\\Calibration_Iteration\\SoundPackage{initialSoundPackageLength}mm_BandWith{ChirpAodFindEcsYDto.BandWidth}_CenterFrequence{initialCenterFrequence}_ZeroNum{initialZeroNum}\\Error_Guid({HtmlLogUniqueId}).csv";
            SaveIdealCsv(list_iterationResult, csvPath);
            var plotListTitleEf = UpdateResultPlotMarkDown(plotList, fitFunction);
            Logger.LogHtmlInformation("Find EcsY OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                OpticsMagType = ResultLaserXyAstigmatismItemDto.OpticsMagTypeEnum,
                SelectedLaserXyAstigmatismItemDto.ChirpAodWaveFilePath,
                OptimumEcsXByDefaultWave = Cache.GetInitialEcsX(),
                FrequenceRateChangeIdea = findItemByNotFit.FrequenceIncrease,
                FrequenceRateChangeResult = ResultLaserXyAstigmatismItemDto.FrequenceIncrease,
                OriEcsX = findItemByNotFit.EcsX,
                OriEcsY = findItemByNotFit.EcsY,
                ResultEcsX = ResultLaserXyAstigmatismItemDto.EcsX,
                ResultEcsY = ResultLaserXyAstigmatismItemDto.EcsY,
                OriEcsError = findItemByNotFit.EcsErrorValue,
                ResultError = ResultLaserXyAstigmatismItemDto.EcsErrorValue,
                EcsError_Frequence_List = new HtmlPlot2DLinesChart([
                    (plotListTitleEf[0][0].title, plotListTitleEf[0][0].points
                        .Select(t => new Point(t.Item1, t.Item2))
                        .ToArray())
                ], "F-EcsError"),
                Fderivatives_EcsY_List = new HtmlPlot2DLinesChart([
                    (plotListTitleEf[1][0].title, plotListTitleEf[1][0].points
                        .Select(t => new Point(t.Item1, t.Item2))
                        .ToArray()),
                    (plotListTitleEf[1][1].title, plotListTitleEf[1][1].points
                        .Select(t => new Point(t.Item1, t.Item2))
                        .ToArray()),
                    (plotListTitleEf[1][2].title, plotListTitleEf[1][2].points
                        .Select(t => new Point(t.Item1, t.Item2))
                        .ToArray())
                ], "FitFunction"),
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(ResultLaserXyAstigmatismItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return result;

            //迭代，根据拟合一次函数，首次输入ecsX，得到F0下发，后续迭代代入deltaEcs，频率变化率根据斜率和符号增加/减少deltaRateChange，得到新的F下发
            void GetOptinumRateChange(double previousEcsError, double previousEcsY = 0, double previousRateChange = 0, int repeatNum = 5)
            {
                var optinumEcsX = Cache.GetInitialEcsX();
                var findItemResult = new LaserXYAstigmatismCalibrationItemDto();
                // 首次输入ecsX，后续迭代输入deltaEcs
                var deltaEcs = previousEcsY == 0 ? optinumEcsX : previousEcsY - optinumEcsX;
                var rateChange = Math.Round(1.0 / (coefficient[0] * deltaEcs + coefficient[1]), 2) + previousRateChange;
                if (double.IsNaN(rateChange) || rateChange == 0)
                {
                    DialogWindowProvider.ShowDialog("Get Rate Change By Relational function Failed! The Points is not enough!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(rateChange));
                }

                // 下发f0
                (var chirpAodF0Dto, isSuccess) = SendChirpAodWave(ChirpAodFindEcsYDto, rateChange);
                (var findItemResultF0, isSuccess) = GetResultDtoByCurrentChirpAodRateChange(chirpAodF0Dto, cancellationToken: cancellationToken);
                if (!isSuccess)
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(findItemResultF0));

                findItemResult = findItemResultF0[0];
                list_iterationResult.Add(findItemResult);
                circleCount++;
                var newDelta = Math.Abs(findItemResult.EcsY - optinumEcsX);
                list_ecsValue.Add(newDelta < previousEcsError);

                Logger.LogHtmlInformation("Iteration Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    optinumEcsX,
                    deltaEcs,
                    rateChange,
                    newDelta,
                    previousEcsError
                }), HtmlLogUniqueId.LoggingHtml());

                if (EnumerableHelper.HasConsecutiveFalse(list_ecsValue, 3) || circleCount > repeatNum || newDelta <= setErrorThreshold)
                    return;
                GetOptinumRateChange(newDelta, findItemResult.EcsY, rateChange);
            }
        }
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

                Cache.GetFindPosition();
                var selectItemFrequenceIncrement = SelectReviewItemDto.FrequenceIncrease;
                var (ecsLimitUpperY, ecsLimitLowerY, ecsLimitIntervalY, ecsYInitial) = Cache.GetEcsYParams();
                var (frequenceIncreaseCount, frequenceIncreaseInterval) = Cache.GetFrequenceParams();
                var (initialCenterFrequence, initialSoundPackageLength, initialZeroNum) = Cache.GetInitialChirpAodWaveParams();
                var setErrorThreshold = Cache.GetReviewThresholdParams();

                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);

                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.OpticsMagTypeEnum, 0.8);

                var ChirpAodDefaultDto = LaserViewModel.ReadChirpAodByCustomFile(Cache.GetChirpAodFilePath());

                ChirpAodReviewSelectDto = new DarkFieldChirpAodWaveDto
                {
                    IncrementChirpAodFilePath = ChirpAodDefaultDto.IncrementChirpAodFilePath,
                    CenterFrequency = initialCenterFrequence,
                    SoundPackageLength = initialSoundPackageLength,
                    ZeroNum = initialZeroNum,
                    RateChange = selectItemFrequenceIncrement
                };

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    SoundPackageLength = initialSoundPackageLength,
                    CenterFrequence = initialCenterFrequence,
                    ZeroNum = initialZeroNum,
                    ChirpAodReviewSelectDto.RegNum,
                    OptimumRateChange = selectItemFrequenceIncrement,
                    FindEcsYRangeAxis = ecsYInitial,
                    EcsYUpperLimit = ecsLimitUpperY,
                    EcsYLowerLimit = ecsLimitLowerY,
                    EcsYLimitInterval = ecsLimitIntervalY,
                    FrequenceIncreaseCount = frequenceIncreaseCount,
                    FrequenceIncreaseInterval = frequenceIncreaseInterval,
                    ErrorThreshold = setErrorThreshold,
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                Thread.Sleep(1000);

                //下发当前mag的prescan默认波形
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.OpticsMagTypeEnum, 0.8);

                // 读取校准缓存记录位置的波形文件并下发采图
                var (_, isSuccess) = SendChirpAodWave(ChirpAodFindEcsYDto, selectItemFrequenceIncrement);
                (var currentResultList, isSuccess) = GetResultDtoByCurrentChirpAodRateChange(ChirpAodFindEcsYDto, cancellationToken: cancellationToken);
                if (isSuccess == false)
                    return false;

                var EcsError = currentResultList[0].EcsErrorValue;
                var result = Math.Abs(EcsError) < setErrorThreshold;
                ResultReviewItemDto = currentResultList[0].Clone();

                Logger.LogHtmlInformation($"Vefify {(isSuccess ? "Success" : "Error")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OpticsMagType = Cache.OpticsMagTypeEnum,
                    Cache.MicroscopeLensInformation.LensName,
                    CurrentFrequenceRateChange = ResultReviewItemDto.FrequenceIncrease,
                    OldEcsError = SelectReviewItemDto.EcsErrorValue,
                    NewEcsError = EcsError,
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

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New EcsError: ({EcsError:f3}) Old EcsError: ({SelectReviewItemDto.EcsErrorValue:f3}) FrequenceIncrement: ({selectItemFrequenceIncrement:f3})", DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }).ConfigureAwait(false);
        }
        finally
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
        }
    }

    /// <summary>
    /// 类型转换，缓存
    /// </summary>
    /// <param name="itemDto"></param>
    /// <param name="isSave"></param>
    /// <returns></returns>
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

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

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
        EcsList = [];
        SelectedLaserXyAstigmatismItemDto = null;
    }

    #endregion 校准

    #region 算法

    private (bool, List<LaserXYAstigmatismCalibrationItemDto>) GetEcsIncrementXyQualityDtoList(double frequence, List<LaserXYAstigmatismCalibrationItemDto> tempItemDtoList, CancellationToken cancellationToken)
    {
        var resultDtoList = tempItemDtoList.Select(s => s.Clone()).ToList();
        foreach (var findFocalItem in resultDtoList.OrderBy(t => t.Index))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var laserXyAstigmatismCalibrationItemDto = findFocalItem;
            laserXyAstigmatismCalibrationItemDto.FrequenceIncrease = frequence;
            if (GetXyQuality(ref laserXyAstigmatismCalibrationItemDto) == false)
            {
                return (false, resultDtoList);
            }
        }

        return (true, resultDtoList);
    }

    private bool GetXyQuality(ref LaserXYAstigmatismCalibrationItemDto laserXyAstigmatismCalibrationItemDto)
    {
        // 防止移动后明场af模式打开
        AfViewModel.ToggleBrightFieldEnable(false);
        AfViewModel.SetSensorEcsValue(laserXyAstigmatismCalibrationItemDto.EcsX);

        if (laserXyAstigmatismCalibrationItemDto.Index == 0) Thread.Sleep(1000);

        var list = LaserViewModel.GetDarkFieldLineScanImageList(
            CalChipSiteModelEnum.ChuckModel,
            Cache.GetFindPosition(),
            800,
            Cache.OpticsMagTypeEnum,
            StageSpeedEnum.Low,
            8,
            StageCoordinateSystemEnum.Dark,
            Cache.CIBConfiguration,
            (true, null),
            false,
            isAutoFocus: false,
            isRtfc: false);

        var channel1DarkFieldImageDto = list.Single(t => t.ChannelId == 1);
        var channel2DarkFieldImageDto = list.Single(t => t.ChannelId == 2);
        var channel3DarkFieldImageDto = list.Single(t => t.ChannelId == 3);

        var size = HalconHelper.GetSize(channel3DarkFieldImageDto.Image);
        var roi = new Rect(0, 0, size.Width, size.Height);

        var (ch3XQuality, _) = CalibrationAlgorithmService.GetXyQuality(channel3DarkFieldImageDto.Image);
        var (_, ch3YQuality) = CalibrationAlgorithmService.ModulationTransferFunction(channel2DarkFieldImageDto.Image, roi);

        var qualityX = ch3XQuality;
        var qualityY = ch3YQuality;

        laserXyAstigmatismCalibrationItemDto.FilePath =
            $"{ImageFileDirectory}\\ECS({laserXyAstigmatismCalibrationItemDto.EcsX})_FrequenceIncrease({laserXyAstigmatismCalibrationItemDto.FrequenceIncrease})_Guid({HtmlLogUniqueId}).jpg";
        laserXyAstigmatismCalibrationItemDto.OriginFilePath = CalibrationConstantsHelper.ImagePathToRawImagePath(laserXyAstigmatismCalibrationItemDto.FilePath);
        laserXyAstigmatismCalibrationItemDto.QualityX = qualityX;
        laserXyAstigmatismCalibrationItemDto.QualityY = qualityY;

        FileHelper.Save(channel3DarkFieldImageDto.Bytes, laserXyAstigmatismCalibrationItemDto.OriginFilePath);
        HalconHelper.Save(channel3DarkFieldImageDto.Image, laserXyAstigmatismCalibrationItemDto.FilePath);

        var ch1FilePath =
            $"{ImageFileDirectory}\\Ch1_ECS({laserXyAstigmatismCalibrationItemDto.EcsX})_FrequenceIncrease({laserXyAstigmatismCalibrationItemDto.FrequenceIncrease})_Guid({HtmlLogUniqueId}).jpg";
        var ch2FilePath =
            $"{ImageFileDirectory}\\Ch2_ECS({laserXyAstigmatismCalibrationItemDto.EcsX})_FrequenceIncrease({laserXyAstigmatismCalibrationItemDto.FrequenceIncrease})_Guid({HtmlLogUniqueId}).jpg";
        HalconHelper.Save(channel1DarkFieldImageDto.Image, ch1FilePath);
        HalconHelper.Save(channel2DarkFieldImageDto.Image, ch2FilePath);

        HOperatorSet.WriteObject(channel1DarkFieldImageDto.Image, ch1FilePath.Replace(".jpg", ".hobj"));
        HOperatorSet.WriteObject(channel2DarkFieldImageDto.Image, ch2FilePath.Replace(".jpg", ".hobj"));

        Logger.LogHtmlInformation($"Get Quality OK, Time: {laserXyAstigmatismCalibrationItemDto.Index}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            FrequenceIncrement = laserXyAstigmatismCalibrationItemDto.FrequenceIncrease,
            EcsValue = laserXyAstigmatismCalibrationItemDto.EcsX,
            ImageQualityX = laserXyAstigmatismCalibrationItemDto.QualityX,
            ImageQualityY = laserXyAstigmatismCalibrationItemDto.QualityY,
            HtmlTab = new HtmlTab(new
            {
                ImageCh1 = new HtmlImage(ch1FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                ImageCh2 = new HtmlImage(ch2FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                ImageCh3 = new HtmlImage(laserXyAstigmatismCalibrationItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }

    /// <summary>
    /// 从一系列高度的集合获得结果dtoItem
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="ecsIncrementXyQualityDtoList"></param>
    /// <returns>list[0]最小值结果，list[1]均值结果</returns>
    private (bool, List<LaserXYAstigmatismCalibrationItemDto>) GetEcsIncrementQualityDtoResultItem(List<LaserXYAstigmatismCalibrationItemDto> ecsIncrementXyQualityDtoList, bool isFindEcsX)
    {
        var list_dto = new List<LaserXYAstigmatismCalibrationItemDto>();
        try
        {
            // xy得分大于100的对象参与结果运算（防止超出景深极端值的干扰）
            var temp = ecsIncrementXyQualityDtoList.Select(s => s.Clone()).Where(t => t.QualityX > 100).ToList();

            // 筛选X、Y得分最低的对象，找出ecs差值最小的两个对象，把y得分最高的ecs值和得分值赋值给x，输出ecsError（根据验证，梯度算法的出来的趋势，分数越小越清晰）
            var qualityXMaxList = temp.Minima(s => s.QualityX).ToList();
            var qualityYMaxList = temp.Maxima(s => s.QualityY).ToList();
            double res = 1000;
            (int XIndex, int YIndex) index = (0, 0);
            foreach (var itemY in qualityYMaxList)
            {
                foreach (var itemX in qualityXMaxList)
                {
                    var r = Math.Abs(itemX.EcsX - itemY.EcsX);
                    if (r > res) continue;

                    res = r;
                    index = (itemX.Index, itemY.Index);
                }
            }

            var resultQualityMinX = qualityXMaxList.Single(s => s.Index == index.XIndex);
            var resultQualityMinY = qualityYMaxList.Single(s => s.Index == index.YIndex);

            var averageEcsX = resultQualityMinX.Index == 0 || temp.Count < 3
                ? temp.Skip(0).Take(temp.Count < 3 ? temp.Count : 3).Average(t => t.EcsX)
                : temp.Count - resultQualityMinX.Index == 1
                    ? temp.Last().EcsX
                    : temp.Skip(resultQualityMinX.Index - 1).Take(3).Average(t => t.EcsX);

            var averageEcsY = resultQualityMinY.Index == 0 || temp.Count < 3
                ? temp.Skip(0).Take(temp.Count < 3 ? temp.Count : 3).Average(t => t.EcsY)
                : temp.Count - resultQualityMinY.Index == 1
                    ? temp.Last().EcsY
                    : temp.Skip(resultQualityMinY.Index - 1).Take(3).Average(t => t.EcsY);

            resultQualityMinX.EcsY = resultQualityMinY.EcsY;
            resultQualityMinX.QualityY = resultQualityMinY.QualityY;
            resultQualityMinX.EcsErrorValue = resultQualityMinX.EcsX - resultQualityMinX.EcsY;

            resultQualityMinY.EcsX = resultQualityMinX.EcsX;
            resultQualityMinY.QualityX = resultQualityMinX.QualityX;
            resultQualityMinY.EcsErrorValue = resultQualityMinY.EcsX - resultQualityMinY.EcsY;

            var resultQualityAverage = isFindEcsX ? resultQualityMinX.Clone() : resultQualityMinY.Clone();
            resultQualityAverage.EcsX = averageEcsX;
            resultQualityAverage.EcsY = averageEcsY;
            resultQualityAverage.EcsErrorValue = resultQualityAverage.EcsX - resultQualityAverage.EcsY;

            list_dto.Add(isFindEcsX ? resultQualityMinX : resultQualityMinY); // 取最小值
            list_dto.Add(resultQualityAverage); // 取均值

            return (true, list_dto);
        }
        catch
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Get Quality Max Error! Select DtoItems Data is Empty."), HtmlLogUniqueId.LoggingHtml());
            return (false, list_dto);
        }
    }

    #endregion 算法

    #region 校准业务

    /// <summary>
    /// 找一段高度范围内的Ecs作为新的轴心，范围上下限以新的轴心滑动
    /// </summary>
    /// <param name="darkFieldChirpAodWaveDto"></param>
    /// <param name="isFindEcsX"></param>
    /// <param name="isAutoSlider"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="guid"></param>
    /// <param name="ecsInitial"></param>
    /// <param name="direction">0：找ecsX，1：找ecsY </param>
    /// <returns></returns>
    private (List<LaserXYAstigmatismCalibrationItemDto>, bool) GetResultDtoByCurrentChirpAodRateChange(DarkFieldChirpAodWaveDto darkFieldChirpAodWaveDto, bool isFindEcsX = false, bool isAutoSlider = false, CancellationToken cancellationToken = default)
    {
        var (ecsLimitUpper, ecsLimitLower, ecsLimitInterval, ecsInitial) = isFindEcsX ? Cache.GetEcsXParams() : Cache.GetEcsYParams();
        Logger.LogHtmlInformation($"Get Result With RateChange: {darkFieldChirpAodWaveDto.RateChange}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            ecsLimitUpper,
            ecsLimitLower,
            ecsLimitInterval,
            ecsInitial,
            darkFieldChirpAodWaveDto.RateChange,
            darkFieldChirpAodWaveDto.IncrementChirpAodFilePath
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
                FrequenceIncrease = ChirpAodDefaultDto.RateChange,
                EcsX = ecsValueTemp,
                EcsY = ecsValueTemp,
                EcsErrorValue = 0,
                QualityX = 0,
                QualityY = 0,
                FilePath = ImageFileDirectory,
                OriginFilePath = ImageFileDirectory,
                ChirpAodWaveFilePath = darkFieldChirpAodWaveDto.IncrementChirpAodFilePath
            });
        }

        cancellationToken.ThrowIfCancellationRequested();
        // ECS执行一轮采图
        var (_, dtoTempListCircle) = GetEcsIncrementXyQualityDtoList(darkFieldChirpAodWaveDto.RateChange, dtoTempList, cancellationToken);
        var (isSuccess, currentResultItem) = GetEcsIncrementQualityDtoResultItem(dtoTempListCircle, isFindEcsX);
        // Log更新结果波形图
        UpdatePlotMarkDown(dtoTempListCircle, currentResultItem);

        if (isSuccess)
        {
            var currentEcs = isFindEcsX ? currentResultItem[1].EcsX : currentResultItem[0].EcsY;
            var currentQuality = isFindEcsX ? currentResultItem[1].QualityX : currentResultItem[0].QualityY;
            var ecsError = Math.Abs(currentEcs - ecsInitial);
            Logger.LogHtmlInformation("Get Result OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                OpticsMagType = Cache.OpticsMagTypeEnum,
                CurrentCircleInitialEcs = ecsInitial,
                FindInitialEcs = currentEcs,
                InitialQualityMax = currentQuality,
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(currentResultItem[0].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            if (isAutoSlider)
            {
                _ = isFindEcsX ? Cache.SetEcsXParams(currentEcs) : Cache.SetEcsYParams(currentEcs);
                // 执行一轮找ecs之后，判断最佳Ecs距离intialEcs是否小于量程的一半，大于时继续迭代
                if (ecsError > (ecsLimitLower + ecsLimitUpper) / 2)
                {
                    DialogWindowProvider.TryShowDialog($"Find Ecs {(isFindEcsX ? "X" : "Y")} value far from initial axis more, EcsError: ({ecsError})," +
                                                       $"Do you want to repeat once use the current ecs value as the new axis?"
                        , out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum != DialogResultEnum.Retry)
                        return (currentResultItem, true);
                    return GetResultDtoByCurrentChirpAodRateChange(darkFieldChirpAodWaveDto, isFindEcsX: isFindEcsX, cancellationToken: cancellationToken);
                }
            }

            return (currentResultItem, true);
        }
        else
            return (currentResultItem, false);
    }

    private (DarkFieldChirpAodWaveDto waveDto, bool isSuccess) SendChirpAodWave(DarkFieldChirpAodWaveDto chirpAodWaveDto, double rateRange, bool isAutoGenerate = true)
    {
        var chirpAodChangeDto = chirpAodWaveDto.Clone();
        chirpAodChangeDto.SampleRate = Cache.SampleRate;

        try
        {
            chirpAodChangeDto = LaserViewModel.GetChirpAodByChangeRateFromFile(chirpAodWaveDto, rateRange);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(@"The specified waveform file does not exist in the folder.Error:{ex}", ex);
            // 生成结果chirpAOD波形
            if (isAutoGenerate)
            {
                var bandWidth = chirpAodWaveDto.SoundPackageLength * rateRange;
                chirpAodChangeDto.BandWidthHigh = chirpAodWaveDto.CenterFrequency + bandWidth / 2d;
                chirpAodChangeDto.BandWidthLow = chirpAodWaveDto.CenterFrequency - bandWidth / 2d;

                (Cache.AodWaveSignal, Cache.AodWaveSignalFourier) = chirpAodChangeDto.GenerateChirpAodWave();

                //chirpAodChangeDto = LaserViewModel.GetChirpAodByChangeRateFromFile(chirpAodChangeDto, rateRange);
            }
            else
                return (chirpAodChangeDto, false);
        }

        chirpAodChangeDto.ZeroNum = chirpAodWaveDto.ZeroNum;
        Cache.SetChirpAodRegNum((short)chirpAodChangeDto.ChirpAodWaveList.Count);
        LaserViewModel.SetChirpAODWaveProfileList([AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode1, chirpAodChangeDto.IncrementChirpAodFilePath)]);

        return (chirpAodChangeDto, true);
    }

    /// <summary>
    /// 拟合EcsY-1/F 一次函数
    /// </summary>
    /// <param name="ecs"></param>
    /// <returns></returns>
    private static (List<(double, double)> fitFunction, double[] coefficient) GetFitRelationalfunction(List<LaserXYAstigmatismCalibrationItemDto> plotList)
    {
        //去坏点
        RemoveAdjacentElementsWithLargeDifference(plotList, 5);
        List<(double, double)> list_fit = [];
        try
        {
            var list_row = plotList.Select(t => t.EcsY).ToList();
            var list_col = plotList.Select(t => 1 / t.FrequenceIncrease).ToList();
            var (k, b, _, _) = PolyFit.Poly1Fit(Vector<double>.Build.DenseOfEnumerable(list_row), Vector<double>.Build.DenseOfEnumerable(list_col));
            for (var i = 0; i < list_row.Count; i++)
            {
                list_fit.Add((list_row[i], k * list_row[i] + b));
            }

            var coefficient = new double[2] { k, b };
            return (list_fit, coefficient);
        }
        catch
        {
            return (list_fit, []);
        }

        static List<LaserXYAstigmatismCalibrationItemDto> RemoveAdjacentElementsWithLargeDifference(List<LaserXYAstigmatismCalibrationItemDto> plotList, double threshold)
        {
            var filteredElements = new List<LaserXYAstigmatismCalibrationItemDto>();
            for (var i = 0; i < plotList.Count - 1; i++)
            {
                var difference = plotList[i + 1].FrequenceIncrease - plotList[i].FrequenceIncrease;
                if (difference <= threshold)
                {
                    filteredElements.Add(plotList[i]);
                }
            }

            // 添加集合中的最后一个元素，因为上面的循环不会包括最后一个元素
            filteredElements.Add(plotList.Last());
            return filteredElements;
        }
    }

    #endregion 校准业务

    #region 日志

    /// <summary>
    /// 更新同音包下关于EcsX/Y-Quality波形图
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="dtoTempListCircle">变高度采图的数据集合</param>
    /// <param name="resultList">变高度采图的结果集合 list[0]→minQuality list[1]→averageQuality</param>
    private void UpdatePlotMarkDown(List<LaserXYAstigmatismCalibrationItemDto> dtoTempListCircle, List<LaserXYAstigmatismCalibrationItemDto> resultList)
    {
        List<(double, double)> plotList1 = [.. dtoTempListCircle.OrderBy(o => o.Index).Select(s => (s.EcsX, s.QualityX))];
        var plotList2 = Cache.EcsPlotList = [.. dtoTempListCircle.OrderBy(o => o.Index).Select(s => (s.EcsX, s.QualityY))];
        List<(string title, List<(double, double)> points)> plotList =
        [
            ("Ecs-QualityX", plotList1),
            ("Ecs-QualityY", plotList2)
        ];
        var chirpAodChangeDto = LaserViewModel.GetChirpAodByChangeRateFromFile(ChirpAodDefaultDto, resultList[0].FrequenceIncrease);

        Logger.LogHtmlInformation($"Get the XY ECS-Quality Wave Success: FrequenceIncrement {resultList[0].FrequenceIncrease}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            OpticsMagType = resultList[0].OpticsMagTypeEnum,
            resultList[0].EcsErrorValue,
            FrequenceIncrement = resultList[0].FrequenceIncrease,
            WaveFilePath = chirpAodChangeDto.IncrementChirpAodFilePath,
            EcsX_MinQuality = resultList[0].EcsX,
            EcsY_MinQuality = resultList[0].EcsY,
            EcsX_AverageQuality = resultList[1].EcsX,
            EcsY_AverageQuality = resultList[1].EcsY,
            resultList[1].QualityX,
            resultList[1].QualityY,
            Ecs_Quality_List = new HtmlPlot2DLinesChart([
                (plotList[0].title, plotList[0].points
                    .Select(t => new Point(t.Item1, t.Item2))
                    .ToArray()),
                (plotList[1].title, plotList[1].points
                    .Select(t => new Point(t.Item1, t.Item2))
                    .ToArray())
            ], "Ecs-QualityX/Y"),
            HtmlTab = new HtmlTab(new
            {
                Image = new HtmlImage(resultList[1].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());
    }

    /// <summary>
    /// 更新关于raterange-EcsError曲线图
    /// 更新关于EcsY-1/rageRange曲线图
    /// </summary>
    private static List<List<(string title, List<(double, double)> points)>> UpdateResultPlotMarkDown(List<LaserXYAstigmatismCalibrationItemDto> plotList, List<(double, double)> fitList)
    {
        var PlotResultList = new List<List<(string, List<(double, double)>)>>();

        List<(double, double)> plotListEf = [.. plotList.OrderBy(s => s.FrequenceIncrease).Select(s => (s.FrequenceIncrease, s.EcsErrorValue))];
        List<(string, List<(double, double)>)> plotListTitleEf =
        [
            ("F-EcsError", plotListEf)
        ];

        List<(string, List<(double, double)>)> plotListTitle_EcsY_F = [];
        List<(double, double)> plotList_EcsY_F = [.. plotList.OrderBy(s => s.FrequenceIncrease).Select(s => (s.EcsY, 1 / s.FrequenceIncrease))];
        plotListTitle_EcsY_F.Add(("EcsY-F", plotList_EcsY_F));
        List<(double, double)> plotList_EcsX_F = [.. plotList.OrderBy(s => s.FrequenceIncrease).Select(s => (s.EcsX, 1 / s.FrequenceIncrease))];
        plotListTitle_EcsY_F.Add(("EcsX-F", plotList_EcsX_F));
        plotListTitle_EcsY_F.Add(("EcsY-F Fit", fitList));

        PlotResultList.Add(plotListTitleEf);
        PlotResultList.Add(plotListTitle_EcsY_F);

        return PlotResultList;
    }

    /// <summary>
    /// 保存结果csv文件
    /// </summary>
    /// <param name="filepath">保存路径</param>
    public static void SaveIdealCsv(List<LaserXYAstigmatismCalibrationItemDto> resultList, string filepath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filepath);
        FileHelper.DeleteFileIfExists(filepath);

        var sb = new StringBuilder();
        sb.Append("RateChange,RateChange-Reciprocal,EcsX,EcsY,EcsErrorValue,QualityY");
        sb.AppendLine();
        foreach (var item in resultList)
        {
            sb.Append($"{item.FrequenceIncrease},{1 / item.FrequenceIncrease},{item.EcsX},{item.EcsY},{item.EcsErrorValue},{item.QualityY}");
            sb.AppendLine();
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    #endregion 日志
}