using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetStep1<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    public string Title => string.Join(", ", Electrodes.Select(t => EnumHelper.ToDescriptionString(t)));

    #region Result

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>> _items = [];

    partial void OnItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetStep1Item<TItem>> newValue)
    {
        foreach (var step1Item in oldValue ?? [])
        {
            step1Item.PropertyChanged -= ItemOnPropertyChanged;
        }

        foreach (var step1Item in newValue)
        {
            step1Item.PropertyChanged -= ItemOnPropertyChanged;
            step1Item.PropertyChanged += ItemOnPropertyChanged;
        }

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AODWaveformElectrodeOffsetStep1Item<TItem>.MaxItem)) return;

            OnPropertyChanged(nameof(Items));
        }
    }

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

    #endregion

    public AODWaveformElectrodeOffsetStep1()
    {
        ScatterPlotControl.Configure(totalPlotCount: 3);
        ScatterPlotControl.SetTitle(0, "Uniformity Items(Y: mW - X: AMP)");
        ScatterPlotControl.SetTitle(1, "Uniformity Amplitude Result(Y: AMP - X: MHz)");
        ScatterPlotControl.SetTitle(2, "Uniformity Measure Power Result(Y: mW - X: MHz)");
    }

    public void RefreshPlot()
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

            item.MaxItem = item.FrequencyItems.Maxima(t => t.MeasurePower).Single();

            isNeedRefreshes[index] = true;
        }

        if (isNeedRefreshes.All(b => b))
        {
            ScatterPlotControl.GetOrAddScatterLine(
                1,
                "Amplitude",
                [
                    .. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, t.MaxItem?.Amplitude ?? 0))
                ],
                Colors.Blue);
            ScatterPlotControl.GetOrAddScatterLine(
                2,
                "Measure Power",
                [
                    .. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, t.MaxItem?.MeasurePower ?? 0))
                ],
                Colors.Blue);
        }

        ScatterPlotControl.AutoScaleRefresh();
    }
}