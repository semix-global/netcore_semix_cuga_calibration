using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform.Generates;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.WPF.Enums;
using Python.Runtime;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeDelayWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : AODWaveformElectrodeDelayCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeDelayItem, new()
    where TResult : AODWaveformElectrodeDelayResult, new()
{
    public string PhaseOptimizerStateFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Python", GetType().Name, "optimizer_state.pkl");

    public override string[] Steps { get; } =
    [
        "Step 1 Measure Noise",
        "Step 2 Electrode Delay",
        "Step 3 Generate AOD Waveform",
        "Step 4 Set AOD Waveform Config"
    ];

    private double? _lastCost;

    protected override void Closing()
    {
        TestSetResultAODWaveformConfigurationCancelCommand.Execute(null);

        AllCancelCommand.Execute(null);

        StepFirstLastCancelCommand.Execute(null);
        StepSecondLastCancelCommand.Execute(null);
        Step1CancelCommand.Execute(null);
        Step0CancelCommand.Execute(null);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken)
    {
        const int stepIndex = 0;

        return await InvokeAsync(stepIndex, async () =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.DefaultAmplitude, 0);
            Guard.IsLessThanOrEqualTo(Cache.DefaultAmplitude, 1);
            Guard.IsGreaterThan(Cache.WaitTime, 0);
            Guard.IsGreaterThan(Cache.TotalMeasurePower, 0);
            Guard.IsGreaterThan(Cache.MeasurePowerTimes, 0);

            Guard.IsTrue(Cache.AODWaveformElectrodeDelayFrequencies.Select(t => t.Frequency).IsIncreasing(true));
            Guard.IsTrue(Cache.AODWaveformElectrodeDelayFrequencies.Select(t => t.Amplitude).IsIncreasing(false));
            Guard.IsTrue(Cache.AODWaveformElectrodeDelayFrequencies.Select(t => t.Amplitude).All(t => t is >= 0d and <= 1d));
            Guard.IsGreaterThanOrEqualTo(Cache.AODWaveformElectrodeDelayFrequencies.Length, 2);
            Guard.IsGreaterThan(Cache.DetailLogInterval, 0);

            Guard.IsGreaterThan(Cache.NoiseMeasureTimes, 1);

            if (Cache.AODWaveformScoreMethodEnum == AODWaveformScoreMethodEnum.BandWidth)
            {
                Guard.IsGreaterThan(Cache.BandWidthScoreThreshold, 0);
                Guard.IsLessThanOrEqualTo(Cache.BandWidthScoreThreshold, 1);
                Guard.IsGreaterThanOrEqualTo(Cache.BandWidthScoreEpsilon, 0);
                Guard.IsLessThanOrEqualTo(Cache.BandWidthScoreEpsilon, 1);
            }

            Guard.IsNotEmpty(Cache.ElectrodeDelayParams);

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            OpticsViewModel.ToggleODFilter(false);

            if (isSilent || (DialogWindowProvider.TryShowDialog(
                    "Yes: reset noise measure state. No: continue from the existing state.",
                    out var dialogResult,
                    DialogButtonsEnum.YesNo,
                    DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes))
            {
                Cache.Step0 = new AODWaveformElectrodeDelay<TItem> { StabilityStartIndex = 0 };
            }

            Cache.Noise = 0d;

            var isSuccess = false;
            Guid? detailLogUniqueId = null;
            string? detailLogFileName = null;

            try
            {
                for (var times = 0; times < Cache.NoiseMeasureTimes; times++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentDetailLogUniqueId = detailLogUniqueId ?? StartDetailLog(times);

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                    try
                    {
                        var item = new AODWaveformElectrodeDelayItem<TItem>
                        {
                            Delays = Generate.Repeat(Cache.ElectrodeDelayParams.Length, 0d)
                        };

                        Cache.Step0.Items = [.. Cache.Step0.Items, item];

                        var isCurrentFrequenciesOk = false;
                        try
                        {
                            await UpdateElectrodeDelayItemAsync(item, null, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                            isCurrentFrequenciesOk = true;
                        }
                        finally
                        {
                            if (isCurrentFrequenciesOk == false) Cache.Step0.Items = [.. Cache.Step0.Items.AsSpan()[..^1]];

                            foreach (var temp in Cache.Step0.Items) temp.IsSelected = false;
                        }
                    }
                    finally
                    {
                        if ((times + 1) % Cache.DetailLogInterval == 0)
                        {
                            Guard.IsNotNull(detailLogUniqueId);
                            Guard.IsNotNull(detailLogFileName);

                            EndDetailLog();
                        }
                    }
                }

                var standardDeviation = Cache.Step0.Items.Select(t => t.Score).StandardDeviation();
                Cache.Noise = standardDeviation * standardDeviation;

                isSuccess = true;
            }
            finally
            {
                EndDetailLog();

                var htmlBullet = new HtmlBullet(new
                {
                    Cache.Noise,
                    Step1Items = new HtmlTable(
                    [
                        .. Cache.Step0.Items
                            .Index()
                            .Select(t => new
                            {
                                Index = t.Index + 1,
                                t.Item.Delays,
                                SubtractBoardCardDelays = Cache.ElectrodeDelayParams
                                    .Index()
                                    .Select(tt => t.Item.Delays[tt.Index] - (tt.Index == 0
                                        ? 0d
                                        : tt.Item.BoardCardDelay))
                                    .ToArray(),
                                t.Item.Score,
                                t.Item.IsSelected,
                                FrequencyItems = new HtmlPlot2DLinesChart([(string.Empty, [.. t.Item.FrequencyItems.Select(tt => new Point(tt.Frequency, tt.Amplitude))])], string.Empty)
                            })
                    ]),
                    Step1Plot = new HtmlContainer([.. Cache.Step0.PlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
                    Step1StabilityPlot = new HtmlContainer([.. Cache.Step0.StabilityPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                });

                if (isSuccess)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            }

            if (isSilent == false)
            {
                DialogWindowProvider.ShowDialog(
                    $"""
                     Please check whether the Step 1 Stability Plots are abnormal. Current noise: {Cache.Noise}
                     If they are abnormal, remeasure the noise or input the noise value manually.
                     """);
            }

            return isSuccess;

            Guid StartDetailLog(int times)
            {
                var startTimes = times + 1;
                var stopTimes = Math.Min(times + Cache.DetailLogInterval, Cache.NoiseMeasureTimes);
                var currentDetailLogUniqueId = Guid.NewGuid();

                detailLogUniqueId = currentDetailLogUniqueId;
                detailLogFileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{startTimes}-{stopTimes}";

                var title = $"{startTimes}-{stopTimes}";
                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {detailLogFileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());

                return currentDetailLogUniqueId;
            }

            void EndDetailLog()
            {
                if (detailLogUniqueId is null || detailLogFileName is null)
                {
                    Guard.IsNull(detailLogUniqueId);
                    Guard.IsNull(detailLogFileName);

                    return;
                }

                try
                {
                    Logger.LogHtmlInformation(detailLogUniqueId.Value.LoggedEndHtml(detailLogFileName));
                }
                finally
                {
                    detailLogUniqueId = null;
                    detailLogFileName = null;
                }
            }
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken)
    {
        const int stepIndex = 1;

        return await InvokeAsync(stepIndex, async () =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.DefaultAmplitude, 0);
            Guard.IsLessThanOrEqualTo(Cache.DefaultAmplitude, 1);
            Guard.IsGreaterThan(Cache.WaitTime, 0);
            Guard.IsGreaterThan(Cache.TotalMeasurePower, 0);
            Guard.IsGreaterThan(Cache.MeasurePowerTimes, 0);

            Guard.IsTrue(Cache.AODWaveformElectrodeDelayFrequencies.Select(t => t.Frequency).IsIncreasing(true));
            Guard.IsTrue(Cache.AODWaveformElectrodeDelayFrequencies.Select(t => t.Amplitude).IsIncreasing(false));
            Guard.IsTrue(Cache.AODWaveformElectrodeDelayFrequencies.Select(t => t.Amplitude).All(t => t is >= 0d and <= 1d));
            Guard.IsGreaterThanOrEqualTo(Cache.AODWaveformElectrodeDelayFrequencies.Length, 2);
            Guard.IsGreaterThan(Cache.DetailLogInterval, 0);

            if (Cache.AODWaveformScoreMethodEnum == AODWaveformScoreMethodEnum.BandWidth)
            {
                Guard.IsGreaterThan(Cache.BandWidthScoreThreshold, 0);
                Guard.IsLessThanOrEqualTo(Cache.BandWidthScoreThreshold, 1);
                Guard.IsGreaterThanOrEqualTo(Cache.BandWidthScoreEpsilon, 0);
                Guard.IsLessThanOrEqualTo(Cache.BandWidthScoreEpsilon, 1);
            }

            Guard.IsNotEmpty(Cache.ElectrodeDelayParams);

            Guard.IsGreaterThanOrEqualTo(Cache.Noise, 0d);

            Guard.IsGreaterThan(Cache.AlgorithmMaxDelay, 0d);
            Guard.IsGreaterThan(Cache.AlgorithmInitialPoints, 0);
            Guard.IsGreaterThan(Cache.AlgorithmEarlyStop, 0);
            Guard.IsGreaterThan(Cache.AlgorithmRetryTimes, 0);

            Guard.IsGreaterThan(Cache.StabilityMeasureTimes, 0);

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            OpticsViewModel.ToggleODFilter(false);

            if (isSilent || (DialogWindowProvider.TryShowDialog(
                    "Yes: reset the algorithm phase optimizer state. No: continue from the existing state.",
                    out var dialogResult,
                    DialogButtonsEnum.YesNo,
                    DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes))
            {
                _lastCost = null;

                if (System.IO.File.Exists(PhaseOptimizerStateFilePath))
                {
                    var backupFilePath = $"{PhaseOptimizerStateFilePath}_{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}";
                    System.IO.File.Move(PhaseOptimizerStateFilePath, backupFilePath);
                }

                Cache.Step1 = new AODWaveformElectrodeDelay<TItem>();
            }
            else if (Cache.Step1.StabilityStartIndex is not null)
            {
                Cache.Step1.Items = [.. Cache.Step1.Items.Take(Cache.Step1.StabilityStartIndex.Value)];
                Cache.Step1.StabilityStartIndex = null;
            }

            Cache.ElectrodeConfigurationResults =
            [
                .. Cache.ElectrodeDelayParams.Select(t => new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                    Delay = 0d,
                    Amplitude = 1d, // 生成result默认幅值都是1
                    IsGenerateAODWaveformZero = false
                })
            ];

            if (Cache.ElectrodeDelayParams.Length == 1) return true;

            var isSuccess = false;
            Guid? detailLogUniqueId = null;
            string? detailLogFileName = null;

            try
            {
                var times = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentDetailLogUniqueId = detailLogUniqueId ?? StartDetailLog(times, Cache.AlgorithmRetryTimes);

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                    try
                    {
                        (isSuccess, var delays, var amplitudes) = AlgorithmSuggest(_lastCost, Cache.ElectrodeDelayParams.Length - 1);
                        if (isSuccess) break;

                        var item = new AODWaveformElectrodeDelayItem<TItem>
                        {
                            Delays =
                            [
                                .. Cache.ElectrodeDelayParams
                                    .Index()
                                    .Select(t => t.Index == 0
                                        ? 0d
                                        : Generate.LinearRangeInt32(0, t.Index - 1).Sum(tt => delays[tt]) + t.Item.BoardCardDelay)
                            ]
                        };

                        Cache.Step1.Items = [.. Cache.Step1.Items, item];

                        var isCurrentFrequenciesOk = false;
                        try
                        {
                            await UpdateElectrodeDelayItemAsync(item, amplitudes, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                            isCurrentFrequenciesOk = true;
                        }
                        finally
                        {
                            if (isCurrentFrequenciesOk == false) Cache.Step1.Items = [.. Cache.Step1.Items.AsSpan()[..^1]];

                            Maxima();
                        }

                        _lastCost = -item.Score;

                        if (++times > Cache.AlgorithmRetryTimes - 1)
                        {
                            isSuccess = false;
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("More than the number of times."), HtmlLogUniqueId.LoggingHtml());

                            break;
                        }
                    }
                    finally
                    {
                        if (times % Cache.DetailLogInterval == 0)
                        {
                            Guard.IsNotNull(detailLogUniqueId);
                            Guard.IsNotNull(detailLogFileName);

                            EndDetailLog();
                        }
                    }
                }

                if (isSuccess)
                {
                    EndDetailLog();

                    Maxima();

                    double[] resultDelays = [.. Cache.ElectrodeConfigurationResults.Select(t => t.Delay)];
                    double[] resultAmplitudes =
                    [
                        .. Cache.ElectrodeConfigurationResults[0].UniformityConfigurations.Index().Select(t =>
                        {
                            var frequencyAmplitude = Cache.AODWaveformElectrodeDelayFrequencies[t.Index].Amplitude;
                            var amplitude = frequencyAmplitude == 0d
                                ? 0d
                                : t.Item.Coefficient / frequencyAmplitude;

                            return Math.Clamp(amplitude, 0d, 1d);
                        })
                    ];

                    Cache.Step1.StabilityStartIndex = Cache.Step1.Items.Length;

                    Logger.LogHtmlInformation("Stability Measure", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    for (var stabilityTimes = 0; stabilityTimes < Cache.StabilityMeasureTimes; stabilityTimes++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var currentDetailLogUniqueId = detailLogUniqueId ?? StartDetailLog(stabilityTimes, Cache.StabilityMeasureTimes, "Stability");

                        Logger.LogHtmlInformation($"{stabilityTimes + 1}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                        try
                        {
                            var item = new AODWaveformElectrodeDelayItem<TItem>
                            {
                                Delays = resultDelays
                            };

                            Cache.Step1.Items = [.. Cache.Step1.Items, item];

                            var isCurrentFrequenciesOk = false;
                            try
                            {
                                await UpdateElectrodeDelayItemAsync(item, resultAmplitudes, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                                isCurrentFrequenciesOk = true;
                            }
                            finally
                            {
                                if (isCurrentFrequenciesOk == false) Cache.Step1.Items = [.. Cache.Step1.Items.AsSpan()[..^1]];

                                item.IsSelected = true;
                            }
                        }
                        finally
                        {
                            if ((stabilityTimes + 1) % Cache.DetailLogInterval == 0)
                            {
                                Guard.IsNotNull(detailLogUniqueId);
                                Guard.IsNotNull(detailLogFileName);

                                EndDetailLog();
                            }
                        }
                    }
                }
            }
            finally
            {
                EndDetailLog();

                var htmlBullet = new HtmlBullet(new
                {
                    Cache.Noise,
                    ElectrodeConfigurationResults = new HtmlTable(
                    [
                        .. Cache.ElectrodeConfigurationResults.Index().Select(t =>
                        {
                            var boardCardDelay = Cache.ElectrodeDelayParams[t.Index].BoardCardDelay;

                            return new
                            {
                                t.Item.OpticsAODElectrodeEnum,
                                boardCardDelay,
                                t.Item.Delay,
                                SubtractBoardCardDelay = t.Item.Delay - (t.Index == 0
                                    ? 0d
                                    : boardCardDelay),
                                t.Item.Amplitude,
                                t.Item.IsGenerateAODWaveformZero
                            };
                        })
                    ]),
                    Step2Items = new HtmlTable(
                    [
                        .. Cache.Step1.Items
                            .Index()
                            .Select(t => new
                            {
                                Index = t.Index + 1,
                                t.Item.Delays,
                                SubtractBoardCardDelays = Cache.ElectrodeDelayParams
                                    .Index()
                                    .Select(tt => t.Item.Delays[tt.Index] - (tt.Index == 0
                                        ? 0d
                                        : tt.Item.BoardCardDelay))
                                    .ToArray(),
                                t.Item.Score,
                                t.Item.IsSelected,
                                FrequencyItems = new HtmlPlot2DLinesChart([(string.Empty, [.. t.Item.FrequencyItems.Select(tt => new Point(tt.Frequency, tt.Amplitude))])], string.Empty)
                            })
                    ]),
                    Step2Plot = new HtmlContainer([.. Cache.Step1.PlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
                    Step2StabilityPlot = new HtmlContainer([.. Cache.Step1.StabilityPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                });

                if (isSuccess)
                {
                    var (index, bestScoreItem) = Cache.Step1.Items.Index().First(t => t.Item.IsSelected);

                    Logger.LogHtmlInformation("Best Score Result", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Index = index + 1,
                        bestScoreItem.Score,
                        bestScoreItem.Delays,
                        SubtractBoardCardDelays = Cache.ElectrodeDelayParams
                            .Index()
                            .Select(tt => bestScoreItem.Delays[tt.Index] - (tt.Index == 0
                                ? 0d
                                : tt.Item.BoardCardDelay))
                            .ToArray(),
                        FrequencyItems = new HtmlPlot2DLinesChart([(string.Empty, [.. bestScoreItem.FrequencyItems.Select(tt => new Point(tt.Frequency, tt.Amplitude))])], string.Empty),
                        MeasurePower = new HtmlPlot2DLinesChart([(string.Empty, [.. bestScoreItem.FrequencyItems.Select(tt => new Point(tt.Frequency, tt.MeasurePower))])], string.Empty),
                        PercentMeasurePowerp = new HtmlPlot2DLinesChart([(string.Empty, [.. bestScoreItem.FrequencyItems.Select(tt => new Point(tt.Frequency, tt.MeasurePower / Cache.TotalMeasurePower))])], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());

                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            }

            return isSuccess;

            void Maxima()
            {
                foreach (var temp in Cache.Step1.Items) temp.IsSelected = false;

                if (Cache.Step1.Items.Length > 0)
                {
                    var bestScoreItem = Cache.Step1.Items.Maxima(t => t.Score).First();
                    bestScoreItem.IsSelected = true;

                    foreach (var (index, result) in Cache.ElectrodeConfigurationResults.Index())
                    {
                        result.Delay = bestScoreItem.Delays[index];
                        result.UniformityConfigurations =
                        [
                            .. bestScoreItem.FrequencyItems.Select(t => new GenerateAODWaveformUniformityConfiguration
                            {
                                Frequency = t.Frequency,
                                Coefficient = t.Amplitude // todo: 后续要除以Cache.Amplitude 因为要归一化
                            })
                        ];
                    }
                }
            }

            Guid StartDetailLog(int times, int detailLogMaxTimes, string additionalName = Constants.EmptyString)
            {
                var startTimes = times + 1;
                var stopTimes = Math.Min(times + Cache.DetailLogInterval, detailLogMaxTimes);
                var currentDetailLogUniqueId = Guid.NewGuid();

                detailLogUniqueId = currentDetailLogUniqueId;
                detailLogFileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}{(string.IsNullOrEmpty(additionalName) ? string.Empty : $"_{additionalName}")}_{startTimes}-{stopTimes}";

                var title = $"{startTimes}-{stopTimes}";
                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {detailLogFileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());

                return currentDetailLogUniqueId;
            }

            void EndDetailLog()
            {
                if (detailLogUniqueId is null || detailLogFileName is null)
                {
                    Guard.IsNull(detailLogUniqueId);
                    Guard.IsNull(detailLogFileName);

                    return;
                }

                try
                {
                    Logger.LogHtmlInformation(detailLogUniqueId.Value.LoggedEndHtml(detailLogFileName));
                }
                finally
                {
                    detailLogUniqueId = null;
                    detailLogFileName = null;
                }
            }
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AllAsync(CancellationToken cancellationToken)
    {
#if NET
        await
#endif
        using var _ = cancellationToken.Register(() =>
        {
            if (Step0Command.CanBeCanceled) Step0Command.Cancel();
            if (Step1Command.CanBeCanceled) Step1Command.Cancel();
            if (StepSecondLastCommand.CanBeCanceled) StepSecondLastCommand.Cancel();
            if (StepFirstLastCommand.CanBeCanceled) StepFirstLastCommand.Cancel();
        });

        var step0Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step0Command.ExecuteAsync( /* isSilent */ true));
        if (await step0Task == false) return;

        var step1Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step1Command.ExecuteAsync( /* isSilent */ true));
        if (await step1Task == false) return;

        var stepSecondLastTask = Guard.IsAssignableToTypeAndReturn<Task<bool>>(StepSecondLastCommand.ExecuteAsync( /* isSilent */ true));
        if (await stepSecondLastTask == false) return;

        await StepFirstLastCommand.ExecuteAsync( /* isSilent */ true);
    }

    private async Task UpdateElectrodeDelayItemAsync(
        AODWaveformElectrodeDelayItem<TItem> delayItem,
        double[]? amplitudes,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        if (amplitudes is not null) Guard.IsEqualTo(amplitudes.Length, Cache.AODWaveformElectrodeDelayFrequencies.Length);

        foreach (var (frequencyIndex, aodWaveformElectrodeDelayFrequency) in Cache.AODWaveformElectrodeDelayFrequencies.Index())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var amplitude = amplitudes?[frequencyIndex] * aodWaveformElectrodeDelayFrequency.Amplitude ?? aodWaveformElectrodeDelayFrequency.Amplitude;

            Logger.LogHtmlInformation($"{aodWaveformElectrodeDelayFrequency.Frequency}(MHz)-[{amplitude}]", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

            var item = new TItem
            {
                ElectrodeConfigurations =
                [
                    .. Cache.ElectrodeDelayParams
                        .Index()
                        .Select(t => new GenerateAODWaveformElectrodeConfiguration
                        {
                            OpticsAODElectrodeEnum = t.Item.OpticsAODElectrodeEnum,
                            Delay = delayItem.Delays[t.Index],
                            Amplitude = amplitude,
                            IsGenerateAODWaveformZero = false,
                            UniformityConfigurations = []
                        })
                ],
                Frequency = aodWaveformElectrodeDelayFrequency.Frequency,
                Amplitude = amplitude
            };

            await UpdateMeasurePowerAsync(item, htmlLogUniqueId, cancellationToken).ConfigureAwait(false);

            Logger.LogHtmlInformation("Electrode Configurations", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                ElectrodeConfigurations = new HtmlTable(
                [
                    .. item.ElectrodeConfigurations.Index().Select(t =>
                    {
                        var boardCardDelay = Cache.ElectrodeDelayParams[t.Index].BoardCardDelay;

                        return new
                        {
                            t.Item.OpticsAODElectrodeEnum,
                            boardCardDelay,
                            t.Item.Delay,
                            SubtractBoardCardDelay = t.Item.Delay - (t.Index == 0
                                ? 0d
                                : boardCardDelay),
                            t.Item.Amplitude,
                            t.Item.IsGenerateAODWaveformZero
                        };
                    })
                ])
            }), htmlLogUniqueId.LoggingHtml());

            delayItem.FrequencyItems = [.. delayItem.FrequencyItems, item];
        }

        delayItem.Score = CalculateScore(delayItem);
    }

    private double CalculateScore(AODWaveformElectrodeDelayItem<TItem> delayItem)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("phase_optimizer", AODWaveformElectrodeDelayWindowViewModelShared.PhaseOptimizerPythonScript);
        using var calculateScore = module.GetAttr("calculate_score");
        using var pyFrequencies = new PyList();
        using var pyEfficiencies = new PyList();

        foreach (var item in delayItem.FrequencyItems)
        {
            using var pyFrequency = item.Frequency.ToPython();
            using var pyEfficiency = (item.MeasurePower / Cache.TotalMeasurePower).ToPython();

            pyFrequencies.Append(pyFrequency);
            pyEfficiencies.Append(pyEfficiency);
        }

        using var pyMethod = EnumHelper.ToDescriptionString(Cache.AODWaveformScoreMethodEnum).ToPython();
        using var pyThreshold = (Cache.AODWaveformScoreMethodEnum == AODWaveformScoreMethodEnum.BandWidth ? (double?)Cache.BandWidthScoreThreshold : null).ToPython();
        using var pyEpsilon = (Cache.AODWaveformScoreMethodEnum == AODWaveformScoreMethodEnum.BandWidth ? (double?)Cache.BandWidthScoreEpsilon : null).ToPython();
        using var pyLambda = Cache.LegacyScoreLambda.ToPython();
        using var pyGamma = Cache.LegacyScoreGamma.ToPython();
        using var result = calculateScore.Invoke(pyFrequencies, pyEfficiencies, pyMethod, pyThreshold, pyEpsilon, pyLambda, pyGamma);

        var score = result.As<double>();

        return score;
    }

    private (bool IsSuccess, double[] Delays, double[] Amplitudes) AlgorithmSuggest(double? previousCost, int delayCount)
    {
        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            Logger.LogTrace("Algorithm Suggest Starting...");

            DirectoryHelper.CreateFileDirectoryIfNotExists(PhaseOptimizerStateFilePath);

            using var _ = Py.GIL();
            using var module = PyModule.FromString("phase_optimizer", AODWaveformElectrodeDelayWindowViewModelShared.PhaseOptimizerPythonScript);

            using var pyPhaseOptimizerStateFilePath = PhaseOptimizerStateFilePath.ToPython();
            module.SetAttr("_STATE_FILE", pyPhaseOptimizerStateFilePath);

            using var suggest = module.GetAttr("suggest");

            using var pyCost = previousCost.ToPython();
            using var pyDelayCount = delayCount.ToPython();
            using var pyNormalCount = 4.ToPython();
            using var pyInitialPoints = Cache.AlgorithmInitialPoints.ToPython();
            using var pyNoise = Cache.Noise.ToPython();
            using var pyEarlyStop = Cache.AlgorithmEarlyStop.ToPython();
            using var pyAcquisitionFunction = Cache.AlgorithmAcquisitionFunctionEnum.ToString().ToPython();
            using var result = suggest.Invoke(pyCost, pyDelayCount, pyNormalCount, pyInitialPoints, pyNoise, pyEarlyStop, pyAcquisitionFunction);

            using var pyDone = Guard.IsNotNullAndReturn(result["done"]);
            using var pyDelays = Guard.IsNotNullAndReturn(result["x_phase"]);
            using var pyNormalParameters = Guard.IsNotNullAndReturn(result["x_normal"]);
            Guard.IsEqualTo(ToDoubles(pyNormalParameters).Length, 4);

            using var pyFrequencies = new PyList();
            foreach (var frequency in Cache.AODWaveformElectrodeDelayFrequencies)
            {
                using var pyFrequency = frequency.Frequency.ToPython();
                pyFrequencies.Append(pyFrequency);
            }

            using var amplitudeCurve = module.GetAttr("single_sigmoid_amplitude_curve");
            using var pyAmplitudeArray = amplitudeCurve.Invoke(pyNormalParameters, pyFrequencies);
            using var pyAmplitudes = pyAmplitudeArray.InvokeMethod("tolist");

            double[] delays = [.. ToDoubles(pyDelays).Select(t => t * Cache.AlgorithmMaxDelay)];
            var amplitudes = ToDoubles(pyAmplitudes);
            Guard.IsEqualTo(delays.Length, delayCount);
            Guard.IsEqualTo(amplitudes.Length, Cache.AODWaveformElectrodeDelayFrequencies.Length);

            return (pyDone.As<bool>(), delays, amplitudes);
        }
        finally
        {
            Logger.LogTrace("Algorithm Suggest Stopped: {TotalMilliseconds}ms", Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
        }
    }

    private static double[] ToDoubles(PyObject pyValues)
    {
        using var values = new PyList(pyValues);
        var result = new double[values.Length()];

        for (var index = 0; index < result.Length; index++)
        {
            using var value = Guard.IsNotNullAndReturn(values[index]);

            result[index] = value.As<double>();
        }

        return result;
    }
}

public static class AODWaveformElectrodeDelayWindowViewModelShared
{
    public static readonly string PhaseOptimizerPythonScript = GetEmbeddedResource("CugaCalibration.Assets.Python.phase_optimizer.py");

    private static string GetEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) ThrowHelper.ThrowArgumentException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}