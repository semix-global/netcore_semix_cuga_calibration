using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Collector;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.ScottPlot.WPF.WPF;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.LightMatching;

public sealed partial class CIBLightMatchingDTO : CalibrationDtoBase, ICloneable<CIBLightMatchingDTO>, IAdaptTo<CalibrationLaserCIBLightMatchingItem>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private OpticsApodizationModeEnum _opticsApodizationModeEnum;

    [ObservableProperty]
    private OpticsPolarizationModeEnum _opticsPolarizationModeEnum;

    [ObservableProperty]
    private CollectorPolarizationModeEnum _collectorPolarizationModeEnum;

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTOItem> _items = [];

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<int, double>> _hazeTargetValues = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private ConcurrentBag<KeyValuePair<int, IScatterPlotControl>> _scatterPlotControls = [];

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<CIBLightMatchingDTOItem>? oldValue, IReadOnlyList<CIBLightMatchingDTOItem> newValue)
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

    private void RefreshPlot()
    {
        var results = (
            from item in Items
            group item by item.CIBInformation.ChannelId
            into g
            orderby g.Key
            select (
                ChannelId: g.Key,
                Items: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
            )).ToArray();

        foreach (var (channelId, items) in results)
        {
            if (HazeTargetValues.TryGetSingle(t => t.Key == channelId, out var hazeTargetValue) == false) continue;
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(channelId, GetScatterPlotControl());

            scatterPlotControl.GetOrAddYLine(0, "Haze Target", hazeTargetValue.Value, color: Colors.Red);

            try
            {
                var hazeCount = items.Max(t => t.HazeItems.Count);
                for (var i = 0; i < hazeCount; i++)
                {
                    var hazes = items.Where(t => i < t.HazeItems.Count)
                        .Select(t => (t.CIBInformation.PMTId, t.DigitalGain, Item: t.HazeItems[i]))
                        .ToArray();

                    var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                        0,
                        $"{i + 1} ({hazes.Maxima(t => Math.Abs(t.Item.Error)).First().Item.Error:0.###})",
                        [.. hazes.Select(t => new Point(t.PMTId, t.Item.Value))],
                        i,
                        new Range(0, hazeCount - 1),
                        markerShape: MarkerShape.HorizontalBar);

                    scatterMarkers.MarkerSize = 30;
                    scatterMarkers.MarkerStyle.LineWidth = 5;
                    scatterMarkers.IsVisible = i == 0 || i == hazeCount - 1;

                    scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                        1,
                        $"{i + 1}",
                        [.. hazes.Select(t => new Point(t.PMTId, t.DigitalGain))],
                        i,
                        new Range(0, hazeCount - 1),
                        markerShape: MarkerShape.HorizontalBar);

                    scatterMarkers.MarkerSize = 30;
                    scatterMarkers.MarkerStyle.LineWidth = 5;
                    scatterMarkers.IsVisible = i == 0 || i == hazeCount - 1;
                }
            }
            finally
            {
                scatterPlotControl.AutoScaleRefresh();
            }
        }
    }

    #region Mapper

    public CIBLightMatchingDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        OpticsApodizationModeEnum = OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        CollectorPolarizationModeEnum = CollectorPolarizationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserCIBLightMatchingItem AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        Speed = ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
        OpticsApodizationModeEnum = (int)OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = (int)OpticsPolarizationModeEnum,
        CollectorPolarizationModeEnum = (int)CollectorPolarizationModeEnum,
        Items = [.. Items.Select(t => t.AdaptTo())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    private IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        var customGrid = new CustomGrid();
        scatterPlotControl.Configure(customGrid, 4,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 2));
                customGrid.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        scatterPlotControl.SetTitle(0, "Haze(Y: PMTValue - X: PMT Id)");
        scatterPlotControl.SetTitle(1, "Haze(Y: Digital Gain - X: PMT Id)");
        scatterPlotControl.SetTitle(2, "Silica Spheres(Y: Value - X: PMT Id)");
        scatterPlotControl.SetTitle(3, "Silica Spheres(Y: Digital Gain + Multiplicative Factors - X: PMT Id)");
        scatterPlotControl.ToggleLegend(0, false);
        scatterPlotControl.ToggleLegend(1, false);
        scatterPlotControl.ToggleLegend(2, false);
        scatterPlotControl.ToggleLegend(3, false);

        return scatterPlotControl;
    }

    #endregion Mapper
}

public sealed partial class CIBLightMatchingDTOItem : ObservableObject, ICloneable<CIBLightMatchingDTOItem>, IAdaptTo<CalibrationLaserCIBLightMatchingItem.Item>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<Item> _hazeItems = [];

    [ObservableProperty]
    private IReadOnlyList<Item> _silicaSphereItems = [];

    [ObservableProperty]
    private double _digitalGain;

    [ObservableProperty]
    private double _multiplicativeFactors;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double DigitalGainPlusMultiplicativeFactors => DigitalGain + MultiplicativeFactors;

    partial void OnHazeItemsChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(HazeItems));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(HazeItems));
    }

    partial void OnSilicaSphereItemsChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(SilicaSphereItems));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(SilicaSphereItems));
    }

    #region Mapper

    public CIBLightMatchingDTOItem Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        HazeItems = [.. HazeItems.Select(t => t.Clone())],
        SilicaSphereItems = [.. SilicaSphereItems.Select(t => t.Clone())],
        DigitalGain = DigitalGain,
        MultiplicativeFactors = MultiplicativeFactors
    };

    public CalibrationLaserCIBLightMatchingItem.Item AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        ChannelId = CIBInformation.ChannelId,
        DigitalGainPlusMultiplicativeFactors = DigitalGainPlusMultiplicativeFactors
    };

    #endregion Mapper


    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        private double _value;

        [ObservableProperty]
        private double _error;

        public Item Clone() => new()
        {
            Value = Value,
            Error = Error
        };
    }
}