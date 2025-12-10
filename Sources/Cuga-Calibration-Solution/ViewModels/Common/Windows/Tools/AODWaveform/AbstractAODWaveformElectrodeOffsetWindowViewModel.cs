using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections;
using Generate = MathNet.Numerics.Generate;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem>
    where TCache : AODWaveformElectrodeOffsetCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    protected abstract void GenerateResultAODWaveform(TResult result, CancellationToken cancellationToken);

    protected abstract void SetResultAODWaveformProfiles(TResult result, CancellationToken cancellationToken);

    protected abstract void SetResultAODWaveformConfig(TResult result, CancellationToken cancellationToken);

    protected override void LoggerResult(int stepIndex)
    {
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(
            stepIndex switch
            {
                0 => new
                {
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))])
                },
                1 => new
                {
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))]),
                    UniformityItems = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))])
                },
                2 or 3 => new HtmlComment("See Above!"),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<object>(nameof(stepIndex), stepIndex, null)
            }
        ), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = CacheProvider.GetOrDefault<TCache>();

            foreach (var step0 in Cache.Step0Items) step0.RefreshPlot();
            foreach (var step1 in Cache.Step1Items) step1.RefreshPlot();
        });
    }

    [RelayCommand]
    private void AddElectrodeOffsetParam()
    {
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (Cache.ElectrodeOffsetParams.Count > electrodeEnums.Length) return;

        var electrodeOffsetParamList = Cache.ElectrodeOffsetParams.ToList();
        electrodeOffsetParamList.Add(new AODWaveformElectrodeOffsetFrequencyPeriodOffsetParam());

        foreach (var (index, item) in electrodeOffsetParamList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        Cache.ElectrodeOffsetParams = electrodeOffsetParamList;
    }

    [RelayCommand]
    private void RemoveElectrodeOffsetParam(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        var electrodeOffsetParamList = Cache.ElectrodeOffsetParams.ToList();
        foreach (AODWaveformElectrodeOffsetFrequencyPeriodOffsetParam selectItem in selectItems)
        {
            if (selectItem.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode1) continue;

            electrodeOffsetParamList.Remove(selectItem);
        }

        foreach (var (index, item) in electrodeOffsetParamList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        Cache.ElectrodeOffsetParams = electrodeOffsetParamList;
    }

    [RelayCommand]
    private void AddElectrodeFrequencyUniformityParam()
    {
        var electrodeFrequencyUniformityParamList = Cache.ElectrodeFrequencyUniformityParams.ToList();
        electrodeFrequencyUniformityParamList.Add(new AODWaveformElectrodeOffsetFrequencyUniformityParam());

        Cache.ElectrodeFrequencyUniformityParams = electrodeFrequencyUniformityParamList;
    }

    [RelayCommand]
    private void RemoveElectrodeFrequencyUniformityParam(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeFrequencyUniformityParamList = Cache.ElectrodeFrequencyUniformityParams.ToList();
        foreach (AODWaveformElectrodeOffsetFrequencyUniformityParam selectItem in selectItems)
        {
            electrodeFrequencyUniformityParamList.Remove(selectItem);
        }

        Cache.ElectrodeFrequencyUniformityParams = electrodeFrequencyUniformityParamList;
    }

    [RelayCommand]
    private void AddResult()
    {
        var resultList = Cache.Results.ToList();
        resultList.Add(new TResult());

        Cache.Results = resultList;
    }

    [RelayCommand]
    private void RemoveResult(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var resultList = Cache.Results.ToList();
        foreach (TResult selectItem in selectItems) resultList.Remove(selectItem);

        Cache.Results = resultList;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SetResultAODWaveformProfilesAsync(TResult result, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                SetResultAODWaveformProfiles(result, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: {nameof(SetResultAODWaveformProfilesAsync)} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogError(ex, nameof(SetResultAODWaveformProfilesAsync));
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SetResultAODWaveformConfigAsync(TResult result, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                SetResultAODWaveformConfig(result, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: {nameof(SetResultAODWaveformConfigAsync)} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogError(ex, nameof(SetResultAODWaveformConfigAsync));
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(0, "Step 1 Electrode Offset", async () =>
        {
            Guard.IsNotEmpty(Cache.ElectrodeOffsetParams);
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetParams.Count, 2);
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            Cache.Step0Items = [];
            Cache.ElectrodeConfigurationResults =
            [
                new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = 0d
                }
            ];

            GenerateFlatnessFixedAODWaveform(cancellationToken);

            foreach (var param in Cache.ElectrodeOffsetParams)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Cache.ElectrodeConfigurationResults.Any(t => t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)) continue;

                var electrodes = (OpticsAODElectrodeEnum[])[.. Cache.ElectrodeConfigurationResults.Select(t => t.OpticsAODElectrodeEnum), param.OpticsAODElectrodeEnum];

                var step0 = new AODWaveformElectrodeOffsetStep0<TItem> { Electrodes = electrodes };
                Cache.Step0Items = [.. Cache.Step0Items, step0];

                Logger.LogHtmlInformation(step0.Title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var offsetFrequencyPeriodCoefficients = Generate.LinearRange(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                foreach (var frequency in Cache.Frequencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var step0Item = new AODWaveformElectrodeOffsetStep0Item<TItem>();
                    step0.Items = [.. step0.Items, step0Item];

                    Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var item = new TItem
                        {
                            ElectrodeConfigurations =
                            [
                                ..Cache.ElectrodeOffsetParams
                                    .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                    {
                                        OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                        OffsetFrequency = Cache.OffsetFrequency,
                                        OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum
                                            ? currentOffsetFrequencyPeriodCoefficient
                                            : Cache.ElectrodeConfigurationResults
                                                .SingleOrDefault(tt => tt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum)
                                                ?.OffsetFrequencyPeriodCoefficient ?? 0,
                                        Amplitude = Cache.DefaultAmplitude,
                                        IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                    })
                            ],
                            Frequency = frequency,
                            Amplitude = Cache.DefaultAmplitude,
                            OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                        };

                        Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                        await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                        step0Item.FrequencyItems = [.. step0Item.FrequencyItems, item];
                    }
                }

                step0.InterpolationMaxima(Cache.InterpolationCount);

                var allWeight = Cache.ElectrodeOffsetFrequencyWeightParams
                    .Where(tt => tt.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)
                    .Select(t => t.Weight)
                    .Sum();

                step0.OffsetFrequencyPeriodCoefficient = step0.ClosestMaximaPoints
                    .Index()
                    .Select(t => Cache.ElectrodeOffsetFrequencyWeightParams
                        .Single(tt => tt.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum &&
                                      Equals(tt.Frequency, step0.Items[t.Index].FrequencyItems[0].Frequency)).Weight * t.Item.X)
                    .Sum() / allWeight;

                if (Cache.IsConfirmAODWaveformElectrodeOffsetResult)
                {
                    var aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<AODWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel>();
                    aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient = step0.OffsetFrequencyPeriodCoefficient.Value;

                    var showDialog = WindowManagerService.ShowDialog(aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel);
                    if (showDialog == true) step0.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient;
                }

                Cache.ElectrodeConfigurationResults =
                [
                    .. Cache.ElectrodeConfigurationResults, new GenerateAODWaveformElectrodeConfiguration
                    {
                        OpticsAODElectrodeEnum = param.OpticsAODElectrodeEnum,
                        OffsetFrequency = Cache.OffsetFrequency,
                        OffsetFrequencyPeriodCoefficient = step0.OffsetFrequencyPeriodCoefficient.Value
                    }
                ];

                GC.Collect();
            }

            return Cache.ElectrodeConfigurationResults.Count == Cache.ElectrodeOffsetParams.Count;
        }, isShowDialog).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(1, "Step 2 Uniformity", async () =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));
            Guard.IsNotEmpty(Cache.ElectrodeFrequencyUniformityParams);

            // 配合界面直接修改结果, 更新step0的界面
            foreach (var step0 in Cache.Step0Items)
            {
                step0.OffsetFrequencyPeriodCoefficient = Cache.ElectrodeConfigurationResults
                    .Single(t => t.OpticsAODElectrodeEnum == step0.Electrodes[^1])
                    .OffsetFrequencyPeriodCoefficient;
            }

            Cache.Step1Items = [];
            foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults)
            {
                electrodeConfiguration.UniformityConfigurations = [];
            }

            GenerateFlatnessFixedAODWaveform(cancellationToken);

            var frequencies = Generate.LinearRange(Cache.Frequencies[0], Cache.StepFrequency, Cache.Frequencies[^1]);
            Guard.IsNotEmpty(frequencies);
            foreach (var electrodeFrequencyUniformityParams in Cache.ElectrodeFrequencyUniformityParams.Chunk(Cache.ElectrodeFrequencyUniformityParamChunkSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var electrodes = electrodeFrequencyUniformityParams.Select(t => t.OpticsAODElectrodeEnum).ToArray();

                var step1 = new AODWaveformElectrodeOffsetStep1<TItem> { Electrodes = electrodes };
                Cache.Step1Items = [.. Cache.Step1Items, step1];

                Logger.LogHtmlInformation(step1.Title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var frequency in frequencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var step1Item = new AODWaveformElectrodeOffsetStep1Item<TItem>();
                    step1.Items = [.. step1.Items, step1Item];

                    Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    var amplitudes = Generate.LinearRange(electrodeFrequencyUniformityParams[0].StartAmplitude, electrodeFrequencyUniformityParams[0].StepAmplitude, electrodeFrequencyUniformityParams[0].StopAmplitude).Reverse().ToArray();
                    Guard.IsNotEmpty(amplitudes);
                    foreach (var amplitude in amplitudes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var item = new TItem
                        {
                            ElectrodeConfigurations =
                            [
                                .. Cache.ElectrodeConfigurationResults
                                    .Select(t =>
                                    {
                                        if (electrodes.Contains(t.OpticsAODElectrodeEnum)) return t.Clone().WithAmplitude(amplitude).WithUniformityConfigurations([]);

                                        var uniformityConfigurationResults = Cache.ElectrodeConfigurationResults
                                            .Single(tt => tt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum)
                                            .UniformityConfigurations;

                                        return t.Clone()
                                            .WithAmplitude(uniformityConfigurationResults.Count > 0
                                                ? uniformityConfigurationResults.Single(tt => Equals(tt.Frequency, frequency)).Coefficient
                                                : Cache.DefaultAmplitude)
                                            .WithUniformityConfigurations([]);
                                    })
                            ],
                            Amplitude = amplitude,
                            Frequency = frequency
                        };

                        Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                        await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                        step1Item.FrequencyItems = [.. step1Item.FrequencyItems, item];
                    }
                }

                foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults.Where(t => electrodes.Contains(t.OpticsAODElectrodeEnum)))
                {
                    var points = step1.ScatterPlotControl.GetScatterLines(1).Single().ScatterSourcePoints.Points;
                    if (points.Count != frequencies.Length) return ThrowHelper.ThrowArgumentException<bool>($"{nameof(points)} count != {nameof(frequencies)} count");

                    electrodeConfiguration.UniformityConfigurations =
                    [
                        ..points.Select(t => new GenerateAODWaveformUniformityConfiguration
                        {
                            Frequency = t.X,
                            Coefficient = t.Y
                        })
                    ];
                }

                GC.Collect();
            }

            return true;
        }, isShowDialog).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(2, "Step 3 Generate AOD Waveform", () =>
        {
            Guard.IsNotEmpty(Cache.ElectrodeOffsetParams);
            Guard.IsEqualTo(Cache.ElectrodeOffsetParams.Count, Cache.ElectrodeConfigurationResults.Count);
            Guard.IsNotEmpty(Cache.Results);

            foreach (var result in Cache.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                GenerateResultAODWaveform(result, cancellationToken);
            }

            return Task.FromResult(true);
        }, isShowDialog).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(3, "Step 4 Set AOD Waveform Config", async () =>
        {
            Guard.IsNotEmpty(Cache.Results);

            foreach (var result in Cache.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await SetResultAODWaveformConfigAsync(result, cancellationToken);
            }

            return true;
        }, isShowDialog).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AllAsync(CancellationToken cancellationToken)
    {
#if NET
        await
#endif
        using
            var _ = cancellationToken.Register(() =>
            {
                if (Step0Command.CanBeCanceled) Step0Command.Cancel();
                if (Step1Command.CanBeCanceled) Step1Command.Cancel();
                if (Step2Command.CanBeCanceled) Step2Command.Cancel();
                if (Step3Command.CanBeCanceled) Step3Command.Cancel();
            });

        var step0Task = GuardUtils.IsAssignableToType<Task<bool>>(Step0Command.ExecuteAsync(false));
        if (await step0Task == false) return;

        var step1Task = GuardUtils.IsAssignableToType<Task<bool>>(Step1Command.ExecuteAsync(false));
        if (await step1Task == false) return;

        var step2Task = GuardUtils.IsAssignableToType<Task<bool>>(Step2Command.ExecuteAsync(false));
        if (await step2Task == false) return;

        await Step3Command.ExecuteAsync(true);
    }
}

[IOCAppService(ServiceType = typeof(AODWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class AODWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}