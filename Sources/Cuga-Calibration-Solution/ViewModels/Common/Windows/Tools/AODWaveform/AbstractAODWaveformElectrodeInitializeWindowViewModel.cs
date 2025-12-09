using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using System.Collections;
using System.ComponentModel;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeInitializeCache<TItem, TResult> : AODWaveformCommonCache
    where TItem : AODWaveformElectrodeInitializeItem, new()
    where TResult : AODWaveformElectrodeInitializeResult, new()
{
    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private int _interpolationCount = 3;

    [ObservableProperty]
    private IReadOnlyList<double> _electrode2OffsetFrequencyPeriodCoefficients = [];

    [ObservableProperty]
    private IReadOnlyList<double> _electrode4OffsetFrequencyPeriodCoefficients = [];

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    partial void OnFrequenciesChanged(IReadOnlyList<double> value) => Weights = [..value.Select(_ => 1)];

    [ObservableProperty]
    private IReadOnlyList<double> _weights = [];

    [ObservableProperty]
    private double _startElectrode3OffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepElectrode3OffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopElectrode3OffsetFrequencyPeriodCoefficient;

    #region Items

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private AODWaveformInitializeStep0<TItem> _step0 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<AODWaveformElectrodeInitializeStep1<TItem>> _step1Items = [];

    #endregion

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    [ObservableProperty]
    private IReadOnlyList<TResult> _results = [];
}

