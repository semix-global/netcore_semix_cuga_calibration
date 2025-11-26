using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace Core.Models.Models.CIB.MMD;

public sealed partial class CIBMMDDto : CalibrationDtoBase, ICloneable<CIBMMDDto>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<CIBMMDItemDto> _items = [];

    [ObservableProperty]
    private double _gainResidual;

    [ObservableProperty]
    private double _gainNorm;

    [ObservableProperty]
    private IReadOnlyList<Point> _gainPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _logGainPoints = [];

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


    public CIBMMDDto()
    {
        var customGrid = new CustomGrid();
        ScatterPlotControl.Configure(customGrid, 6,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 3, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 3, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 3, 2));
                customGrid.Set(plots[3], new GridCell(1, 1, 3, 2));
                customGrid.Set(plots[4], new GridCell(2, 0, 3, 2));
                customGrid.Set(plots[5], new GridCell(2, 1, 3, 2));
            });

        ScatterPlotControl.SetTitle(0, "Origin(Y: PMTValue - X: V)");
        ScatterPlotControl.SetTitle(1, "Origin(Y: mW - X: Coefficient)");
        ScatterPlotControl.SetTitle(2, "Gain(Y: Gain - X: V)");
        ScatterPlotControl.SetTitle(3, "LogGain(Y: Log Gain - X: V)");
        ScatterPlotControl.SetTitle(4, "Gain(Y:  - X: )");
        ScatterPlotControl.SetTitle(5, "LogGain(Y:  - X: )");
    }

    public CIBMMDDto Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Items = [..Items.Select(t => t.Clone())],
        GainResidual = GainResidual,
        GainNorm = GainNorm,
        GainPoints = [..GainPoints],
        LogGainPoints = [..LogGainPoints]
    };

    public void RefreshPlot()
    {
        var items = Items.Where(t => double.IsNaN(t.MeasurePower) == false).ToArray();
        if (items.Length > 0)
        {
            ScatterPlotControl.GetOrAddScatterLine(
                0,
                "Attenuator",
                [.. items.Select(t => new Point(t.Coefficient, t.MeasurePower))]);

            foreach (var item in items)
            {
                var itemItems = item.Items.Where(t => double.IsNaN(t.PMTValue) == false).ToArray();
                if (itemItems.Length > 0)
                    ScatterPlotControl.GetOrAddScatterLine(
                        1,
                        $"{item.Coefficient}",
                        [.. itemItems.Select(t => new Point(t.Gain, t.PMTValue))]);
            }
        }

        if (GainPoints.Count > 0)
            ScatterPlotControl.GetOrAddScatterLine(
                2,
                $"Gain Residual: {GainResidual:0.000#} Gain Norm: {GainNorm:0.###}",
                GainPoints);

        if (LogGainPoints.Count > 0)
            ScatterPlotControl.GetOrAddScatterLine(
                3,
                $"Residual: {GainResidual:0.000#} GainNorm: {GainNorm:0.###}",
                LogGainPoints);

        ScatterPlotControl.AutoScaleRefresh();
    }
}

public sealed partial class CIBMMDItemDto : CalibrationCacheBase, ICloneable<CIBMMDItemDto>
{
    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private double _measurePower;

    [ObservableProperty]
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private double _protectedCount;

    public CIBMMDItemDto Clone() => new()
    {
        Coefficient = Coefficient,
        MeasurePower = MeasurePower,
        Items = [..Items.Select(t => t.Clone())]
    };

    public sealed class Item : ICloneable<Item>
    {
        public double Gain { get; init; }

        public double PMTValue { get; set; }

        public Item Clone() => new()
        {
            Gain = Gain,
            PMTValue = PMTValue
        };
    }
}