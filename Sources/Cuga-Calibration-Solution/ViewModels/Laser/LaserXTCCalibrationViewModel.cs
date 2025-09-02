using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Setting;
using CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Collections.ObjectModel;
using System.IO;

#if NETFRAMEWORK
using MoreLinq.Extensions;

#endif

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserXTCCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXTCCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public IReadOnlyList<LaserLightInformation> LaserLightInformationList => ApplicationCookie.LaserLightInformationList;

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Find Gain" },
        new() { StepName = "Is Revise" },
        new() { StepName = "XTC Calibration" }
    ];

    [ObservableProperty]
    private double _time;

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private SettingDarkFieldGainViewModel _autoGainSettingDarkFieldGainViewModel = HostApplication.GetRequiredService<SettingDarkFieldGainViewModel>();

    [ObservableProperty]
    private ObservableCollection<LaserXTCCalibrationItemDto> _laserXTCCalibrationItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<LaserXTCCalibrationItemDto> _resultLaserXTCCalibrationItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    [ObservableProperty]
    private List<(string Title, double[])> _pointList = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserXTCCalibrationItemDto> _reviewList = [];

    [ObservableProperty]
    private LaserXTCCalibrationItemDto? _selectReviewItemDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserXTCCalibrationCache _cache = new();

    [ObservableProperty]
    private LaserXTCCalibrationItemDto[] _calibrations = [];

    [ObservableProperty]
    private IReadOnlyList<DarkFieldPmtDelayDto> _sampleValueList = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var calChipDto, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = calChipDto;

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

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserXTCCalibrationCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserXTCCalibrationItemDto>();

        if (Cache.MicroscopeLensInformation.LensCode == -1) Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        Cache.FindPosition = MicroscopeCalChip.HazeBrightFieldMachinePosition;
        //MicroscopeViewModel.SwitchGetCurrentMicroscopeLensInformation(Cache.MicroscopeMagnificationEnum);
        StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
        return true;
    }

    protected override Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsMagTypeEnum)
                .ThenBy(t => t.PmtId)
        ];

        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
        return Task.FromResult(true);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                LaserXTCCalibrationItemDtoList = [];
                return true;

            case 2:
                Cache.CurrentDarkFieldImageListToPrescanListCacheItem.Reset();
                return true;

            case 3:
                return true;

            case 4:
                if (ResultLaserXTCCalibrationItemDtoList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find XTC Cib!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    foreach (var (index, LaserXTCCalibrationItemDto) in ResultLaserXTCCalibrationItemDtoList.Select((dto, i) => (i, dto)))
                    {
                        LaserXTCCalibrationItemDto.IsCalibrated = true;
                        if (Save(LaserXTCCalibrationItemDto, cancellationToken, index == ResultLaserXTCCalibrationItemDtoList.Count - 1)) continue;

                        LaserXTCCalibrationItemDto.IsCalibrated = true;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                //DialogWindowProvider.ShowDialog("Find XTC Ok!");

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
    private void Review(LaserXTCCalibrationItemDto laserXTCCalibrationItemDto)
    {
        DialogWindowProvider.ShowImage([
            (laserXTCCalibrationItemDto.Channel1ImageFilePath, "ch1"),
            (laserXTCCalibrationItemDto.Channel1ImageFilePath, "ch2"),
            (laserXTCCalibrationItemDto.Channel1ImageFilePath, "ch3")
        ]);
    }

    [RelayCommand]
    private void ImageProjectionYsReview(LaserXTCCalibrationItemDto laserXTCCalibrationItemDto)
    {
        DialogWindowProvider.ShowPlot([
            ("ch1", laserXTCCalibrationItemDto.Channel1DarkFieldImageProjectionYs),
            ("ch2", laserXTCCalibrationItemDto.Channel2DarkFieldImageProjectionYs),
            ("ch3", laserXTCCalibrationItemDto.Channel3DarkFieldImageProjectionYs)
        ]);
    }

    [RelayCommand]
    private void ChangePrescanFile()
    {
        var dialog = DialogWindowProvider.TryShowSelectFilePathDialog(".txt", out var filePath);
        if (dialog == false) return;

        Cache.PrescanFilePath = filePath;
    }

    [RelayCommand]
    private async Task GetPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                Cache.FindPosition = result;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoPointAsync(string name)
    {
        try
        {
            await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
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
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;
            Cache.FilePath = detectImageDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.OpticsMagTypeEnum,
                Cache.LaserLightInformation,
                Cache.FindPosition,
                Cache.WidthPixel
            }), HtmlLogUniqueId.LoggingHtml());

            // 先从第8个PMT开始，然后调整偏移量 把前7和后7确认好
            List<LaserXTCCalibrationItemDto> pmtList =
            [
                new()
                {
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    PmtId = 8,
                    FindPosition = Cache.FindPosition
                }
            ];
            //前7倒叙计算
            for (var i = 7; i >= 1; i--)
            {
                var pmt = new LaserXTCCalibrationItemDto
                {
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    PmtId = i,
                    FindPosition = Cache.FindPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i))
                };
                pmtList.Add(pmt);
            }

            //// 后7正序计算
            for (var i = 9; i <= 15; i++)
            {
                var pmt = new LaserXTCCalibrationItemDto
                {
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    PmtId = i,
                    FindPosition = Cache.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8))
                };
                pmtList.Add(pmt);
            }

            var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;
            if (pmtConfig?.Count > 0)
            {
                LaserXTCCalibrationItemDtoList = [.. pmtList.Where(t => pmtConfig[t.PmtId - 1].Enabled).ToList()];
            }

            LaserViewModel.ToggleOpticsPolarization(OpticsPolarizationTypeEnum.P);

            foreach (var pmtItem in LaserXTCCalibrationItemDtoList)
            {
                var (isSuccess, gain) = await AutoGainSettingDarkFieldGainViewModel.AutoPmtGainAsync(Cache.LaserLightInformation.Coefficient, pmtItem.FindPosition, CalChipSiteModelEnum.HazeModel, HtmlLogUniqueId, cancellationToken, false, pmtItem.PmtId, 3, Cache.OpticsMagTypeEnum).ConfigureAwait(false);
                if ((isSuccess) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Auto Pmt Gain Error!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                LaserViewModel.SetGain(gain);

                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                pmtItem.Gain = gain;

                Logger.LogHtmlInformation($"PmtId: {pmtItem.PmtId}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    pmtItem.PmtId,
                    pmtItem.FindPosition,
                    Gain = gain
                }), HtmlLogUniqueId.LoggingHtml());
            }

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var item = Cache.CurrentDarkFieldImageListToPrescanListCacheItem;
            var prescanFilePath = Cache.PrescanFilePath;
            var maxWindowStartIndex = item.MaxWindowStartIndex;
            var minWindowStartIndex = item.MinWindowStartIndex;
            var windowToMinAmount = item.WindowToMinAmount;
            var judgeWindowStartIndex = item.JudgeWindowStartIndex;
            var judgeWindowEndIndex = item.JudgeWindowEndIndex;
            var servings = item.Servings;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.OpticsMagTypeEnum,
                Cache.LaserLightInformation,
                Cache.FindPosition,
                Cache.WidthPixel,
                prescanFilePath,
                maxWindowStartIndex,
                minWindowStartIndex,
                windowToMinAmount,
                judgeWindowStartIndex,
                judgeWindowEndIndex
            }), HtmlLogUniqueId.LoggingHtml());
            if (item.IsOk)
            {
                LoggerResult();
                return true;
            }

            item.Reset();

            if (File.Exists(prescanFilePath) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Please select a Prescan File!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var yPixelHeight = LaserViewModel.GetDarkFieldLineScanImageYPixelHeight(Cache.OpticsMagTypeEnum);

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

            var prescanDto = AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode1, prescanFilePath, CalibrationSetting.SettingCommonParam.MainLaserLightInformation.Coefficient);

            LaserViewModel.SetGain(LaserXTCCalibrationItemDtoList.SingleOrDefault(t => t.PmtId == 8).Gain);

            #region Max窗口

            cancellationToken.ThrowIfCancellationRequested();
            var windowPrescanList = SetPrescanByteListByWindow(maxWindowStartIndex, windowToMinAmount);
            var temp = prescanDto.Clone();
            temp.ApplyCoefficientWindowList(windowPrescanList);
            var (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(temp, LaserXTCCalibrationItemDtoList.SingleOrDefault(t => t.PmtId == 8));
            using var _1 = channel1DarkFieldImageDto;
            using var _2 = channel2DarkFieldImageDto;
            using var _3 = channel3DarkFieldImageDto;
            if (isSuccess == false) return false;

            var sgolayfiltList = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(channel3DarkFieldImageDto.ProjectionYs));
            item.MaxWindowDarkImageListMinIndex = judgeWindowStartIndex + sgolayfiltList.SubVector(judgeWindowStartIndex, judgeWindowEndIndex - judgeWindowStartIndex + 1).MinimumIndex();
            item.MaxWindowPrescanList = windowPrescanList;
            item.MaxScatterDarkFieldImageList = [.. channel3DarkFieldImageDto.ProjectionYs];
            item.MaxSmoothDarkFieldImageList = [.. sgolayfiltList];

            #endregion Max窗口

            #region Min窗口

            cancellationToken.ThrowIfCancellationRequested();
            windowPrescanList = SetPrescanByteListByWindow(minWindowStartIndex, windowToMinAmount);
            temp = prescanDto.Clone();
            temp.ApplyCoefficientWindowList(windowPrescanList);
            (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(temp, LaserXTCCalibrationItemDtoList.SingleOrDefault(t => t.PmtId == 8));
            using var _4 = channel1DarkFieldImageDto;
            using var _5 = channel2DarkFieldImageDto;
            using var _6 = channel3DarkFieldImageDto;
            if (isSuccess == false) return false;

            sgolayfiltList = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable([.. channel3DarkFieldImageDto.ProjectionYs]));

            item.MinWindowDarkImageListMinIndex = judgeWindowStartIndex + sgolayfiltList.SubVector(judgeWindowStartIndex, judgeWindowEndIndex - judgeWindowStartIndex + 1).MinimumIndex();
            item.MinWindowPrescanList = windowPrescanList;
            item.MinScatterDarkFieldImageList = [.. channel3DarkFieldImageDto.ProjectionYs];
            item.MinSmoothDarkFieldImageList = [.. sgolayfiltList];

            item.IsReviseDarkFieldImageToPrescan =
                item.MinWindowDarkImageListMinIndex > item.MaxWindowDarkImageListMinIndex; // 需要反向
            LoggerResult();

            #endregion Min窗口

            return true;

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
                    ], "DarkFieldImageList"),
                    item.IsReviseDarkFieldImageToPrescan
                }), HtmlLogUniqueId.LoggingHtml());
            }

            List<double> SetPrescanByteListByWindow(int startIndex, int windowToMinAmountTemp)
            {
                var k = 1d / windowToMinAmountTemp;
                var prescanList = prescanDto.ShortList;
                var middleIndex = startIndex + windowToMinAmountTemp;
                var endIndex = startIndex + windowToMinAmountTemp * 2;
                if (endIndex > prescanList.Count) throw new CalibrationException($"{nameof(endIndex)}: {endIndex} > {nameof(prescanList)}{nameof(prescanList.Count)}: {prescanList.Count}");

                var coefficient = CalibrationSetting.SettingCommonParam.MainLaserLightInformation.Coefficient;
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

    [RelayCommand]
    private void Step2CalibrateClear()
    {
        Cache.CurrentDarkFieldImageListToPrescanListCacheItem.Reset();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            ClearCalibrationTemp();
            //获取CH1,CH2,CH3的值
            var sampleValueCH = LaserViewModel.GetCIBDelayList();
            SampleValueList = sampleValueCH;

            foreach (var laserXTCCalibrationItemDto in LaserXTCCalibrationItemDtoList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                laserXTCCalibrationItemDto.CH1Delay = SampleValueList.FirstOrDefault(t => t.PmtId == laserXTCCalibrationItemDto.PmtId && t.ChannelId == 1).PmtDelay;
                laserXTCCalibrationItemDto.CH2Delay = SampleValueList.FirstOrDefault(t => t.PmtId == laserXTCCalibrationItemDto.PmtId && t.ChannelId == 2).PmtDelay;
                laserXTCCalibrationItemDto.CH3Delay = SampleValueList.FirstOrDefault(t => t.PmtId == laserXTCCalibrationItemDto.PmtId && t.ChannelId == 3).PmtDelay;

                var (isSuccessPmtDelay, ch1PmtDelay, ch2PmtDelay) = GetXTCCalibration(laserXTCCalibrationItemDto);
                if (!isSuccessPmtDelay) return false;

                laserXTCCalibrationItemDto.CH1Delay = Cache.CurrentDarkFieldImageListToPrescanListCacheItem.IsReviseDarkFieldImageToPrescan ? laserXTCCalibrationItemDto.CH1Delay + ch1PmtDelay : laserXTCCalibrationItemDto.CH1Delay - ch1PmtDelay;
                SampleValueList.FirstOrDefault(t => t.PmtId == laserXTCCalibrationItemDto.PmtId && t.ChannelId == 1).PmtDelay = Convert.ToInt32(laserXTCCalibrationItemDto.CH1Delay);

                laserXTCCalibrationItemDto.CH2Delay = Cache.CurrentDarkFieldImageListToPrescanListCacheItem.IsReviseDarkFieldImageToPrescan ? laserXTCCalibrationItemDto.CH2Delay + ch2PmtDelay : laserXTCCalibrationItemDto.CH2Delay - ch2PmtDelay;
                SampleValueList.FirstOrDefault(t => t.PmtId == laserXTCCalibrationItemDto.PmtId && t.ChannelId == 2).PmtDelay = Convert.ToInt32(laserXTCCalibrationItemDto.CH2Delay);

                SynchronizationContextProvider.Send(() => ResultLaserXTCCalibrationItemDtoList.Add(laserXTCCalibrationItemDto));

                Logger.LogHtmlInformation($"PMT ID: {laserXTCCalibrationItemDto.PmtId},OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    laserXTCCalibrationItemDto.OpticsMagTypeEnum,
                    laserXTCCalibrationItemDto.PmtId,
                    laserXTCCalibrationItemDto.FindPosition,
                    laserXTCCalibrationItemDto.CH1Delay,
                    laserXTCCalibrationItemDto.CH2Delay,
                    laserXTCCalibrationItemDto.CH3Delay
                }), HtmlLogUniqueId.LoggingHtml());
            }

            //把更新后的值返回给 cuga 接口
            LaserViewModel.SetCIBDelayList(SampleValueList);
            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        Cache.OpticsMagTypeEnum = SelectReviewItemDto.OpticsMagTypeEnum;
        await InvokeVerifyAsync(() =>
        {
            ClearCalibrationTemp();
            VerifyFileName = $"_PmtId_{SelectReviewItemDto.PmtId}";
            var result = false;
            var (isVerifySuccessPmtDelay, ch1PmtDelay, ch2PmtDelay) = GetXTCCalibration(SelectReviewItemDto);
            if (!isVerifySuccessPmtDelay) return false;

            if (Math.Abs(ch1PmtDelay) <= Cache.Threshold && Math.Abs(ch2PmtDelay) <= Cache.Threshold) result = true;

            if (!result)
            {
                Logger.LogHtmlInformation($"PMT ID: {SelectReviewItemDto.PmtId}, {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    SelectReviewItemDto.PmtId,
                    VerifyCH1DelayOffset = ch1PmtDelay,
                    VerifyCH2DelayOffset = ch2PmtDelay
                }), HtmlLogUniqueId.LoggingHtml());
                SelectReviewItemDto.IsVerified = false;
                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")},PMT ID: {SelectReviewItemDto.PmtId}, " + $"ch1: ({ch1PmtDelay:f2}) ch2: ({ch2PmtDelay:f2})", DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                return false;
            }
            else
            {
                SelectReviewItemDto.IsVerified = true;
                Logger.LogHtmlInformation($"PMT ID: {SelectReviewItemDto.PmtId}, {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    SelectReviewItemDto.PmtId,
                    VerifyCH1DelayOffset = ch1PmtDelay,
                    VerifyCH2DelayOffset = ch2PmtDelay
                }), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, PMT ID: {SelectReviewItemDto.PmtId},ch1: ({ch1PmtDelay:f2}) ch2: ({ch2PmtDelay:f2})", DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                return true;
            }
        }).ConfigureAwait(false);
    }

    private (bool, double Ch1PmtDelay, double Ch1PmtDelay2) GetXTCCalibration(LaserXTCCalibrationItemDto laserXTCCalibrationItemDto)
    {
        var detectImageDirectory = ImageFileDirectory;
        Logger.LogHtmlInformation($"{laserXTCCalibrationItemDto.PmtId} Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.MicroscopeLensInformation.LensName,
            StageSpeedEnum.Low,
            laserXTCCalibrationItemDto.PmtId,
            laserXTCCalibrationItemDto.CH1Delay,
            laserXTCCalibrationItemDto.CH2Delay,
            laserXTCCalibrationItemDto.CH3Delay,
            Cache.WidthPixel,
            Cache.OpticsMagTypeEnum,
            laserXTCCalibrationItemDto.FindPosition,
            Cache.LaserLightInformation,
            laserXTCCalibrationItemDto.Gain,
            Cache.PrescanInterval,
            detectImageDirectory
        }), HtmlLogUniqueId.LoggingHtml());

        LaserViewModel.SetGain(laserXTCCalibrationItemDto.Gain);

        Thread.Sleep(1000);
        var prescanDto = AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode1, Cache.PrescanFilePath, Cache.LaserLightInformation.Coefficient);
        var k = 1d / Cache.PrescanInterval;
        var resultWindow = new List<double>();
        var startIndex = Cache.PrescanStartIndex;
        var midIndex = startIndex + Cache.PrescanInterval;
        var endIndex = midIndex + Cache.PrescanInterval;
        for (var i = 0; i < startIndex; i++)
        {
            resultWindow.Add(Cache.LaserLightInformation.Coefficient);
        }

        for (var i = startIndex; i < midIndex; i++)
        {
            var rate = (1 - (i - startIndex) * k) * Cache.LaserLightInformation.Coefficient;
            resultWindow.Add(rate);
        }

        for (var i = midIndex; i < endIndex; i++)
        {
            var rate = ((i - midIndex) * k + k) * Cache.LaserLightInformation.Coefficient;
            resultWindow.Add(rate);
        }

        for (var i = endIndex; i < prescanDto.ShortList.Count; i++)
        {
            resultWindow.Add(Cache.LaserLightInformation.Coefficient);
        }

        var temp = prescanDto.Clone();
        temp.ApplyCoefficientWindowList(resultWindow);
        var (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(temp, laserXTCCalibrationItemDto);
        if (isSuccess == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Get Dark Field Line Scan Image Error!"), HtmlLogUniqueId.LoggingHtml());
            return (false, 0, 0);
        }

        using var _1 = channel1DarkFieldImageDto;
        using var _2 = channel2DarkFieldImageDto;
        using var _3 = channel3DarkFieldImageDto;
        var dateTime2String = DateTimeHelper.DateTime2String(DateTime.Now, "yyyyMMddHHmmss");
        laserXTCCalibrationItemDto.Channel1ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{dateTime2String}_Channel1).jpg";
        HalconHelper.Save(channel1DarkFieldImageDto.Image, laserXTCCalibrationItemDto.Channel1ImageFilePath);
        laserXTCCalibrationItemDto.Channel2ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{dateTime2String}_Channel2).jpg";
        HalconHelper.Save(channel2DarkFieldImageDto.Image, laserXTCCalibrationItemDto.Channel2ImageFilePath);
        laserXTCCalibrationItemDto.Channel3ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{dateTime2String}_Channel3).jpg";
        HalconHelper.Save(channel3DarkFieldImageDto.Image, laserXTCCalibrationItemDto.Channel3ImageFilePath);

        laserXTCCalibrationItemDto.Channel1DarkFieldImageProjectionYs = channel1DarkFieldImageDto.ProjectionYs;
        laserXTCCalibrationItemDto.Channel2DarkFieldImageProjectionYs = channel2DarkFieldImageDto.ProjectionYs;
        laserXTCCalibrationItemDto.Channel3DarkFieldImageProjectionYs = channel3DarkFieldImageDto.ProjectionYs;
        var sgolayfiltList1 = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(channel1DarkFieldImageDto.ProjectionYs));
        var darkChannel1DarkFieldImageYsMaxPixel = Cache.PrescanSkipCount + Vector<double>.Build.DenseOfEnumerable([.. sgolayfiltList1.Skip(Cache.PrescanSkipCount).SkipLast(Cache.PrescanSkipCount)]).MinimumIndex();
        var sgolayfiltList2 = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(channel2DarkFieldImageDto.ProjectionYs));
        var darkChannel2DarkFieldImageYsMaxPixel = Cache.PrescanSkipCount + Vector<double>.Build.DenseOfEnumerable([.. sgolayfiltList2.Skip(Cache.PrescanSkipCount).SkipLast(Cache.PrescanSkipCount)]).MinimumIndex();
        var sgolayfiltList3 = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(channel3DarkFieldImageDto.ProjectionYs));
        var darkChannel3DarkFieldImageYsMaxPixel = Cache.PrescanSkipCount + Vector<double>.Build.DenseOfEnumerable([.. sgolayfiltList3.Skip(Cache.PrescanSkipCount).SkipLast(Cache.PrescanSkipCount)]).MinimumIndex();

        Logger.LogHtmlInformation($"{laserXTCCalibrationItemDto.PmtId}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            laserXTCCalibrationItemDto.PrescanFilePath,
            DarkChannel1DarkFieldImageYsMaxPixel = darkChannel1DarkFieldImageYsMaxPixel,
            DarkChannel2DarkFieldImageYsMaxPixel = darkChannel2DarkFieldImageYsMaxPixel,
            DarkChannel3DarkFieldImageYsMaxPixel = darkChannel3DarkFieldImageYsMaxPixel,
            DarkFieldImageProjectionYAverage = new HtmlPlot2DLinesChart([
                (nameof(laserXTCCalibrationItemDto.Channel1DarkFieldImageProjectionYs), laserXTCCalibrationItemDto.Channel1DarkFieldImageProjectionYs.ToPoints()),
                (nameof(laserXTCCalibrationItemDto.Channel2DarkFieldImageProjectionYs), laserXTCCalibrationItemDto.Channel2DarkFieldImageProjectionYs.ToPoints()),
                (nameof(laserXTCCalibrationItemDto.Channel3DarkFieldImageProjectionYs), laserXTCCalibrationItemDto.Channel3DarkFieldImageProjectionYs.ToPoints()),
                ("SmoothCh1", sgolayfiltList1.ToPoints()),
                ("SmoothCh2", sgolayfiltList2.ToPoints()),
                ("SmoothCh3", sgolayfiltList3.ToPoints())
            ], "DarkFieldImageList"),
            Image = new HtmlTab(new
            {
                Channel1 = new HtmlImage(laserXTCCalibrationItemDto.Channel1ImageFilePath),
                Channel2 = new HtmlImage(laserXTCCalibrationItemDto.Channel2ImageFilePath),
                Channel3 = new HtmlImage(laserXTCCalibrationItemDto.Channel3ImageFilePath)
            })
        }), HtmlLogUniqueId.LoggingHtml());
        return (true, darkChannel3DarkFieldImageYsMaxPixel - darkChannel1DarkFieldImageYsMaxPixel, darkChannel3DarkFieldImageYsMaxPixel - darkChannel2DarkFieldImageYsMaxPixel);
    }

    private (bool IsSuccess,
        DarkFieldImageDto Channel1DarkFieldImageDto,
        DarkFieldImageDto Channel2DarkFieldImageDto,
        DarkFieldImageDto Channel3DarkFieldImageDto)
        GetDarkFieldLineScanImage(PrescanAODWaveformProfile darkFieldPrescanDto, LaserXTCCalibrationItemDto laserXTCCalibrationItem)
    {
        LaserViewModel.SetPrescanAODWaveProfileList([darkFieldPrescanDto]);

        var list = LaserViewModel.GetDarkFieldLineScanImageList(
            CalChipSiteModelEnum.HazeModel,
            laserXTCCalibrationItem.FindPosition,
            Cache.WidthPixel,
            Cache.OpticsMagTypeEnum,
            StageSpeedEnum.Low,
            laserXTCCalibrationItem.PmtId,
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

    private bool Save(LaserXTCCalibrationItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;

        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.PmtId == itemDto.PmtId && t.OpticsMagTypeEnum == itemDto.OpticsMagTypeEnum) == false),
            itemDto.Clone()
        ];
        if (isSave == false) return true;

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && CacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultLaserXTCCalibrationItemDtoList.Clear);
    }

    #endregion 校准
}