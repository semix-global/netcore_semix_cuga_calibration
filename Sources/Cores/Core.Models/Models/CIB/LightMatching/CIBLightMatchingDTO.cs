using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Collector;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.ScottPlot.WPF.Plottables;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.LightMatching;

public sealed partial class CIBLightMatchingDTO : CalibrationDtoBase, ICloneable<CIBLightMatchingDTO>, IAdaptTo<CalibrationLaserCIBLightMatchingItem>
{
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
    private ConcurrentBag<KeyValuePair<int, double>> _hazeTargetPMTValues = [];

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<int, double>> _silicaSphereAveragePMTValues = [];

    [ObservableProperty]
    private double? _silicaSphereTargetPMTValue;

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

    // ReSharper disable UnusedParameterInPartialMethod

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

    partial void OnHazeTargetPMTValuesChanged(ConcurrentBag<KeyValuePair<int, double>> value) => RefreshPlot();

    partial void OnSilicaSphereAveragePMTValuesChanged(ConcurrentBag<KeyValuePair<int, double>> value) => RefreshPlot();

    partial void OnSilicaSphereTargetPMTValueChanged(double? value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBLightMatchingDTO()
    {
    }

    public CIBLightMatchingDTO(IReadOnlyList<int> cibInformationChannelIds) : this()
    {
        ScatterPlotControls = [.. cibInformationChannelIds.Select(t => new KeyValuePair<int, IScatterPlotControl>(t, GetScatterPlotControl()))];
    }

    private void RefreshPlot()
    {
        var results = (
            from itemItem in Items
            group itemItem by itemItem.CIBInformation.ChannelId
            into g
            orderby g.Key
            select (
                ChannelId: g.Key,
                ItemItems: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
            )).ToArray();

        foreach (var (channelId, itemItems) in results)
        {
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(channelId, GetScatterPlotControl());

            try
            {
                if (HazeTargetPMTValues.TryGetSingle(t => t.Key == channelId, out var hazeTargetPMTValueKvp))
                {
                    scatterPlotControl.GetOrAddYLine(0, "Target", hazeTargetPMTValueKvp.Value, color: Colors.Red);

                    var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                        2,
                        "Result",
                        [.. itemItems.Select(t => new Point(t.CIBInformation.PMTId, t.DigitalGain))],
                        color: Colors.Red,
                        markerShape: MarkerShape.HorizontalBar);

                    SetScatterMarkersStyle(scatterMarkers);

                    var hazeCount = itemItems.Max(t => t.HazeItems.Count);
                    for (var i = 0; i < hazeCount; i++)
                    {
                        var hazes = itemItems.Where(t => i < t.HazeItems.Count)
                            .Select(t => (t.CIBInformation.PMTId, Item: t.HazeItems[i]))
                            .ToArray();

                        scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                            0,
                            $"{i + 1}",
                            [.. hazes.Select(t => new Point(t.PMTId, t.Item.PMTValue))],
                            i,
                            new Range(0, hazeCount - 1),
                            markerShape: MarkerShape.HorizontalBar);

                        SetScatterMarkersStyle(scatterMarkers);
                        scatterMarkers.IsVisible = i == 0 || i == hazeCount - 1;

                        scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                            1,
                            $"Error: {i + 1}",
                            [.. hazes.Select(t => new Point(t.PMTId, t.Item.Error))],
                            i,
                            new Range(0, hazeCount - 1),
                            markerShape: MarkerShape.HorizontalBar);

                        SetScatterMarkersStyle(scatterMarkers);
                        scatterMarkers.IsVisible = i == 0 || i == hazeCount - 1;

                        scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                            1,
                            $"Digital Gain: {i + 1}",
                            [.. hazes.Select(t => new Point(t.PMTId, t.Item.Result))],
                            i,
                            new Range(0, hazeCount - 1),
                            markerShape: MarkerShape.HorizontalBar);

                        SetScatterMarkersStyle(scatterMarkers);
                        scatterMarkers.IsVisible = i == 0 || i == hazeCount - 1;
                    }
                }

