using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
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
    public partial IReadOnlyList<CIBMMDDTOItem> SelectItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBMMDDTOItem> NewItems { get; set; } = [];

    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    partial void OnSelectItemsChanged(IReadOnlyList<CIBMMDDTOItem>? oldValue, IReadOnlyList<CIBMMDDTOItem> newValue)
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
        ScatterPlotControl.SetTitle("Origin(Y: PMT Value(DC) - X: V)");
    }

    private void RefreshPlot()
    {
        try
        {
            var tempSelectItems = (from item in SelectItems
                    let itemItems = item.Items.Where(t => double.IsNaN(t.PMTValue) == false).ToArray()
                    where itemItems.Length > 0
                    select new
                    {
                        LegendText = $"{item.Coefficient:0.###}",
                        Points = itemItems.Select(t => new Point(t.Gain, t.PMTValue)).ToArray()
                    }
                ).ToArray();
            var tempNewItems = (from item in NewItems
                    let itemItems = item.Items.Where(t => double.IsNaN(t.PMTValue) == false).ToArray()
                    where itemItems.Length > 0
                    select new
                    {
                        LegendText = $"{item.Coefficient:0.###} - Aging",
                        Points = itemItems.Select(t => new Point(t.Gain, t.PMTValue)).ToArray()
                    }
                ).ToArray();

            var scatterLines = ScatterPlotControl.GetOrAddScatterLines(tempSelectItems.Length + tempNewItems.Length);

            foreach (var (index, temp) in tempSelectItems.Index())
            {
                scatterLines[index].Update(temp.LegendText, temp.Points, Constants.Category10.GetColor(index));
            }

            foreach (var (index, temp) in tempNewItems.Index())
            {
                scatterLines[tempSelectItems.Length + index].Update(temp.LegendText, temp.Points, Constants.Category10.GetColor(index).Lighten(0.7));
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