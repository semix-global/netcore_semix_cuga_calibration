using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using System.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.CIB;

public sealed partial class CIBAgingResult : ObservableCacheBase, ICloneable<CIBAgingResult>
{
    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial bool IsModify { get; set; }

    public IReadOnlyList<CIBAgingItem> Items { get; set; } = [];

    public CIBAgingResult Clone() => new()
    {
        Items = [.. Items.Select(t => t.Clone())]
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
    public partial IReadOnlyList<CIBAgingSampleItem> SampleItems { get; set; } = [];

    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

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

    partial void OnSampleItemsChanged(IReadOnlyList<CIBAgingSampleItem>? oldValue, IReadOnlyList<CIBAgingSampleItem> newValue)
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
        PlotDataSource.SetTitle("Origin(Y: PMT Value(DC) - X: V)");
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
                                       Item = new CIBAgingSelectItem(item.Coefficient, item.MeasurePower),
                                       Points = itemItems.Select(t => new Point(t.Gain, t.PMTValue)).ToArray()
                                   }
                ).ToArray();

            var scatterLines = PlotDataSource.GetOrAddScatterLines(tempSelectItems.Length * 2);
            var xLines = PlotDataSource.GetOrAddXLines(SampleItems.Count > 0
                ? SampleItems.Select(t => t.Items.Count).Aggregate((t1, t2) => t1 + t2)
                : 0);

            var xLineIndex = 0;
            foreach (var (index, temp) in tempSelectItems.Index())
            {
                var color = Constants.Category10.GetColor(2 * index);

                scatterLines[2 * index].Update(temp.Item.ToString(), temp.Points, color.Lighten(0.6));

                var newItem = NewItems.ElementAtOrDefault(index);
                if (newItem is not null)
                {
                    var newItemPoints = newItem.Items
                        .Where(t => double.IsNaN(t.PMTValue) == false)
                        .Select(t => new Point(t.Gain, t.PMTValue))
                        .ToArray();

                    if (newItemPoints.Length > 0)
                    {
                        scatterLines[2 * index + 1].Update($"{temp.Item} - Aging: {new CIBAgingSelectItem(newItem.Coefficient, newItem.MeasurePower)}", newItemPoints, color);
                        scatterLines[2 * index + 1].IsVisible = true;
                    }
                    else scatterLines[2 * index + 1].IsVisible = false;
                }

                var sampleItem = SampleItems.SingleOrDefault(t => temp.Item == new CIBAgingSelectItem(t.Coefficient, t.MeasurePower));
                if (sampleItem is not null)
                {
                    foreach (var item in sampleItem.Items)
                    {
                        xLines[xLineIndex].Update(string.Empty, item.Gain, color.Lighten(0.6));

                        xLineIndex++;
                    }
                }
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
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

public sealed partial class CIBAgingSampleItem : ObservableObject, ICloneable<CIBAgingSampleItem>
{
    [ObservableProperty]
    public partial double Coefficient { get; set; }

    [ObservableProperty]
    public partial double MeasurePower { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Item> Items { get; set; } = [];

    [ObservableProperty]
    public partial bool IsOk { get; set; }

    public CIBAgingSampleItem Clone() => new()
    {
        Coefficient = Coefficient,
        MeasurePower = MeasurePower,
        Items = [.. Items.Select(t => t.Clone())],
        IsOk = IsOk
    };

    public object ToHtmlAnonymous() => new
    {
        Coefficient,
        MeasurePower,
        IsOk,
        Items = new HtmlTable([
            .. Items.Select(t => new
            {
                t.IsOk,
                t.Gain,
                t.DecayRatio,
                t.OldPMTValue,
                t.NewPMTValue
            })
        ])
    };

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        public partial double Gain { get; set; }

        [ObservableProperty]
        public partial double OldPMTValue { get; set; }

        [ObservableProperty]
        public partial double NewPMTValue { get; set; }

        [ObservableProperty]
        public partial double DecayRatio { get; set; }

        [ObservableProperty]
        public partial bool IsOk { get; set; }

        public Item Clone() => new()
        {
            Gain = Gain,
            OldPMTValue = OldPMTValue,
            NewPMTValue = NewPMTValue,
            DecayRatio = DecayRatio,
            IsOk = IsOk
        };
    }
}