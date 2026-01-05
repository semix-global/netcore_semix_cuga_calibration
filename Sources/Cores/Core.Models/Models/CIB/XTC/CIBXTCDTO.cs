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
using Net.Utilities.ScottPlot.WPF.Plottables;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.XTC;

public sealed partial class CIBXTCDTO : CalibrationDtoBase, ICloneable<CIBXTCDTO>, IAdaptTo<CalibrationLaserCIBXTCItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<CIBXTCDTOItem> _items = [];

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<int, double>> _targetPMTValues = [];

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

    partial void OnItemsChanged(IReadOnlyList<CIBXTCDTOItem>? oldValue, IReadOnlyList<CIBXTCDTOItem> newValue)
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

    partial void OnTargetPMTValuesChanged(ConcurrentBag<KeyValuePair<int, double>> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBXTCDTO()
    {
    }

    public CIBXTCDTO(IReadOnlyList<int> cibInformationChannelIds) : this()
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
                if (TargetPMTValues.TryGetSingle(t => t.Key == channelId, out var targetPMTValueKvp) == false) return;
                scatterPlotControl.GetOrAddXLine(0, "Target", targetPMTValueKvp.Value, color: Colors.Red);

                scatterPlotControl.GetOrAddScatterLine(
                    2,
                    "Result",
                    [.. itemItems.Select(t => new Point(t.CIBInformation.PMTId, t.Delay))],
                    Colors.Red);

                var count = itemItems.Max(t => t.Items.Count);
                for (var i = 0; i < count; i++)
                {
                    var itemItemsData = itemItems.Where(t => i < t.Items.Count)
                        .Select(t => (t.CIBInformation.PMTId, Item: t.Items[i]))
                        .ToArray();

                    foreach (var (pmtId, itemItemData) in itemItemsData)
                    {
                        var scatterLineImageHorizontalProjects = scatterPlotControl.GetOrAddScatterLine(
                            0,
                            $"{i + 1}: {pmtId}",
                            [.. itemItemData.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                            i,
                            new Range(0, count - 1));
                        scatterLineImageHorizontalProjects.IsVisible = i == count - 1;
                    }

                    var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                        1,
                        $"Error: {i + 1}",
                        [.. itemItemsData.Select(t => new Point(t.PMTId, t.Item.Error))],
                        i,
                        new Range(0, count - 1),
                        markerShape: MarkerShape.HorizontalBar);
                    SetScatterMarkersStyle(scatterMarkers);
                    scatterMarkers.IsVisible = i == 0 || i == count - 1;

                    scatterMarkers = scatterPlotControl.GetOrAddScatterMarkers(
                        1,
                        $"Delay: {i + 1}",
                        [.. itemItemsData.Select(t => new Point(t.PMTId, t.Item.Delay))],
                        i,
                        new Range(0, count - 1),
                        markerShape: MarkerShape.HorizontalBar);
                    SetScatterMarkersStyle(scatterMarkers);
                    scatterMarkers.IsVisible = i == 0 || i == count - 1;
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

        scatterPlotControl.Configure(new Rows(), 3);

        scatterPlotControl.SetTitle(0, "Horizontal Projects(Y: Log - X: px)");
        scatterPlotControl.SetTitle(1, "Details(Y: Delay - X: PMT Id)");
        scatterPlotControl.SetTitle(2, "Result(Y: Delay - X: PMT Id)");
        scatterPlotControl.ToggleInvisibleLegendItem(0, false);
        scatterPlotControl.ToggleInvisibleLegendItem(1, false);

        return scatterPlotControl;
    }

    #region Mapper

    public CIBXTCDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        TargetPMTValues = [.. TargetPMTValues],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserCIBXTCItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Items = [.. Items.Select(t => t.AdaptTo())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class CIBXTCDTOItem : ObservableObject, ICloneable<CIBXTCDTOItem>, IAdaptTo<CalibrationLaserCIBXTCItem.Item>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    private double _delay;

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

    public CIBXTCDTOItem Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        Delay = Delay
    };

    public CalibrationLaserCIBXTCItem.Item AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        ChannelId = CIBInformation.ChannelId,
        Delay = Delay
    };

    #endregion Mapper

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        private IReadOnlyList<double> _imageHorizontalProjects = [];

        [ObservableProperty]
        private double _error;

        [ObservableProperty]
        private double _delay;

        [ObservableProperty]
        private bool _isOk;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        public Item Clone() => new()
        {
            ImageHorizontalProjects = [.. ImageHorizontalProjects],
            Error = Error,
            Delay = Delay,
            IsOk = IsOk,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }
}