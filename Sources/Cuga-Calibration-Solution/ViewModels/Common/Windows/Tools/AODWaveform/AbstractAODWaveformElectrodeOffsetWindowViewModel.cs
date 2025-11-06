using System.Collections;
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
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetParam : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    #region AOD Waveform Electrode Offset

    [ObservableProperty]
    private double _startOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopOffsetFrequencyPeriodCoefficient;

    #endregion AOD Waveform Electrode Offset

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        StartOffsetFrequencyPeriodCoefficient,
        StepOffsetFrequencyPeriodCoefficient,
        StopOffsetFrequencyPeriodCoefficient
    };
}

public partial class AODWaveformElectrodeOffsetCache<TItem> : AODWaveformCommonCache
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    #region Param

    [ObservableProperty]
    private double _offsetFrequency;

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

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetParam> _electrodeOffsetParams =
    [
        new()
        {
            OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1
        }
    ];

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
    private IReadOnlyList<AODWaveformElectrodeOffsetResult<TItem>> _electrodeOffsetItems = [];

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetResult2<TItem>> _amplitudeItems = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformUniformityConfiguration> _uniformityConfigurationResults = [];

    #endregion Result

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: LiteDB.BsonIgnore]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public AODWaveformElectrodeOffsetCache()
    {
        ScatterPlotControl.Configure(totalPlotCount: 2);
        ScatterPlotControl.SetTitle(0, $"{nameof(AmplitudeItems)}(Y: mW - X: AMP)");
        ScatterPlotControl.SetTitle(1, $"{nameof(AmplitudeItems)}(Y: AMP - X: MHz)");
    }

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
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

    public void RefreshPlot()
    {
        foreach (var aodWaveformElectrodeOffsetResult in ElectrodeOffsetItems) aodWaveformElectrodeOffsetResult.RefreshPlot();

        foreach (var (index, item) in AmplitudeItems.Index())
        {
            ScatterPlotControl.GetOrAddScatterLine(
                0,
                $"{item.Frequency}(MHz)",
                [..item.FrequencyItems.Select(t => new Point(t.Amplitude, t.MeasurePower))],
                index,
                new Range(0, AmplitudeItems.Count - 1));

            item.MaxMeasurePowerAmplitude = GuardUtils.IsNotNullAndReturn(item.FrequencyItems.MaxBy(t => t.MeasurePower)).Amplitude;
        }

        if (AmplitudeItems.Count <= 0) return;

        ScatterPlotControl.GetOrAddScatterLine(
            1,
            "(MHz)",
            [..AmplitudeItems.Select(t => new Point(t.Frequency, t.MaxMeasurePowerAmplitude))],
            Colors.Blue);
    }
}