public partial class AODWaveformElectrodeInitializeItem : AODWaveformCommonItem
{
    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurations = [];

    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    public override object ToHtmlAnonymous() => new
    {
        Frequency,
        OffsetFrequencyPeriodCoefficient,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

public class AODWaveformElectrodeInitializeResult : ObservableCacheBase;

public abstract partial class AbstractAODWaveformElectrodeInitializeWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem>
    where TCache : AODWaveformElectrodeInitializeCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeInitializeItem, new()
    where TResult : AODWaveformElectrodeInitializeResult, new()
{
    protected abstract void GenerateAODWaveform(TItem item, CancellationToken cancellationToken);

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
                    ElectrodeOffsetItems = Cache.Step0.ScatterPlotControl.GetHtmlPlot2DLinesChart()
                },
                1 => new
                {
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))]),
                    UniformityItems = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))])
                },
                2 or 3 => new HtmlComment("See Above!"),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<object>(nameof(stepIndex), stepIndex, null)
            }
        ), HtmlLogUniqueId.LoggingHtml());
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
        return await InvokeAsync(0, "Step 1 Electrode2 Electrode4", async () =>
        {
            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Count, 4);
            Cache.Step0.Items = [];

            await GetElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2], Cache.Electrode2OffsetFrequencyPeriodCoefficients).ConfigureAwait(false);

            await GetElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode4OffsetFrequencyPeriodCoefficients).ConfigureAwait(false);

            return true;

            async Task GetElectrodeMaxMeasurePowerAsync(IReadOnlyList<OpticsAODElectrodeEnum> electrodes, IReadOnlyList<double> offsetFrequencyPeriodCoefficients)
            {
                var step0Item = new AODWaveformInitializeStep0Item<TItem> { Electrodes = electrodes };
                Cache.Step0.Items = [.. Cache.Step0.Items, step0Item];

                Logger.LogHtmlInformation(step0Item.Title, HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                foreach (var offsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                {
                    var item = new TItem
                    {
                        ElectrodeConfigurations =
                        [
                            ..Cache.ElectrodeConfigurationResults
                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                {
                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                    OffsetFrequency = t.OffsetFrequency,
                                    OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum == electrodes[^1]
                                        ? offsetFrequencyPeriodCoefficient
                                        : 0,
                                    Amplitude = Cache.DefaultAmplitude,
                                    IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                })
                        ],
                        OffsetFrequencyPeriodCoefficient = offsetFrequencyPeriodCoefficient
                    };

                    Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                    try
                    {
                        GenerateAODWaveform(item, cancellationToken);
                        SetAODWaveformProfiles(item);

                        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
                        LaserViewModel.ToggleOpticsMagType(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                        await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                        var measurePower = LaserViewModel.GetOpticalMeasurePower();

                        item.MeasurePower = measurePower;

                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
                    }
                    finally
                    {
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                    }

                    step0Item.Items = [.. step0Item.Items, item];
                }

                Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == electrodes[^1])
                    .OffsetFrequencyPeriodCoefficient = step0Item.Items.Maxima(t => t.MeasurePower).Single().OffsetFrequencyPeriodCoefficient;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlTable([..Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]), HtmlLogUniqueId.LoggingHtml());
            }
        }, isShowDialog).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(1, "Step 2 Electrode3", async () =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsEqualTo(Cache.Frequencies.Count, Cache.Weights.Count);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            Cache.Step1Items = [];
            GenerateFlatnessFixedAODWaveform(cancellationToken);

            var electrodes = (IReadOnlyList<OpticsAODElectrodeEnum>)[OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3];

            var step1 = new AODWaveformElectrodeInitializeStep1<TItem> { Electrodes = electrodes };
            Cache.Step1Items = [.. Cache.Step1Items, step1];

            Logger.LogHtmlInformation(step1.Title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            var offsetFrequencyPeriodCoefficients = Generate.LinearRange(Cache.StartElectrode3OffsetFrequencyPeriodCoefficient, Cache.StepElectrode3OffsetFrequencyPeriodCoefficient, Cache.StopElectrode3OffsetFrequencyPeriodCoefficient);
            Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

            foreach (var frequency in Cache.Frequencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var step1Item = new AODWaveformElectrodeInitializeStep1Item<TItem>();
                step1.Items = [.. step1.Items, step1Item];

                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = new TItem
                    {
                        ElectrodeConfigurations =
                        [
                            ..Cache.ElectrodeConfigurationResults
                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                {
                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                    OffsetFrequency = t.OffsetFrequency,
                                    OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode3
                                        ? currentOffsetFrequencyPeriodCoefficient
                                        : t.OffsetFrequencyPeriodCoefficient,
                                    Amplitude = Cache.DefaultAmplitude,
                                    IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                })
                        ],
                        Frequency = frequency,
                        OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                    };

                    Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                    await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                    step1Item.FrequencyItems = [.. step1Item.FrequencyItems, item];
                }
            }

            step1.InterpolationMaxima(Cache.InterpolationCount);

            step1.OffsetFrequencyPeriodCoefficient = step1.ClosestMaximaPoints
                .Index()
                .Select(t => Cache.Weights[t.Index] * t.Item.X)
                .Sum() / Cache.Weights.Sum();

            Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode3)
                .OffsetFrequencyPeriodCoefficient = step1.OffsetFrequencyPeriodCoefficient.Value;
            Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode4)
                .OffsetFrequencyPeriodCoefficient += step1.OffsetFrequencyPeriodCoefficient.Value;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlTable([..Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]), HtmlLogUniqueId.LoggingHtml());

            return true;
        }, isShowDialog).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(2, "Step 3 Generate AOD Waveform", () =>
        {
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

public sealed partial class AODWaveformInitializeStep0<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeInitializeItem, new()
{
    #region Result

    [ObservableProperty]
    private IReadOnlyList<AODWaveformInitializeStep0Item<TItem>> _items = [];

    partial void OnItemsChanged(IReadOnlyList<AODWaveformInitializeStep0Item<TItem>>? oldValue, IReadOnlyList<AODWaveformInitializeStep0Item<TItem>> newValue)
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

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
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

    public AODWaveformInitializeStep0()
    {
        ScatterPlotControl.SetTitle("Offset Frequency Period(Y: mW - X: 2pi)");
    }

    #endregion

    private void RefreshPlot()
    {
        foreach (var (index, item) in Items.Index())
        {
            if (item.Items.Count <= 0) continue;

            ScatterPlotControl.GetOrAddScatterLine(
                item.Title,
                [.. item.Items.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                index,
                new Range(0, Items.Count - 1));
        }

        ScatterPlotControl.AutoScaleRefresh();
    }
}

public sealed partial class AODWaveformInitializeStep0Item<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeInitializeItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    public string Title => string.Join(", ", Electrodes.Select(t => EnumHelper.ToDescriptionString(t)));

    [ObservableProperty]
    private IReadOnlyList<TItem> _items = [];
}

public sealed partial class AODWaveformElectrodeInitializeStep1<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeInitializeItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    public string Title => string.Join(", ", Electrodes.Select(t => EnumHelper.ToDescriptionString(t)));

    #region Result

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeInitializeStep1Item<TItem>> _items = [];

    partial void OnItemsChanged(IReadOnlyList<AODWaveformElectrodeInitializeStep1Item<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeInitializeStep1Item<TItem>> newValue)
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

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
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

    public AODWaveformElectrodeInitializeStep1()
    {
        ScatterPlotControl.ToggleLegend(false);
    }

    private void RefreshPlot()
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

public sealed partial class AODWaveformElectrodeInitializeStep1Item<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeInitializeItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<TItem> _frequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _frequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _frequencyMaximaPoints = [];
}