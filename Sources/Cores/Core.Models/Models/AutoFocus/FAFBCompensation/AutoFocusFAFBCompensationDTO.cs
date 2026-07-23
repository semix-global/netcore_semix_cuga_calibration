using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AutoFocus.FAFBCompensation;

[CacheVersion("1.0.0")]
public sealed partial class AutoFocusFAFBCompensationDTO : CalibrationDTOBase<AutoFocusFAFBCompensationDTO>
{
    [ObservableProperty]
    public partial double KA { get; set; }

    [ObservableProperty]
    public partial double OffsetA { get; set; }

    [ObservableProperty]
    public partial double FARSquared { get; set; }

    [ObservableProperty]
    public partial double KB { get; set; }

    [ObservableProperty]
    public partial double OffsetB { get; set; }

    [ObservableProperty]
    public partial double FBRSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AutoFocusFAFBCompensationDTOItem> CalibratingItems { get; set; } = [];

    [ObservableProperty]
    public partial double? LeastSquaresMinECS { get; set; }

    [ObservableProperty]
    public partial double? LeastSquaresMaxECS { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> LeastSquareFindPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<AutoFocusFAFBCompensationDTOItem> VerifyItems { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource CalibratingPlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource VerifyPlotDataSource { get; set; } = new PlotDataSource();

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnCalibratingItemsChanged(IReadOnlyList<AutoFocusFAFBCompensationDTOItem> oldValue, IReadOnlyList<AutoFocusFAFBCompensationDTOItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshCalibratingPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshCalibratingPlot();
    }

    partial void OnLeastSquaresMinECSChanged(double? value) => RefreshCalibratingPlot();

    partial void OnLeastSquaresMaxECSChanged(double? value) => RefreshCalibratingPlot();

    partial void OnLeastSquareFindPointsChanged(IReadOnlyList<Point> value) => RefreshCalibratingPlot();

    partial void OnVerifyItemsChanged(IReadOnlyList<AutoFocusFAFBCompensationDTOItem> oldValue, IReadOnlyList<AutoFocusFAFBCompensationDTOItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshVerifyPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshVerifyPlot();
    }

    // ReSharper restore UnusedParameterInPartialMethod

    public AutoFocusFAFBCompensationDTO()
    {
        var customGrid = new CustomGrid();
        CalibratingPlotDataSource.Configure(customGrid, 3,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 2, colSpan: 2));
            });

        CalibratingPlotDataSource.SetTitle(0, "F N (Y: None - X: ECS)");
        CalibratingPlotDataSource.SetTitle(1, "Y: ECS - X: ms");
        CalibratingPlotDataSource.SetTitle(2, "F N Compensation (Y: None - X: ECS)");
        CalibratingPlotDataSource.ToggleLegend(0, false);
        CalibratingPlotDataSource.ToggleLegend(1, false);
        CalibratingPlotDataSource.ToggleLegend(2, false);

