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
    private double _lowFrequency;

    [ObservableProperty]
    private double _middleFrequency;

    [ObservableProperty]
    private double _highFrequency;

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
    private IReadOnlyList<AODWaveformElectrodeOffsetStep0Item<TItem>> _step0Items = [];

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>> _step1Items = [];

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

    partial void OnStep1ItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>> newValue)
    {
        foreach (var step1Item in oldValue ?? [])
        {
            step1Item.PropertyChanged -= Step1ItemOnPropertyChanged;
        }

        foreach (var step1Item in newValue)
        {
            step1Item.PropertyChanged -= Step1ItemOnPropertyChanged;
            step1Item.PropertyChanged += Step1ItemOnPropertyChanged;
        }
    }

    private void Step1ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshPlot();
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
        }

        if (Step1Items.Count > 0)
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
        LowFrequency,
        MiddleFrequency,
        HighFrequency,
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
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

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
                    ]),
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

        foreach (var step0Item in Cache.Step0Items) step0Item.RefreshPlot();
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

                var header = string.Join(", ", electrodes);
                Logger.LogHtmlInformation(header, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var step0Item = new AODWaveformElectrodeOffsetStep0Item<TItem>();
                step0Item.Step0ScatterPlotControl.SetTitle(header);
                Cache.Step0Items = [.. Cache.Step0Items, step0Item];

                var offsetFrequencyPeriodCoefficients = Generate.LinearRange(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                await InvokeItemsAsync(Cache.LowFrequency, item => step0Item.LowFrequencyItems = [.. step0Item.LowFrequencyItems, item]);
                await InvokeItemsAsync(Cache.MiddleFrequency, item => step0Item.MiddleFrequencyItems = [.. step0Item.MiddleFrequencyItems, item]);
                await InvokeItemsAsync(Cache.HighFrequency, item => step0Item.HighFrequencyItems = [.. step0Item.HighFrequencyItems, item]);

                step0Item.InterpolationMaxima(Cache.InterpolationCount);

                var aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<AODWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel>();
                aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient = GuardUtils.IsNotNullAndReturn(step0Item.OffsetFrequencyPeriodCoefficient);

                var showDialog = WindowManagerService.ShowDialog(aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel);
                if (showDialog == true) step0Item.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient;

                Cache.ElectrodeConfigurationResults =
                [
                    .. Cache.ElectrodeConfigurationResults, new GenerateAODWaveformElectrodeConfiguration
                    {
                        OpticsAODElectrodeEnum = param.OpticsAODElectrodeEnum,
                        OffsetFrequency = Cache.OffsetFrequency,
                        OffsetFrequencyPeriodCoefficient = GuardUtils.IsNotNullAndReturn(step0Item.OffsetFrequencyPeriodCoefficient)
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
                            Electrodes = electrodes,
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
                                                ?.OffsetFrequencyPeriodCoefficient ?? 0
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
            Cache.Step1Items = [];
            Cache.UniformityConfigurationResults = [];

            GenerateFixedAODWaveform(cancellationToken);

            var frequencies = Generate.LinearRange(Cache.LowFrequency, Cache.StepFrequency, Cache.HighFrequency);
            Guard.IsNotEmpty(frequencies);
            var amplitudes = Generate.LinearRange(Cache.StartAmplitude, Cache.StepAmplitude, Cache.StopAmplitude).Reverse().ToArray();
            Guard.IsNotEmpty(amplitudes);

            foreach (var frequency in frequencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var step1Item = new AODWaveformElectrodeOffsetStep1Item<TItem>();
                Cache.Step1Items = [.. Cache.Step1Items, step1Item];

                foreach (var amplitude in amplitudes)
                {
                    var item = new TItem
                    {
                        Electrodes = [.. Cache.ElectrodeConfigurationResults.Select(t => t.OpticsAODElectrodeEnum)],
                        ElectrodeConfigurations = Cache.ElectrodeConfigurationResults,
                        Amplitude = amplitude,
                        Frequency = frequency,
                    };

                    Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                    step1Item.FrequencyItems = [.. step1Item.FrequencyItems, item];
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

public sealed partial class AODWaveformElectrodeOffsetStep0Item<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    #region Low Frequency

    [ObservableProperty]
    private IReadOnlyList<TItem> _lowFrequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _lowFrequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _lowFrequencyMaximaPoints = [];

    partial void OnLowFrequencyItemsChanged(IReadOnlyList<TItem> value)
    {
        _ = value;

        RefreshPlot();
    }

    partial void OnLowFrequencyInterpolationPointsChanged(IReadOnlyList<Point> value)
    {
        _ = value;

        RefreshPlot();
    }

    partial void OnLowFrequencyMaximaPointsChanged(IReadOnlyList<Point> value)
    {
        _ = value;

        RefreshPlot();
    }

    #endregion

    #region Middle Frequency

    [ObservableProperty]
    private IReadOnlyList<TItem> _middleFrequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _middleFrequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _middleFrequencyMaximaPoints = [];

    partial void OnMiddleFrequencyItemsChanged(IReadOnlyList<TItem> value)
    {
        _ = value;

        RefreshPlot();
    }

    partial void OnMiddleFrequencyInterpolationPointsChanged(IReadOnlyList<Point> value)
    {
        _ = value;

        RefreshPlot();
    }

    partial void OnMiddleFrequencyMaximaPointsChanged(IReadOnlyList<Point> value)
    {
        _ = value;

        RefreshPlot();
    }

    #endregion

    #region High Frequency

    [ObservableProperty]
    private IReadOnlyList<TItem> _highFrequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _highFrequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _highFrequencyMaximaPoints = [];

    partial void OnHighFrequencyItemsChanged(IReadOnlyList<TItem> value)
    {
        _ = value;

        RefreshPlot();
    }

    partial void OnHighFrequencyInterpolationPointsChanged(IReadOnlyList<Point> value)
    {
        _ = value;

        RefreshPlot();
    }

    partial void OnHighFrequencyMaximaPointsChanged(IReadOnlyList<Point> value)
    {
        _ = value;

        RefreshPlot();
    }

    #endregion

    #region Result

    [ObservableProperty]
    private IReadOnlyList<Point> _closestMaximaPoints = [];

    [ObservableProperty]
    private double? _offsetFrequencyPeriodCoefficient;

    partial void OnClosestMaximaPointsChanged(IReadOnlyList<Point> value)
    {
        _ = value;

        RefreshPlot();
    }

    partial void OnOffsetFrequencyPeriodCoefficientChanged(double? value)
    {
        _ = value;

        RefreshPlot();
    }

    #endregion

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

    public AODWaveformElectrodeOffsetStep0Item()
    {
        Step0ScatterPlotControl.Configure();
        Step0ScatterPlotControl.ToggleLegend(false);
    }

    public void RefreshPlot()
    {
        Step0ScatterPlotControl.SetTitle($"{(OffsetFrequencyPeriodCoefficient is null ? string.Empty : $"Result: {OffsetFrequencyPeriodCoefficient.Value:0.###}(2pi) | ")}{Step0ScatterPlotControl.GetTitle().Split('|')[^1]}");

        if (LowFrequencyItems.Count > 0)
        {
            var scatterMarkersOrigin = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {LowFrequencyItems[0].Frequency:0.###}(MHz)",
                [.. LowFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                0,
                new Range(0, 2),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            Step0ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {LowFrequencyItems[0].Frequency:0.###}(MHz)",
                LowFrequencyInterpolationPoints,
                0,
                new Range(0, 2));

            var scatterMarkersMaxima = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {LowFrequencyItems[0].Frequency:0.###}(MHz)",
                LowFrequencyMaximaPoints,
                0,
                new Range(0, 2),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        if (MiddleFrequencyItems.Count > 0)
        {
            var scatterMarkersOrigin = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {MiddleFrequencyItems[0].Frequency:0.###}(MHz)",
                [.. MiddleFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                1,
                new Range(0, 2),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            Step0ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {MiddleFrequencyItems[0].Frequency:0.###}(MHz)",
                MiddleFrequencyInterpolationPoints,
                1,
                new Range(0, 2));

            var scatterMarkersMaxima = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {MiddleFrequencyItems[0].Frequency:0.###}(MHz)",
                MiddleFrequencyMaximaPoints,
                1,
                new Range(0, 2),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        if (HighFrequencyItems.Count > 0)
        {
            var scatterMarkersOrigin = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {HighFrequencyItems[0].Frequency:0.###}(MHz)",
                [.. HighFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                2,
                new Range(0, 2),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            Step0ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {HighFrequencyItems[0].Frequency:0.###}(MHz)",
                HighFrequencyInterpolationPoints,
                2,
                new Range(0, 2));

            var scatterMarkersMaxima = Step0ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {HighFrequencyItems[0].Frequency:0.###}(MHz)",
                HighFrequencyMaximaPoints,
                2,
                new Range(0, 2),
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
        LowFrequencyInterpolationPoints = [];
        LowFrequencyMaximaPoints = [];

        MiddleFrequencyInterpolationPoints = [];
        MiddleFrequencyMaximaPoints = [];

        HighFrequencyInterpolationPoints = [];
        HighFrequencyMaximaPoints = [];

        ClosestMaximaPoints = [];

        var (lowFrequencyInterpolationX, lowFrequencyInterpolationY) = Interpolator.SplineInterpolation(
            Vector<double>.Build.Dense([.. LowFrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
            Vector<double>.Build.Dense([.. LowFrequencyItems.Select(t => t.MeasurePower)]),
            densityFactor);
        LowFrequencyInterpolationPoints = [.. lowFrequencyInterpolationX.Index().Select(t => new Point(t.Item, lowFrequencyInterpolationY[t.Index]))];

        var (lowFrequencyMaximaX, lowFrequencyMaximaY) = Extremumor.FindMaxima(
            Vector<double>.Build.Dense([.. LowFrequencyInterpolationPoints.Select(t => t.X)]),
            Vector<double>.Build.Dense([.. LowFrequencyInterpolationPoints.Select(t => t.Y)]));
        LowFrequencyMaximaPoints =
        [
            .. lowFrequencyMaximaY
                .Index()
                .Where(t => t.Item > lowFrequencyInterpolationY.Average())
                .Select(t => new Point(lowFrequencyMaximaX[t.Index], t.Item))
        ];

        var (middleFrequencyInterpolationX, middleFrequencyInterpolationY) = Interpolator.SplineInterpolation(
            Vector<double>.Build.Dense([.. MiddleFrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
            Vector<double>.Build.Dense([.. MiddleFrequencyItems.Select(t => t.MeasurePower)]),
            densityFactor);
        MiddleFrequencyInterpolationPoints = [.. middleFrequencyInterpolationX.Index().Select(t => new Point(t.Item, middleFrequencyInterpolationY[t.Index]))];

        var (middleFrequencyMaximaX, middleFrequencyMaximaY) = Extremumor.FindMaxima(
            Vector<double>.Build.Dense([.. MiddleFrequencyInterpolationPoints.Select(t => t.X)]),
            Vector<double>.Build.Dense([.. MiddleFrequencyInterpolationPoints.Select(t => t.Y)]));
        MiddleFrequencyMaximaPoints =
        [
            .. middleFrequencyMaximaY
                .Index()
                .Where(t => t.Item > middleFrequencyInterpolationY.Average())
                .Select(t => new Point(middleFrequencyMaximaX[t.Index], t.Item))
        ];

        var (highFrequencyInterpolationX, highFrequencyInterpolationY) = Interpolator.SplineInterpolation(
            Vector<double>.Build.Dense([.. HighFrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
            Vector<double>.Build.Dense([.. HighFrequencyItems.Select(t => t.MeasurePower)]),
            densityFactor);
        HighFrequencyInterpolationPoints = [.. highFrequencyInterpolationX.Index().Select(t => new Point(t.Item, highFrequencyInterpolationY[t.Index]))];

        var (highFrequencyMaximaX, highFrequencyMaximaY) = Extremumor.FindMaxima(
            Vector<double>.Build.Dense([.. HighFrequencyInterpolationPoints.Select(t => t.X)]),
            Vector<double>.Build.Dense([.. HighFrequencyInterpolationPoints.Select(t => t.Y)]));
        HighFrequencyMaximaPoints =
        [
            .. highFrequencyMaximaY
                .Index()
                .Where(t => t.Item > highFrequencyInterpolationY.Average())
                .Select(t => new Point(highFrequencyMaximaX[t.Index], t.Item))
        ];

        var (x1, y1, x2, y2, x3, y3, _) = Extremumor.FindClosestTriplet(
            lowFrequencyMaximaX, lowFrequencyMaximaY,
            middleFrequencyMaximaX, middleFrequencyMaximaY,
            highFrequencyMaximaX, highFrequencyMaximaY
        );

        ClosestMaximaPoints = [new Point(x1, y1), new Point(x2, y2), new Point(x3, y3)];

        OffsetFrequencyPeriodCoefficient = ClosestMaximaPoints.Average(t => t.X);
    }
}

public sealed partial class AODWaveformElectrodeOffsetStep1Item<TItem> : ObservableCacheBase
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