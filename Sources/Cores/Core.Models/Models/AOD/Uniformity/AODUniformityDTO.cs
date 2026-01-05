using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using System.ComponentModel;
using Core.Models.Enums.Optics;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.Uniformity;

public sealed partial class AODUniformityDTO : CalibrationDtoBase, ICloneable<AODUniformityDTO>, IAdaptTo<CalibrationLaserAODUniformityItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<OpticsPolarizationModeEnum, double>> _opticsPolarizationModeEnumMeasurePowers = [];

    [ObservableProperty]
    private AODUniformityDTOItem _item = new();

    [ObservableProperty]
    private IReadOnlyList<AODUniformityDTOItem> _items = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private ConcurrentBag<KeyValuePair<int, IScatterPlotControl>> _scatterPlotControls = [];

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<AODUniformityDTOItem>? oldValue, IReadOnlyList<AODUniformityDTOItem> newValue)
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

    // ReSharper restore UnusedParameterInPartialMethod

    public AODUniformityDTO()
    {
        ScatterPlotControl.Configure(new Rows(), 3);

        ScatterPlotControl.SetTitle(0, "Horizontal Projects(Y: Log - X: px)");
        ScatterPlotControl.SetTitle(1, "Details(Y: PMT Value(Log) - X: px)");
        ScatterPlotControl.SetTitle(2, "Result(Y: Uniformity - X: px)");
    }

    public AODUniformityDTO(IReadOnlyList<int> cibInformationChannelIds) : this()
    {
        ScatterPlotControls = [.. cibInformationChannelIds.Select(t => new KeyValuePair<int, IScatterPlotControl>(t, GetScatterPlotControl()))];
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.GetOrAddScatterLine(
                2,
                "Result",
                [.. Item.Uniformities.Index().Select(t => new Point(t.Index, t.Item))],
                Colors.Red);

            foreach (var (i, itemItemData) in Item.Items.Index())
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    0,
                    $"{i + 1}: {Item.CIBInformation}",
                    [.. itemItemData.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    i,
                    new Range(0, Item.Items.Count - 1));

                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"{i + 1}: {Item.CIBInformation} Error: [{itemItemData.MinRate:0.###}, {itemItemData.MaxRate:0.###}]",
                    [.. itemItemData.Uniformities.Index().Select(t => new Point(t.Index, t.Item))],
                    i,
                    new Range(0, Item.Items.Count - 1));
            }

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
                    var count = itemItems.Max(t => t.Items.Count);
                    for (var i = 0; i < count; i++)
                    {
                        var itemItemsData = itemItems.Where(t => i < t.Items.Count)
                            .Select(t => (t.CIBInformation.PMTId, Item: t.Items[i]))
                            .ToArray();
                        var minPMTId = itemItemsData.Min(t => t.PMTId);
                        var maxPMTId = itemItemsData.Max(t => t.PMTId);

                        foreach (var (pmtId, itemItemData) in itemItemsData)
                        {
                            scatterPlotControl.GetOrAddScatterLine(
                                $"{i + 1}: {pmtId} Error: [{itemItemData.MinRate:0.###}, {itemItemData.MaxRate:0.###}]",
                                [.. itemItemData.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                                pmtId,
                                new Range(minPMTId, maxPMTId)).IsVisible = i == count - 1;
                        }
                    }
                }
                finally
                {
                    scatterPlotControl.AutoScaleRefresh();
                }
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    private static IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        scatterPlotControl.SetTitle("Horizontal Projects(Y: Log - X: px)");
        scatterPlotControl.ToggleInvisibleLegendItem(false);

        return scatterPlotControl;
    }

    #region Mapper

    public AODUniformityDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        OpticsPolarizationModeEnumMeasurePowers = [..OpticsPolarizationModeEnumMeasurePowers],
        Item = Item.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserAODUniformityItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Coefficient = LaserLightInformation.Coefficient,
        OpticsPolarizationModeEnumMeasurePowers = [..OpticsPolarizationModeEnumMeasurePowers.Select(t => new KeyValuePair<CgPolarizationTypeEnum, double>(t.Key.ToCgPolarizationTypeEnum(), t.Value))],
        OpticsPolarizationModeEnum = Item.OpticsPolarizationModeEnum.ToCgPolarizationTypeEnum(),
        Uniformities = [.. Item.Uniformities],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class AODUniformityDTOItem : ObservableObject, ICloneable<AODUniformityDTOItem>
{
    [ObservableProperty]
    private OpticsPolarizationModeEnum _opticsPolarizationModeEnum;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    private IReadOnlyList<double> _uniformities = [];

    partial void OnItemsChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(Items));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Items));
    }

    #region Mapper

    public AODUniformityDTOItem Clone() => new()
    {
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        Uniformities = [.. Uniformities]
    };

    #endregion Mapper

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        private IReadOnlyList<double> _imageHorizontalProjects = [];

        [ObservableProperty]
        private double _minRate;

        [ObservableProperty]
        private double _maxRate;

        [ObservableProperty]
        private IReadOnlyList<double> _uniformities = [];

        [ObservableProperty]
        private bool _isOk;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        public Item Clone() => new()
        {
            ImageHorizontalProjects = [.. ImageHorizontalProjects],
            MinRate = MinRate,
            MaxRate = MaxRate,
            Uniformities = [.. Uniformities],
            IsOk = IsOk,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }
}