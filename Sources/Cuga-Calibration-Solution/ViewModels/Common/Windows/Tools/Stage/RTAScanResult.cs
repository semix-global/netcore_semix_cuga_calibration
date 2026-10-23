using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.DarkField;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Newtonsoft.Json;
using Complex = System.Numerics.Complex;
using PlotGrid = ScottPlot.MultiplotLayouts.Grid;
using RTAScanSample = Core.Services.Interfaces.ICalibrationAlgorithmService.RTAScanSample;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class RTARowScan : ObservableObject
{
    public int Row { get; set; }

    public bool IsForward { get; set; }

    public Point StartPosition { get; set; }

    public Point StopPosition { get; set; }

    /// <summary>
    /// 采集时的数值采样率, 单位 kHz. 独立保存以免回看时使用新配置.
    /// </summary>
    public double SampleRate { get; set; }

    [ObservableProperty]
    public partial DarkFieldRawScanImageDTO? RawScanImage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnalysisSuccessful), nameof(FFTStatus))]
    public partial RTAScanSample[] Samples { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnalysisSuccessful))]
    public partial string AnalysisError { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsAnalysisSuccessful => Samples.Length > 0 && string.IsNullOrEmpty(AnalysisError);

    [JsonIgnore]
    public string FFTStatus => SampleRate is > 0d and <= double.MaxValue && GetFFTInput().Length >= 4
        ? "Available"
        : "Unavailable: invalid sample rate or fewer than four consecutive valid samples";

    public Point[] GetPositionPoints(bool isX)
    {
        if ((SampleRate is > 0d and <= double.MaxValue) == false) return [];

        return [.. GetValidSamples().Select(t => new Point(t.XPixel / SampleRate, isX ? t.XPixel : t.YPixel))];
    }

    public RTAScanSample[] GetFFTInput()
    {
        var samples = GetValidSamples();
        var bestStart = 0;
        var bestCount = 0;
        var start = 0;

        for (var index = 0; index < samples.Length; index++)
        {
            if (index > 0 && (Math.Abs((long)samples[index].GridRowIndex - samples[index - 1].GridRowIndex) != 1
                              || samples[index].XPixel <= samples[index - 1].XPixel)) start = index;

            var count = index - start + 1;
            if (count <= bestCount) continue;

            bestStart = start;
            bestCount = count;
        }

        return samples.Skip(bestStart).Take(bestCount).ToArray();
    }

    public Point[] GetFrequencyPoints(bool isX)
    {
        if ((SampleRate is > 0d and <= double.MaxValue) == false) return [];

        var samples = GetFFTInput();

        if (samples.Length < 4) return [];

        var pixelInterval = (samples[^1].XPixel - samples[0].XPixel) / (samples.Length - 1);

        if ((pixelInterval is > 0d and <= double.MaxValue) == false) return [];

        // 保留原始均值直接 FFT, 使用连续段的平均格距近似等效时间间隔.
        var effectiveSampleRate = SampleRate * 1000d / pixelInterval;
        var vector = Vector<Complex>.Build.DenseOfEnumerable(samples.Select(t => new Complex(isX ? t.XPixel : t.YPixel, 0d)));
        var (frequencies, magnitudes) = vector.FastFourierTransform().GetPositiveFrequencies(effectiveSampleRate);

        return [.. frequencies.Zip(magnitudes, (frequency, magnitude) => new Point(frequency, magnitude))];
    }

    private RTAScanSample[] GetValidSamples() => [.. Samples
        .Where(t => t.ValidColumnCount > 0
                    && t.XPixel is >= double.MinValue and <= double.MaxValue
                    && t.YPixel is >= double.MinValue and <= double.MaxValue)
        .OrderBy(t => t.XPixel)];
}

public sealed partial class RTAScanResult : ObservableObject
{
    [ObservableProperty]
    public partial RTARowScan[] RowScans { get; set; } = [];

    [JsonIgnore]
    public IPlotDataSource PlotDataSource { get; } = new PlotDataSource();

    public RTAScanResult()
    {
        PlotDataSource.Configure(new PlotGrid(rows: 4, columns: 2), 8);

        foreach (var (rowIndex, title) in new[] { "Forward X", "Forward Y", "Reverse X", "Reverse Y" }.Index())
        {
            PlotDataSource.SetTitle(rowIndex * 2, $"{title} Mean Position (Y: px - X: equivalent time, ms)");
            PlotDataSource.SetTitle(rowIndex * 2 + 1, $"{title} FFT (Y: FFT Magnitude - X: Hz)");
        }
    }

    partial void OnRowScansChanged(RTARowScan[] value) => Refresh();

    public void Refresh()
    {
        try
        {
            for (var plotIndex = 0; plotIndex < 8; plotIndex++) PlotDataSource.Clear(plotIndex);

            foreach (var rowScan in RowScans)
            {
                var firstPlotIndex = rowScan.IsForward ? 0 : 4;
                var title = $"Row {rowScan.Row}";

                for (var axisIndex = 0; axisIndex < 2; axisIndex++)
                {
                    var isX = axisIndex == 0;
                    var plotIndex = firstPlotIndex + axisIndex * 2;
                    var positions = rowScan.GetPositionPoints(isX);
                    if (positions.Length > 0) PlotDataSource.GetOrAddScatterLine(plotIndex, title, positions);

                    var frequencies = rowScan.GetFrequencyPoints(isX);
                    if (frequencies.Length > 0) PlotDataSource.GetOrAddScatterLine(plotIndex + 1, title, frequencies);
                }
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }
}