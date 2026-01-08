using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.XTC;

public sealed partial class CIBXTCDTO : CalibrationDtoBase, ICloneable<CIBXTCDTO>, IAdaptTo<CalibrationLaserCIBXTCItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private CIBXTCDTOItem.Item _startWindowItem = new();

    [ObservableProperty]
    private CIBXTCDTOItem.Item _stopWindowItem = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public bool IsReverse => StartWindowItem.ProjectMinPixel > StopWindowItem.ProjectMinPixel;

    [ObservableProperty]
    private IReadOnlyList<CIBXTCDTOItem> _items = [];

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<int, double>> _targetPixelValues = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _forwardAndReverseScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private ConcurrentBag<KeyValuePair<int, IScatterPlotControl>> _scatterPlotControls = [];

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnStartWindowItemChanged(CIBXTCDTOItem.Item? oldValue, CIBXTCDTOItem.Item newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshForwardAndReversePlot();
    }

    partial void OnStopWindowItemChanged(CIBXTCDTOItem.Item? oldValue, CIBXTCDTOItem.Item newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshForwardAndReversePlot();
    }

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

    partial void OnTargetPixelValuesChanged(ConcurrentBag<KeyValuePair<int, double>> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBXTCDTO()
    {
        ForwardAndReverseScatterPlotControl.Configure(new Columns(), 2);

        ForwardAndReverseScatterPlotControl.SetTitle(0, "Window(Y: Coefficient - X: sa)");
        ForwardAndReverseScatterPlotControl.SetTitle(1, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
    }

    public CIBXTCDTO(IReadOnlyList<int> cibInformationPMTIds) : this()
    {
        ScatterPlotControls = [.. cibInformationPMTIds.Select(t => new KeyValuePair<int, IScatterPlotControl>(t, GetScatterPlotControl()))];
    }

    private void RefreshForwardAndReversePlot()
    {
        try
        {
            if (StartWindowItem.Window.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    0,
                    "Start",
                    [.. StartWindowItem.Window.Index().Select(t => new Point(t.Index, t.Item))],
                    Colors.Blue);

            if (StartWindowItem.ImageHorizontalProjects.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    "Start",
                    [.. StartWindowItem.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    Colors.Blue);

            if (StartWindowItem.SmoothImageHorizontalProjects.Count > 0)
            {
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    "Start Smooth",
                    [.. StartWindowItem.SmoothImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    Colors.DarkBlue);

                ForwardAndReverseScatterPlotControl.GetOrAddXLine(
                    1,
                    "Start Smooth Min Pixel",
                    StartWindowItem.ProjectMinPixel,
                    Colors.DarkBlue);
            }

            if (StopWindowItem.Window.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    0,
                    "Stop",
                    [.. StopWindowItem.Window.Index().Select(t => new Point(t.Index, t.Item))],
                    Colors.Red);

            if (StopWindowItem.ImageHorizontalProjects.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    "Stop",
                    [.. StopWindowItem.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    Colors.Red);

            if (StopWindowItem.SmoothImageHorizontalProjects.Count > 0)
            {
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    "Stop Smooth",
                    [.. StopWindowItem.SmoothImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    Colors.DarkRed);

                ForwardAndReverseScatterPlotControl.GetOrAddXLine(
                    1,
                    "Stop Smooth Min Pixel",
                    StopWindowItem.ProjectMinPixel,
                    Colors.DarkRed);
            }
        }
        finally
        {
            ForwardAndReverseScatterPlotControl.AutoScaleRefresh();
        }
    }

    private void RefreshPlot()
    {
        var results = (
            from itemItem in Items
            group itemItem by itemItem.CIBInformation.PMTId
            into g
            orderby g.Key
            select (
                PMTId: g.Key,
                ItemItems: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
            )).ToArray();

        foreach (var (pmtId, itemItems) in results)
        {
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(pmtId, GetScatterPlotControl());

            scatterPlotControl.Clear(0);
            scatterPlotControl.Clear(1);

            try
            {
                if (TargetPixelValues.TryGetSingle(t => t.Key == pmtId, out var targetPMTValueKvp) == false) return;
                scatterPlotControl.GetOrAddXLine(1, "Target", targetPMTValueKvp.Value, Colors.Red);

                scatterPlotControl.GetOrAddScatterLine(
                    2,
                    "Result",
                    [.. itemItems.Select(t => new Point(t.CIBInformation.ChannelId, t.Delay))],
                    Colors.Red);

                var count = itemItems.Max(t => t.Items.Count);
                for (var i = 0; i < count; i++)
                {
                    var itemItemsData = itemItems.Where(t => i < t.Items.Count)
                        .Select(t => (t.CIBInformation.ChannelId, Item: t.Items[i]))
                        .ToArray();

                    var minChannelId = itemItemsData.Min(t => t.ChannelId);
                    var maxChannelId = itemItemsData.Max(t => t.ChannelId);

                    foreach (var (channelId, itemItemData) in itemItemsData)
                    {
                        scatterPlotControl.GetOrAddScatterLine(
                            0,
                            "Window",
                            [.. itemItemData.Window.Index().Select(t => new Point(t.Index, t.Item))],
                            Colors.Red);

                        var color = Constants.Turbo.GetColor(i, new Range(0, count - 1));
                        if (minChannelId != maxChannelId) color = color.Lighten((1 - new Range(minChannelId, maxChannelId).Normalize(channelId)) * 0.8);

                        scatterPlotControl.GetOrAddScatterLine(
                            1,
                            $"{i + 1}: {nameof(CIBInformation.ChannelId)}({channelId}) Error: {itemItemData.Error:0.###}",
                            [.. itemItemData.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                            color).IsVisible = i == count - 1;

                        scatterPlotControl.GetOrAddScatterLine(
                            1,
                            $"Smooth: {i + 1}: {nameof(CIBInformation.ChannelId)}({channelId})",
                            [.. itemItemData.SmoothImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                            color).IsVisible = i == count - 1;

                        scatterPlotControl.GetOrAddXLine(
                            1,
                            $"{i + 1} {nameof(CIBInformation.ChannelId)}({channelId})",
                            itemItemData.ProjectMinPixel,
                            color).IsVisible = i == count - 1;
                    }
                }
            }
            finally
            {
                scatterPlotControl.AutoScaleRefresh();
            }
        }
    }

    private static IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        scatterPlotControl.Configure(new Rows(), 3);

        scatterPlotControl.SetTitle(0, "Window(Y: Coefficient - X: sa)");
        scatterPlotControl.SetTitle(1, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
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
        TargetPixelValues = [.. TargetPixelValues],
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
        private IReadOnlyList<double> _window = [];

        [ObservableProperty]
        private IReadOnlyList<double> _imageHorizontalProjects = [];

        [ObservableProperty]
        private IReadOnlyList<double> _smoothImageHorizontalProjects = [];

        [ObservableProperty]
        private int _projectMinPixel;

        [ObservableProperty]
        private double _error;

        [ObservableProperty]
        private bool _isOk;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        public Item Clone() => new()
        {
            Window = [.. Window],
            ImageHorizontalProjects = [.. ImageHorizontalProjects],
            SmoothImageHorizontalProjects = [.. SmoothImageHorizontalProjects],
            ProjectMinPixel = ProjectMinPixel,
            Error = Error,
            IsOk = IsOk,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }
}