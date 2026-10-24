using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class RTAScanResult : ObservableObject
{
    [Newtonsoft.Json.JsonProperty]
    private readonly ProductivityInformation _productivityInformation;

    [Newtonsoft.Json.JsonProperty]
    private readonly double _xPixelSize;

    [Newtonsoft.Json.JsonProperty]
    private readonly double _yPixelSize;

    [ObservableProperty]
    public partial ICalibrationAlgorithmService.RTAScanSample[] ForwardRTAScanSamples { get; set; } = [];

    [ObservableProperty]
    public partial ICalibrationAlgorithmService.RTAScanSample[] ReverseRTAScanSamples { get; set; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial IPlotDataSource TimeDomainPlotDataSource { get; set; } = new PlotDataSource();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial IPlotDataSource FrequencyDomainPlotDataSource { get; set; } = new PlotDataSource();

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnForwardRTAScanSamplesChanged(ICalibrationAlgorithmService.RTAScanSample[] value) => Refresh();

    partial void OnReverseRTAScanSamplesChanged(ICalibrationAlgorithmService.RTAScanSample[] value) => Refresh();

    // ReSharper restore UnusedParameterInPartialMethod

    [Newtonsoft.Json.JsonConstructor]
    public RTAScanResult(
        [Newtonsoft.Json.JsonProperty(nameof(_productivityInformation))]
        ProductivityInformation productivityInformation,
        [Newtonsoft.Json.JsonProperty(nameof(_xPixelSize))]
        double xPixelSize,
        [Newtonsoft.Json.JsonProperty(nameof(_yPixelSize))]
        double yPixelSize)
    {
        _productivityInformation = productivityInformation;
        _xPixelSize = xPixelSize;
        _yPixelSize = yPixelSize;

        var customTimeDomainPlotDataSource = new CustomGrid();
        TimeDomainPlotDataSource.Configure(customTimeDomainPlotDataSource, 4,
            plots =>
            {
                customTimeDomainPlotDataSource.Set(plots[0], new GridCell(0, 0, 2, 2));
                customTimeDomainPlotDataSource.Set(plots[1], new GridCell(0, 1, 2, 2));
                customTimeDomainPlotDataSource.Set(plots[2], new GridCell(1, 0, 2, 2));
                customTimeDomainPlotDataSource.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        TimeDomainPlotDataSource.SetTitle(0, "Time Domain Signal Forward X(Y: um - X: ms)");
        TimeDomainPlotDataSource.SetTitle(1, "Time Domain Signal Forward Y(Y: um - X: ms)");
        TimeDomainPlotDataSource.SetTitle(2, "Time Domain Signal Reverse X(Y: um - X: ms)");
        TimeDomainPlotDataSource.SetTitle(3, "Time Domain Signal Reverse Y(Y: um - X: ms)");

        var customFrequencyDomainPlotDataSource = new CustomGrid();
        FrequencyDomainPlotDataSource.Configure(customFrequencyDomainPlotDataSource, 4,
            plots =>
            {
                customFrequencyDomainPlotDataSource.Set(plots[0], new GridCell(0, 0, 2, 2));
                customFrequencyDomainPlotDataSource.Set(plots[1], new GridCell(0, 1, 2, 2));
                customFrequencyDomainPlotDataSource.Set(plots[2], new GridCell(1, 0, 2, 2));
                customFrequencyDomainPlotDataSource.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        FrequencyDomainPlotDataSource.SetTitle(0, "Spectrum FFT Analysis Forward X(Y: Modulus - X: Hz)");
        FrequencyDomainPlotDataSource.SetTitle(1, "Spectrum FFT Analysis Forward Y(Y: Modulus - X: Hz)");
        FrequencyDomainPlotDataSource.SetTitle(2, "Spectrum FFT Analysis Reverse X(Y: Modulus - X: Hz)");
        FrequencyDomainPlotDataSource.SetTitle(3, "Spectrum FFT Analysis Reverse Y(Y: Modulus - X: Hz)");
    }

    public RTAScanResult() : this(ProductivityInformation.Default, 0d, 0d)
    {
    }

    private void Refresh()
    {
        try
        {
            Point[] forwardXes = [.. ForwardRTAScanSamples.Select(x => new Point(x.X / _productivityInformation.SampleRate, x.Error.X * _xPixelSize))];
            Point[] forwardYes = [.. ForwardRTAScanSamples.Select(x => new Point(x.X / _productivityInformation.SampleRate, x.Error.Y * _yPixelSize))];

            TimeDomain(0, forwardXes);
            TimeDomain(1, forwardYes);
            FrequencyDomain(0, forwardXes);
            FrequencyDomain(1, forwardYes);

            Point[] reverseXes = [.. ReverseRTAScanSamples.Select(x => new Point(x.X / _productivityInformation.SampleRate, x.Error.X * _xPixelSize))];
            Point[] reverseYes = [.. ReverseRTAScanSamples.Select(x => new Point(x.X / _productivityInformation.SampleRate, x.Error.Y * _yPixelSize))];

            TimeDomain(2, reverseXes);
            TimeDomain(3, reverseYes);
            FrequencyDomain(2, reverseXes);
            FrequencyDomain(3, reverseYes);
        }
        finally
        {
            TimeDomainPlotDataSource.AutoScaleRefresh();
            FrequencyDomainPlotDataSource.AutoScaleRefresh();
        }

        return;

        void TimeDomain(int plotIndex, Point[] points)
        {
            var scatterLines = TimeDomainPlotDataSource.GetOrAddScatterLines(plotIndex, points.Length > 0 ? 1 : 0);

            if (scatterLines.Count <= 0) return;

            scatterLines[0].Update(string.Empty, points);
        }

        void FrequencyDomain(int plotIndex, Point[] points)
        {
            var scatterLines = TimeDomainPlotDataSource.GetOrAddScatterLines(plotIndex, points.Length > 0 ? 1 : 0);

            if (scatterLines.Count <= 0) return;

            var timeStep = Math.Abs(points[1].X - points[0].X);

            var signals = Vector<double>.Build.Dense([.. points.Select(t => t.Y)]);

            var fftResult = signals.ToComplex().FastFourierTransform();
            var (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(1d / timeStep * 1000d);

            scatterLines[0].Update(string.Empty, [.. fftFrequencies.Index().Select(t => new Point(t.Item, fftMagnitudes[t.Index]))]);
        }
    }
}