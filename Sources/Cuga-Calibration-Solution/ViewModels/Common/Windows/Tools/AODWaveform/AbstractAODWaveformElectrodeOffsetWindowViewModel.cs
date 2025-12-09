using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using ScottPlot;
using System.Collections;
using System.ComponentModel;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetCache<TItem, TResult> : AODWaveformCommonCache
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    #region Param

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private int _interpolationCount = 3;

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetParam> _electrodeOffsetParams =
    [
        new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }
    ];

    partial void OnElectrodeOffsetParamsChanged(IReadOnlyList<AODWaveformElectrodeOffsetParam> value)
    {
        var oldElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams;

        ElectrodeOffsetFrequencyWeightParams =
        [
            ..value
                .Where(t => t.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode1)
                .SelectMany(t => Frequencies.Select(tt => new AODWaveformOffsetElectrodeFrequencyWeightParam
                {
                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                    Frequency = tt,
                    Weight = oldElectrodeOffsetFrequencyWeightParams
                        .FirstOrDefault(ttt => ttt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum && Equals(ttt.Frequency, tt))
                        ?.Weight ?? 1d
                }))
        ];

        ElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams.DistinctBy(t => (t.OpticsAODElectrodeEnum, t.Frequency)).ToArray();
    }

    [ObservableProperty]
    private bool _isConfirmAODWaveformElectrodeOffsetResult = true;

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    partial void OnFrequenciesChanged(IReadOnlyList<double> value)
    {
        var oldElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams;

        ElectrodeOffsetFrequencyWeightParams =
        [
            ..ElectrodeOffsetParams
                .Where(t => t.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode1)
                .SelectMany(t => value.Select(tt => new AODWaveformOffsetElectrodeFrequencyWeightParam
                {
                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                    Frequency = tt,
                    Weight = oldElectrodeOffsetFrequencyWeightParams
                        .FirstOrDefault(ttt => ttt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum && Equals(ttt.Frequency, tt))
                        ?.Weight ?? 1d
                }))
        ];

        ElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams.DistinctBy(t => (t.OpticsAODElectrodeEnum, t.Frequency)).ToArray();
    }

    [ObservableProperty]
    private double _stepFrequency;

    [ObservableProperty]
    private IReadOnlyList<AODWaveformOffsetElectrodeFrequencyWeightParam> _electrodeOffsetFrequencyWeightParams = [];

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeFrequencyUniformityParam> _electrodeFrequencyUniformityParams = [];

    [ObservableProperty]
    private int _electrodeFrequencyUniformityParamChunkSize;

    #endregion Param

    #region Items

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep0<TItem>> _step0Items = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep1<TItem>> _step1Items = [];

    partial void OnStep0ItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetStep0<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetStep0<TItem>> newValue)
    {
        foreach (var step0 in oldValue ?? [])
        {
            step0.PropertyChanged -= Step0ItemOnPropertyChanged;
        }

        foreach (var step0 in newValue)
        {
            step0.PropertyChanged -= Step0ItemOnPropertyChanged;
            step0.PropertyChanged += Step0ItemOnPropertyChanged;
        }

        return;

        void Step0ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not AODWaveformElectrodeOffsetStep0<TItem> step0) return;

            step0.RefreshPlot();
        }
    }

    partial void OnStep1ItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetStep1<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetStep1<TItem>> newValue)
    {
        foreach (var step1 in oldValue ?? [])
        {
            step1.PropertyChanged -= Step1ItemOnPropertyChanged;
        }

        foreach (var step1 in newValue)
        {
            step1.PropertyChanged -= Step1ItemOnPropertyChanged;
            step1.PropertyChanged += Step1ItemOnPropertyChanged;
        }

        return;

        void Step1ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not AODWaveformElectrodeOffsetStep1<TItem> step1) return;

            step1.RefreshPlot();
        }
    }

    #endregion Items

    #region Result

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    [ObservableProperty]
    private IReadOnlyList<TResult> _results = [];

    #endregion Result

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        InterpolationCount,
        ElectrodeOffsetParams = new HtmlTable([.. ElectrodeOffsetParams.Select(t => t.ToHtmlAnonymous())]),
        IsConfirmAODWaveformElectrodeOffsetResult,
        Frequencies,
        StepFrequency,
        ElectrodeOffsetFrequencyWeightParams = new HtmlTable([.. ElectrodeOffsetFrequencyWeightParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeFrequencyUniformityParams = new HtmlTable([.. ElectrodeFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeFrequencyUniformityParamChunkSize,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

public partial class AODWaveformElectrodeOffsetItem : AODWaveformCommonItem
{
    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurations = [];

    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _amplitude;

    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    public override object ToHtmlAnonymous() => new
    {
        Frequency,
        Amplitude,
        OffsetFrequencyPeriodCoefficient,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

public class AODWaveformElectrodeOffsetResult : ObservableCacheBase;

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
        electrodeOffsetParamList.Add(new AODWaveformElectrodeOffsetParam());

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
        foreach (AODWaveformElectrodeOffsetParam selectItem in selectItems)
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
        electrodeFrequencyUniformityParamList.Add(new AODWaveformElectrodeFrequencyUniformityParam());

        Cache.ElectrodeFrequencyUniformityParams = electrodeFrequencyUniformityParamList;
    }

    [RelayCommand]
    private void RemoveElectrodeFrequencyUniformityParam(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeFrequencyUniformityParamList = Cache.ElectrodeFrequencyUniformityParams.ToList();
        foreach (AODWaveformElectrodeFrequencyUniformityParam selectItem in selectItems)
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

            GenerateFixedAODWaveform(cancellationToken);

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

            GenerateFixedAODWaveform(cancellationToken);

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

public sealed partial class AODWaveformElectrodeOffsetParam : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _startOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopOffsetFrequencyPeriodCoefficient;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        StartOffsetFrequencyPeriodCoefficient,
        StepOffsetFrequencyPeriodCoefficient,
        StopOffsetFrequencyPeriodCoefficient
    };
}

public sealed partial class AODWaveformOffsetElectrodeFrequencyWeightParam : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _weight;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        Frequency,
        Weight
    };
}

public sealed partial class AODWaveformElectrodeFrequencyUniformityParam : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _startAmplitude = 1;

    [ObservableProperty]
    private double _stepAmplitude = 1;

    [ObservableProperty]
    private double _stopAmplitude = 1;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        StartAmplitude,
        StepAmplitude,
        StopAmplitude
    };
}

