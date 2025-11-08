using System.Collections;
using System.ComponentModel;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.IO;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using ScottPlot;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetCache<TItem> : AODWaveformCommonCache
    where TItem : AODWaveformElectrodeOffsetItem, new()
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

    #region AOD Waveform Frequency

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    [ObservableProperty]
    private double _stepFrequency;

    #endregion AOD Waveform Frequency

    #region AOD Waveform Amplitud

    [ObservableProperty]
    private double _startAmplitude = 1;

    [ObservableProperty]
    private double _stepAmplitude = 1;

    [ObservableProperty]
    private double _stopAmplitude = 1;

    #endregion AOD Waveform Amplitud

    #endregion Param

    #region Items

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep0<TItem>> _step0Items = [];

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep1<TItem>> _step1Items = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: LiteDB.BsonIgnore]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _step1ScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

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
            if (sender is not AODWaveformElectrodeOffsetStep0<TItem> step0Item) return;

            step0Item.RefreshPlot();
            OnPropertyChanged(nameof(Step0Items));
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
            RefreshPlot();
            OnPropertyChanged(nameof(Step1Items));
        }
    }

    #endregion Items

    #region Result

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformUniformityConfiguration> _uniformityConfigurationResults = [];

    #endregion Result

    public AODWaveformElectrodeOffsetCache()
    {
        Step1ScatterPlotControl.Configure(totalPlotCount: 3);
        Step1ScatterPlotControl.SetTitle(0, "Step1 Uniformity Items(Y: mW - X: AMP)");
        Step1ScatterPlotControl.SetTitle(1, "Step1 Uniformity Amplitude Result(Y: AMP - X: MHz)");
        Step1ScatterPlotControl.SetTitle(2, "Step1 Uniformity Measure Power Result(Y: mW - X: MHz)");
    }

    public void RefreshPlot()
    {
        Step1ScatterPlotControl.Clear();

        var isNeedRefresh = false;
        foreach (var (index, item) in Step1Items.Index())
        {
            if (item.FrequencyItems.Count <= 0) continue;

            Step1ScatterPlotControl.GetOrAddScatterLine(
                0,
                $"{item.FrequencyItems[0].Frequency}(MHz)",
                [..item.FrequencyItems.Select(t => new Point(t.Amplitude, t.MeasurePower))],
                index,
                new Range(0, Step1Items.Count - 1));

            item.MaxItem = GuardUtils.IsNotNullAndReturn(item.FrequencyItems.MaxBy(t => t.MeasurePower));

            isNeedRefresh = true;
        }

        if (isNeedRefresh)
        {
            Step1ScatterPlotControl.GetOrAddScatterLine(
                1,
                "Amplitude",
                [
                    .. Step1Items.Select(t => new Point(t.FrequencyItems[0].Frequency, t.MaxItem?.Amplitude ?? 0))
                ],
                Colors.Blue);
            Step1ScatterPlotControl.GetOrAddScatterLine(
                2,
                "Measure Power",
                [
                    .. Step1Items.Select(t => new Point(t.FrequencyItems[0].Frequency, t.MaxItem?.MeasurePower ?? 0))
                ],
                Colors.Blue);
        }

        Step1ScatterPlotControl.AutoScaleRefresh();
    }

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        InterpolationCount,
        Frequencies,
        StepFrequency,
        AODWaveformElectrodeOffsetParams = new HtmlTable([.. ElectrodeOffsetParams.Select(t => t.ToHtmlAnonymous())]),
        StartAmplitude,
        StepAmplitude,
        StopAmplitude,
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

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem>
    where TCache : AODWaveformElectrodeOffsetCache<TItem>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    protected string AODWaveformCsvResultFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "CSV", $"{GetType().Name}.CSV");

    protected override void LoggerResult(int stepIndex)
    {
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(
            stepIndex switch
            {
                0 => new
                {
                    ElectrodeOffsetItems = new HtmlContainer([..Cache.Step0Items.Select(t => t.Step0ScatterPlotControl.GetHtmlPlot2DLinesChart())])
                },
                1 => new
                {
                    ElectrodeOffsetItems = new HtmlContainer([..Cache.Step0Items.Select(t => t.Step0ScatterPlotControl.GetHtmlPlot2DLinesChart())]),
                    AmplitudeItems = new HtmlContainer([
                        Cache.Step1ScatterPlotControl.GetHtmlPlot2DLinesChart(0),
                        Cache.Step1ScatterPlotControl.GetHtmlPlot2DLinesChart(1),
                        Cache.Step1ScatterPlotControl.GetHtmlPlot2DLinesChart(2)
                    ])
                },
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<object>(nameof(stepIndex), stepIndex, null)
            }
        ), HtmlLogUniqueId.LoggingHtml());
    }


    [RelayCommand]
    private void Loaded()
    {
        Cache = CacheProvider.GetOrDefault<TCache>();
        Cache.RefreshPlot();

        foreach (var step0 in Cache.Step0Items) step0.RefreshPlot();
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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(0, "Step 1 Electrode Offset", async () =>
        {
            Guard.IsTrue(Cache.Frequencies.Count >= 2);
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

                Logger.LogHtmlInformation(string.Join(", ", electrodes), HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var step0 = new AODWaveformElectrodeOffsetStep0<TItem> { Electrodes = electrodes };
                Cache.Step0Items = [.. Cache.Step0Items, step0];

                var offsetFrequencyPeriodCoefficients = Generate.LinearRange(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                foreach (var frequency in Cache.Frequencies)
                {
                    var step0Item = new AODWaveformElectrodeOffsetStep0Item<TItem>();
                    step0.Items = [.. step0.Items, step0Item];

                    await InvokeItemsAsync(frequency, item => step0Item.FrequencyItems = [.. step0Item.FrequencyItems, item]);
                }

                step0.InterpolationMaxima(Cache.InterpolationCount);

                var aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<AODWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel>();
                aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient = GuardUtils.IsNotNullAndReturn(step0.OffsetFrequencyPeriodCoefficient);

                var showDialog = WindowManagerService.ShowDialog(aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel);
                if (showDialog == true) step0.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient;

                Cache.ElectrodeConfigurationResults =
                [
                    .. Cache.ElectrodeConfigurationResults, new GenerateAODWaveformElectrodeConfiguration
                    {
                        OpticsAODElectrodeEnum = param.OpticsAODElectrodeEnum,
                        OffsetFrequency = Cache.OffsetFrequency,
                        OffsetFrequencyPeriodCoefficient = GuardUtils.IsNotNullAndReturn(step0.OffsetFrequencyPeriodCoefficient)
                    }
                ];

                continue;

                async Task InvokeItemsAsync(double frequency, Action<TItem> action)
                {
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

                        action.Invoke(item);
                    }
                }
            }

            return Cache.ElectrodeConfigurationResults.Count == Cache.ElectrodeOffsetParams.Count;
        }, isShowDialog).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isShowDialog, CancellationToken cancellationToken)
    {
        return await InvokeAsync(1, "Step 2 Uniformity", async () =>
        {
            Guard.IsTrue(Cache.Frequencies.Count >= 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            foreach (var step0 in Cache.Step0Items)
            {
                step0.OffsetFrequencyPeriodCoefficient = Cache.ElectrodeConfigurationResults
                    .Single(t => t.OpticsAODElectrodeEnum == step0.Electrodes[^1])
                    .OffsetFrequencyPeriodCoefficient;
            }

            Cache.Step1Items = [];
            Cache.UniformityConfigurationResults = [];

            GenerateFixedAODWaveform(cancellationToken);

            var frequencies = Generate.LinearRange(Cache.Frequencies[0], Cache.StepFrequency, Cache.Frequencies[^1]);
            Guard.IsNotEmpty(frequencies);
            var amplitudes = Generate.LinearRange(Cache.StartAmplitude, Cache.StepAmplitude, Cache.StopAmplitude).Reverse().ToArray();
            Guard.IsNotEmpty(amplitudes);

            foreach (var frequency in frequencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var step1 = new AODWaveformElectrodeOffsetStep1<TItem>();
                Cache.Step1Items = [.. Cache.Step1Items, step1];

                foreach (var amplitude in amplitudes)
                {
                    var item = new TItem
                    {
                        ElectrodeConfigurations =
                        [
                            .. Cache.ElectrodeConfigurationResults
                                .Select(t => t.Clone()
                                    .WithAmplitude(t.OpticsAODElectrodeEnum is OpticsAODElectrodeEnum.Electrode3 or OpticsAODElectrodeEnum.Electrode4
                                        ? amplitude
                                        : Cache.DefaultAmplitude))
                        ],
                        Amplitude = amplitude,
                        Frequency = frequency
                    };

                    Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                    step1.FrequencyItems = [.. step1.FrequencyItems, item];
                }
            }

            Cache.UniformityConfigurationResults =
            [
                .. Cache.Step1Items.Select(t => new GenerateAODWaveformUniformityConfiguration
                {
                    Frequency = t.FrequencyItems[0].Frequency,
                    Coefficient = GuardUtils.IsNotNullAndReturn(t.MaxItem).OffsetFrequencyPeriodCoefficient
                })
            ];

            return Cache.UniformityConfigurationResults.Count == frequencies.Length;
        }, isShowDialog).ConfigureAwait(false);
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AllAsync(CancellationToken cancellationToken)
    {
#if NET
        await using
#else
        using
#endif
            var _ = cancellationToken.Register(() =>
            {
                if (Step0Command.CanBeCanceled) Step0Command.Cancel();
                if (Step1Command.CanBeCanceled) Step1Command.Cancel();
            });

        var step0Task = GuardUtils.IsAssignableToType<Task<bool>>(Step0Command.ExecuteAsync(false));
        if (await step0Task == false) return;

        await Step1Command.ExecuteAsync(true);
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

public sealed partial class AODWaveformElectrodeOffsetStep0<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

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
    [property: LiteDB.BsonIgnore]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _step0ScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    #endregion

    public AODWaveformElectrodeOffsetStep0()
    {
        Step0ScatterPlotControl.Configure();
        Step0ScatterPlotControl.ToggleLegend(false);
    }

    public void RefreshPlot()
    {
        Step0ScatterPlotControl.SetTitle($"{(OffsetFrequencyPeriodCoefficient is null ? string.Empty : $"Result: {OffsetFrequencyPeriodCoefficient.Value:0.###}(2pi) | ")}{string.Join(", ", Electrodes)}(Y: mW - X: 2pi)");

        foreach (var (index, item) in Items.Index())
        {
            if (item.FrequencyItems.Count <= 0) continue;

            var scatterMarkersOrigin = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                [.. item.FrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                index,
                new Range(0, Items.Count - 1),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            Step0ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                item.FrequencyInterpolationPoints,
                index,
                new Range(0, Items.Count - 1));

            var scatterMarkersMaxima = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                item.FrequencyMaximaPoints,
                index,
                new Range(0, Items.Count - 1),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        if (ClosestMaximaPoints.Count > 0)
        {
            var scatterMarkersClosestMaxima = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                "Closest Maxima",
                ClosestMaximaPoints,
                Colors.Blue,
                markerShape: MarkerShape.FilledSquare);
            scatterMarkersClosestMaxima.MarkerSize = 20;
        }

        Step0ScatterPlotControl.AutoScaleRefresh();
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

        var (results, _) = Extremumor.FindClosestExtremum([..Items.Select(t => Vector<double>.Build.DenseOfEnumerable(t.FrequencyMaximaPoints.Select(tt => tt.X)))]);

        foreach (var (index, (xIndex, xValue)) in results.Index())
        {
            ClosestMaximaPoints = [.. ClosestMaximaPoints, new Point(xValue, Items[index].FrequencyMaximaPoints[xIndex].Y)];
        }

        OffsetFrequencyPeriodCoefficient = ClosestMaximaPoints.Average(t => t.X);
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
    private IReadOnlyList<TItem> _frequencyItems = [];

    #region Result

    [ObservableProperty]
    private TItem? _maxItem;

    #endregion
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