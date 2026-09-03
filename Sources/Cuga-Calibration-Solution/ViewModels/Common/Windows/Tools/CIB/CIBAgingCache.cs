using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.CIB.MMD;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.CIB;

public sealed partial class CIBAgingCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial CIBMMDCache CIBMMDCache { get; set; } = new();

    [ObservableProperty]
    public partial double CoefficientStep { get; set; } = 0.1;

    [ObservableProperty]
    public partial int FindCoefficientRetryTimes { get; set; } = 7;

    [ObservableProperty]
    public partial double MeasurePowerRatioThreshold { get; set; } = 0.1;

    [ObservableProperty]
    public partial double AgingPMTValueNoises { get; set; } = 20;

    [ObservableProperty]
    public partial int AgingSampleCount { get; set; } = 10;

    [ObservableProperty]
    public partial double AgingRatioThreshold { get; set; } = 0.1;

    [ObservableProperty]
    public partial IReadOnlyList<CIBAgingSelectItem> Agings { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBAgingSelectItem> SelectedAgings { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBAgingCoefficientFindItem> CoefficientFindItems { get; set; } = [];
}

public sealed record CIBAgingSelectItem(double Coefficient, double MeasurePower)
{
    public override string ToString() => $"{Coefficient:0.###}: {MeasurePower:0.###}mW";
}

public sealed partial class CIBAgingCoefficientFindItem : ObservableObject
{
    [ObservableProperty]
    public partial CIBAgingSelectItem SelectItem { get; set; } = new(0, 0);

    [ObservableProperty]
    public partial IReadOnlyList<Point> FindMeasurePowerPoints { get; set; } = [];

    [ObservableProperty]
    public partial double TargetMeasurePower { get; set; }

    [ObservableProperty]
    public partial double UpperMeasurePower { get; set; }

    [ObservableProperty]
    public partial double LowerMeasurePower { get; set; }

    [ObservableProperty]
    public partial Point? AnswerMeasurePowerPoint { get; set; }

    [ObservableProperty]
    public partial double? AnswerMeasurePowerRatio { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnFindMeasurePowerPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnTargetMeasurePowerChanged(double value) => RefreshPlot();

    partial void OnUpperMeasurePowerChanged(double value) => RefreshPlot();

    partial void OnLowerMeasurePowerChanged(double value) => RefreshPlot();

    partial void OnAnswerMeasurePowerPointChanged(Point? value) => RefreshPlot();

    partial void OnAnswerMeasurePowerRatioChanged(double? value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBAgingCoefficientFindItem()
    {
        PlotDataSource.SetTitle("Coefficient Find(Y: mW - X: Coefficient)");
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterLines = PlotDataSource.GetOrAddScatterLines(FindMeasurePowerPoints.Count > 0 ? 1 : 0);
            scatterLines.ElementAtOrDefault(0)?.Update("Search Measure Power Points", FindMeasurePowerPoints, Constants.Category10.GetColor(0));

            var yLines = PlotDataSource.GetOrAddYLines(3 + (AnswerMeasurePowerPoint is not null ? 1 : 0));
            yLines[0].Update("Target Measure Power", TargetMeasurePower, Colors.DarkRed);
            yLines[1].Update("Upper Measure Power", UpperMeasurePower, Colors.OrangeRed);
            yLines[1].LinePattern = LinePattern.Dashed;
            yLines[2].Update("Lower Measure Power", LowerMeasurePower, Colors.OrangeRed);
            yLines[2].LinePattern = LinePattern.Dashed;
            yLines.ElementAtOrDefault(3)?.Update($"Answer Measure Power{(AnswerMeasurePowerRatio is not null ? $": {AnswerMeasurePowerRatio:0.###}" : string.Empty)}", Guard.IsNotNullAndReturn(AnswerMeasurePowerPoint).Y, Colors.Green);

            var xLines = PlotDataSource.GetOrAddXLines(AnswerMeasurePowerPoint is not null ? 1 : 0);
            xLines.ElementAtOrDefault(0)?.Update($"Answer Coefficient{(AnswerMeasurePowerRatio is not null ? $": {AnswerMeasurePowerRatio:0.###}" : string.Empty)}", Guard.IsNotNullAndReturn(AnswerMeasurePowerPoint).X, Colors.Green);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }
}