                if (SilicaSphereTargetPMTValue is not null)
                {
                    scatterPlotControl.GetOrAddYLine(3, "Target", SilicaSphereTargetPMTValue.Value, color: Colors.Red);

                    if (SilicaSphereAveragePMTValues.TryGetSingle(t => t.Key == channelId, out var silicaSphereAveragePMTValueKvp))
                    {
                        var yLine = scatterPlotControl.GetOrAddYLine(3, "Average", silicaSphereAveragePMTValueKvp.Value, color: Colors.Yellow);
                        yLine.LinePattern = LinePattern.Solid;

                        var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                            5,
                            "Result",
                            [.. itemItems.Select(t => new Point(t.CIBInformation.PMTId, t.DigitalGainPlusMultiplicativeFactors))],
                            color: Colors.Red,
                            markerShape: MarkerShape.HorizontalBar);

                        SetScatterMarkersStyle(scatterMarkers);

                        var silicaSphereCount = itemItems.Max(t => t.SilicaSphereItems.Count);
                        for (var i = 0; i < silicaSphereCount; i++)
                        {
                            var silicaSpheres = itemItems.Where(t => i < t.SilicaSphereItems.Count)
                                .Select(t => (t.CIBInformation.PMTId, Item: t.SilicaSphereItems[i]))
                                .ToArray();

                            scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                                3,
                                $"{i + 1}",
                                [.. silicaSpheres.Select(t => new Point(t.PMTId, t.Item.PMTValue))],
                                i,
                                new Range(0, silicaSphereCount - 1),
                                markerShape: MarkerShape.HorizontalBar);

                            SetScatterMarkersStyle(scatterMarkers);
                            scatterMarkers.IsVisible = i == 0 || i == silicaSphereCount - 1;

                            scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                                4,
                                $"Error: {i + 1}",
                                [.. silicaSpheres.Select(t => new Point(t.PMTId, t.Item.Error))],
                                i,
                                new Range(0, silicaSphereCount - 1),
                                markerShape: MarkerShape.HorizontalBar);

                            SetScatterMarkersStyle(scatterMarkers);
                            scatterMarkers.IsVisible = i == 0 || i == silicaSphereCount - 1;

                            scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                                4,
                                $"Multiplicative Factors: {i + 1}",
                                [.. silicaSpheres.Select(t => new Point(t.PMTId, t.Item.Result))],
                                i,
                                new Range(0, silicaSphereCount - 1),
                                markerShape: MarkerShape.HorizontalBar);

                            SetScatterMarkersStyle(scatterMarkers);
                            scatterMarkers.IsVisible = i == 0 || i == silicaSphereCount - 1;
                        }
                    }
                }
            }
            finally
            {
                scatterPlotControl.AutoScaleRefresh();
            }
        }

        return;

        static void SetScatterMarkersStyle(ScatterMarkers scatterMarkers)
        {
            scatterMarkers.MarkerSize = 30;
            scatterMarkers.MarkerStyle.LineWidth = 5;
        }
    }

    private static IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        var customGrid = new CustomGrid();
        scatterPlotControl.Configure(customGrid, 6,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 3, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 3, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 3, 2));
                customGrid.Set(plots[3], new GridCell(1, 1, 3, 2));
                customGrid.Set(plots[4], new GridCell(2, 0, 3, 2));
                customGrid.Set(plots[5], new GridCell(2, 1, 3, 2));
            });

        scatterPlotControl.SetTitle(0, "Haze(Y: Log - X: PMT Id)");
        scatterPlotControl.SetTitle(1, "Haze Details(Y: PMT Value(Log) - X: PMT Id)");
        scatterPlotControl.SetTitle(2, "Haze Result(Y: Digital Gain - X: PMT Id)");
        scatterPlotControl.SetTitle(3, "Silica Spheres(Y: PMT Value(Log) - X: PMT Id)");
        scatterPlotControl.SetTitle(4, "Silica Spheres Details(Y: PMT Value(Log) - X: PMT Id)");
        scatterPlotControl.SetTitle(5, "Silica Spheres Result(Y: Digital Gain + Multiplicative Factors - X: PMT Id)");
        scatterPlotControl.ToggleInvisibleLegendItem(0, false);
        scatterPlotControl.ToggleInvisibleLegendItem(1, false);
        scatterPlotControl.ToggleInvisibleLegendItem(3, false);
        scatterPlotControl.ToggleInvisibleLegendItem(4, false);

        return scatterPlotControl;
    }

    #region Mapper

    public CIBLightMatchingDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        OpticsApodizationModeEnum = OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        CollectorPolarizationModeEnum = CollectorPolarizationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        HazeTargetPMTValues = [.. HazeTargetPMTValues],
        SilicaSphereAveragePMTValues = [.. SilicaSphereAveragePMTValues],
        SilicaSphereTargetPMTValue = SilicaSphereTargetPMTValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserCIBLightMatchingItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        OpticsApodizationModeEnum = (int)OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.ToCgPolarizationTypeEnum(),
        CollectorPolarizationModeEnum = CollectorPolarizationModeEnum.ToCgNDFTypeEnum(),
        Items = [.. Items.Select(t => t.AdaptTo())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

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
        private double _pMTValue;

        [ObservableProperty]
        private double _error;

        [ObservableProperty]
        private double _result;

        [ObservableProperty]
        private bool _isOk;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        [ObservableProperty]
        [property: System.Text.Json.Serialization.JsonIgnore]
        [property: System.Xml.Serialization.XmlIgnore]
        [property: LiteDB.BsonIgnore]
        private IReadOnlyList<Point> _histogram = [];

        public Item Clone() => new()
        {
            PMTValue = PMTValue,
            Error = Error,
            Result = Result,
            IsOk = IsOk,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }
}