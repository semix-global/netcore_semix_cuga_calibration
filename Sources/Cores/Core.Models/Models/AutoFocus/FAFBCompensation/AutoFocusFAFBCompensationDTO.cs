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
    public partial double KB { get; set; }

    [ObservableProperty]
    public partial double OffsetB { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AutoFocusFAFBCompensationDTOItem> CalibratingItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<AutoFocusFAFBCompensationDTOItem> VerifyItems { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource CalibratingPlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource VerifyPlotDataSource { get; set; } = new PlotDataSource();

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnCalibratingItemsChanged(IReadOnlyList<AutoFocusFAFBCompensationDTOItem> value) => RefreshPlot(CalibratingPlotDataSource, CalibratingItems);

    partial void OnVerifyItemsChanged(IReadOnlyList<AutoFocusFAFBCompensationDTOItem> value) => RefreshPlot(VerifyPlotDataSource, VerifyItems);

    // ReSharper restore UnusedParameterInPartialMethod

    public AutoFocusFAFBCompensationDTO()
    {
        Configure(CalibratingPlotDataSource);
        Configure(VerifyPlotDataSource);

        return;

        static void Configure(IPlotDataSource plotDataSource)
        {
            plotDataSource.Configure(new Columns(), 2);

            plotDataSource.SetTitle(0, "F N (Y: None - X: ECS)");
            plotDataSource.SetTitle(1, "F N (Y: None - X: ms)");
        }
    }

    private static void RefreshPlot(IPlotDataSource plotDataSource, IReadOnlyList<AutoFocusFAFBCompensationDTOItem> items)
    {
        try
        {
            var scatterLines0 = plotDataSource.GetOrAddScatterLines(0, items.Count * 5);
            var scatterLines1 = plotDataSource.GetOrAddScatterLines(1, items.Count * 6);
            var xLines = plotDataSource.GetOrAddXLines(0, items.Count);
            var yLines = plotDataSource.GetOrAddXLines(0, items.Count);

            foreach (var (index, item) in items.Index())
            {
                scatterLines0.ElementAtOrDefault(index * 5 + 0)?.Update($"{item.DSWFindBrightMachinePosition}: FA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FAs[t.Index]))], Constants.Category10.GetColor(0));
                scatterLines0.ElementAtOrDefault(index * 5 + 1)?.Update("NA", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NAs[t.Index]))], Constants.Category10.GetColor(1));
                scatterLines0.ElementAtOrDefault(index * 5 + 2)?.Update("FB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.FBs[t.Index]))], Constants.Category10.GetColor(2));
                scatterLines0.ElementAtOrDefault(index * 5 + 3)?.Update("NB", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NBs[t.Index]))], Constants.Category10.GetColor(3));
                scatterLines0.ElementAtOrDefault(index * 5 + 4)?.Update("NSC", [.. item.ECSes.Index().Select(t => new Point(t.Item, item.NSCs[t.Index]))], Constants.Category10.GetColor(4));

                scatterLines1.ElementAtOrDefault(index * 6 + 0)?.Update("ECS", [.. item.ECSes.Index().Select(t => new Point(t.Index, t.Item))], Constants.Category10.GetColor(0));
                scatterLines1.ElementAtOrDefault(index * 6 + 1)?.Update("FA", [.. item.FAs.Index().Select(t => new Point(t.Index, t.Item))], Constants.Category10.GetColor(1));
                scatterLines1.ElementAtOrDefault(index * 6 + 2)?.Update("NA", [.. item.NAs.Index().Select(t => new Point(t.Index, t.Item))], Constants.Category10.GetColor(2));
                scatterLines1.ElementAtOrDefault(index * 6 + 3)?.Update("FB", [.. item.FBs.Index().Select(t => new Point(t.Index, t.Item))], Constants.Category10.GetColor(3));
                scatterLines1.ElementAtOrDefault(index * 6 + 4)?.Update("NB", [.. item.NBs.Index().Select(t => new Point(t.Index, t.Item))], Constants.Category10.GetColor(4));
                scatterLines1.ElementAtOrDefault(index * 6 + 5)?.Update("NSC", [.. item.NSCs.Index().Select(t => new Point(t.Index, t.Item))], Constants.Category10.GetColor(5));

                xLines.ElementAtOrDefault(index)?.Update("Zero ECS", item.NSCZeroPoint?.X ?? 0d, Constants.Turbo.GetColor(index, new Range(0, items.Count)));
                xLines.ElementAtOrDefault(index)?.LinePattern = LinePattern.Dashed;
                yLines.ElementAtOrDefault(index)?.Update("Zero NSC", item.NSCZeroPoint?.Y ?? 0d, Constants.Turbo.GetColor(index, new Range(0, items.Count)));
                yLines.ElementAtOrDefault(index)?.LinePattern = LinePattern.Dashed;
            }
        }
        finally
        {
            plotDataSource.AutoScaleRefresh();
        }
    }

    public override AutoFocusFAFBCompensationDTO Clone() => new()
    {
        KA = KA,
        OffsetA = OffsetA,
        KB = KB,
        OffsetB = OffsetB,
        CalibratingItems = [.. CalibratingItems.Select(t => t.Clone())],
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
    public partial Point DSWFindBrightMachinePosition { get; set; }

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
    public partial Point? NSCZeroPoint { get; set; }

    public AutoFocusFAFBCompensationDTOItem Clone() => new()
    {
        DSWFindBrightMachinePosition = DSWFindBrightMachinePosition,
        ECSes = [.. ECSes],
        FAs = [.. FAs],
        NAs = [.. NAs],
        FBs = [.. FBs],
        NBs = [.. NBs],
        NSCs = [.. NSCs],
        NSCZeroPoint = NSCZeroPoint
    };
}