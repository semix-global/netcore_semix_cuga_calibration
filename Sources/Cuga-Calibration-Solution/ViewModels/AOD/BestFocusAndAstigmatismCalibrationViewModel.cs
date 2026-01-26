using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.AOD.BestFocusAndAstigmatism;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.CIB.IlluminationProfile;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Channels;
using Interpolator = Core.Utilities.Interpolator;


namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(BestFocusAndAstigmatismCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class BestFocusAndAstigmatismCalibrationViewModel() : CalibrationViewModelBase
{
    #region 属性

    private string ChirpFileDirectory => Path.Combine(
        AppHomeDirectory,
        "Chirp",
        nameof(BestFocusAndAstigmatismCalibrationViewModel),
        DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName),
        DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Select Optics Apodization Mode" },
        new() { StepName = "CIB Config" },
        new() { StepName = "P5" },
        new() { StepName = "AOD Waveform Config" },
        new() { StepName = "Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private BestFocusAndAstigmatismDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationAndApodizationStatus> _calibrationStatuses = [];

    [ObservableProperty]
    private BestFocusAndAstigmatismItemDto _selectedCalibratingItem = new();

    [ObservableProperty]
    private IReadOnlyCollection<BestFocusAndAstigmatismChannelItemDto> _selectedCalibratingChannelItems = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<BestFocusAndAstigmatismDTO> _reviews = [];

    [ObservableProperty]
    private BestFocusAndAstigmatismDTO? _selectReviewItemDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private BestFocusAndAstigmatismCache _cache = new();

    [ObservableProperty]
    private BestFocusAndAstigmatismDTO[] _calibrations = [];

    private AODDelayDTO[] LaserAodDelayItemList { get; set; } = [];

    private CIBXPixelSizeDTO[] LaserXPixelSizeItemList { get; set; } = [];

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowBrightFieldViewModel>();

    [ObservableProperty]
    private AlignmentWindowDarkFieldViewModel _alignmentWindowDarkFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowDarkFieldViewModel>();

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODDelayDTO>(out var laserAodDelayItemDtos, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserAodDelayItemList = laserAodDelayItemDtos;

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODAlignmentDTO>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<CIBIlluminationProfileDTO>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<CIBXPixelSizeDTO>(out var laserXPixelSizeItemDtos, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserXPixelSizeItemList = laserXPixelSizeItemDtos;

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPixelSizeItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserLineCentricityItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        AlignmentCacheDarkField = RecipeCacheProvider.GetOrDefault<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<BestFocusAndAstigmatismCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<BestFocusAndAstigmatismDTO>();

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses = ProductivityInformationAndApodizationStatus.CreateList(ApplicationCookie.OpticsMagTypeProductivityInformations);

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibrationStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .OpticsApodizationModeCalibrationStatusList
                        .Single(ttt => ttt.SelectedItem == t.ApodizationModeEnum)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        if (ApplicationCookie.CIBInformations.Contains(Cache.CIBInformation) == false) Cache.CIBInformation = ApplicationCookie.CIBInformations.First();

        if (Cache.PmtConfigList.Count == 0) Cache.PmtConfigList = [.. CalibrationSetting.SettingPmtConfigParam.PmtConfigList.Select(t => t.Clone())];

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PMTInterval;

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

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
                .ThenBy(t => t.ApodizationModeEnum)
        ];

        if (Reviews.All(t => t.IsCalibrated == false))
            return false;

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.ImageCollectionConfiguration.StartPoint);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                if (ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation) == false) Cache.Item.LaserLightInformation = ApplicationCookie.LaserLightInformations.First();

                return true;

            case 2:
                DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                Cache.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;
                return true;

            case 3:
                {
                    Cache.Item.DefaultGenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation.Clone();
                    // var defaultChirpAodWaveProfileLst = ConfigureViewModel.GetChirpAODWaveProfiles(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);
                    // Cache.Item.DefaultGenerateChirpAODWaveformParam.ZeroSampleCount = defaultChirpAodWaveProfileLst[0].ZeroSampleCount;
                    // // 有AOD Delay结果时，默认chirp波形使用该delay值
                    // var laserAodDelayItem = LaserAodDelayItemList.SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation);
                    // if (laserAodDelayItem is not null && laserAodDelayItem.IsOk)
                    // {
                    //     var delayTime = Convert.ToInt32(laserAodDelayItem.RefinedChirpAODDelay);
                    //     Cache.Item.DefaultGenerateChirpAODWaveformParam.ZeroSampleCount = delayTime;
                    // }

                    return true;
                }
            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.ImageCollectionConfiguration.StartPoint);
                return true;
            case 5:
                {
                    Calibrations =
                    [
                        .. Calibrations
                        .Where(t => t.ProductivityInformation != Cache.ProductivityInformation
                                    || t.ApodizationModeEnum != Cache.ApodizationModeEnum)
                    ];

                    CalibrationStatuses.Single(t => t.SelectedItem == Cache.ProductivityInformation)
                        .OpticsApodizationModeCalibrationStatusList
                        .Single(t => t.SelectedItem == Cache.ApodizationModeEnum)
                        .IsCalibrated = true;

                    DialogWindowProvider.ShowDialog($"{Cache.ProductivityInformation}-{Cache.ApodizationModeEnum.ToHexString()} " +
                                                    $"best focus and astigmatism calibration ok!");

                    IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                    if (IsCalibrated == false) CalibrationStepIndex = -1;

                    return true;
                }
            default:
                return true;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand]
    private void ChangeAllSelection(object isSelectAll)
    {
        try
        {
            var isEnabled = Convert.ToBoolean(isSelectAll);
            foreach (var t in Cache.PmtConfigList) t.Enabled = isEnabled;
        }
        catch
        {
            ThrowHelper.ThrowArgumentException("Command Parameter Convert to Boolean Invalid!");
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
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
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ApodizationModeEnum,
                Cache.CalChipSiteModelEnum
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                AstigmatisPMTId = Cache.CIBInformation.PMTId,
                AstigmatismChannelId = Cache.CIBInformation.ChannelId,
                PMTEnableList = new HtmlTable([.. Cache.PmtConfigList.Select(t => (t.Id, t.Enabled))]),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var alignmentResult = new AlignmentResultDto();
            if (Cache.IsDarkFieldAlignment)
            {
                if (AlignmentCacheDarkField.IsOk)
                    alignmentResult = StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        Cache.ProductivityInformation,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum);
                else
                {
                    var alignmentWindowDarkFieldViewModel = AlignmentWindowDarkFieldViewModel;
                    Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel) == true, nameof(alignmentWindowDarkFieldViewModel));
                    AlignmentCacheDarkField = alignmentWindowDarkFieldViewModel.Cache;
                }
            }
            else
            {
                if (AlignmentCacheBrightField.IsOk)
                    alignmentResult = StageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
                else
                {
                    var alignmentWindowBrightFieldViewModel = AlignmentWindowBrightFieldViewModel;
                    Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel) == true, nameof(alignmentWindowBrightFieldViewModel));
                    AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
                }
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                DefaultGenerateChirpAODWaveformParam = new HtmlQuote(Cache.Item.DefaultGenerateChirpAODWaveformParam.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;

                ClearCalibrationTemp();

                // 下发默认波形
                if (LaserViewModel.TrySendAodFile(Cache.ProductivityInformation, Cache.ProductivityInformation.OpticsIlluminationModeEnum, (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation), false, out var errorMessage) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Send Default Aod Wave Failed.Error: " + errorMessage), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                var centerPosition = new Point(
                    (Cache.Item.ImageCollectionConfiguration.StartPoint.X + Cache.Item.ImageCollectionConfiguration.EndPoint.X) / 2,
                    Cache.Item.ImageCollectionConfiguration.StartPoint.Y
                );
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(centerPosition);

                // var (afEcs, afMotor) = LaserViewModel.RuntimeAfCalibration(
                //     Cache.Item.CIBConfiguration,
                //     Cache.CIBInformation,
                //     centerPosition,
                //     Cache.Item.LaserLightInformation,
                //     Cache.ProductivityInformation,
                //     out _,
                //     calChipSiteModelEnum: Cache.CalChipSiteModelEnum,
                //     stageCoordinateSystemEnum: StageCoordinateSystemEnum.Dark,
                //     saveImageFileDirectory: ImageFileDirectory,
                //     logGuid: HtmlLogUniqueId,
                //     logName: "Calibration");

                // Cache.Item.CenterPositionAfEcs = afEcs;
                var zLimitMin = Cache.Item.CenterPositionAfEcs - Cache.Item.ZMinLimit;
                var zLimitMax = Cache.Item.CenterPositionAfEcs + Cache.Item.ZMaxLimit;

                Cache.Item.ImageCollectionConfiguration.ZStartEcs = zLimitMin;
                Cache.Item.ImageCollectionConfiguration.ZEndEcs = zLimitMax;
                Cache.Item.ImageCollectionConfiguration.XSpeedValue = Cache.ProductivityInformation.XSpeedValue;

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageFileDirectory = detectImageDirectory,
                    Cache.Item.IsMultiPMTOnceCollection,
                    Cache.Item.AlgorithmImageQualityTypeEnum,
                    centerPosition,
                    Cache.Item.CenterPositionAfEcs,
                    StartMachinePosition = StageViewModel.DarkFieldToMachinePosition(Cache.Item.ImageCollectionConfiguration.ExtensionStartPoint),
                    EndMachinePosition = StageViewModel.DarkFieldToMachinePosition(Cache.Item.ImageCollectionConfiguration.ExtensionEndPoint),
                    Cache.Item.ZMinLimit,
                    Cache.Item.ZMaxLimit,
                    afEcsLimitMin = zLimitMin,
                    afEcsLimitMax = zLimitMax,
                    Cache.Item.StartSpectralDensity,
                    Cache.Item.SpectralDensityStepCount,
                    SpectralDensityStep = Cache.Item.StepSpectralDensity,
                    DefaultGenerateChirpAODWaveformParam = new HtmlQuote(Cache.Item.DefaultGenerateChirpAODWaveformParam.ToHtmlAnonymous()),
                }), HtmlLogUniqueId.LoggingHtml());

                // 创建一个Channel用于实现生产者-消费者模式
                var channel = Channel.CreateUnbounded<(int index, BestFocusAndAstigmatismItemDto bestFocusAndAstigmatismItemDto)>();

                #region 消费者

                // 启动消费者任务
                var consumerCount = Environment.ProcessorCount;
                var consumerTasks = new List<Task>();

                for (var i = 0; i < consumerCount; i++)
                {
                    consumerTasks.Add(Task.Run(async () =>
                    {
                        await foreach (var (index, bestFocusAndAstigmatismItemDto) in channel.Reader.ReadAllAsync(cancellationToken))
                        {
                            try
                            {
                                GetMultiPMTBestFocusAndAstigmatism(cancellationToken, bestFocusAndAstigmatismItemDto, index);

                                AddCalibrationTemp(bestFocusAndAstigmatismItemDto);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error processing spectral density {bestFocusAndAstigmatismItemDto.SpectralDensity}: {ex}"), HtmlLogUniqueId.LoggingHtml());
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                    }, cancellationToken));
                }

                #endregion

                #region 生产者

                // 启动生产者任务
                var producerTask = Task.Run(async () =>
                {
                    try
                    {
                        foreach (var (index, spectralDensity) in Enumerable.Range(0, Cache.Item.SpectralDensityStepCount)
                                     .Select(t => Cache.Item.StartSpectralDensity + t * Cache.Item.StepSpectralDensity)
                                     .Select((d, i) => (i, d)))
                        {
                            var bestFocusAndAstigmatismItemDto = await GetMultiPMTDarkFieldLineScanImageListAsync(spectralDensity, cancellationToken);

                            // 将任务发送到通道
                            await channel.Writer.WriteAsync((index, bestFocusAndAstigmatismItemDto), cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error writing spectral density: {ex}"), HtmlLogUniqueId.LoggingHtml());
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    finally
                    {
                        // 关闭写入端，表示生产者已经完成
                        channel.Writer.Complete();
                    }
                }, cancellationToken);

                #endregion

                // 等待所有任务完成
                await Task.WhenAll(producerTask, Task.WhenAll(consumerTasks));

                // var listRow = CalibratingItem.Items.Select(t => GuardUtils.IsNotNullAndReturn(t.SingleOrDefaultChannelItem(Cache.CIBInformation.PMTId, Cache.CIBInformation.ChannelId)).YBestFocusEcs);
                //
                // var listCol = CalibratingItem.Items.Select(t => 1 / t.SpectralDensity);
                //
                // var (k, b, _, _) = PolynomialLeastSquares.Polynomial1Fit(Vector<double>.Build.DenseOfEnumerable(listRow), Vector<double>.Build.DenseOfEnumerable(listCol));
                //
                // var resultItemDto = CalibratingItem.Items.Minima(t =>
                //     Math.Abs(GuardUtils.IsNotNullAndReturn(t.SingleOrDefaultChannelItem(Cache.CIBInformation.PMTId, Cache.CIBInformation.ChannelId)).XYBestFocusOffsetEcs)
                // ).First();
                //
                // var channelItem = GuardUtils.IsNotNullAndReturn(resultItemDto.SingleOrDefaultChannelItem(Cache.CIBInformation.PMTId, Cache.CIBInformation.ChannelId));
                // var xyEcsOffset = channelItem.XYBestFocusOffsetEcs;
                // var calibrationResult = Math.Abs(xyEcsOffset) < Cache.Item.XYBestFocusEcsOffsetThreshold;
                // if (calibrationResult) return true;
                //
                // // todo:迭代
                // var spectralDensity = Math.Round(1.0 / (k * channelItem.XBestFocusEcs + b), 2);
                // if (double.IsNaN(spectralDensity) || spectralDensity == 0)
                // {
                //     DialogWindowProvider.ShowDialog("Get Rate Change By Relational function Failed! The Points is not enough!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                //     ThrowHelper.ThrowArgumentOutOfRangeException(nameof(spectralDensity));
                // }
                //
                // var darkFieldImageDtoList = GetMultiPMTDarkFieldLineScanImageList(spectralDensity);
                // var resultItem = GetMultiPMTBestFocusAndAstigmatism(cancellationToken, darkFieldImageDtoList, spectralDensity);
                // channelItem = GuardUtils.IsNotNullAndReturn(resultItem.SingleOrDefaultChannelItem(Cache.CIBInformation.PMTId, Cache.CIBInformation.ChannelId));
                // xyEcsOffset = channelItem.XYBestFocusOffsetEcs;
                // calibrationResult = Math.Abs(xyEcsOffset) < Cache.Item.XYBestFocusEcsOffsetThreshold;

                var calibrationResult = true;

                CalibratingItem.IsCalibrated = calibrationResult;

                Guard.IsTrue(Save(CalibratingItem, cancellationToken));

                Logger.LogHtmlInformation($"Calibration {(calibrationResult ? "Success" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    StartMachinePosition = StageViewModel.DarkFieldToMachinePosition(Cache.Item.ImageCollectionConfiguration.ExtensionStartPoint),
                    EndMachinePosition = StageViewModel.DarkFieldToMachinePosition(Cache.Item.ImageCollectionConfiguration.ExtensionEndPoint),
                    ImageCollectionConfiguration = new HtmlQuote(Cache.Item.ImageCollectionConfiguration.ToHtmlAnonymous()),
                    Result = new HtmlQuote(CalibratingItem.ToFlatnessHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());

                return calibrationResult;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Calibrate Failed.Error: {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                if (LaserViewModel.TrySendAodFile(Cache.ProductivityInformation, Cache.ProductivityInformation.OpticsIlluminationModeEnum, (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation), false, out var errorMessage) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Send Default Aod Wave Failed.Error: " + errorMessage), HtmlLogUniqueId.LoggingHtml());
                }
            }
        });

        //迭代，根据拟合一次函数，首次输入ecsX，得到F0下发，后续迭代代入deltaEcs，频率变化率根据斜率改变deltaRateChange，得到新的F下发
    }

    private void GetMultiPMTBestFocusAndAstigmatism(CancellationToken cancellationToken, BestFocusAndAstigmatismItemDto bestFocusAndAstigmatismItemDto, int index = 0)
    {
        // 处理当前批次的图像数据
        foreach (var channelItemDto in bestFocusAndAstigmatismItemDto.ChannelItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = $"(PMT{channelItemDto.PmtId},Channel{channelItemDto.ChannelId})_Guid({Guid.NewGuid()}).jpg";
            var filePath = Path.Combine(ImageFileDirectory, $"{Cache.ProductivityInformation}-{Cache.ApodizationModeEnum.ToDescriptionOrString()}");
            var originImageFilePath = Path.Combine(filePath, "Origin", fileName);
            var linearImageFilePath = Path.Combine(filePath, "Linear", fileName);

            var bytes = File.ReadAllBytes(channelItemDto.RawFilePath);
            var image = RawImageFactory.CreateImage(bytes);

            using var darkFieldImageDto = new DarkFieldImageDTO { Image = image };
            darkFieldImageDto.Image.Save(originImageFilePath);

            var linerImage = CalibrationAlgorithmService.DarkFieldRawImageToLinearImage(darkFieldImageDto.Image);
            linerImage.Save(linearImageFilePath);

            var inputDarkFieldImage = Cache.Item.CIBConfiguration is { IsAutoGainControl: true, CIBProfileMode: CIBProfileModeEnum.PMTLog }
                ? linerImage
                : darkFieldImageDto.Image;

            if (Cache.Item.AlgorithmImageQualityTypeEnum is not AlgorithmImageQualityTypeEnum.StrehlRatio)
                throw new NotImplementedException("Only Strehl Ratio is implemented in this version.");

            var resultPlots = CalibrationAlgorithmService.GetXYStrehlRatio(inputDarkFieldImage);

            var timeSamplesCount = (bestFocusAndAstigmatismItemDto.TriggerEndIndex - bestFocusAndAstigmatismItemDto.TriggerStartIndex) + 1;
            var ecsBuffers = bestFocusAndAstigmatismItemDto.TraceBuffers.Skip(bestFocusAndAstigmatismItemDto.TriggerStartIndex).Take(timeSamplesCount).Select(t => (t.Trigger, t.Ecs)).ToList();
            // 用x采样率插值ECS buffer
            var (interpolationX, interpolationY) = Interpolator.SplineInterpolation(
                Vector<double>.Build.Dense([.. ecsBuffers.Select((t, i) => i)]),
                Vector<double>.Build.Dense([.. ecsBuffers.Select(t => t.Ecs)]),
                (Convert.ToInt32(bestFocusAndAstigmatismItemDto.LineScanRate / Cache.TraceBufferSamplingRate)),
                3);

            var ecsInterpolationBuffers = interpolationX.Index().Select(t => (Pixel: t.Index, ECS: interpolationY[t.Index])).ToList();

            Point[] xFitPoints = [];
            Point[] yFitPoints = [];
            Point[] grayFitPoints = [];
            try
            {
                xFitPoints = CalibrationAlgorithmService.SmoothStrehlFunction(resultPlots.Select(t => t.Position.X).ToArray(), resultPlots.Select(t => t.XStrehlRatio).ToArray());
                yFitPoints = CalibrationAlgorithmService.SmoothStrehlFunction(resultPlots.Select(t => t.Position.X).ToArray(), resultPlots.Select(t => t.YStrehlRatio).ToArray());
                grayFitPoints = CalibrationAlgorithmService.SmoothStrehlFunction(resultPlots.Select(t => t.Position.X).ToArray(), resultPlots.Select(t => t.GrayValue).ToArray());
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Get XYStrehlRatio Failed.Error: {ex}"), HtmlLogUniqueId.LoggingHtml());
            }

            var xBestFocusXPixel = xFitPoints.Length > 0 ? xFitPoints.Maxima(t => t.Y).First().X : 0;
            var yBestFocusXPixel = yFitPoints.Length > 0 ? yFitPoints.Maxima(t => t.Y).First().X : 0;

            channelItemDto.OriginFilePath = originImageFilePath;
            channelItemDto.LinearFilePath = linearImageFilePath;
            channelItemDto.ECSInterpolationBuffers = ecsInterpolationBuffers.AsReadOnly();
            channelItemDto.Positions = resultPlots.Select(t => t.Position).ToList().AsReadOnly();
            channelItemDto.XQualitys = resultPlots.Select(t => t.XStrehlRatio).ToList().AsReadOnly();
            channelItemDto.YQualitys = resultPlots.Select(t => t.YStrehlRatio).ToList().AsReadOnly();
            channelItemDto.GrayValues = resultPlots.Select(t => t.GrayValue).ToList().AsReadOnly();
            channelItemDto.XFitPositions = xFitPoints;
            channelItemDto.YFitPositions = yFitPoints;
            channelItemDto.GrayFitPositions = grayFitPoints;

            channelItemDto.XBestFocusEcs = xBestFocusXPixel >= 0 && xBestFocusXPixel < ecsInterpolationBuffers.Count ? ecsInterpolationBuffers[Convert.ToInt32(xBestFocusXPixel)].ECS : ecsInterpolationBuffers[0].ECS;
            channelItemDto.YBestFocusEcs = yBestFocusXPixel >= 0 && yBestFocusXPixel < ecsInterpolationBuffers.Count ? ecsInterpolationBuffers[Convert.ToInt32(yBestFocusXPixel)].ECS : ecsInterpolationBuffers[0].ECS;
        }

        Logger.LogHtmlInformation($"spectralDensity: {bestFocusAndAstigmatismItemDto.SpectralDensity} Times: {index}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            Result = new HtmlQuote(bestFocusAndAstigmatismItemDto.ToFlatnessHtmlAnonymous()),
            Details = new HtmlContainer([
                    ..bestFocusAndAstigmatismItemDto.ChannelGroupItemDtoList.Select(t =>
                        new HtmlExpand(t.ChannelName,
                            new HtmlTable([
                                ..t.ChannelItems
                                    .Select(tt => tt.ToFlatnessHtmlAnonymous())
                            ])))
                ]
            ),
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private async Task<BestFocusAndAstigmatismItemDto> GetMultiPMTDarkFieldLineScanImageListAsync(double spectralDensity, CancellationToken cancellationToken)
    {
        var (generateChirpAODWaveformParam, chirpAODWaveformProfiles) = GenerateAndSendChirpAodWave(spectralDensity, cancellationToken);

        var isAppliedLineCentricityResult = CacheProvider.GetOrDefaultArray<LaserLineCentricityItemDto>()
            .SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation
                                  && t.OpticsIlluminationMode == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                  && t is { PmtId: CalibrationConstantsHelper.MainPmtId, IsOk: true }) is not null;

        var time = Cache.Item.ImageCollectionConfiguration.XUniformTime + 10d;
        var task = Task.Run(() => AfViewModel.GetZAndXSyncModeTraceBufferList(TimeSpan.FromSeconds(time)), cancellationToken);
        var darkFieldImageDtoList = Cache.Item.IsMultiPMTOnceCollection
            ? LaserViewModel.GetMultiPMTDarkFieldLineScanImageByOnce(
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum,
                StageCoordinateSystemEnum.Dark,
                Cache.Item.ImageCollectionConfiguration,
                (false, Cache.Item.LaserLightInformation),
                true,
                Cache.Item.CIBConfiguration,
                Cache.PmtConfigList,
                Cache.ApodizationModeEnum)
            : LaserViewModel.GetMultiPMTDarkFieldLineScanImageByEnumerable(
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum,
                StageCoordinateSystemEnum.Dark,
                Cache.Item.ImageCollectionConfiguration,
                (false, Cache.Item.LaserLightInformation),
                true,
                Cache.Item.CIBConfiguration,
                Cache.PmtConfigList,
                Cache.ApodizationModeEnum,
                Cache.PmtInterval,
                isAppliedLineCentricityResult);

        var traceBuffers = await task.ConfigureAwait(false);

        var bestFocusAndAstigmatismItemDto = new BestFocusAndAstigmatismItemDto
        {
            SpectralDensity = spectralDensity,
            GenerateChirpAODWaveformParam = generateChirpAODWaveformParam,
            ChirpAODWaveformProfiles = chirpAODWaveformProfiles,
            TraceBuffers = traceBuffers,
            ChannelItems =
            [
                ..darkFieldImageDtoList.Select(t => new BestFocusAndAstigmatismChannelItemDto
                {
                    PmtId = t.PMTId,
                    ChannelId = t.ChannelId,
                    RawFilePath = t.RawImageFilePath
                })
            ]
        };
        // todo:改成读cuga配置
        bestFocusAndAstigmatismItemDto.LineScanRate = (double)darkFieldImageDtoList.First().Width / (bestFocusAndAstigmatismItemDto.TriggerEndIndex - bestFocusAndAstigmatismItemDto.TriggerStartIndex + 1);
        return bestFocusAndAstigmatismItemDto;
    }

    private void ClearCalibrationTemp()
    {
        CalibratingItem = new()
        {
            ProductivityInformation = Cache.ProductivityInformation.Clone(),
            ApodizationModeEnum = Cache.ApodizationModeEnum
        };
        SynchronizationContextProvider.Send(CalibratingItem.Items.Clear);
    }

    private void AddCalibrationTemp(BestFocusAndAstigmatismItemDto itemDto)
    {
        SynchronizationContextProvider.Send(() => CalibratingItem.Items = [.. CalibratingItem.Items, itemDto]);

        SelectedCalibratingItem = itemDto.Clone();
    }

    private bool Save(BestFocusAndAstigmatismDTO item, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(item);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.ProductivityInformation != item.ProductivityInformation)
                .Where(t => t.ApodizationModeEnum != item.ApodizationModeEnum),
            item.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准

    #region 算法

    private (GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam, IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles) GenerateAndSendChirpAodWave(double spectralDensity, CancellationToken cancellationToken)
    {
        var bandWidth = Cache.Item.DefaultGenerateChirpAODWaveformParam.SoundPacketLength * spectralDensity;
        var generateChirpAODWaveformParam = Cache.Item.DefaultGenerateChirpAODWaveformParam.Clone();
        generateChirpAODWaveformParam.BandWidth = bandWidth;
        generateChirpAODWaveformParam.DirectoryPath = ChirpFileDirectory;

        var (aodWaveformResult, exception) = AODWaveformGenerator1.GenerateChirpAODWaveform(generateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        var chirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);

        //LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, chirpAODWaveformProfiles);

        return (generateChirpAODWaveformParam, chirpAODWaveformProfiles);
    }

    #endregion
}
