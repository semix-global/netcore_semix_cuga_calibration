using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Humanizer;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyUniformity<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    public string Title => string.Join(", ", Electrodes.Select(t => t.Humanize()));

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>> _items = [];

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

    partial void OnItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    public AODWaveformElectrodeOffsetFrequencyUniformity()
    {
        ScatterPlotControl.Configure(totalPlotCount: 3);
        ScatterPlotControl.SetTitle(0, "Uniformity Items(Y: mW - X: AMP)");
        ScatterPlotControl.SetTitle(1, "Uniformity Amplitude Result(Y: AMP - X: MHz)");
        ScatterPlotControl.SetTitle(2, "Uniformity Measure Power Result(Y: mW - X: MHz)");
    }

    private void RefreshPlot()
    {
        var isNeedRefreshes = new bool[Items.Count];
        foreach (var (index, item) in Items.Index())
        {
            if (item.FrequencyItems.Count <= 0) continue;

            ScatterPlotControl.GetOrAddScatterLine(
                0,
                $"{item.FrequencyItems[0].Frequency}(MHz)",
                [.. item.FrequencyItems.Select(t => new Point(t.Amplitude, t.MeasurePower))],
                index,
                new Range(0, Items.Count - 1));

            item.MaxItem = item.FrequencyItems.Maxima(t => t.MeasurePower).First();

            isNeedRefreshes[index] = true;
        }

        if (isNeedRefreshes.All(b => b))
        {
            ScatterPlotControl.GetOrAddScatterLine(
                1,
                "Amplitude",
                [.. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, GuardUtils.IsNotNullAndReturn(t.MaxItem).Amplitude))],
                Colors.Blue);
            ScatterPlotControl.GetOrAddScatterLine(
                2,
                "Measure Power",
                [.. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, GuardUtils.IsNotNullAndReturn(t.MaxItem).MeasurePower))],
                Colors.Blue);
        }

        ScatterPlotControl.AutoScaleRefresh();
    }
}