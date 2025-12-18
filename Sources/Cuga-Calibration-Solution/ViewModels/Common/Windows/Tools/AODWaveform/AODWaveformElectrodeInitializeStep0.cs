using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeInitializeStep0<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeInitializeStep0Item<TItem>> _items = [];

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

    partial void OnItemsChanged(IReadOnlyList<AODWaveformElectrodeInitializeStep0Item<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeInitializeStep0Item<TItem>> newValue)
    {
        foreach (var step0Item in oldValue ?? []) step0Item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var step0Item in newValue)
        {
            step0Item.PropertyChanged -= ItemOnPropertyChanged;
            step0Item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    public AODWaveformElectrodeInitializeStep0()
    {
        ScatterPlotControl.SetTitle("Offset Frequency Period(Y: mW - X: 2pi)");
    }

    private void RefreshPlot()
    {
        ScatterPlotControl.Clear();

        foreach (var (index, item) in Items.Index())
        {
            if (item.Items.Count <= 0) continue;

            ScatterPlotControl.GetOrAddScatterLine(
                item.Title,
                [.. item.Items.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                index,
                new Range(0, Items.Count - 1));
        }

        ScatterPlotControl.AutoScaleRefresh();
    }
}