public sealed partial class AODWaveformElectrodeOffsetResult<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    #region Low Frequency

    [ObservableProperty]
    private IReadOnlyList<TItem> _lowFrequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _lowFrequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _lowFrequencyInterpolationMaximaPoints = [];

    #endregion

    #region Middle Frequency

    [ObservableProperty]
    private IReadOnlyList<TItem> _middleFrequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _middleFrequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _middleFrequencyInterpolationMaximaPoints = [];

    #endregion

    #region High Frequency

    [ObservableProperty]
    private IReadOnlyList<TItem> _highFrequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _highFrequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _highFrequencyInterpolationMaximaPoints = [];

    #endregion

    [ObservableProperty]
    private IReadOnlyList<Point> _interpolationClosestMaximaPoints = [];

    public double? OffsetFrequencyPeriodCoefficient => InterpolationClosestMaximaPoints.Count > 0 ? InterpolationClosestMaximaPoints.Average(t => t.X) : null;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: LiteDB.BsonIgnore]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public AODWaveformElectrodeOffsetResult()
    {
        ScatterPlotControl.Configure();
    }

    public void InterpolationMaxima()
    {
        LowFrequencyInterpolationPoints = [];
        LowFrequencyInterpolationMaximaPoints = [];

        MiddleFrequencyInterpolationPoints = [];
        MiddleFrequencyInterpolationMaximaPoints = [];

        HighFrequencyInterpolationPoints = [];
        HighFrequencyInterpolationMaximaPoints = [];

        InterpolationClosestMaximaPoints = [];

        var (lowFrequencyInterpolationX, lowFrequencyInterpolationY) = Interpolator.SplineInterpolation(
            Vector<double>.Build.Dense([..LowFrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
            Vector<double>.Build.Dense([..LowFrequencyItems.Select(t => t.MeasurePower)]),
            3);
        LowFrequencyInterpolationPoints = [.. lowFrequencyInterpolationX.Index().Select(t => new Point(t.Item, lowFrequencyInterpolationY[t.Index]))];

        var (lowFrequencyMaximaX, lowFrequencyMaximaY) = Extremumor.FindLocalMaxima(
            Vector<double>.Build.Dense([..LowFrequencyInterpolationPoints.Select(t => t.X)]),
            Vector<double>.Build.Dense([..LowFrequencyInterpolationPoints.Select(t => t.Y)]),
            isContainsEdge: true);
        LowFrequencyInterpolationMaximaPoints = [.. lowFrequencyMaximaX.Index().Select(t => new Point(t.Item, lowFrequencyMaximaY[t.Index]))];

        var (middleFrequencyInterpolationX, middleFrequencyInterpolationY) = Interpolator.SplineInterpolation(
            Vector<double>.Build.Dense([..MiddleFrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
            Vector<double>.Build.Dense([..MiddleFrequencyItems.Select(t => t.MeasurePower)]),
            3);
        MiddleFrequencyInterpolationPoints = [.. middleFrequencyInterpolationX.Index().Select(t => new Point(t.Item, middleFrequencyInterpolationY[t.Index]))];

        var (middleFrequencyMaximaX, middleFrequencyMaximaY) = Extremumor.FindLocalMaxima(
            Vector<double>.Build.Dense([..MiddleFrequencyInterpolationPoints.Select(t => t.X)]),
            Vector<double>.Build.Dense([..MiddleFrequencyInterpolationPoints.Select(t => t.Y)]),
            isContainsEdge: true);
        MiddleFrequencyInterpolationMaximaPoints = [.. middleFrequencyMaximaX.Index().Select(t => new Point(t.Item, middleFrequencyMaximaY[t.Index]))];

        var (highFrequencyInterpolationX, highFrequencyInterpolationY) = Interpolator.SplineInterpolation(
            Vector<double>.Build.Dense([..HighFrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
            Vector<double>.Build.Dense([..HighFrequencyItems.Select(t => t.MeasurePower)]),
            3);
        HighFrequencyInterpolationPoints = [.. highFrequencyInterpolationX.Index().Select(t => new Point(t.Item, highFrequencyInterpolationY[t.Index]))];

        var (highFrequencyMaximaX, highFrequencyMaximaY) = Extremumor.FindLocalMaxima(
            Vector<double>.Build.Dense([..HighFrequencyInterpolationPoints.Select(t => t.X)]),
            Vector<double>.Build.Dense([..HighFrequencyInterpolationPoints.Select(t => t.Y)]),
            isContainsEdge: true);
        HighFrequencyInterpolationMaximaPoints = [.. highFrequencyMaximaX.Index().Select(t => new Point(t.Item, highFrequencyMaximaY[t.Index]))];

        var (x1, y1, x2, y2, x3, y3, _) = Extremumor.FindClosestTriplet(
            lowFrequencyMaximaX, lowFrequencyMaximaY,
            middleFrequencyMaximaX, middleFrequencyMaximaY,
            highFrequencyMaximaX, highFrequencyMaximaY
        );

        InterpolationClosestMaximaPoints = [new Point(x1, y1), new Point(x2, y2), new Point(x3, y3)];

        RefreshPlot();
    }

    public void RefreshPlot()
    {
        ScatterPlotControl.SetTitle($"{(OffsetFrequencyPeriodCoefficient is null ? string.Empty : $"Result: {OffsetFrequencyPeriodCoefficient.Value:0.###}(2pi) | ")}{string.Join(",", Electrodes)}(Y: mW - X: 2pi)");

        if (LowFrequencyItems.Count > 0)
        {
            var scatterMarkersOrigin = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {LowFrequencyItems[0].Frequency:0.###}(MHz)",
                [..LowFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                0,
                new Range(0, 2),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {LowFrequencyItems[0].Frequency:0.###}(MHz)",
                LowFrequencyInterpolationPoints,
                0,
                new Range(0, 2));

            var scatterMarkersMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {LowFrequencyItems[0].Frequency:0.###}(MHz)",
                LowFrequencyInterpolationMaximaPoints,
                0,
                new Range(0, 2),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        if (MiddleFrequencyItems.Count > 0)
        {
            var scatterMarkersOrigin = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {MiddleFrequencyItems[0].Frequency:0.###}(MHz)",
                [..MiddleFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                1,
                new Range(0, 2),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {MiddleFrequencyItems[0].Frequency:0.###}(MHz)",
                MiddleFrequencyInterpolationPoints,
                1,
                new Range(0, 2));

            var scatterMarkersMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {MiddleFrequencyItems[0].Frequency:0.###}(MHz)",
                MiddleFrequencyInterpolationMaximaPoints,
                1,
                new Range(0, 2),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        if (HighFrequencyItems.Count > 0)
        {
            var scatterMarkersOrigin = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {HighFrequencyItems[0].Frequency:0.###}(MHz)",
                [..HighFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                2,
                new Range(0, 2),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {HighFrequencyItems[0].Frequency:0.###}(MHz)",
                HighFrequencyInterpolationPoints,
                2,
                new Range(0, 2));

            var scatterMarkersMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {HighFrequencyItems[0].Frequency:0.###}(MHz)",
                HighFrequencyInterpolationMaximaPoints,
                2,
                new Range(0, 2),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        var scatterMarkersClosestMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
            "Closest Maxima",
            InterpolationClosestMaximaPoints,
            Colors.Blue,
            markerShape: MarkerShape.FilledSquare);
        scatterMarkersClosestMaxima.MarkerSize = 20;

        ScatterPlotControl.AutoScaleRefresh();
    }
}

public sealed partial class AODWaveformElectrodeOffsetResult2<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _maxMeasurePowerAmplitude;

    [ObservableProperty]
    private IReadOnlyList<TItem> _frequencyItems = [];
}

public partial class AODWaveformElectrodeOffsetItem : AODWaveformCommonItem
{
    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurations = [];

    [ObservableProperty]
    private double _amplitude;

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

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem>
    where TCache : AODWaveformElectrodeOffsetCache<TItem>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    protected string AODWaveformCsvResultFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "CSV", $"{GetType().Name}.CSV");

    protected override void LoggerResult()
    {
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            ElectrodeOffsetItems = new HtmlContainer([..Cache.ElectrodeOffsetItems.Select(t => t.ScatterPlotControl.GetHtmlPlot2DLinesChart())])
        }), HtmlLogUniqueId.LoggingHtml());
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
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Action", async () =>
        {
            Cache.ElectrodeOffsetItems = [];
            Cache.AmplitudeItems = [];
            Cache.ElectrodeConfigurationResults =
            [
                new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = 0d
                }
            ];
            Cache.UniformityConfigurationResults = [];

            Logger.LogHtmlInformation("Electrode Offset", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

            GenerateFixedAODWaveform(cancellationToken);

            foreach (var param in Cache.ElectrodeOffsetParams)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Cache.ElectrodeConfigurationResults.Any(t => t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)) continue;

                Logger.LogHtmlInformation($"{param.OpticsAODElectrodeEnum}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var aodWaveformElectrodeOffsetResult = new AODWaveformElectrodeOffsetResult<TItem>
                {
                    Electrodes = [..Cache.ElectrodeConfigurationResults.Select(t => t.OpticsAODElectrodeEnum), param.OpticsAODElectrodeEnum]
                };

                Cache.ElectrodeOffsetItems = [.. Cache.ElectrodeOffsetItems, aodWaveformElectrodeOffsetResult];

                var offsetFrequencyPeriodCoefficients = Generate.LinearRange(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                await InvokeItemsAsync(Cache.LowFrequency, item => aodWaveformElectrodeOffsetResult.LowFrequencyItems = [.. aodWaveformElectrodeOffsetResult.LowFrequencyItems, item]);
                await InvokeItemsAsync(Cache.MiddleFrequency, item => aodWaveformElectrodeOffsetResult.MiddleFrequencyItems = [.. aodWaveformElectrodeOffsetResult.MiddleFrequencyItems, item]);
                await InvokeItemsAsync(Cache.HighFrequency, item => aodWaveformElectrodeOffsetResult.HighFrequencyItems = [.. aodWaveformElectrodeOffsetResult.HighFrequencyItems, item]);

                aodWaveformElectrodeOffsetResult.InterpolationMaxima();

                Cache.ElectrodeConfigurationResults =
                [
                    .. Cache.ElectrodeConfigurationResults, new GenerateAODWaveformElectrodeConfiguration
                    {
                        OpticsAODElectrodeEnum = param.OpticsAODElectrodeEnum,
                        OffsetFrequency = Cache.OffsetFrequency,
                        OffsetFrequencyPeriodCoefficient = GuardUtils.IsNotNullAndReturn(aodWaveformElectrodeOffsetResult.OffsetFrequencyPeriodCoefficient)
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
                                            : Cache.ElectrodeConfigurationResults.SingleOrDefault(tt => tt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum)?.OffsetFrequencyPeriodCoefficient ?? 0
                                    })
                            ],
                            Amplitude = Cache.DefaultAmplitude,
                            Frequency = frequency,
                            OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                        };

                        Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                        await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                        action.Invoke(item);

                        Cache.RefreshPlot();
                    }
                }
            }

            var frequencies = Generate.LinearRange(Cache.LowFrequency, Cache.StepFrequency, Cache.HighFrequency);
            Guard.IsNotEmpty(frequencies);
            var amplitudes = Generate.LinearRange(Cache.StartAmplitude, Cache.StepAmplitude, Cache.StopAmplitude).Reverse().ToArray();
            Guard.IsNotEmpty(amplitudes);

            Logger.LogHtmlInformation("Frequency", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

            foreach (var frequency in frequencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                var aodWaveformElectrodeOffsetResult2 = new AODWaveformElectrodeOffsetResult2<TItem>
                {
                    Frequency = frequency
                };

                Cache.AmplitudeItems = [.. Cache.AmplitudeItems, aodWaveformElectrodeOffsetResult2];

                foreach (var amplitude in amplitudes)
                {
                    var item = new TItem
                    {
                        ElectrodeConfigurations = Cache.ElectrodeConfigurationResults,
                        Amplitude = amplitude,
                        Frequency = frequency,
                    };

                    Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                    aodWaveformElectrodeOffsetResult2.FrequencyItems = [.. aodWaveformElectrodeOffsetResult2.FrequencyItems, item];

                    Cache.RefreshPlot();
                }
            }

            Cache.UniformityConfigurationResults =
            [
                .. Cache.AmplitudeItems.Select(t => new GenerateAODWaveformUniformityConfiguration
                {
                    Frequency = t.Frequency,
                    Coefficient = t.MaxMeasurePowerAmplitude
                })
            ];

            DialogWindowProvider.ShowDialog("OK");

            return true;
        }).ConfigureAwait(false);
    }
}