public sealed partial class AODWaveformElectrodeOffsetStep0<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    public string Title => string.Join(", ", Electrodes.Select(t => EnumHelper.ToDescriptionString(t)));

    #region Result

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep0Item<TItem>> _items = [];

    partial void OnItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetStep0Item<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetStep0Item<TItem>> newValue)
    {
        foreach (var step0Item in oldValue ?? [])
        {
            step0Item.PropertyChanged -= ItemOnPropertyChanged;
        }

        foreach (var step0Item in newValue)
        {
            step0Item.PropertyChanged -= ItemOnPropertyChanged;
            step0Item.PropertyChanged += ItemOnPropertyChanged;
        }

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Items));
        }
    }

    [ObservableProperty]
    private IReadOnlyList<Point> _closestMaximaPoints = [];

    [ObservableProperty]
    private double? _offsetFrequencyPeriodCoefficient;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    #endregion

    public AODWaveformElectrodeOffsetStep0()
    {
        ScatterPlotControl.ToggleLegend(false);
    }

    public void RefreshPlot()
    {
        ScatterPlotControl.SetTitle($"Result: {(OffsetFrequencyPeriodCoefficient is null ? "-" : $"{OffsetFrequencyPeriodCoefficient:0.###}(2pi)")} (Y: mW - X: 2pi)");

        foreach (var (index, item) in Items.Index())
        {
            if (item.FrequencyItems.Count <= 0) continue;

            var scatterMarkersOrigin = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                [.. item.FrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                index,
                new Range(0, Items.Count - 1),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                item.FrequencyInterpolationPoints,
                index,
                new Range(0, Items.Count - 1));

            var scatterMarkersMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                item.FrequencyMaximaPoints,
                index,
                new Range(0, Items.Count - 1),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        if (ClosestMaximaPoints.Count > 0)
        {
            var scatterMarkersClosestMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
                "Closest Maxima",
                ClosestMaximaPoints,
                Colors.Blue,
                markerShape: MarkerShape.FilledSquare);
            scatterMarkersClosestMaxima.MarkerSize = 20;
        }

        ScatterPlotControl.AutoScaleRefresh();
    }

    public void InterpolationMaxima(int densityFactor)
    {
        ClosestMaximaPoints = [];

        foreach (var item in Items)
        {
            item.FrequencyInterpolationPoints = [];
            item.FrequencyMaximaPoints = [];

            var (frequencyInterpolationX, frequencyInterpolationY) = Interpolator.SplineInterpolation(
                Vector<double>.Build.Dense([.. item.FrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
                Vector<double>.Build.Dense([.. item.FrequencyItems.Select(t => t.MeasurePower)]),
                densityFactor);
            item.FrequencyInterpolationPoints = [.. frequencyInterpolationX.Index().Select(t => new Point(t.Item, frequencyInterpolationY[t.Index]))];

            var (frequencyMaximaX, frequencyMaximaY) = Extremumor.FindMaxima(
                Vector<double>.Build.Dense([.. item.FrequencyInterpolationPoints.Select(t => t.X)]),
                Vector<double>.Build.Dense([.. item.FrequencyInterpolationPoints.Select(t => t.Y)]));
            item.FrequencyMaximaPoints =
            [
                .. frequencyMaximaY
                    .Index()
                    .Where(t => t.Item > frequencyInterpolationY.Average())
                    .Select(t => new Point(frequencyMaximaX[t.Index], t.Item))
            ];
        }

        var (results, _) = Extremumor.FindClosestExtremum([.. Items.Select(t => Vector<double>.Build.DenseOfEnumerable(t.FrequencyMaximaPoints.Select(tt => tt.X)))]);

        foreach (var (index, (xIndex, xValue)) in results.Index())
        {
            ClosestMaximaPoints = [.. ClosestMaximaPoints, new Point(xValue, Items[index].FrequencyMaximaPoints[xIndex].Y)];
        }
    }
}

public sealed partial class AODWaveformElectrodeOffsetStep0Item<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<TItem> _frequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _frequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _frequencyMaximaPoints = [];
}

public sealed partial class AODWaveformElectrodeOffsetStep1<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    public string Title => string.Join(", ", Electrodes.Select(t => EnumHelper.ToDescriptionString(t)));

    #region Result

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>> _items = [];

    partial void OnItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>> newValue)
    {
        foreach (var step1Item in oldValue ?? [])
        {
            step1Item.PropertyChanged -= ItemOnPropertyChanged;
        }

        foreach (var step1Item in newValue)
        {
            step1Item.PropertyChanged -= ItemOnPropertyChanged;
            step1Item.PropertyChanged += ItemOnPropertyChanged;
        }

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AODWaveformElectrodeOffsetStep1Item<TItem>.MaxItem)) return;

            OnPropertyChanged(nameof(Items));
        }
    }

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    #endregion

    public AODWaveformElectrodeOffsetStep1()
    {
        ScatterPlotControl.Configure(totalPlotCount: 3);
        ScatterPlotControl.SetTitle(0, "Uniformity Items(Y: mW - X: AMP)");
        ScatterPlotControl.SetTitle(1, "Uniformity Amplitude Result(Y: AMP - X: MHz)");
        ScatterPlotControl.SetTitle(2, "Uniformity Measure Power Result(Y: mW - X: MHz)");
    }

    public void RefreshPlot()
    {
        var isNeedRefreshes = new bool[Items.Count];
        foreach (var (index, item) in Items.Index())
        {
            if (item.FrequencyItems.Count <= 0) continue;

            ScatterPlotControl.GetOrAddScatterLine(
                0,
                $"{item.FrequencyItems[0].Frequency}(MHz)",
                [.. item.FrequencyItems.Select(t => new Point(t.Amplitude, t.MeasurePower))],
                index,
                new Range(0, Items.Count - 1));

            item.MaxItem = item.FrequencyItems.Maxima(t => t.MeasurePower).Single();

            isNeedRefreshes[index] = true;
        }

        if (isNeedRefreshes.All(b => b))
        {
            ScatterPlotControl.GetOrAddScatterLine(
                1,
                "Amplitude",
                [
                    .. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, t.MaxItem?.Amplitude ?? 0))
                ],
                Colors.Blue);
            ScatterPlotControl.GetOrAddScatterLine(
                2,
                "Measure Power",
                [
                    .. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, t.MaxItem?.MeasurePower ?? 0))
                ],
                Colors.Blue);
        }

        ScatterPlotControl.AutoScaleRefresh();
    }
}

public sealed partial class AODWaveformElectrodeOffsetStep1Item<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<TItem> _frequencyItems = [];

    [ObservableProperty]
    private TItem? _maxItem;
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