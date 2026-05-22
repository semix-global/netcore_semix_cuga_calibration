using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.CIB;

public sealed class CIBAgingResult : ObservableCacheBase, ICloneable<CIBAgingResult>
{
    public IReadOnlyList<CIBAgingItem> Items { get; set; } = [];

    public CIBAgingResult Clone() => new()
    {
        Items = [..Items.Select(t => t.Clone())]
    };
}

public sealed partial class CIBAgingItem : ObservableObject, ICloneable<CIBAgingItem>
{
    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<CIBMMDDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<CIBMMDDTOItem> SelectItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBMMDDTOItem> NewItems { get; set; } = [];

    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    partial void OnItemsChanged(IReadOnlyList<CIBMMDDTOItem>? oldValue, IReadOnlyList<CIBMMDDTOItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();
    }

    partial void OnNewItemsChanged(IReadOnlyList<CIBMMDDTOItem>? oldValue, IReadOnlyList<CIBMMDDTOItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();

    public CIBAgingItem()
    {
        ScatterPlotControl.SetTitle(0, "Origin(Y: mW - X: Coefficient)");
        ScatterPlotControl.SetTitle(1, "Origin(Y: PMT Value(DC) - X: V)");
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterMarkerses = ScatterPlotControl.GetOrAddScatterMarkerses(0, Items.Count > 0 ? 1 : 0);
            scatterMarkerses.ElementAtOrDefault(0)?.Update(
                string.Empty,
                [.. Items.Select(t => new Point(t.Coefficient, t.MeasurePower))],
                Constants.Category10.GetColor(0));

            var temps = (from item in Items
                    let itemItems = item.Items.Where(t => double.IsNaN(t.PMTValue) == false).ToArray()
                    where itemItems.Length > 0
                    select new
                    {
                        LegendText = $"Origin {item.Coefficient:0.###}",
                        Points = itemItems.Select(t => new Point(t.Gain, t.PMTValue)).ToArray()
                    }
                ).ToArray();

            var scatterLines = ScatterPlotControl.GetOrAddScatterLines(1, temps.Length + NewItems.Count);

            foreach (var (index, temp) in temps.Index())
            {
                scatterLines[index].Update(temp.LegendText, temp.Points, Constants.Category10.GetColor(index));
            }

            foreach (var (index, newItem) in NewItems.Index())
            {
                var newItemItems = newItem.Items.Where(t => double.IsNaN(t.PMTValue) == false).ToArray();
                if (newItemItems.Length == 0) continue;

                scatterLines[temps.Length + index]?.Update(
                    $"New {newItem.Coefficient:0.###}",
                    newItemItems.Select(t => new Point(t.Gain, t.PMTValue)).ToArray(),
                    Constants.Category10.GetColor(temps.Length + index));
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public CIBAgingItem Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        NewItems = [.. NewItems.Select(t => t.Clone())],
        IsOk = IsOk
    };
}