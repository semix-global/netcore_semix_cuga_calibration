using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.MMD;
using Core.Utilities;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.OpticalPowerMeter;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Helpers.Structs;
using Constants = Net.Utilities.Models.Constants;
using Generate = MathNet.Numerics.Generate;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.CIB;

[IOCAppService(ServiceType = typeof(CIBAgingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBAgingWindowViewModel(
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
    public partial CIBAgingCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial CIBAgingResult Result { get; set; } = new();

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

            Cache.CIBMMDCache = applicationCookieService.GetCache<CIBMMDCache>();
            Cache.Agings =
            [
                ..Result.Items[0].Items
                    .Where(t => Cache.CIBMMDCache.UseODFilterMeasurePowerPoints
                        .Select(tt => tt.X)
                        .Contains(t.Coefficient))
                    .Select(t => new CIBAgingSelectItem(t.Coefficient, t.MeasurePower))
            ];
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
            var hazeBFPosition = stageViewModel.MachineToBrightFieldPosition(Cache.CIBMMDCache.HazeFindBFMachinePosition);

            logger.LogHtmlInformation("Aging Test Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                detectImageDirectory,
                MeasureMaxPowerPosition = laserOpticalPowerMeter.MaxMeasurePowerPosition,
                allCibInformations,
                hazeBFPosition,
                Cache.CoefficientStep,
                CalibratingRetryTimes = Cache.FindCoefficientRetryTimes,
                Cache.MeasurePowerThreshold,
                Cache.SampleCount,
                Cache.AgingThreshold,
                Cache.Agings,
                Cache.SelectedAgings
            }), htmlLogUniqueId.LoggingHtml());

            laserViewModel.SetChirpAODWaveProfiles(Cache.CIBMMDCache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.CIBMMDCache.ChirpAODWaveformProfiles);
            cibViewModel.SetCIBProfileModeEnum(allCibInformations, CIBProfileModeEnum.PMTVoltage);
            opticsViewModel.ToggleODFilter(false);

            var gains = Generate.LinearRange(Cache.CIBMMDCache.StartGain, Cache.CIBMMDCache.StepGain, Cache.CIBMMDCache.StopGain);
            Guard.IsNotEmpty(gains);

            try
            {
                foreach (var cibAgingItem in Result.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    cibAgingItem.SelectItems = [..cibAgingItem.Items.Where(t => Cache.SelectedAgings.Contains(new CIBAgingSelectItem(t.Coefficient, t.MeasurePower)))];
                    cibAgingItem.NewItems =
                    [
                        .. Cache.SelectedAgings.Select(t => new CIBMMDDTOItem
                        {
                            Coefficient = t.Coefficient,
                            MeasurePower = t.MeasurePower,
                            Items = [..gains.Select(ttt => new CIBMMDDTOItem.Item { Gain = ttt, PMTValue = double.NaN })]
                        })
                    ];
                }

                foreach (var (coefficientIndex, cibAgingSelectItem) in Cache.SelectedAgings.Index())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.LogHtmlInformation(cibAgingSelectItem.ToString(), HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation("Find Coefficient", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerMeter.MaxMeasurePowerPosition);

                    var times = 0;
                    var currentCoefficient = cibAgingSelectItem.Coefficient;
                    double currentMeasurePower;
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

                        logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                        {
                            times,
                            currentCoefficient,
                            currentMeasurePower
                        }), htmlLogUniqueId.LoggingHtml());

                        if (Math.Abs(currentMeasurePower - cibAgingSelectItem.MeasurePower) / cibAgingSelectItem.MeasurePower <= Cache.MeasurePowerThreshold)
                        {
                            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                            {
                                cibAgingSelectItem.MeasurePower,
                                currentMeasurePower,
                                currentCoefficient,
                                Cache.MeasurePowerThreshold
                            }), htmlLogUniqueId.LoggingHtml());

                            break;
                        }

                        if (currentMeasurePower > cibAgingSelectItem.MeasurePower)
                        {
                            currentCoefficient -= Cache.CoefficientStep;
                        }
                        else
                        {
                            currentCoefficient += Cache.CoefficientStep;
                        }

                        if (++times > Cache.FindCoefficientRetryTimes - 1)
                        {
                            ThrowHelper.ThrowInvalidOperationException($"Failed to find suitable coefficient for target power {cibAgingSelectItem.MeasurePower:0.###} within {Cache.FindCoefficientRetryTimes} attempts");

                            break;
                        }
                    }

                    foreach (var cibAgingItem in Result.Items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        cibAgingItem.NewItems[coefficientIndex].Coefficient = currentCoefficient;
                        cibAgingItem.NewItems[coefficientIndex].MeasurePower = currentMeasurePower;
                    }

                    logger.LogHtmlInformation("Agings", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                    microscopeViewModel.SwitchMicroscopeLensInformation(Cache.CIBMMDCache.MicroscopeLensInformation);
                    stageViewModel.SetAbsoluteStageTheta(0d);
                    stageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);
                    try
                    {
                        cibViewModel.SetGain(allCibInformations, Cache.CIBMMDCache.StartGain);
                        laserViewModel.SetPrescanAODWaveProfiles(Cache.CIBMMDCache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.CIBMMDCache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(currentCoefficient))]);
                        laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                        foreach (var (gainIndex, gain) in gains.Index())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            logger.LogHtmlInformation($"{gain:0.###}", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                            var noProtectedCIBMMDDtos = (IReadOnlyList<CIBAgingItem>)[.. Result.Items.Where(t => t.Items[coefficientIndex].ProtectedOverflowProtectedPMTValueCount < Cache.CIBMMDCache.ProtectedOverflowProtectedPMTValueCount /* 不超过保护次数 */)];
                            if (noProtectedCIBMMDDtos.All(t => double.IsNaN(t.Items[coefficientIndex].Items[gainIndex].PMTValue) == false)) continue;

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

                            logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, htmlLogUniqueId.LoggingHtml());
                            await Task.WhenAll(cibPMTImages.Index().Select(t => Task.Run(() =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var (index, darkFieldImage) = t;
                                using var _ = darkFieldImage;

                                using var hImage = darkFieldImage.Image.ToHImage();
                                var pmtValue = hImage.GetIntensity().Average;

                                var item = noProtectedCIBMMDDtos[index];
                                var itemItem = item.Items[coefficientIndex];
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
                    }
                    finally
                    {
                        laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
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

            return true;
        }, cancellationToken);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Cache.Id = 0;
            Result.Id = 0;
            cacheProvider.Set(Cache, cancellationTokenSource.Token);
            cacheProvider.Set(Result, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
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