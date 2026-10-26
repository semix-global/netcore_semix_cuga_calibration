using CommunityToolkit.Diagnostics;
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
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

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

        var customGrid = new CustomGrid();
        PlotDataSource.Configure(customGrid, 4,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 2));
                customGrid.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        PlotDataSource.SetTitle(0, "Time Domain Signal X(Y: um - X: ms)");
        PlotDataSource.SetTitle(1, "Time Domain Signal Y(Y: um - X: ms)");
        PlotDataSource.SetTitle(2, "Spectrum FFT Analysis X(Y: Modulus - X: Hz)");
        PlotDataSource.SetTitle(3, "Spectrum FFT Analysis Y(Y: Modulus - X: Hz)");
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
            Point[] reverseXes = [.. ReverseRTAScanSamples.Select(x => new Point(x.X / _productivityInformation.SampleRate, x.Error.X * _xPixelSize))];
            Point[] reverseYes = [.. ReverseRTAScanSamples.Select(x => new Point(x.X / _productivityInformation.SampleRate, x.Error.Y * _yPixelSize))];

            var isForwardAvailable = forwardXes.Length > 0;
            var isReverseAvailable = reverseXes.Length > 0;
            var plotCount = (isForwardAvailable ? 1 : 0) + (isReverseAvailable ? 1 : 0);

            if (isReverseAvailable) Guard.IsEqualTo(plotCount, 2, "Reverse data is available but forward data is not. Both forward and reverse data must be available for plotting.");

            var xTimeDomainScatterLines = PlotDataSource.GetOrAddScatterLines(0, plotCount);
            var yTimeDomainScatterLines = PlotDataSource.GetOrAddScatterLines(1, plotCount);
            var xFrequencyDomainScatterLines = PlotDataSource.GetOrAddScatterLines(2, plotCount);
            var yFrequencyDomainScatterLines = PlotDataSource.GetOrAddScatterLines(3, plotCount);

            xTimeDomainScatterLines.ElementAtOrDefault(0)?.Update("Forward", forwardXes, color: Colors.Blue);
            yTimeDomainScatterLines.ElementAtOrDefault(0)?.Update("Forward", forwardYes, color: Colors.Blue);

            xTimeDomainScatterLines.ElementAtOrDefault(1)?.Update("Reverse", reverseXes, color: Colors.Red);
            yTimeDomainScatterLines.ElementAtOrDefault(1)?.Update("Reverse", reverseYes, color: Colors.Red);

            xFrequencyDomainScatterLines.ElementAtOrDefault(0)?.Update("Forward", FrequencyDomain(forwardXes), color: Colors.Blue);
            yFrequencyDomainScatterLines.ElementAtOrDefault(0)?.Update("Forward", FrequencyDomain(forwardYes), color: Colors.Blue);

            xFrequencyDomainScatterLines.ElementAtOrDefault(1)?.Update("Reverse", FrequencyDomain(reverseXes), color: Colors.Red);
            yFrequencyDomainScatterLines.ElementAtOrDefault(1)?.Update("Reverse", FrequencyDomain(reverseYes), color: Colors.Red);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }

        return;

        Point[] FrequencyDomain(Point[] points)
        {
            if (points.Length < 2) return [];

            var timeStep = Math.Abs(points[1].X - points[0].X);

            Guard.IsTrue(double.IsFinite(timeStep) && timeStep > 0d, "Time step must be a finite positive number.");

            var signals = Vector<double>.Build.Dense([.. points.Select(t => t.Y)]);

            var fftResult = signals.ToComplex().FastFourierTransform();
            var (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(1d / timeStep * 1000d);

            return [.. fftFrequencies.Index().Select(t => new Point(t.Item, fftMagnitudes[t.Index]))];
        }
    }
}