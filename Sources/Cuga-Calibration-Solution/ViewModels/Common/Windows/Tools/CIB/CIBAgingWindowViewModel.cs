using System.IO;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Constants = Net.Utilities.Models.Constants;
using Generate = MathNet.Numerics.Generate;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.CIB;

[IOCAppService(ServiceType = typeof(CIBAgingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBAgingWindowViewModel(
    ApplicationCookie applicationCookie,
    LaserViewModel laserViewModel,
    CIBViewModel cibViewModel,
    StageViewModel stageViewModel,
    OpticsViewModel opticsViewModel,
    MicroscopeViewModel microscopeViewModel,
    IApplicationCookieService applicationCookieService,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<CIBAgingWindowViewModel> logger,
    IOptions<ApplicationSetting> options,
    ICacheProvider cacheProvider) : ViewModelBase
{
    public string Name => "CIB Aging";

    public string ImageFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(CIBAgingWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, "AODWaveform", nameof(CIBAgingWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    [ObservableProperty]
    public partial ApplicationCookie ApplicationCookie { get; set; } = applicationCookie;

    [ObservableProperty]
    public partial CIBAgingCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial CIBAgingResult Result { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<CIBAgingItem> SelectedResultItems { get; set; } = [];

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = cacheProvider.GetOrDefault<CIBAgingCache>();
            Result = cacheProvider.GetOrDefault<CIBAgingResult>();
            if (Result.Items.Count <= 0)
            {
                dialogWindowProvider.ShowDialog("No aging result data! Please run aging test first.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            Cache.Agings = [..Cache.CIBMMDCache.NotUseODFilterMeasurePowerPoints.Select(t => new CIBAgingSelectItem(t.X, t.Y))];
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> ActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeAsync("Action", async htmlLogUniqueId =>
        {
            if (Cache.SelectedAgings.Count == 0)
            {
                dialogWindowProvider.ShowDialog("Please select agings!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var detectImageDirectory = ImageFileDirectory;
            var laserOpticalPowerMeter = applicationCookieService.GetCalibrations<LaserOpticalPowerMeterDTO>(cancellationToken)
                .Single(t => t.ProductivityInformation.OpticsIlluminationModeEnum == Cache.CIBMMDCache.ProductivityInformation.OpticsIlluminationModeEnum
                             && t.ProductivityInformation.OpticsMagType == Cache.CIBMMDCache.ProductivityInformation.OpticsMagType
                             && t.IsOk);
            var allCibInformations = Result.Items.Select(t => t.CIBInformation).ToArray();

            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                detectImageDirectory,
                MeasureMaxPowerPosition = laserOpticalPowerMeter.MaxMeasurePowerPosition,
                CIBMMDCache = new HtmlQuote(new
                {
                    Cache.CIBMMDCache.MicroscopeLensInformation,
                    OpticsConfiguration = new HtmlQuote(Cache.CIBMMDCache.OpticsConfiguration.ToHtmlAnonymous()),
                    Cache.CIBMMDCache.HazeFindBFMachinePosition,
                    Cache.CIBMMDCache.ProductivityInformation,
                    GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.CIBMMDCache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                    GenerateChirpAODWaveformParam = new HtmlQuote(Cache.CIBMMDCache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                    Cache.CIBMMDCache.MeasurePowerWaitTime,
                    Cache.CIBMMDCache.PMTValueWaitTime,
                    Cache.CIBMMDCache.StartGain,
                    Cache.CIBMMDCache.StepGain,
                    Cache.CIBMMDCache.StopGain,
                    Cache.CIBMMDCache.ProtectedPMTValue,
                    Cache.CIBMMDCache.ProtectedOverflowProtectedPMTValueCount,
                    Cache.CIBMMDCache.ImageWidth
                }),
                allCibInformations,
                Cache.CoefficientStep,
                Cache.FindCoefficientRetryTimes,
                Cache.MeasurePowerRatioThreshold,
                Cache.AgingPMTValueNoises,
                Cache.AgingSampleCount,
                Cache.AgingRatioThreshold,
                Cache.Agings,
                Cache.SelectedAgings
            }), htmlLogUniqueId.LoggingHtml());

            microscopeViewModel.SwitchMicroscopeLensInformation(Cache.CIBMMDCache.MicroscopeLensInformation);

            var hazeBFPosition = stageViewModel.MachineToBrightFieldPosition(Cache.CIBMMDCache.HazeFindBFMachinePosition);
            stageViewModel.SetAbsoluteStageTheta(0d);
            stageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            Cache.CIBMMDCache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.CIBMMDCache.ProductivityInformation;
            Cache.CIBMMDCache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            var prescanAODWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.CIBMMDCache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
            Cache.CIBMMDCache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
            Cache.CIBMMDCache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

            Cache.CIBMMDCache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.CIBMMDCache.ProductivityInformation;
            Cache.CIBMMDCache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            var chirpAODWaveformResult = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.CIBMMDCache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
            Cache.CIBMMDCache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
            Cache.CIBMMDCache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

            laserViewModel.SetPrescanAODWaveProfiles(Cache.CIBMMDCache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.CIBMMDCache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(Cache.CIBMMDCache.StartCoefficient))]);
            laserViewModel.SetChirpAODWaveProfiles(Cache.CIBMMDCache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.CIBMMDCache.ChirpAODWaveformProfiles);

            cibViewModel.SetCIBProfileModeEnum(allCibInformations, CIBProfileModeEnum.PMTVoltage);
            cibViewModel.SetGain(allCibInformations, Cache.CIBMMDCache.StartGain);
            opticsViewModel.ToggleODFilter(false);

            logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.CIBMMDCache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.CIBMMDCache.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. Cache.CIBMMDCache.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.CIBMMDCache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.CIBMMDCache.ChirpAODWaveformResultFilePath,
                ChirpAODWaveformProfiles = new HtmlTable([.. Cache.CIBMMDCache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
            }), htmlLogUniqueId.LoggingHtml());

            var gains = Generate.LinearRange(Cache.CIBMMDCache.StartGain, Cache.CIBMMDCache.StepGain, Cache.CIBMMDCache.StopGain);
            Guard.IsNotEmpty(gains);

            Result.IsModify = true;
            foreach (var cibAgingItem in Result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                cibAgingItem.SelectItems = [];
                cibAgingItem.NewItems = [];
                foreach (var cibAgingSelectItem in Cache.SelectedAgings)
                {
                    cibAgingItem.SelectItems =
                    [
                        ..cibAgingItem.SelectItems,
                        cibAgingItem.Items.Single(t => cibAgingSelectItem == new CIBAgingSelectItem(t.Coefficient, t.MeasurePower)).Clone()
                    ];
                    cibAgingItem.NewItems =
                    [
                        .. cibAgingItem.NewItems,
                        new CIBMMDDTOItem
                        {
                            Coefficient = cibAgingSelectItem.Coefficient,
                            MeasurePower = cibAgingSelectItem.MeasurePower,
                            Items = [..gains.Select(ttt => new CIBMMDDTOItem.Item { Gain = ttt, PMTValue = double.NaN })]
                        }
                    ];
                }
            }

            Cache.CoefficientFindItems =
            [
                .. Cache.SelectedAgings.Select(t => new CIBAgingCoefficientFindItem
                {
                    SelectItem = t,
                    FindMeasurePowerPoints = [],
                    TargetMeasurePower = t.MeasurePower,
                    UpperMeasurePower = t.MeasurePower * (1 + Cache.MeasurePowerRatioThreshold),
                    LowerMeasurePower = t.MeasurePower * (1 - Cache.MeasurePowerRatioThreshold),
                    AnswerMeasurePowerPoint = null,
                    AnswerMeasurePowerRatio = null
                })
            ];

            try
            {
                foreach (var (coefficientIndex, cibAgingSelectItem) in Cache.SelectedAgings.Index())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.LogHtmlInformation(cibAgingSelectItem.ToString(), HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                    #region 获取功率

                    logger.LogHtmlInformation("Find Coefficient", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerMeter.MaxMeasurePowerPosition);
                    double currentCoefficient;
                    double currentMeasurePower;
                    var targetMeasurePower = cibAgingSelectItem.MeasurePower;
                    var step = Cache.CoefficientStep;
                    var isSuccess = false;
                    try
                    {
                        currentCoefficient = cibAgingSelectItem.Coefficient;
                        double? prevDiff = null;

                        var times = 0;
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header5, htmlLogUniqueId.LoggingHtml());

                            var temp = currentCoefficient;
                            laserViewModel.SetPrescanAODWaveProfiles(Cache.CIBMMDCache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.CIBMMDCache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(temp))]);
                            laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                            await Task.Delay(TimeSpan.FromSeconds(Cache.CIBMMDCache.MeasurePowerWaitTime), cancellationToken).ConfigureAwait(false);

                            try
                            {
                                currentMeasurePower = laserViewModel.GetOpticalMeasurePower();
                            }
                            finally
                            {
                                laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                            }

                            Cache.CoefficientFindItems[coefficientIndex].FindMeasurePowerPoints = [.. ((IReadOnlyList<Point>)[.. Cache.CoefficientFindItems[coefficientIndex].FindMeasurePowerPoints, new Point(currentCoefficient, currentMeasurePower)]).OrderBy(t => t.X)];

                            var diff = currentMeasurePower - targetMeasurePower;
                            var measurePowerRatio = diff / targetMeasurePower;

                            logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                            {
                                times,
                                diff,
                                measurePowerRatio,
                                currentCoefficient,
                                currentMeasurePower,
                                targetMeasurePower
                            }), htmlLogUniqueId.LoggingHtml());

                            if (Math.Abs(measurePowerRatio) <= Cache.MeasurePowerRatioThreshold)
                            {
                                isSuccess = true;

                                Cache.CoefficientFindItems[coefficientIndex].AnswerMeasurePowerPoint = new Point(currentCoefficient, currentMeasurePower);
                                Cache.CoefficientFindItems[coefficientIndex].AnswerMeasurePowerRatio = measurePowerRatio;

                                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                                {
                                    diff,
                                    measurePowerRatio,
                                    currentCoefficient,
                                    currentMeasurePower,
                                    targetMeasurePower
                                }), htmlLogUniqueId.LoggingHtml());

                                break;
                            }

                            if (prevDiff.HasValue && Math.Sign(diff) != Math.Sign(prevDiff.Value)) step /= 2;

                            if (currentMeasurePower > targetMeasurePower)
                            {
                                currentCoefficient -= step;

                                if (currentCoefficient < 0) ThrowHelper.ThrowInvalidOperationException();
                            }
                            else
                            {
                                currentCoefficient += step;

                                if (currentCoefficient > 1) ThrowHelper.ThrowInvalidOperationException();
                            }

                            if (++times > Cache.FindCoefficientRetryTimes - 1) ThrowHelper.ThrowInvalidOperationException($"Failed to find suitable coefficient for target power {targetMeasurePower:0.###} within {Cache.FindCoefficientRetryTimes} attempts");

                            prevDiff = diff;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header5, htmlLogUniqueId.LoggingHtml());

                        continue;
                    }
                    finally
                    {
                        var htmlContainer = new HtmlContainer([.. Cache.CoefficientFindItems[coefficientIndex].ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]);
                        if (isSuccess)
                            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, htmlContainer, htmlLogUniqueId.LoggingHtml());
                        else
                            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, htmlContainer, htmlLogUniqueId.LoggingHtml());
                    }

                    #endregion

                    logger.LogHtmlInformation("Agings", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                    foreach (var cibAgingItem in Result.Items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        cibAgingItem.NewItems[coefficientIndex].Coefficient = currentCoefficient;
                        cibAgingItem.NewItems[coefficientIndex].MeasurePower = currentMeasurePower;
                    }

                    stageViewModel.SetAbsoluteStageTheta(0d);
                    stageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

                    isSuccess = false;
                    try
                    {
                        cibViewModel.SetGain(allCibInformations, Cache.CIBMMDCache.StartGain);
                        laserViewModel.SetPrescanAODWaveProfiles(Cache.CIBMMDCache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.CIBMMDCache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(currentCoefficient))]);
                        laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                        foreach (var (gainIndex, gain) in gains.Index())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            logger.LogHtmlInformation($"{gain:0.###}", HtmlHeaderLevelEnum.Header5, htmlLogUniqueId.LoggingHtml());

                            var noProtectedCIBMMDDtos = (IReadOnlyList<CIBAgingItem>)[.. Result.Items.Where(t => t.NewItems[coefficientIndex].ProtectedOverflowProtectedPMTValueCount < Cache.CIBMMDCache.ProtectedOverflowProtectedPMTValueCount /* 不超过保护次数 */)];
                            if (noProtectedCIBMMDDtos.All(t => double.IsNaN(t.NewItems[coefficientIndex].Items[gainIndex].PMTValue) == false)) continue;

                            var cibInformations = (IReadOnlyList<CIBInformation>)[.. noProtectedCIBMMDDtos.Select(t => t.CIBInformation)];
                            cibViewModel.SetGain(allCibInformations, gain);

                            await Task.Delay(TimeSpan.FromSeconds(Cache.CIBMMDCache.PMTValueWaitTime), cancellationToken).ConfigureAwait(false);

                            var cibPMTImages = await cibViewModel.GetPMTImagesAsync(
                                Cache.CIBMMDCache.ProductivityInformation,
                                StageCoordinateSystemEnum.Dark,
                                hazeBFPosition,
                                Cache.CIBMMDCache.ImageWidth,
                                cibInformations,
                                (true, null),
                                (false, Cache.CIBMMDCache.OpticsConfiguration),
                                (true, null),
                                (true, null),
                                true,
                                cancellationToken);

                            logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header6, htmlLogUniqueId.LoggingHtml());
                            await Task.WhenAll(cibPMTImages.Index().Select(t => Task.Run(() =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var (index, darkFieldImage) = t;
                                using var _ = darkFieldImage;

                                var pmtValue = darkFieldImage.Image.GetIntensity().Average;

                                var item = noProtectedCIBMMDDtos[index];
                                var itemItem = item.NewItems[coefficientIndex];
                                var itemItemData = itemItem.Items[gainIndex];
                                itemItemData.RawImageFilePath = darkFieldImage.RawImageFilePath;

                                if (pmtValue >= Cache.CIBMMDCache.ProtectedPMTValue /* 超过保护值 */) itemItem.ProtectedOverflowProtectedPMTValueCount++;
                                itemItemData.PMTValue = pmtValue;

                                if (gainIndex == gains.Length - 1) /* 最后一次 */ SaveImage();

                                if (itemItem.ProtectedOverflowProtectedPMTValueCount >= Cache.CIBMMDCache.ProtectedOverflowProtectedPMTValueCount /* 超过保护次数 */)
                                {
                                    SaveImage();
                                    cibViewModel.SetGain([item.CIBInformation], Cache.CIBMMDCache.StartGain);
                                }

                                logger.LogHtmlInformation(item.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                                {
                                    itemItemData.Gain,
                                    itemItemData.PMTValue,
                                    itemItemData.RawImageFilePath,
                                    Image = string.IsNullOrWhiteSpace(itemItemData.ImageFilePath) ? (BaseHtmlElement)new HtmlComment("The image was not saved. For details, see the raw file path.") : new HtmlImage(itemItemData.ImageFilePath)
                                }), htmlLogUniqueId.LoggingHtml());

                                return;

                                void SaveImage()
                                {
                                    var imageFilePath = Path.Combine(detectImageDirectory, item.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                                    darkFieldImage.Image.SaveImage(imageFilePath);
                                    itemItemData.ImageFilePath = imageFilePath;
                                }
                            }, cancellationToken)));
                        }

                        isSuccess = true;
                    }
                    finally
                    {
                        laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);

                        var htmlContainer = new HtmlContainer([.. Result.Items.Select(t => new HtmlExpand(t.CIBInformation.ToString(), new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))]);
                        if (isSuccess)
                            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, htmlContainer, htmlLogUniqueId.LoggingHtml());
                        else
                            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, htmlContainer, htmlLogUniqueId.LoggingHtml());
                    }
                }
            }
            finally
            {
                cibViewModel.SetAGC(allCibInformations, true);
                cibViewModel.SetCIBProfileModeEnum(allCibInformations, CIBProfileModeEnum.PMTLog);
                stageViewModel.SetAbsoluteStageTheta(0d);
                stageViewModel.SetBrightFieldAbsoluteStageXy(hazeBFPosition);
            }

            logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

            foreach (var cibAgingItem in Result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Algorithm(cibAgingItem, htmlLogUniqueId);
            }

            var result = SelectedResultItems.All(t => t.IsOk);

            var errorMessageStringBuilder = new StringBuilder();
            foreach (var selectedResultItem in SelectedResultItems)
            {
                errorMessageStringBuilder.AppendLine($"{selectedResultItem.CIBInformation}: {(selectedResultItem.IsOk ? "OK" : "Already aged")}");
            }

            dialogWindowProvider.ShowDialog($"""
                                             Algorithm : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }, cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> AlgorithmAsync(CancellationToken cancellationToken)
    {
        return InvokeAsync("Algorithm", htmlLogUniqueId =>
        {
            if (SelectedResultItems.Count == 0) return Task.FromResult(false);

            logger.LogHtmlInformation("Algorithm Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AgingPMTValueNoises,
                Cache.AgingSampleCount,
                Cache.AgingRatioThreshold
            }), htmlLogUniqueId.LoggingHtml());

            Result.IsModify = true;
            foreach (var cibAgingItem in SelectedResultItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Algorithm(cibAgingItem, htmlLogUniqueId);
            }

            return Task.FromResult(SelectedResultItems.All(t => t.IsOk));
        }, cancellationToken);
    }

    private void Algorithm(CIBAgingItem item, Guid htmlLogUniqueId)
    {
        var htmlList = new List<BaseHtmlElement>();

        var htmlContainer = new HtmlContainer(htmlList);

        item.SampleItems = [];
        item.IsOk = false;

        try
        {
            foreach (var (coefficientIndex, selectItem) in item.SelectItems.Index())
            {
                var newItem = item.NewItems[coefficientIndex];
                var validIndices = Generate.LinearRangeInt32(0, selectItem.Items.Count - 1)
                    .Where(i => double.IsNaN(selectItem.Items[i].PMTValue) == false
                                && double.IsNaN(newItem.Items[i].PMTValue) == false
                                && selectItem.Items[i].PMTValue >= Cache.AgingPMTValueNoises
                                && newItem.Items[i].PMTValue >= Cache.AgingPMTValueNoises)
                    .ToArray();

                if (validIndices.Length == 0) continue;

                var cibAgingSampleItem = new CIBAgingSampleItem
                {
                    Coefficient = selectItem.Coefficient,
                    MeasurePower = selectItem.MeasurePower,
                    Items = [],
                    IsOk = false
                };

                item.SampleItems = [.. item.SampleItems, cibAgingSampleItem];

                foreach (var index in SampleEvenly(validIndices, Cache.AgingSampleCount))
                {
                    var oldPMTValue = selectItem.Items[index].PMTValue;
                    var newPMTValue = newItem.Items[index].PMTValue;
                    var decayRate = oldPMTValue == 0 ? double.PositiveInfinity : (newPMTValue - oldPMTValue) / oldPMTValue;
                    var isOk = decayRate > 0 || Math.Abs(decayRate) <= Cache.AgingRatioThreshold;

                    cibAgingSampleItem.Items =
                    [
                        ..cibAgingSampleItem.Items, new CIBAgingSampleItem.Item
                        {
                            Gain = selectItem.Items[index].Gain,
                            OldPMTValue = oldPMTValue,
                            NewPMTValue = newPMTValue,
                            DecayRate = decayRate,
                            IsOk = isOk
                        }
                    ];
                }

                cibAgingSampleItem.IsOk = cibAgingSampleItem.Items.All(t => t.IsOk);
            }

            item.IsOk = item.SampleItems.Count > 0 && item.SampleItems.All(t => t.IsOk);
        }
        catch (Exception ex)
        {
            htmlList.Add(new HtmlQuote(new
            {
                Exception = ex
            }));
        }
        finally
        {
            htmlList.Add(new HtmlBullet(new
            {
                item.IsOk,
                SampleItems = new HtmlTable([.. item.SampleItems.Select(t => t.ToHtmlAnonymous())]),
                Plot = new HtmlContainer([.. item.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }));

            if (item.IsOk)
                logger.LogHtmlInformation($"OK: {item.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlContainer, htmlLogUniqueId.LoggingHtml());
            else
                logger.LogHtmlError($"Error: {item.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlContainer, htmlLogUniqueId.LoggingHtml());
        }
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Cache.Id = 0;
            cacheProvider.Set(Cache, cancellationTokenSource.Token);

            if (Result.IsModify)
            {
                Result.Id = 0;
                cacheProvider.Set(Result, cancellationTokenSource.Token);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }

    private static IReadOnlyList<T> SampleEvenly<T>(IReadOnlyList<T> items, int sampleCount)
    {
        if (items.Count <= sampleCount) return items;

        var result = new List<T>();
        var binSize = (double)items.Count / sampleCount;

        for (var i = 0; i < sampleCount; i++)
        {
            var startIndex = (int)(i * binSize);
            var endIndex = (int)((i + 1) * binSize);
            var randomIndex = Random.Shared.Next(startIndex, Math.Min(endIndex, items.Count));
            result.Add(items[randomIndex]);
        }

        return result;
    }

    private async Task<bool> InvokeAsync(string title, Func<Guid, Task<bool>> func, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            var isSuccess = false;

            var htmlLogUniqueId = Guid.NewGuid();
            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header2, htmlLogUniqueId.LoggingHtml());

            try
            {
                isSuccess = await func.Invoke(htmlLogUniqueId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: {title} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());
                    return false;
                }

                logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());
                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: {title} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml($"{Name.Replace(" ", string.Empty)}_{title.Replace(" ", string.Empty)}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
                dialogWindowProvider.ShowDialog($"{Name}: {title} Success");
            else
                dialogWindowProvider.ShowDialog($"{Name}: {title} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }, cancellationToken).ConfigureAwait(false);
    }
}