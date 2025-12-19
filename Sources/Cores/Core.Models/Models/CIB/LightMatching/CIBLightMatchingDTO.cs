using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Collector;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
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

    public CIBLightMatchingDTO()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Haze(Y: PMTValue - X: PMT Id)");
        ScatterPlotControl.SetTitle(1, "Silica Spheres(Y: PMTValue - X: PMT Id)");
    }

    private void RefreshPlot()
    {
        try
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
                var hazeCount = items.Max(t => t.Hazes.Count);
                for (var i = 0; i < hazeCount; i++)
                {
                    var scatterMarkers = ScatterPlotControl.GetOrAddScatterMarkers(
                        0,
                        $"{i + 1}: {channelId}",
                        items
                            .Where(t => i < t.Hazes.Count)
                            .Select(t => new Point(t.CIBInformation.PMTId, t.Hazes[i].PMTValue))
                            .ToArray(),
                        i,
                        new Range(0, hazeCount - 1),
                        markerShape: MarkerShape.HorizontalBar);

                    scatterMarkers.MarkerSize = 20;
                    scatterMarkers.IsVisible = i == hazeCount - 1;
                }

                var silicaSpheresCount = items.Max(t => t.SilicaSpheres.Count);
                for (var i = 0; i < silicaSpheresCount; i++)
                {
                    var scatterMarkers = ScatterPlotControl.GetOrAddScatterMarkers(
                        1,
                        $"{i + 1}: {channelId}",
                        items
                            .Where(t => i < t.SilicaSpheres.Count)
                            .Select(t => new Point(t.CIBInformation.PMTId, t.SilicaSpheres[i].PMTValue))
                            .ToArray(),
                        i,
                        new Range(0, hazeCount - 1),
                        markerShape: MarkerShape.HorizontalBar);

                    scatterMarkers.MarkerSize = 20;
                    scatterMarkers.IsVisible = i == hazeCount - 1;
                }
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
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
        Items = [..Items.Select(t => t.AdaptTo())],
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
    private IReadOnlyList<Item> _hazes = [];

    [ObservableProperty]
    private IReadOnlyList<Item> _silicaSpheres = [];

    [ObservableProperty]
    private double _digitalGain;

    [ObservableProperty]
    private double _multiplicativeFactors;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double DigitalGainPlusMultiplicativeFactors => DigitalGain + MultiplicativeFactors;

    partial void OnHazesChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(Hazes));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Hazes));
    }

    partial void OnSilicaSpheresChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(SilicaSpheres));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(SilicaSpheres));
    }

    #region Mapper

    public CIBLightMatchingDTOItem Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Hazes = [.. Hazes.Select(t => t.Clone())],
        SilicaSpheres = [.. SilicaSpheres.Select(t => t.Clone())],
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

        public Item Clone() => new()
        {
            PMTValue = PMTValue
        };
    }
}