        VerifyPlotDataSource.Configure(new Columns(), 2);
        VerifyPlotDataSource.SetTitle(0, "F N (Y: None - X: ECS)");
        VerifyPlotDataSource.SetTitle(1, "Y: ECS - X: ms");
        VerifyPlotDataSource.ToggleLegend(0, false);
        VerifyPlotDataSource.ToggleLegend(1, false);
    }

    private void RefreshCalibratingPlot()
    {
        try
        {
            var compensations = CalibratingItems
                .Index()
                .Where(t => t.Item.ECSes.Count == t.Item.FAPerNACompensations.Count
                            && t.Item.ECSes.Count == t.Item.FBPerNBCompensations.Count
                            && t.Item.ECSes.Count == t.Item.NSCCompensations.Count
                            && t.Item.ECSes.Count > 0)
                .ToArray();

            var originScatterLines0 = CalibratingPlotDataSource.GetOrAddScatterLines(0, CalibratingItems.Count * 7);
            var originScatterLines1 = CalibratingPlotDataSource.GetOrAddScatterLines(1, CalibratingItems.Count);
            var compensationScatterLines = CalibratingPlotDataSource.GetOrAddScatterLines(2, compensations.Length * 3);

            foreach (var (index, item) in CalibratingItems.Index())
            {
                originScatterLines0.ElementAtOrDefault(index * 7 + 0)?.Update($"{item.FindBrightMachinePosition}: FA / NA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAs[t.Index] / item.NAs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines0.ElementAtOrDefault(index * 7 + 0)?.MarkerStyle.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 1)?.Update($"{item.FindBrightMachinePosition}: FB / NB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FBs[t.Index] / item.NBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines0.ElementAtOrDefault(index * 7 + 1)?.MarkerStyle.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 2)?.Update($"{item.FindBrightMachinePosition}: NSC", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAs[t.Index] / item.NAs[t.Index] - item.FBs[t.Index] / item.NBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines0.ElementAtOrDefault(index * 7 + 2)?.MarkerStyle.IsVisible = false;

                originScatterLines0.ElementAtOrDefault(index * 7 + 3)?.Update($"{item.FindBrightMachinePosition}: FA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines0.ElementAtOrDefault(index * 7 + 3)?.MarkerStyle.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 3)?.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 4)?.Update($"{item.FindBrightMachinePosition}: NA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NAs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines0.ElementAtOrDefault(index * 7 + 4)?.MarkerStyle.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 4)?.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 5)?.Update($"{item.FindBrightMachinePosition}: FB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines0.ElementAtOrDefault(index * 7 + 5)?.MarkerStyle.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 5)?.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 6)?.Update($"{item.FindBrightMachinePosition}: NB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines0.ElementAtOrDefault(index * 7 + 6)?.MarkerStyle.IsVisible = false;
                originScatterLines0.ElementAtOrDefault(index * 7 + 6)?.IsVisible = false;

                originScatterLines1.ElementAtOrDefault(index)?.Update($"{item.FindBrightMachinePosition}: ECS", [.. item.ECSes.Index().Select(t => new Point(t.Index, t.Item))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                originScatterLines1.ElementAtOrDefault(index)?.MarkerStyle.IsVisible = false;
            }

            foreach (var (index, item) in compensations)
            {
                compensationScatterLines.ElementAtOrDefault(index * 3 + 0)?.Update($"{item.FindBrightMachinePosition}: FA / NA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAPerNACompensations[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                compensationScatterLines.ElementAtOrDefault(index * 3 + 0)?.MarkerStyle.IsVisible = false;
                compensationScatterLines.ElementAtOrDefault(index * 3 + 1)?.Update($"{item.FindBrightMachinePosition}: FB / NB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FBPerNBCompensations[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                compensationScatterLines.ElementAtOrDefault(index * 3 + 1)?.MarkerStyle.IsVisible = false;
                compensationScatterLines.ElementAtOrDefault(index * 3 + 2)?.Update($"{item.FindBrightMachinePosition}: NSC", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NSCCompensations[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, CalibratingItems.Count)));
                compensationScatterLines.ElementAtOrDefault(index * 3 + 2)?.MarkerStyle.IsVisible = false;
            }

            var originXLines = CalibratingPlotDataSource.GetOrAddXLines(0, (LeastSquaresMinECS is not null ? 1 : 0) + (LeastSquaresMaxECS is not null ? 1 : 0));
            originXLines.ElementAtOrDefault(0)?.Update("Min", LeastSquaresMinECS ?? 0d, Colors.DarkRed);
            originXLines.ElementAtOrDefault(0)?.LinePattern = LinePattern.Solid;
            originXLines.ElementAtOrDefault(1)?.Update("Max", LeastSquaresMaxECS ?? 0d, Colors.DarkRed);
            originXLines.ElementAtOrDefault(1)?.LinePattern = LinePattern.Solid;

            var originScatterMarkerses = CalibratingPlotDataSource.GetOrAddScatterMarkerses(2, LeastSquareFindPoints.Count > 0 ? 1 : 0);
            originScatterMarkerses.ElementAtOrDefault(0)?.Update("Least Square Find Points", [.. LeastSquareFindPoints], Colors.Green, MarkerShape.Asterisk);
            originScatterMarkerses.ElementAtOrDefault(0)?.MarkerSize = 20f;
        }
        finally
        {
            CalibratingPlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshVerifyPlot()
    {
        try
        {
            var scatterLines0 = VerifyPlotDataSource.GetOrAddScatterLines(0, VerifyItems.Count * 7);
            var scatterLines1 = VerifyPlotDataSource.GetOrAddScatterLines(1, VerifyItems.Count);
            var xLines = VerifyPlotDataSource.GetOrAddXLines(VerifyItems.Count);
            var yLines = VerifyPlotDataSource.GetOrAddYLines(VerifyItems.Count);

            foreach (var (index, item) in VerifyItems.Index())
            {
                scatterLines0.ElementAtOrDefault(index * 7 + 0)?.Update($"{item.FindBrightMachinePosition}: FA / NA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAs[t.Index] / item.NAs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines0.ElementAtOrDefault(index * 7 + 0)?.MarkerStyle.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 1)?.Update($"{item.FindBrightMachinePosition}: FB / NB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FBs[t.Index] / item.NBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines0.ElementAtOrDefault(index * 7 + 1)?.MarkerStyle.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 2)?.Update($"{item.FindBrightMachinePosition}: NSC", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAs[t.Index] / item.NAs[t.Index] - item.FBs[t.Index] / item.NBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines0.ElementAtOrDefault(index * 7 + 2)?.MarkerStyle.IsVisible = false;

                scatterLines0.ElementAtOrDefault(index * 7 + 3)?.Update($"{item.FindBrightMachinePosition}: FA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines0.ElementAtOrDefault(index * 7 + 3)?.MarkerStyle.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 3)?.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 4)?.Update($"{item.FindBrightMachinePosition}: NA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NAs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines0.ElementAtOrDefault(index * 7 + 4)?.MarkerStyle.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 4)?.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 5)?.Update($"{item.FindBrightMachinePosition}: FB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines0.ElementAtOrDefault(index * 7 + 5)?.MarkerStyle.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 5)?.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 6)?.Update($"{item.FindBrightMachinePosition}: NB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NBs[t.Index]))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines0.ElementAtOrDefault(index * 7 + 6)?.MarkerStyle.IsVisible = false;
                scatterLines0.ElementAtOrDefault(index * 7 + 6)?.IsVisible = false;

                scatterLines1.ElementAtOrDefault(index)?.Update($"{item.FindBrightMachinePosition}: ECS", [.. item.ECSes.Index().Select(t => new Point(t.Index, t.Item))], Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                scatterLines1.ElementAtOrDefault(index)?.MarkerStyle.IsVisible = false;

                xLines.ElementAtOrDefault(index)?.Update($"{item.FindBrightMachinePosition}: Zero ECS", item.NSCZeroPoint?.X ?? 0d, Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                xLines.ElementAtOrDefault(index)?.LinePattern = LinePattern.Dashed;
                yLines.ElementAtOrDefault(index)?.Update($"{item.FindBrightMachinePosition}: Zero NSC", item.NSCZeroPoint?.Y ?? 0d, Constants.Turbo.GetColor(index, new Range(0, VerifyItems.Count)));
                yLines.ElementAtOrDefault(index)?.LinePattern = LinePattern.Dashed;
            }
        }
        finally
        {
            VerifyPlotDataSource.AutoScaleRefresh();
        }
    }

    public override AutoFocusFAFBCompensationDTO Clone() => new()
    {
        KA = KA,
        OffsetA = OffsetA,
        FARSquared = FARSquared,
        KB = KB,
        OffsetB = OffsetB,
        FBRSquared = FBRSquared,
        CalibratingItems = [.. CalibratingItems.Select(t => t.Clone())],
        LeastSquaresMinECS = LeastSquaresMinECS,
        LeastSquaresMaxECS = LeastSquaresMaxECS,
        LeastSquareFindPoints = [.. LeastSquareFindPoints],
        VerifyItems = [.. VerifyItems.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class AutoFocusFAFBCompensationDTOItem : ObservableObject, ICloneable<AutoFocusFAFBCompensationDTOItem>
{
    [ObservableProperty]
    public partial Point FindBrightMachinePosition { get; set; }

    [ObservableProperty]
    public partial double? AverageECS { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<double> ECSes { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> FAs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> NAs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> FBs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> NBs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> NSCs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> FAPerNACompensations { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> FBPerNBCompensations { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> NSCCompensations { get; set; } = [];

    [ObservableProperty]
    public partial Point? NSCZeroPoint { get; set; }

    public AutoFocusFAFBCompensationDTOItem Clone() => new()
    {
        FindBrightMachinePosition = FindBrightMachinePosition,
        AverageECS = AverageECS,
        ECSes = [.. ECSes],
        FAs = [.. FAs],
        NAs = [.. NAs],
        FBs = [.. FBs],
        NBs = [.. NBs],
        NSCs = [.. NSCs],
        FAPerNACompensations = [.. FAPerNACompensations],
        FBPerNBCompensations = [.. FBPerNBCompensations],
        NSCCompensations = [.. NSCCompensations],
        NSCZeroPoint = NSCZeroPoint
    };
}