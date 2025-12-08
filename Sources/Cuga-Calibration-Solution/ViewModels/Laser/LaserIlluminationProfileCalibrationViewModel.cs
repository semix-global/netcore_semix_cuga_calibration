using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;
using Humanizer;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Collections.ObjectModel;
using System.IO;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserIlluminationProfileCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserIlluminationProfileCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{EnumHelper.ToDescriptionString(Cache.ProductivityInformation)}-{Cache.LaserLightInformation}";

    public override string CalibrateFileName => $"{EnumHelper.ToDescriptionString(Cache.ProductivityInformation)}-{Cache.LaserLightInformation}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Select a Coefficient" },
        new() { StepName = "Gain" },
        new() { StepName = "Find Window" },
        new() { StepName = "Find V Prescan" },
        new() { StepName = "Find Max Coefficient" },
        new() { StepName = "Illumination" }
    ];

    public string PrescanFileDirectory => Path.Combine(AppHomeDirectory, "Prescan", nameof(LaserIlluminationProfileCalibrationViewModel), DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private SettingDarkFieldGainViewModel _autoGainSettingDarkFieldGainViewModel = HostApplication.GetRequiredService<SettingDarkFieldGainViewModel>();

    [ObservableProperty]
    private SettingDarkFieldGainViewModel _darkFieldImageListToPrescanListSettingDarkFieldGainViewModel = HostApplication.GetRequiredService<SettingDarkFieldGainViewModel>();

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumAndLaserLightInformationCalibration> _calibrationStatusList = [];

    [ObservableProperty]
    private ObservableCollection<LaserLightInformationStatus> _calibrationStatusListItem = [];

    [ObservableProperty]
    private ObservableCollection<LaserIlluminationProfileItemDto> _calibrationLaserIlluminationProfileDtoList = [];

    [ObservableProperty]
    private LaserIlluminationProfileItemDto? _selectCalibrateItemDto;

    [ObservableProperty]
    private ObservableCollection<LaserIlluminationProfileCalibrationPmtIdItem> _pmtIdItemList = [];

    [ObservableProperty]
    private List<WpfPlotModel> _plotList = [];

    [ObservableProperty]
    private List<WpfPlotModel> _plotAverageList = [];

    [ObservableProperty]
    private List<WpfPlotModel> _plotPrescanList = [];

    [ObservableProperty]
    private double _illuminationCoefficientLimitMin;

    [ObservableProperty]
    private double _illuminationCoefficientLimitMax;

    [ObservableProperty]
    private double _illuminationTargetValue;

    [ObservableProperty]
    private double _tValue;

    [ObservableProperty]
    private ObservableCollection<int> _channelIdList = [1, 2, 3];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserIlluminationProfileItemDto> _reviewList = [];

    [ObservableProperty]
    private LaserIlluminationProfileItemDto? _selectReviewItemDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserIlluminationProfileCache _cache = new();

    [ObservableProperty]
    private LaserIlluminationProfileItemDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    [ObservableProperty]
    private LaserOpticalPowerMeterDto[] _laserOpticalPowers = [];

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var microscopeCalChip, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = microscopeCalChip;

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserOpticalPowerMeterDto>(out var laserOpticalPowers, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserOpticalPowers = laserOpticalPowers;

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

        if (CalibrationStatusList.Count == 0)
        {
            CalibrationStatusList =
            [
                ..ApplicationCookie.NIOpticsMagTypeProductivityInformations.Select(t => new OpticsMagTypeEnumAndLaserLightInformationCalibration { ProductivityInformation = t, LaserLightInformationStatusList = [.. LaserLightInformationStatus.CreateList(ApplicationCookie.LaserLightInformations)] })
            ];
        }

        if (CalibrationStatusListItem.Count == 0)
        {
            CalibrationStatusListItem = [.. LaserLightInformationStatus.CreateList(ApplicationCookie.LaserLightInformations)];
        }


        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserIlluminationProfileCache>();
        Cache.CurrentCalibrationCacheItem.Reset();
        Clear();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserIlluminationProfileItemDto>();

        foreach (var calibration in Calibrations)
        {
            var laserLightInformationStatus = CalibrationStatusList
                .SingleOrDefault(t => t.ProductivityInformation == calibration.ProductivityInformation)
                ?.LaserLightInformationStatusList
                .SingleOrDefault(t => t.LaserLightInformation == calibration.LaserLightInformation);

            if (laserLightInformationStatus is not null) laserLightInformationStatus.IsCalibrated = calibration.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.CurrentDarkFieldImageListToPrescanListCacheItem.Reset();

        Cache.FindPosition = GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;
        StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
                .ThenBy(t => t.PmtId)
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;

        StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(Cache.FindPosition);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                foreach (var temp in CalibrationStatusList.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).LaserLightInformationStatusList)
                {
                    CalibrationStatusListItem.Single(t => t.LaserLightInformation == temp.LaserLightInformation).IsCalibrated = temp.IsCalibrated;
                }

                var calibrationSettingMiddleMagSettingDarkFieldGainParam = CalibrationSetting.SettingDarkFieldGainParam;
                AutoGainSettingDarkFieldGainViewModel.SettingDarkFieldGainParam = calibrationSettingMiddleMagSettingDarkFieldGainParam.Single(t => t is { PmtId: 8, ChannelId: 3 });
                AutoGainSettingDarkFieldGainViewModel.ProductivityInformation = Cache.ProductivityInformation;

                DarkFieldImageListToPrescanListSettingDarkFieldGainViewModel.SettingDarkFieldGainParam = calibrationSettingMiddleMagSettingDarkFieldGainParam.Single(t => t is { PmtId: 8, ChannelId: 3 });
                DarkFieldImageListToPrescanListSettingDarkFieldGainViewModel.ProductivityInformation = Cache.ProductivityInformation;

                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;

            case 2:
                AutoGainSettingDarkFieldGainViewModel.PlotList = [];

                return true;

            case 3:
                if (Cache.CurrentDarkFieldImageListToPrescanListCacheItem.IsOk == false)
                    DarkFieldImageListToPrescanListSettingDarkFieldGainViewModel.PlotList = [];

                return true;

            case 4:
                if (Cache.CurrentCalibrationCacheItem.IsOk == false)
                    PlotList = [];
                Cache.CurrentDarkFieldImageListToPrescanListCacheItem.WaveFormVPrescanList = [];
                Cache.CurrentDarkFieldImageListToPrescanListCacheItem.WaveFormVDarkFieldImageList = [];
                Cache.CurrentDarkFieldImageListToPrescanListCacheItem.WaveFormVSmoothDarkFieldImageList = [];
                return Cache.CurrentDarkFieldImageListToPrescanListCacheItem.IsOk;

            case 5:
                return true;

            case 6:
                Clear();

                return Cache.CurrentCalibrationCacheItem.IsOk;

            case 7:
                if (SelectCalibrateItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find Ratio Value!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    SelectCalibrateItemDto.IsCalibrated = true;
                    if (ToIlluminationIntensity(SelectCalibrateItemDto, cancellationToken) == false)
                    {
                        SelectCalibrateItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.ProductivityInformation == Cache.ProductivityInformation)
                    .LaserLightInformationStatusList.Single(t => t.LaserLightInformation == Cache.LaserLightInformation)
                    .IsCalibrated = true;
                DialogWindowProvider.ShowDialog("Find Illumination Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private void ReviewImage(string filePaths)
    {
        if (string.IsNullOrEmpty(filePaths)) return;
        DialogWindowProvider.ShowImage([(filePaths, Cache.PmtId.ToString())]);
    }

    [RelayCommand]
    private void ReviewPlot(List<double> list)
    {
        if (list.Count == 0) return;
        DialogWindowProvider.ShowPlot([.. GetChannelDarkFieldImageList(list).Select(t => (t.Item1, t.Item2.ToArray()))]);
    }

    [RelayCommand]
    private void Review(List<double> list)
    {
        DialogWindowProvider.ShowPlot([.. list]);
    }

    [RelayCommand]
    private async Task SavePrescanAodWaveAsync(LaserIlluminationProfileItemDto? laserIlluminationProfileItemDto)
    {
        try
        {
            await Task.Run(() =>
            {
                if (laserIlluminationProfileItemDto is null) return;

                var tryShowSaveFilePathDialog = DialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath);
                if (tryShowSaveFilePathDialog == false) return;

                foreach (var prescanAODWaveformProfile in laserIlluminationProfileItemDto.PrescanAODWaveformProfileList) prescanAODWaveformProfile.ApplyCoefficientWindowList(laserIlluminationProfileItemDto.PrescanRateList);

                AODWaveformResultFactory.CreatePrescanList(laserIlluminationProfileItemDto.PrescanAODWaveformProfileList, directoryPath);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Grabbing Image Failed", Name);
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
            var result = StageViewModel.GetBrightFieldStagePosition();

            Cache.FindPosition = result;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.PmtId,
                Cache.ProductivityInformation,
                Cache.FindPosition,
                Cache.WidthPixel
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
                Cache.MicroscopeLensInformation.LensName,
                Cache.PmtId,
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.FindPosition,
                Cache.WidthPixel
            }), HtmlLogUniqueId.LoggingHtml());

            var contains = ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation);
            if (contains) return contains;

            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Laser Light Information is not exist!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var mainPmtCacheItem = new LaserIlluminationProfileCalibrationPmtIdItem
            {
                PmtId = CalibrationConstantsHelper.MainPmtId,
                ChannelId = Cache.ChannelId,
                PmtIdPosition = Cache.FindPosition
            };

            Cache.CurrentCalibrationCacheItem.LaserIlluminationProfileCalibrationPmt = mainPmtCacheItem;

            var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;
            if (pmtConfig.Single((t => t.Id == CalibrationConstantsHelper.MainPmtId)).Enabled == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Config pmt8 setting is enable!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            SynchronizationContextProvider.Send(() =>
            {
                PmtIdItemList.Clear();
                PmtIdItemList.Add(Cache.CurrentCalibrationCacheItem.LaserIlluminationProfileCalibrationPmt);
            });

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.FindPosition,
                Cache.WidthPixel
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.ToggleOpticsPolarizationMode(OpticsPolarizationModeEnum.P);

            var (isSuccess, gain) = await AutoGainSettingDarkFieldGainViewModel.AutoPmtGainAsync(Cache.LaserLightInformation.Coefficient, Cache.FindPosition, CalChipSiteModelEnum.HazeModel, Cache.ProductivityInformation, HtmlLogUniqueId, cancellationToken, false, Cache.PmtId, Cache.ChannelId).ConfigureAwait(false);
            if ((isSuccess) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Auto Pmt Gain Error!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            LaserViewModel.SetGain(gain);

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            mainPmtCacheItem.Gain = gain;

            Logger.LogHtmlInformation($"PmtId: {mainPmtCacheItem.PmtId}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                mainPmtCacheItem.PmtId,
                mainPmtCacheItem.ChannelId,
                Findposition = mainPmtCacheItem.PmtIdPosition,
                Gain = gain
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand]
    private void Step3CalibrateClear()
    {
        Cache.CurrentDarkFieldImageListToPrescanListCacheItem.Reset();
        DarkFieldImageListToPrescanListSettingDarkFieldGainViewModel.PlotList = [];
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var item = Cache.CurrentDarkFieldImageListToPrescanListCacheItem;
            var maxWindowStartIndex = item.MaxWindowStartIndex;
            var minWindowStartIndex = item.MinWindowStartIndex;
            var windowToMinAmount = item.WindowToMinAmount;
            var judgeWindowStartIndex = item.JudgeWindowStartIndex;
            var judgeWindowEndIndex = item.JudgeWindowEndIndex;
            var servings = item.Servings;
            var prescanAODWaveProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.PmtId,
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.FindPosition,
                Cache.WidthPixel,
                maxWindowStartIndex,
                minWindowStartIndex,
                windowToMinAmount,
                judgeWindowStartIndex,
                judgeWindowEndIndex,
                servings,
                prescanAODWaveProfiles = string.Join(",", prescanAODWaveProfiles.Select(t => t.FilePath))
            }), HtmlLogUniqueId.LoggingHtml());

            if (item.IsOk)
            {
                LoggerResult();
                return true;
            }

            item.Reset();

            var yPixelHeight = LaserViewModel.GetDarkFieldLineScanImageYPixelHeight(Cache.ProductivityInformation);
            if (servings > yPixelHeight || yPixelHeight % servings != 0)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Servings must be a factor of {yPixelHeight}!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            if (maxWindowStartIndex <= minWindowStartIndex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Param Error {nameof(maxWindowStartIndex)}: {maxWindowStartIndex}, {nameof(minWindowStartIndex)}: {minWindowStartIndex}!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            LaserViewModel.SetGain(Cache.CurrentCalibrationCacheItem.LaserIlluminationProfileCalibrationPmt.Gain);

            #region Max窗口

            cancellationToken.ThrowIfCancellationRequested();
            var windowPrescanList = SetPrescanByteListByWindow(maxWindowStartIndex, windowToMinAmount);
            foreach (var prescanAODWaveformProfile in prescanAODWaveProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(windowPrescanList);

            var (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(prescanAODWaveProfiles, Cache.PmtId, Cache.FindPosition);
            using var _1 = channel1DarkFieldImageDto;
            using var _2 = channel2DarkFieldImageDto;
            using var _3 = channel3DarkFieldImageDto;
            if (isSuccess == false) return false;
            if (Cache.ChannelId == 1) GetMaxWindowScanImage(item, channel1DarkFieldImageDto);
            else if (Cache.ChannelId == 2) GetMaxWindowScanImage(item, channel2DarkFieldImageDto);
            else if (Cache.ChannelId == 3) GetMaxWindowScanImage(item, channel3DarkFieldImageDto);

            #endregion Max窗口

            #region Min窗口

            cancellationToken.ThrowIfCancellationRequested();
            windowPrescanList = SetPrescanByteListByWindow(minWindowStartIndex, windowToMinAmount);
            foreach (var prescanAODWaveformProfile in prescanAODWaveProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(windowPrescanList);

            (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(prescanAODWaveProfiles, Cache.PmtId, Cache.FindPosition);
            using var _4 = channel1DarkFieldImageDto;
            using var _5 = channel2DarkFieldImageDto;
            using var _6 = channel3DarkFieldImageDto;
            if (isSuccess == false) return false;
            if (Cache.ChannelId == 1) GetMinWindowScanImage(item, channel1DarkFieldImageDto);
            else if (Cache.ChannelId == 2) GetMinWindowScanImage(item, channel2DarkFieldImageDto);
            else if (Cache.ChannelId == 3) GetMinWindowScanImage(item, channel3DarkFieldImageDto);

            item.IsReviseDarkFieldImageToPrescan = item.MinWindowDarkImageListMinIndex > item.MaxWindowDarkImageListMinIndex; // 需要反向

            #endregion Min窗口

            #region 获取T1

            if (item.IsReviseDarkFieldImageToPrescan)
            {
                var sgolayfiltList = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(channel3DarkFieldImageDto.ProjectionYs.AsEnumerable().Reverse()));
                item.T1 = judgeWindowStartIndex + sgolayfiltList.SubVector(judgeWindowStartIndex, judgeWindowEndIndex - judgeWindowStartIndex + 1).MinimumIndex();
            }
            else
            {
                item.T1 = item.MinWindowDarkImageListMinIndex;
            }

            #endregion 获取T1

            #region DarkFieldImage <=> Prescan 对应关系

            TValue = (maxWindowStartIndex - minWindowStartIndex) / (double)Math.Abs(item.MaxWindowDarkImageListMinIndex - item.MinWindowDarkImageListMinIndex);
            if (HostEnvironment.IsDevelopment()) TValue = 3.18;
            item.PrescanStartIndex = (int)Math.Floor(minWindowStartIndex + windowToMinAmount - item.T1 * TValue);
            item.PrescanEndIndex = (int)Math.Floor(minWindowStartIndex + windowToMinAmount + (yPixelHeight - item.T1) * TValue);
            if (item.PrescanStartIndex < 0
                || prescanAODWaveProfiles[0].ShortList.Count <= item.PrescanStartIndex
                || item.PrescanEndIndex < 0
                || prescanAODWaveProfiles[0].ShortList.Count <= item.PrescanEndIndex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Param Error {nameof(judgeWindowStartIndex)}: {judgeWindowStartIndex}, {nameof(judgeWindowEndIndex)}: {judgeWindowEndIndex}!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            item.IsOk = true;

            LoggerResult();

            #endregion DarkFieldImage <=> Prescan 对应关系

            LaserViewModel.SetGain(Cache.CurrentCalibrationCacheItem.LaserIlluminationProfileCalibrationPmt.Gain);

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            return true;

            void GetMaxWindowScanImage(LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem item, DarkFieldImageDto darkFieldImageDto)
            {
                var sgolayfiltList = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(darkFieldImageDto.ProjectionYs));
                item.MaxWindowDarkImageListMinIndex = judgeWindowStartIndex + sgolayfiltList.SubVector(judgeWindowStartIndex, judgeWindowEndIndex - judgeWindowStartIndex + 1).MinimumIndex();
                item.MaxWindowPrescanList = windowPrescanList;
                item.MaxScatterDarkFieldImageList = [.. darkFieldImageDto.ProjectionYs];
                item.MaxSmoothDarkFieldImageList = [.. sgolayfiltList];
            }

            void GetMinWindowScanImage(LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem item, DarkFieldImageDto darkFieldImageDto)
            {
                var sgolayfiltList = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(darkFieldImageDto.ProjectionYs));
                item.MinWindowDarkImageListMinIndex = judgeWindowStartIndex + sgolayfiltList.SubVector(judgeWindowStartIndex, judgeWindowEndIndex - judgeWindowStartIndex + 1).MinimumIndex();
                item.MinWindowPrescanList = windowPrescanList;
                item.MinScatterDarkFieldImageList = [.. darkFieldImageDto.ProjectionYs];
                item.MinSmoothDarkFieldImageList = [.. sgolayfiltList];
            }

            void LoggerResult()
            {
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    item.T1,
                    item.PrescanStartIndex,
                    item.PrescanEndIndex,
                    WindowPrescanList = new HtmlPlot2DLinesChart([(nameof(item.MaxWindowPrescanList), item.MaxWindowPrescanList.ToPoints()), (nameof(item.MinWindowPrescanList), item.MinWindowPrescanList.ToPoints())], "WindowPrescanList"),
                    DarkFieldImageList = new HtmlPlot2DLinesChart([
                        (nameof(item.MaxScatterDarkFieldImageList), item.MaxScatterDarkFieldImageList.ToPoints()), (nameof(item.MinScatterDarkFieldImageList), item.MinScatterDarkFieldImageList.ToPoints()),
                        (nameof(item.MaxSmoothDarkFieldImageList), item.MaxSmoothDarkFieldImageList.ToPoints()), (nameof(item.MinSmoothDarkFieldImageList), item.MinSmoothDarkFieldImageList.ToPoints())
                    ], "DarkFieldImageList")
                }), HtmlLogUniqueId.LoggingHtml());
            }

            List<double> SetPrescanByteListByWindow(int startIndex, int windowToMinAmountTemp)
            {
                var k = 1d / windowToMinAmountTemp;
                var prescanList = prescanAODWaveProfiles[0].ShortList;
                var middleIndex = startIndex + windowToMinAmountTemp;
                var endIndex = startIndex + windowToMinAmountTemp * 2;
                if (endIndex > prescanList.Count) throw new CalibrationException($"{nameof(endIndex)}: {endIndex} > {nameof(prescanList)}{nameof(prescanList.Count)}: {prescanList.Count}");

                var coefficient = Cache.LaserLightInformation.Coefficient;
                var resultPrescanWindowList = new List<double>();

                for (var i = 0; i < startIndex; i++) // 1-1499, 都是按照系数来
                {
                    resultPrescanWindowList.Add(coefficient);
                }

                for (var i = startIndex; i < middleIndex; i++) // 1500-2499,按照斜率为-1/1000, 1500为1下降到0.0001
                {
                    var rate = (1 - (i - startIndex) * k) * coefficient;
                    resultPrescanWindowList.Add(rate);
                }

                for (var i = middleIndex; i < endIndex; i++) // 2500-3499,按照斜率为1/1000, 1500为0.001上升到1
                {
                    var rate = ((i - middleIndex) * k + 0.001) * coefficient;
                    resultPrescanWindowList.Add(rate);
                }

                for (var i = endIndex; i < prescanList.Count; i++) // 剩下按照系数来
                {
                    resultPrescanWindowList.Add(coefficient);
                }

                return resultPrescanWindowList;
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var itemCache = Cache.CurrentDarkFieldImageListToPrescanListCacheItem;
            itemCache.ServingToPrescanListIndicesList.Clear();
            itemCache.ServingToDarkFieldImageListIndicesList.Clear();
            var prescanAODWaveProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);
            var coefficient = Cache.LaserLightInformation.Coefficient;

            var resultPrescanWindowList = new List<double>();
            var prescanCount = itemCache.PrescanEndIndex - itemCache.PrescanStartIndex;
            var minPrescanIndexList = new List<int>();
            // 按照波形幅值可以分成几份
            var waveFormCount = prescanCount / itemCache.WaveFormVInterval / 3;
            var k = 1d / itemCache.WaveFormVInterval;
            var waveFormVStartIndex = itemCache.PrescanStartIndex;
            var startIndex = itemCache.PrescanStartIndex + itemCache.WaveFormVInterval;
            var middleIndex = itemCache.PrescanStartIndex + itemCache.WaveFormVInterval * 2;
            var endIndex = itemCache.PrescanStartIndex + itemCache.WaveFormVInterval * 3;
            var waveFormVEndIndex = itemCache.PrescanStartIndex + itemCache.WaveFormVInterval * 3 * waveFormCount;

            for (var i = 0; i < itemCache.PrescanStartIndex; i++) // 第一份按照原来系数计算
            {
                resultPrescanWindowList.Add(coefficient);
            }

            foreach (var index in Enumerable.Range(0, waveFormCount)) //生成V波形波算法
            {
                minPrescanIndexList.Add(startIndex);
                for (var i = waveFormVStartIndex; i < startIndex; i++) // 下降波形生成按照k系数下降
                {
                    var rate = Math.Round((1 - (i - waveFormVStartIndex) * k) * coefficient, 3);
                    resultPrescanWindowList.Add(rate);
                }

                for (var i = startIndex; i < middleIndex; i++) //上升波形生成按照k系数上升
                {
                    var rate = Math.Round(((i - startIndex) * k + k) * coefficient, 3);
                    resultPrescanWindowList.Add(rate);
                }

                for (var i = middleIndex; i < endIndex; i++) // 按照原来系数生成
                {
                    resultPrescanWindowList.Add(coefficient);
                }

                waveFormVStartIndex += itemCache.WaveFormVInterval * 3;
                startIndex += itemCache.WaveFormVInterval * 3;
                middleIndex += itemCache.WaveFormVInterval * 3;
                endIndex += itemCache.WaveFormVInterval * 3;
            }

            for (var i = waveFormVEndIndex; i < prescanAODWaveProfiles[0].ShortList.Count; i++) //结束部分按照原来系数计算
            {
                resultPrescanWindowList.Add(coefficient);
            }

            itemCache.WaveFormVPrescanMinList = minPrescanIndexList;
            itemCache.WaveFormVPrescanList = [.. resultPrescanWindowList];
            foreach (var prescanAODWaveformProfile in prescanAODWaveProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(resultPrescanWindowList);

            var (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(prescanAODWaveProfiles, Cache.PmtId, Cache.FindPosition);
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Get Dark Field Line Scan Image Error!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            using var _1 = channel1DarkFieldImageDto;
            using var _2 = channel2DarkFieldImageDto;
            using var _3 = channel3DarkFieldImageDto;

            using var darkFieldImageDto = Cache.ChannelId switch
            {
                1 => channel1DarkFieldImageDto,
                2 => channel2DarkFieldImageDto,
                3 => channel3DarkFieldImageDto,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<DarkFieldImageDto>(nameof(Cache.ChannelId))
            };
            //是否需要进行反转
            var projectionYs = itemCache.IsReviseDarkFieldImageToPrescan ? darkFieldImageDto.ProjectionYs.AsEnumerable().Reverse() : darkFieldImageDto.ProjectionYs;
            var sgolayfiltList = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(projectionYs));
            itemCache.WaveFormVDarkFieldImageList = [.. projectionYs];
            itemCache.WaveFormVSmoothDarkFieldImageList = [.. sgolayfiltList];
            //取Image波谷最小值
            var minIndexList = new List<int>();
            var startIndexValue = Convert.ToInt32(itemCache.WaveFormVInterval / 2 / TValue);
            var valueCount = Convert.ToInt32(itemCache.WaveFormVInterval * 3 / TValue);
            foreach (var index in Enumerable.Range(1, waveFormCount))
            {
                if (startIndexValue + valueCount > sgolayfiltList.Count)
                {
                    valueCount = sgolayfiltList.Count - startIndexValue;
                }

                var minIndex = startIndexValue + sgolayfiltList.SubVector(startIndexValue, valueCount).MinimumIndex();
                minIndexList.Add(minIndex);
                startIndexValue += Convert.ToInt32(itemCache.WaveFormVInterval * 3 / TValue);
            }

            itemCache.WaveFormVImageMinList = minIndexList;
            //重新找波形起始位置和结束位置
            if (minIndexList.Count > 1 && minPrescanIndexList.Count > 1)
            {
                var imageIndex = minIndexList.ToList().Max() - minIndexList.ToList().Min();
                var prescanIndex = minPrescanIndexList.ToList().Max() - minPrescanIndexList.ToList().Min();
                itemCache.PrescanStartIndex = minPrescanIndexList[0] - (int)(minIndexList[0] * Math.Round(((double)prescanIndex / imageIndex), 2));
                itemCache.PrescanEndIndex = minPrescanIndexList[^1] +
                                            (int)((sgolayfiltList.Count - minIndexList[^1]) * Math.Round(((double)prescanIndex / imageIndex), 2));
            }

            // 计算每一个照片像素点对应的Prescan波形位置
            var resultServingToPrescanListIndicesList = new List<int[]>();
            var (servingToPrescanListIndicesList, servingToDarkFieldImageListIndicesList) = SetPrescanServings(minPrescanIndexList[0] - itemCache.PrescanStartIndex, minIndexList[0], 0);
            resultServingToPrescanListIndicesList = [.. servingToPrescanListIndicesList];
            foreach (var index in Enumerable.Range(1, waveFormCount - 1))
            {
                var minPrescanIndex = minPrescanIndexList[index] - minPrescanIndexList[index - 1];
                var minIndex = minIndexList[index] - minIndexList[index - 1];
                (servingToPrescanListIndicesList, servingToDarkFieldImageListIndicesList) = SetPrescanServings(minPrescanIndex, minIndex, minPrescanIndexList[index - 1] - itemCache.PrescanStartIndex);
                resultServingToPrescanListIndicesList = [.. resultServingToPrescanListIndicesList, .. servingToPrescanListIndicesList];
            }

            (servingToPrescanListIndicesList, servingToDarkFieldImageListIndicesList) = SetPrescanServings(itemCache.PrescanEndIndex - minPrescanIndexList[waveFormCount - 1], sgolayfiltList.Count - minIndexList[waveFormCount - 1], minPrescanIndexList[waveFormCount - 1] - itemCache.PrescanStartIndex);
            resultServingToPrescanListIndicesList = [.. resultServingToPrescanListIndicesList, .. servingToPrescanListIndicesList];

            var everyCount = resultServingToPrescanListIndicesList.Count / itemCache.Servings;
            for (var g = 0; g < itemCache.Servings; g++)
            {
                var indexList = new List<int>();
                for (var i = 0; i < everyCount; i++)
                {
                    indexList = [.. indexList, .. resultServingToPrescanListIndicesList[i + everyCount * g]];
                }

                itemCache.ServingToPrescanListIndicesList.Add([.. indexList]);
            }

            // 计算光斑曲线Ej每一份的点
            for (var i = 0; i < itemCache.Servings; i++)
            {
                var indexList = new List<int>();
                var count = itemCache.MinScatterDarkFieldImageList.Count;

                for (var j = 0; j < count / itemCache.Servings; j++)
                {
                    indexList.Add(j + i * count / itemCache.Servings);
                }

                itemCache.ServingToDarkFieldImageListIndicesList.Add([.. indexList]);
            }

            LoggerResult();

            return true;

            (List<int[]> ServingToPrescanListIndicesList, List<int[]> ServingToDarkFieldImageListIndicesList) SetPrescanServings(int prescanValueIndex, int imageValueIndex, int prescanIndex)
            {
                var servingToPrescanListIndicesList = new List<int[]>();
                var everyCount = prescanValueIndex / imageValueIndex; // 计算每一份最小个数，向下取整
                var remainderCount = prescanValueIndex % imageValueIndex; // 计算余数
                if (remainderCount == 0)
                {
                    // 刚好整除，每一份个数相同m
                    for (var k = 0; k < imageValueIndex; k++)
                    {
                        var indexList = new List<int>();
                        for (var j = 0; j < everyCount; j++)
                        {
                            indexList.Add(prescanIndex + itemCache.PrescanStartIndex + j + k * everyCount);
                        }

                        servingToPrescanListIndicesList.Add([.. indexList]);
                    }
                }
                else
                {
                    var servingCount = imageValueIndex / remainderCount; //计算每隔几份补偿一个prescan
                    //是否取余数，每一份多加一个
                    var isRemainder = false;
                    var everyPrescanCount = 0;
                    var remainderPrescanCount = 0;
                    var servingPrescanCount = 0;
                    var prescanStartIndex = itemCache.PrescanStartIndex;
                    int k;
                    if (servingCount > 1)
                    {
                        servingCount--;
                        for (k = 0; k < imageValueIndex; k++)
                        {
                            if (isRemainder && remainderPrescanCount < remainderCount && servingPrescanCount == servingCount)
                            {
                                servingPrescanCount = 0;
                                everyPrescanCount = everyCount + 1;
                                remainderPrescanCount++;
                                isRemainder = false;
                            }
                            else
                            {
                                servingPrescanCount++;
                                everyPrescanCount = everyCount;
                                isRemainder = true;
                            }

                            var indexList = new List<int>();
                            for (var j = 0; j < everyPrescanCount; j++)
                            {
                                indexList.Add(prescanIndex + prescanStartIndex++);
                            }

                            servingToPrescanListIndicesList.Add([.. indexList]);
                        }
                    }
                    else
                    {
                        var yCount = imageValueIndex % remainderCount;
                        var tCount = remainderCount - yCount;
                        for (k = 0; k < yCount * 2; k++)
                        {
                            if (isRemainder && remainderPrescanCount < remainderCount && servingPrescanCount == servingCount)
                            {
                                servingPrescanCount = 0;
                                everyPrescanCount = everyCount + 1;
                                remainderPrescanCount++;
                                isRemainder = false;
                            }
                            else
                            {
                                servingPrescanCount++;
                                everyPrescanCount = everyCount;
                                isRemainder = true;
                            }

                            var indexList = new List<int>();
                            for (var j = 0; j < everyPrescanCount; j++)
                            {
                                indexList.Add(prescanIndex + prescanStartIndex++);
                            }

                            servingToPrescanListIndicesList.Add([.. indexList]);
                        }

                        for (var i = 0; i < tCount; i++)
                        {
                            var indexList = new List<int>();
                            for (var j = 0; j < everyCount + 1; j++)
                            {
                                indexList.Add(prescanIndex + prescanStartIndex++);
                            }

                            servingToPrescanListIndicesList.Add([.. indexList]);
                        }
                    }
                }

                return (servingToPrescanListIndicesList, []);
            }

            void LoggerResult()
            {
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    itemCache.T1,
                    itemCache.Servings,
                    itemCache.WaveFormVInterval,
                    itemCache.PrescanStartIndex,
                    itemCache.PrescanEndIndex,
                    PrescanVList = new HtmlPlot2DLinesChart([(nameof(itemCache.WaveFormVPrescanList), itemCache.WaveFormVPrescanList.ToPoints())], "PrescanVList"),
                    PrescanVMinIndexList = string.Join(", ", itemCache.WaveFormVPrescanMinList),
                    DarkFieldImageVList = new HtmlPlot2DLinesChart([
                        (nameof(itemCache.WaveFormVDarkFieldImageList), itemCache.WaveFormVDarkFieldImageList.ToPoints()),
                        (nameof(itemCache.WaveFormVSmoothDarkFieldImageList), itemCache.WaveFormVSmoothDarkFieldImageList.ToPoints())
                    ], "DarkFieldImageVList"),
                    ImageVMinIndexList = string.Join(", ", itemCache.WaveFormVImageMinList),
                    ServingToPrescanListIndicesList = new HtmlExpand("ServingToPrescanListIndicesList", new HtmlTable([.. itemCache.ServingToPrescanListIndicesList.Select((t, i) => new { Serving = i, Value = t })])),
                    ServingToDarkFieldImageListIndicesList = new HtmlExpand("ServingToDarkFieldImageListIndicesList", new HtmlTable([.. itemCache.ServingToDarkFieldImageListIndicesList.Select((t, i) => new { Serving = i, Value = t })]))
                }), HtmlLogUniqueId.LoggingHtml());
            }
        });
    }

    [RelayCommand]
    private void Step5CalibrateClear()
    {
        Cache.CurrentCalibrationCacheItem.Reset();
        PlotList = [];
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var item = Cache.CurrentCalibrationCacheItem;
            var darkFieldImageListToPrescanListCacheItem = Cache.CurrentDarkFieldImageListToPrescanListCacheItem;
            var prescanAODWaveProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);
            var servings = Cache.CurrentDarkFieldImageListToPrescanListCacheItem.Servings;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.FindPosition,
                Cache.WidthPixel,
                prescanAODWaveProfiles = string.Join(",", prescanAODWaveProfiles.Select(t => t.FilePath)),
                servings
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.SetGain(Cache.CurrentCalibrationCacheItem.LaserIlluminationProfileCalibrationPmt.Gain);

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            if (item.IsOk)
            {
                LoggerResult();
                return true;
            }

            item.Reset();
            PlotList = [];

            // 从0.5开始,每次递增0.05,直到1, 循环11次
            foreach (var c in Enumerable.Range(0, 11).Select(t => 0.5 * Cache.LaserLightInformation.Coefficient + t * 0.05 * Cache.LaserLightInformation.Coefficient))
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var prescanAODWaveformProfile in prescanAODWaveProfiles) prescanAODWaveformProfile.ApplyCoefficient(c);

                var (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(prescanAODWaveProfiles, 8, Cache.FindPosition);
                using var _1 = channel1DarkFieldImageDto;
                using var _2 = channel2DarkFieldImageDto;
                using var _3 = channel3DarkFieldImageDto;

                if (isSuccess == false) return false;
                var darkFieldImageList = new List<double>();
                if (Cache.ChannelId == 1) darkFieldImageList = Cache.GetDarkFieldImageList([.. channel1DarkFieldImageDto.ProjectionYs]);
                if (Cache.ChannelId == 2) darkFieldImageList = Cache.GetDarkFieldImageList([.. channel2DarkFieldImageDto.ProjectionYs]);
                if (Cache.ChannelId == 3) darkFieldImageList = Cache.GetDarkFieldImageList([.. channel3DarkFieldImageDto.ProjectionYs]);

                var servingToDarkFieldImageListAverages = darkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList
                    .Select(t => t.Average(tt => darkFieldImageList[tt]))
                    .ToArray();
                item.CoefficientToServingToDarkFieldImageListAveragesList.Add((c, servingToDarkFieldImageListAverages));

                PlotList = [.. PlotList, new WpfPlotModel($"{c:f3}", [.. servingToDarkFieldImageListAverages.ToPoints()], (0.5 * Cache.LaserLightInformation.Coefficient, Cache.LaserLightInformation.Coefficient, c))];
            }

            for (var serving = 0; serving < darkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList.Count; serving++)
            {
                var averageList = item.CoefficientToServingToDarkFieldImageListAveragesList.Select(t => t.ServingToDarkFieldImageListAverages[serving]).ToList();
                var findDescendingSequenceIndex = Vector<double>.Build.DenseOfEnumerable(averageList).MaximumIndex();
                item.ServingToMaxCoefficientList.Add(item.CoefficientToServingToDarkFieldImageListAveragesList[findDescendingSequenceIndex].Coefficient);
            }

            PlotList = [.. PlotList, new WpfPlotModel("Serving To Max Coefficient", [.. item.ServingToMaxCoefficientList.ToPoints()])];

            item.IsOk = true;

            LoggerResult();

            return true;

            void LoggerResult()
            {
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    DarkFieldImageOfServingAverageList = new HtmlPlot2DLinesChart([
                        .. item.CoefficientToServingToDarkFieldImageListAveragesList.Select(t => ($"{t.Coefficient}", t.ServingToDarkFieldImageListAverages.ToPoints())),
                        (nameof(item.ServingToMaxCoefficientList), [..item.ServingToMaxCoefficientList.ToPoints()])
                    ], "DarkFieldImageOfServingAverageList")
                }), HtmlLogUniqueId.LoggingHtml());
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step6CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Clear();
            PlotPrescanList = [];
            PlotAverageList = [];
            var item = Cache.CurrentCalibrationCacheItem;
            var darkFieldImageListToPrescanListCacheItem = Cache.CurrentDarkFieldImageListToPrescanListCacheItem;
            var prescanAODWaveProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);
            var servings = darkFieldImageListToPrescanListCacheItem.Servings;
            var judgeDarkFieldImageListRateSkipCout = item.JudgeDarkFieldImageListRateSkipCout;
            var repeatCoefficientInterval = item.RepeatCoefficientInterval;
            var coefficientLimit = item.CoefficientLimit;
            var repeatCount = item.RepeatCount;
            var thresholdDarkFieldImageListRateMin = Cache.CalibrateThresholdDarkFieldImageListRateMin;
            var thresholdDarkFieldImageListRateMax = Cache.CalibrateThresholdDarkFieldImageListRateMax;
            var detectImageDirectory = ImageFileDirectory;
            var cacheCoefficientLimitMin = IlluminationCoefficientLimitMin = Math.Max(0, Cache.LaserLightInformation.Coefficient - Cache.LaserLightInformation.Coefficient * coefficientLimit);
            var cacheCoefficientLimitMax = IlluminationCoefficientLimitMax = Math.Min(1d, Cache.LaserLightInformation.Coefficient + Cache.LaserLightInformation.Coefficient * coefficientLimit);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.FindPosition,
                Cache.WidthPixel,
                prescanAODWaveProfiles = string.Join(",", prescanAODWaveProfiles.Select(t => t.FilePath)),
                servings,
                judgeDarkFieldImageListRateSkipCout,
                Cache.DarkImageListRangeThreshold,
                repeatCoefficientInterval,
                coefficientLimit,
                repeatCount,
                thresholdDarkFieldImageListRateMin,
                thresholdDarkFieldImageListRateMax,
                detectImageDirectory,
                cacheCoefficientLimitMin,
                cacheCoefficientLimitMax
            }), HtmlLogUniqueId.LoggingHtml());

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            var prescanRateList = Vector<double>.Build.Dense(prescanAODWaveProfiles[0].ShortList.Count, Cache.LaserLightInformation.Coefficient);
            var illuminationIntensityConsistentDto = new LaserIlluminationProfileItemDto
            {
                Index = 0,
                LaserLightInformation = Cache.LaserLightInformation,
                ProductivityInformation = Cache.ProductivityInformation,
                PmtId = CalibrationConstantsHelper.MainPmtId,
                PrescanAODWaveformProfileList = prescanAODWaveProfiles,
                FindPosition = Cache.FindPosition,
                PrescanRateList = [.. prescanRateList]
            };

            foreach (var prescanAODWaveformProfile in prescanAODWaveProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(illuminationIntensityConsistentDto.PrescanRateList);

            LaserViewModel.SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum.OI, prescanAODWaveProfiles);

            int? targetServing = null;
            double? targetValue = null;

            var servingToMaxCoefficientList = item.ServingToMaxCoefficientList.ToList();
            var lastGreaterThanIndexList = new List<int>();

            Logger.LogHtmlInformation("Get Illumination", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            foreach (var index in Enumerable.Range(1, repeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tempIlluminationProfileDto = illuminationIntensityConsistentDto.Clone();
                tempIlluminationProfileDto.Index = index;
                SetPrescanList(tempIlluminationProfileDto);
                SynchronizationContextProvider.Send(() => CalibrationLaserIlluminationProfileDtoList.Add(tempIlluminationProfileDto));
                if (TryCheckIlluminationIsOk(detectImageDirectory, prescanAODWaveProfiles, tempIlluminationProfileDto, out var isOk) == false) return false;

                if (isOk) return await GetResultAsync().ConfigureAwait(false);

                var darkFieldImageList = tempIlluminationProfileDto.ChannelDarkFieldProjectYsList;
                PlotAverageList = [.. PlotAverageList, new WpfPlotModel(index.ToString(), [.. darkFieldImageList.ToPoints()], (1, Cache.CurrentCalibrationCacheItem.RepeatCount, index))];
                var servingToDarkFieldImageListAverageList = darkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList
                    .Select(t => t.Average(tt => darkFieldImageList[tt]))
                    .ToList();

                var ceiling = (int)Math.Ceiling((double)judgeDarkFieldImageListRateSkipCout / darkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList[0].Length);

                const double targetCoefficient = 1;

                var skipList = servingToDarkFieldImageListAverageList.Skip(ceiling).SkipLast(ceiling).ToList();
                var targetAverage = skipList.Average();
                // 光强=1时只能往最小的拉
                targetServing ??= Cache.LaserLightInformation.Coefficient < targetCoefficient
                    ? ceiling + Vector<double>.Build.DenseOfEnumerable(skipList.Select(t => Math.Abs(t - targetAverage))).MinimumIndex()
                    : ceiling + Vector<double>.Build.DenseOfEnumerable(skipList).MinimumIndex();
                targetValue ??= IlluminationTargetValue = Cache.LaserLightInformation.Coefficient < targetCoefficient
                    ? targetAverage
                    : servingToDarkFieldImageListAverageList[targetServing.Value];

                var greaterThanIndexList = new List<(int Serving, double Value)>();
                var lessThanIndexList = new List<(int Serving, double Value)>();
                var resultList = new List<bool>();
                var okIndexList = new List<(int Serving, double Value)>();
                // 跳过前后ceiling个
                foreach (var serving in Enumerable.Range(0, servingToDarkFieldImageListAverageList.Count).Skip(ceiling).SkipLast(ceiling))
                {
                    if (serving == targetServing.Value || okIndexList.Any(t => t.Serving == serving))
                    {
                        resultList.Add(true);
                        continue;
                    }

                    var value = servingToDarkFieldImageListAverageList[serving];
                    if (value > targetValue * (thresholdDarkFieldImageListRateMax - Cache.CalibrateThreshold * 0.5))
                    {
                        servingToMaxCoefficientList[serving] -= repeatCoefficientInterval;
                        if (servingToMaxCoefficientList[serving] <= cacheCoefficientLimitMin)
                        {
                            servingToMaxCoefficientList[serving] = cacheCoefficientLimitMin;
                            okIndexList.Add((serving, value));
                            resultList.Add(true);
                        }
                        else
                        {
                            greaterThanIndexList.Add((serving, value));
                        }
                    }
                    else if (value < targetValue * (thresholdDarkFieldImageListRateMin + Cache.CalibrateThreshold * 0.5))
                    {
                        servingToMaxCoefficientList[serving] += repeatCoefficientInterval;
                        if (servingToMaxCoefficientList[serving] >= cacheCoefficientLimitMax)
                        {
                            servingToMaxCoefficientList[serving] = cacheCoefficientLimitMax;
                            okIndexList.Add((serving, value));
                            resultList.Add(true);
                        }
                        else
                        {
                            lessThanIndexList.Add((serving, value));
                        }
                    }
                    else
                    {
                        okIndexList.Add((serving, value));
                        resultList.Add(true);
                    }
                }

                if (lastGreaterThanIndexList.Count > 0 && lessThanIndexList.Count > 0)
                {
                    if (lastGreaterThanIndexList.SequenceEqual(lessThanIndexList.Select(t => t.Serving).OrderBy(t => t))) repeatCoefficientInterval *= 0.7;
                }

                lastGreaterThanIndexList = [.. greaterThanIndexList.Select(t => t.Serving).OrderBy(t => t)];

                if (resultList.Count == servingToDarkFieldImageListAverageList.Count - ceiling * 2 && resultList.All(t => t)) return await GetResultAsync().ConfigureAwait(false);

                if (tempIlluminationProfileDto.ChannelDarkFieldProjectYsList.Count == 0) return false;

                PlotPrescanList = [.. PlotPrescanList, new WpfPlotModel(index.ToString(), [.. GetPrescan1080List(tempIlluminationProfileDto.PrescanRateList).ToPoints()], (1, Cache.CurrentCalibrationCacheItem.RepeatCount, index))];

                Logger.LogHtmlInformation($"{tempIlluminationProfileDto.Index}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    targetServing,
                    targetValue,
                    tempIlluminationProfileDto.ChannelId,
                    okIndexList = new HtmlExpand(nameof(okIndexList), new HtmlTable([.. okIndexList.Select(t => new { t.Serving, t.Value })])),
                    greaterThanIndexList = new HtmlExpand(nameof(greaterThanIndexList), new HtmlTable([.. greaterThanIndexList.Select(t => new { t.Serving, t.Value })])),
                    lessThanIndexList = new HtmlExpand(nameof(lessThanIndexList), new HtmlTable([.. lessThanIndexList.Select(t => new { t.Serving, t.Value })])),
                    cacheCoefficientLimitMin,
                    cacheCoefficientLimitMax,
                    tempIlluminationProfileDto.DarkFieldImageListRateMin,
                    tempIlluminationProfileDto.DarkFieldImageListRateMax,
                    PrescanRateList = new HtmlPlot2DLinesChart([
                        (nameof(tempIlluminationProfileDto.PrescanRateList), GetPrescan1080List(tempIlluminationProfileDto.PrescanRateList).ToPoints())
                    ], "PrescanRateList"),
                    DarkFieldImageCh3List = new HtmlPlot2DLinesChart(GetChannelDarkFieldImageList(tempIlluminationProfileDto.ChannelDarkFieldProjectYsList), "DarkFieldImageCh3List"),
                    HtmlTab = new HtmlTab(new
                    {
                        CH1 = new HtmlImage(tempIlluminationProfileDto.Channel1ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH2 = new HtmlImage(tempIlluminationProfileDto.Channel2ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH3 = new HtmlImage(tempIlluminationProfileDto.Channel3ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());
            }

            return await GetResultAsync().ConfigureAwait(false);

            void SetPrescanList(LaserIlluminationProfileItemDto result)
            {
                for (var serving = 0; serving < servingToMaxCoefficientList.Count; serving++)
                {
                    if (serving == 0)
                    {
                        for (var i = 0; i < darkFieldImageListToPrescanListCacheItem.ServingToPrescanListIndicesList[serving].Last() + 1; i++)
                        {
                            result.PrescanRateList[i] = servingToMaxCoefficientList[serving];
                        }
                    }
                    else if (serving == servings - 1)
                    {
                        for (var i = darkFieldImageListToPrescanListCacheItem.ServingToPrescanListIndicesList[serving][0]; i < result.PrescanRateList.Count; i++)
                        {
                            result.PrescanRateList[i] = servingToMaxCoefficientList[serving];
                        }
                    }
                    else
                    {
                        foreach (var t in darkFieldImageListToPrescanListCacheItem.ServingToPrescanListIndicesList[serving]) result.PrescanRateList[t] = servingToMaxCoefficientList[serving];
                    }
                }
            }

            async Task<bool> GetResultAsync()
            {
                SelectCalibrateItemDto = CalibrationLaserIlluminationProfileDtoList
                    .Select(t => (Judge: new Point(t.DarkFieldImageListRateMin - thresholdDarkFieldImageListRateMin, t.DarkFieldImageListRateMax - thresholdDarkFieldImageListRateMax).ToOriginLength, Result: t))
                    .OrderBy(t => t.Judge)
                    .First().Result;

                var isOkSelect = thresholdDarkFieldImageListRateMin < SelectCalibrateItemDto.DarkFieldImageListRateMin
                                 && SelectCalibrateItemDto.DarkFieldImageListRateMin < thresholdDarkFieldImageListRateMax
                                 && thresholdDarkFieldImageListRateMin < SelectCalibrateItemDto.DarkFieldImageListRateMax
                                 && SelectCalibrateItemDto.DarkFieldImageListRateMax < thresholdDarkFieldImageListRateMax;

                if (isOkSelect == false)
                {
                    DialogWindowProvider.TryShowDialog($"""
                                                        {nameof(SelectCalibrateItemDto.DarkFieldImageListRateMin).Humanize(LetterCasing.Title)}: {SelectCalibrateItemDto.DarkFieldImageListRateMin:f3}
                                                        {nameof(SelectCalibrateItemDto.DarkFieldImageListRateMax).Humanize(LetterCasing.Title)}: {SelectCalibrateItemDto.DarkFieldImageListRateMax:f3}
                                                        Whether to enable the value?
                                                        """, out var dialogButtonsEnum, DialogButtonsEnum.OKCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum != DialogResultEnum.OK) return false;
                }

                foreach (var prescanAODWaveformProfile in prescanAODWaveProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(SelectCalibrateItemDto.PrescanRateList);

                SelectCalibrateItemDto.PrescanAODWaveformResultList = AODWaveformResultFactory.CreatePrescanList(prescanAODWaveProfiles, PrescanFileDirectory);

                LaserViewModel.SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum.OI, prescanAODWaveProfiles);

                // StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPowerPosition);
                //
                // (var isSuccess, SelectCalibrateItemDto.PolarizationPPower) = await GetPowerAsync(OpticsPolarizationModeEnum.P).ConfigureAwait(false);
                // if (isSuccess == false) return false;
                // (isSuccess, SelectCalibrateItemDto.PolarizationSPower) = await GetPowerAsync(OpticsPolarizationModeEnum.S).ConfigureAwait(false);
                // if (isSuccess == false) return false;
                // (isSuccess, SelectCalibrateItemDto.PolarizationCPower) = await GetPowerAsync(OpticsPolarizationModeEnum.C).ConfigureAwait(false);
                // if (isSuccess == false) return false;

                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(Cache.FindPosition);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    PrescanAODWaveformResultList = string.Join(",", SelectCalibrateItemDto.PrescanAODWaveformResultList.Select(t => t.FilePath)),
                    SelectCalibrateItemDto.PolarizationPPower,
                    SelectCalibrateItemDto.PolarizationSPower,
                    SelectCalibrateItemDto.PolarizationCPower,
                    SelectCalibrateItemDto.DarkFieldImageListRateMin,
                    SelectCalibrateItemDto.DarkFieldImageListRateMax,
                    PrescanRateList = new HtmlPlot2DLinesChart([
                        (nameof(SelectCalibrateItemDto.PrescanRateList), GetPrescan1080List(SelectCalibrateItemDto.PrescanRateList).ToPoints())
                    ], "PrescanRateList"),
                    DarkFieldImageList = new HtmlPlot2DLinesChart(GetChannelDarkFieldImageList(SelectCalibrateItemDto.ChannelDarkFieldProjectYsList), "DarkFieldImageList"),
                    HtmlTab = new HtmlTab(new
                    {
                        CH1 = new HtmlImage(SelectCalibrateItemDto.Channel1ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH2 = new HtmlImage(SelectCalibrateItemDto.Channel2ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH3 = new HtmlImage(SelectCalibrateItemDto.Channel3ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }

            async Task<(bool IsSuccess, double Result)> GetPowerAsync(OpticsPolarizationModeEnum opticsPolarizationModeEnum)
            {
                LaserViewModel.ToggleOpticsPolarizationMode(opticsPolarizationModeEnum);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                var result = LaserViewModel.GetOpticalMeasurePower();
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);

                return (true, result);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            Cache.ProductivityInformation = SelectReviewItemDto.ProductivityInformation;
            Cache.LaserLightInformation = SelectReviewItemDto.LaserLightInformation;

            var item = Cache.CurrentCalibrationCacheItem;
            var darkFieldImageListToPrescanListCacheItem = Cache.CurrentDarkFieldImageListToPrescanListCacheItem;
            var servings = darkFieldImageListToPrescanListCacheItem.Servings;
            var judgeDarkFieldImageListRateSkipCout = item.JudgeDarkFieldImageListRateSkipCout;
            var repeatCoefficientInterval = item.RepeatCoefficientInterval;
            var coefficientLimit = item.CoefficientLimit;
            var thresholdDarkFieldImageListRateMin = Cache.VerifyThresholdDarkFieldImageListRateMin;
            var thresholdDarkFieldImageListRateMax = Cache.VerifyThresholdDarkFieldImageListRateMax;
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.PmtId,
                Cache.ProductivityInformation,
                Cache.FindPosition,
                Cache.WidthPixel,
                prescanAODWaveProfiles = string.Join(",", SelectReviewItemDto.PrescanAODWaveformProfileList.Select(t => t.FilePath)),
                servings,
                judgeDarkFieldImageListRateSkipCout,
                Cache.DarkImageListRangeThreshold,
                repeatCoefficientInterval,
                coefficientLimit,
                thresholdDarkFieldImageListRateMin,
                thresholdDarkFieldImageListRateMax,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            SelectReviewItemDto.IsVerified = false;

            var temp = SelectReviewItemDto.Clone();

            LaserViewModel.ToggleOpticsPolarizationMode(OpticsPolarizationModeEnum.P);
            LaserViewModel.SetGain(Cache.CurrentCalibrationCacheItem.LaserIlluminationProfileCalibrationPmt.Gain);

            var prescanDto = AODWaveformProfileFactory.CreatePrescanList(SelectReviewItemDto.PrescanAODWaveformResultList);

            if (TryCheckIlluminationIsOk(detectImageDirectory, prescanDto, in temp, out var result, isCalibrate: false) == false) return false;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                temp.DarkFieldImageListRateMin,
                temp.DarkFieldImageListRateMax,
                PrescanRateList = new HtmlPlot2DLinesChart([
                    (nameof(temp.PrescanRateList), temp.PrescanRateList.ToPoints())
                ], nameof(temp.PrescanRateList)),
                DarkFieldImageList = new HtmlPlot2DLinesChart([
                    (nameof(temp.Channel1DarkFieldImageList), temp.Channel1DarkFieldImageList.ToPoints()),
                    (nameof(temp.Channel2DarkFieldImageList), temp.Channel2DarkFieldImageList.ToPoints()),
                    (nameof(temp.Channel3DarkFieldImageList), temp.Channel3DarkFieldImageList.ToPoints())
                ], "DarkFieldImageList"),
                HtmlTab = new HtmlTab(new
                {
                    CH1 = new HtmlImage(temp.Channel1ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    CH2 = new HtmlImage(temp.Channel2ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    CH3 = new HtmlImage(temp.Channel3ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            SelectReviewItemDto.IsVerified = result;

            if (ToIlluminationIntensity(SelectReviewItemDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                SelectReviewItemDto.IsVerified = false;
                return false;
            }

            DialogWindowProvider.ShowDialog($"Verify {(SelectReviewItemDto.IsVerified ? "OK" : "Failed")}, Min: {temp.DarkFieldImageListRateMin} Max: {temp.DarkFieldImageListRateMax}", DialogButtonsEnum.OK,
                SelectReviewItemDto.IsVerified ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return SelectReviewItemDto.IsVerified;
        }).ConfigureAwait(false);
    }

    private bool TryCheckIlluminationIsOk(string detectImageDirectory, IReadOnlyList<PrescanAODWaveformProfile> prescanDto, in LaserIlluminationProfileItemDto laserIlluminationProfileItemDto, out bool isOk, bool isCalibrate = true)
    {
        var darkFieldImageListToPrescanListCacheItem = Cache.CurrentDarkFieldImageListToPrescanListCacheItem;
        var judgeDarkFieldImageListRateSkipCout = Cache.CurrentCalibrationCacheItem.JudgeDarkFieldImageListRateSkipCout;
        var pmtCacheItem = Cache.CurrentCalibrationCacheItem.LaserIlluminationProfileCalibrationPmt;
        isOk = false;

        if (isCalibrate)
        {
            foreach (var prescanAODWaveformProfile in prescanDto) prescanAODWaveformProfile.ApplyCoefficientWindowList(laserIlluminationProfileItemDto.PrescanRateList);
        }

        LaserViewModel.SetGain(pmtCacheItem.Gain);

        Thread.Sleep(1000);
        var (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(prescanDto, pmtCacheItem.PmtId, pmtCacheItem.PmtIdPosition);
        using var _1 = channel1DarkFieldImageDto;
        using var _2 = channel2DarkFieldImageDto;
        using var _3 = channel3DarkFieldImageDto;
        if (isSuccess == false) return false;

        var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
        laserIlluminationProfileItemDto.Channel1DarkFieldImageList = Cache.GetDarkFieldImageList([.. channel1DarkFieldImageDto.ProjectionYs]);
        laserIlluminationProfileItemDto.Channel1ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{middleFileDateTimeFormat}_PmtId_{pmtCacheItem.PmtId}_Channel1_{laserIlluminationProfileItemDto.Index}).jpg";
        channel1DarkFieldImageDto.Image.Save(laserIlluminationProfileItemDto.Channel1ImageFilePath);
        laserIlluminationProfileItemDto.Channel2DarkFieldImageList = Cache.GetDarkFieldImageList([.. channel2DarkFieldImageDto.ProjectionYs]);
        laserIlluminationProfileItemDto.Channel2ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{middleFileDateTimeFormat}_PmtId_{pmtCacheItem.PmtId}_Channel2_{laserIlluminationProfileItemDto.Index}).jpg";
        channel2DarkFieldImageDto.Image.Save(laserIlluminationProfileItemDto.Channel2ImageFilePath);
        laserIlluminationProfileItemDto.Channel3ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{middleFileDateTimeFormat}_PmtId_{pmtCacheItem.PmtId}_Channel3_{laserIlluminationProfileItemDto.Index}).jpg";
        channel3DarkFieldImageDto.Image.Save(laserIlluminationProfileItemDto.Channel3ImageFilePath);
        //是否需要进行反转
        if (Cache.CurrentDarkFieldImageListToPrescanListCacheItem.IsReviseDarkFieldImageToPrescan)
        {
            channel1DarkFieldImageDto.ProjectionYs.Reverse();
            channel2DarkFieldImageDto.ProjectionYs.Reverse();
            channel3DarkFieldImageDto.ProjectionYs.Reverse();
        }

        Logger.LogHtmlInformation($"PmtId: {pmtCacheItem.PmtId}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            Cache.MicroscopeLensInformation.LensName,
            Cache.ProductivityInformation,
            Cache.LaserLightInformation,
            pmtCacheItem.PmtId,
            pmtCacheItem.ChannelId,
            pmtCacheItem.PmtIdPosition,
            pmtCacheItem.Gain,
            DarkFieldImageList = new HtmlPlot2DLinesChart([
                ("ch1", channel1DarkFieldImageDto.ProjectionYs.ToPoints()),
                ("ch2", channel2DarkFieldImageDto.ProjectionYs.ToPoints()),
                ("ch3", channel3DarkFieldImageDto.ProjectionYs.ToPoints())
            ], "DarkFieldImageList"),
            HtmlTab = new HtmlTab(new
            {
                CH1 = new HtmlImage(laserIlluminationProfileItemDto.Channel1ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                CH2 = new HtmlImage(laserIlluminationProfileItemDto.Channel2ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                CH3 = new HtmlImage(laserIlluminationProfileItemDto.Channel3ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        var channelDarkFieldImage = (Cache.ChannelId switch
        {
            1 => laserIlluminationProfileItemDto.Channel1ImageFilePath,
            2 => laserIlluminationProfileItemDto.Channel2ImageFilePath,
            3 => laserIlluminationProfileItemDto.Channel3ImageFilePath,
            _ => throw new ArgumentOutOfRangeException()
        });

        List<double> channelProjectionYsList = Cache.ChannelId switch
        {
            1 => [.. channel1DarkFieldImageDto.ProjectionYs],
            2 => [.. channel2DarkFieldImageDto.ProjectionYs],
            3 => [.. channel3DarkFieldImageDto.ProjectionYs],
            _ => throw new ArgumentOutOfRangeException()
        };

        laserIlluminationProfileItemDto.ChannelImageFilePath = channelDarkFieldImage;
        laserIlluminationProfileItemDto.ChannelDarkFieldProjectYsList = Cache.GetDarkFieldImageList(channelProjectionYsList); // 方向

        var darkFieldImageList = laserIlluminationProfileItemDto.ChannelDarkFieldProjectYsList;
        var darkFieldImageListOfServing = darkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList
            .Select(t => t.Average(tt => darkFieldImageList[tt])) // 每份 指定通道投影的均值的集合做平均
            .ToList(); // 每份投影单独平均后的集合，索引是份数

        // 份数去头去尾
        var ceiling = (int)Math.Ceiling((double)judgeDarkFieldImageListRateSkipCout / darkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList[0].Length);
        var skipList = darkFieldImageListOfServing.Skip(ceiling).SkipLast(ceiling).ToList();
        if (Math.Abs(skipList.Max() - skipList.Min()) > Cache.DarkImageListRangeThreshold)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Dark Field Image Over Range({Cache.DarkImageListRangeThreshold}), Please contact after-sales!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        var average = skipList.Average();
        var averageRateList = skipList
            .Select(yValue => yValue / average)
            .ToList(); // 每份均值和总投影均值的比值集合
        laserIlluminationProfileItemDto.DarkFieldImageListRateMin = averageRateList.Min();
        laserIlluminationProfileItemDto.DarkFieldImageListRateMax = averageRateList.Max();

        var thresholdDarkFieldImageListRateMin = isCalibrate ? Cache.CalibrateThresholdDarkFieldImageListRateMin : Cache.VerifyThresholdDarkFieldImageListRateMin;
        var thresholdDarkFieldImageListRateMax = isCalibrate ? Cache.CalibrateThresholdDarkFieldImageListRateMax : Cache.VerifyThresholdDarkFieldImageListRateMax;

        isOk = thresholdDarkFieldImageListRateMin < laserIlluminationProfileItemDto.DarkFieldImageListRateMin
               && laserIlluminationProfileItemDto.DarkFieldImageListRateMin < thresholdDarkFieldImageListRateMax
               && thresholdDarkFieldImageListRateMin < laserIlluminationProfileItemDto.DarkFieldImageListRateMax
               && laserIlluminationProfileItemDto.DarkFieldImageListRateMax < thresholdDarkFieldImageListRateMax;

        return true;
    }

    private (bool IsSuccess,
        DarkFieldImageDto Channel1DarkFieldImageDto,
        DarkFieldImageDto Channel2DarkFieldImageDto,
        DarkFieldImageDto Channel3DarkFieldImageDto)
        GetDarkFieldLineScanImage(IReadOnlyList<PrescanAODWaveformProfile> darkFieldPrescanDto, int pmtId = 8, Point position = default)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum.OI, darkFieldPrescanDto);

        var list = LaserViewModel.GetDarkFieldLineScanImageList(
            CalChipSiteModelEnum.HazeModel,
            position,
            Cache.WidthPixel,
            Cache.ProductivityInformation,
            Cache.OpticsIlluminationModeEnum,
            pmtId,
            StageCoordinateSystemEnum.Bright,
            Cache.CIBConfiguration,
            (true, null),
            false);

        var channel1DarkFieldImageDto = list.Single(t => t.ChannelId == 1);
        var channel2DarkFieldImageDto = list.Single(t => t.ChannelId == 2);
        var channel3DarkFieldImageDto = list.Single(t => t.ChannelId == 3);

        return (
            true,
            channel1DarkFieldImageDto,
            channel2DarkFieldImageDto,
            channel3DarkFieldImageDto
        );
    }

    private IReadOnlyList<(string, IReadOnlyList<Point>)> GetChannelDarkFieldImageList(List<double> result)
        => [("Average", Cache.GetDarkFieldImageList(result).ToPoints())];

    private List<double> GetPrescan1080List(List<double> result)
    {
        var coefficientList = new List<double>();
        for (var serving = 0; serving < Cache.CurrentDarkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList.Count; serving++)
        {
            var sumCoefficient = 0d;
            var coefficientCount = Cache.CurrentDarkFieldImageListToPrescanListCacheItem.ServingToPrescanListIndicesList[serving].Length;
            foreach (var t in Cache.CurrentDarkFieldImageListToPrescanListCacheItem.ServingToPrescanListIndicesList[serving])
            {
                sumCoefficient += result[t];
            }

            for (var i = 0; i < Cache.CurrentDarkFieldImageListToPrescanListCacheItem.ServingToDarkFieldImageListIndicesList[serving].ToList().Count; i++)
            {
                coefficientList.Add(Math.Round(sumCoefficient / coefficientCount, 3));
            }
        }

        return coefficientList;
    }

    private bool ToIlluminationIntensity(LaserIlluminationProfileItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;

        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.PmtId == itemDto.PmtId && t.ProductivityInformation == itemDto.ProductivityInformation && t.LaserLightInformation == itemDto.LaserLightInformation) == false),
            itemDto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependIlluminationProfileCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void Clear()
    {
        IlluminationCoefficientLimitMin = 0;
        IlluminationCoefficientLimitMax = 0;
        IlluminationTargetValue = 0;
        SynchronizationContextProvider.Send(CalibrationLaserIlluminationProfileDtoList.Clear);
    }

    #endregion 校准
}