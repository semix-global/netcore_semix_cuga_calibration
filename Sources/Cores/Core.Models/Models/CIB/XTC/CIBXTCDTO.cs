using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
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
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.XTC;

[CacheVersion("1.0.0")]
public sealed partial class CIBXTCDTO : CalibrationDtoBase, ICloneable<CIBXTCDTO>, IAdaptTo<CalibrationLaserCIBXTCItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial AODUniformityDTO.WindowItem StartWindowItem { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial AODUniformityDTO.WindowItem StopWindowItem { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public bool IsReverse => StartWindowItem.HorizontalProjectMinPixel > StopWindowItem.HorizontalProjectMinPixel;

    [ObservableProperty]
    public partial IReadOnlyList<CIBXTCDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial ConcurrentDictionary<int, double> TargetPixelValues { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IScatterPlotControl ForwardAndReverseScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial ConcurrentDictionary<int, IScatterPlotControl> ScatterPlotControls { get; set; } = [];

    #region Partial Method

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnStartWindowItemChanged(AODUniformityDTO.WindowItem? oldValue, AODUniformityDTO.WindowItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        OnPropertyChanged(nameof(IsReverse));
        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsReverse));
            RefreshForwardAndReversePlot();
        }
    }

    partial void OnStopWindowItemChanged(AODUniformityDTO.WindowItem? oldValue, AODUniformityDTO.WindowItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        OnPropertyChanged(nameof(IsReverse));
        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsReverse));
            RefreshForwardAndReversePlot();
        }
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

    partial void OnTargetPixelValuesChanged(ConcurrentDictionary<int, double> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    #endregion

    public CIBXTCDTO()
    {
        ForwardAndReverseScatterPlotControl.Configure(new Columns(), 2);

        ForwardAndReverseScatterPlotControl.SetTitle(0, "Window(Y: Coefficient - X: sa)");
        ForwardAndReverseScatterPlotControl.SetTitle(1, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
    }

    public CIBXTCDTO(IReadOnlyList<int> cibInformationPMTIds) : this()
    {
        ScatterPlotControls = new ConcurrentDictionary<int, IScatterPlotControl>(cibInformationPMTIds.Select(t => new KeyValuePair<int, IScatterPlotControl>(t, GetScatterPlotControl())));
    }

    private void RefreshForwardAndReversePlot()
    {
        try
        {
            Refresh(StartWindowItem, "Start", Colors.Blue, Colors.DarkBlue);
            Refresh(StopWindowItem, "Stop", Colors.Red, Colors.DarkRed);
        }
        finally
        {
            ForwardAndReverseScatterPlotControl.AutoScaleRefresh();
        }

        return;

        void Refresh(AODUniformityDTO.WindowItem windowItem, string title, Color primaryColor, Color secondaryColor)
        {
            if (windowItem.Window.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    0,
                    title,
                    [.. windowItem.Window.ToPoints()],
                    primaryColor);

            if (windowItem.ImageHorizontalProjects.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    title,
                    [.. windowItem.ImageHorizontalProjects.ToPoints()],
                    primaryColor);

            if (windowItem.SmoothImageHorizontalProjects.Count > 0)
            {
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"{title} Smooth",
                    [.. windowItem.SmoothImageHorizontalProjects.ToPoints()],
                    secondaryColor);

                ForwardAndReverseScatterPlotControl.GetOrAddXLine(
                    1,
                    $"{title} Smooth Min Pixel",
                    windowItem.HorizontalProjectMinPixel,
                    secondaryColor);
            }
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
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(pmtId, _ => GetScatterPlotControl());

            scatterPlotControl.Clear(0);
            scatterPlotControl.Clear(1);

            try
            {
                if (TargetPixelValues.TryGetSingle(t => t.Key == pmtId, out var targetPMTValueKvp))
                {
                    scatterPlotControl.GetOrAddXLine(1, "Target", targetPMTValueKvp.Value, Colors.Red);

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
                                [.. itemItemData.Window.ToPoints()],
                                Colors.Red);

                            var color = Constants.Turbo.GetColor(i, new Range(0, count - 1));
                            if (minChannelId != maxChannelId) color = color.Lighten((1 - new Range(minChannelId, maxChannelId).Normalize(channelId)) * 0.8);

                            scatterPlotControl.GetOrAddScatterLine(
                                1,
                                $"{i + 1}: {nameof(CIBInformation.ChannelId)}({channelId}) Error: {itemItemData.Error:0.###}",
                                [.. itemItemData.ImageHorizontalProjects.ToPoints()],
                                color).IsVisible = i == count - 1;

                            scatterPlotControl.GetOrAddScatterLine(
                                1,
                                $"Smooth: {i + 1}: {nameof(CIBInformation.ChannelId)}({channelId})",
                                [.. itemItemData.SmoothImageHorizontalProjects.ToPoints()],
                                color).IsVisible = i == count - 1;

                            scatterPlotControl.GetOrAddXLine(
                                1,
                                $"{i + 1} {nameof(CIBInformation.ChannelId)}({channelId})",
                                itemItemData.HorizontalProjectMinPixel,
                                color).IsVisible = i == count - 1;
                        }
                    }
                }

                scatterPlotControl.GetOrAddScatterLine(
                    2,
                    "Result",
                    [.. itemItems.Select(t => new Point(t.CIBInformation.ChannelId, t.Delay))],
                    Colors.Red);
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
        scatterPlotControl.SetTitle(2, "Result(Y: Delay - X: Channel Id)");
        scatterPlotControl.ToggleInvisibleLegendItem(0, false);
        scatterPlotControl.ToggleInvisibleLegendItem(1, false);

        return scatterPlotControl;
    }

    #region Mapper

    public CIBXTCDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        TargetPixelValues = new ConcurrentDictionary<int, double>(TargetPixelValues),
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
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IReadOnlyList<Item> Items { get; set; } = [];

    [ObservableProperty]
    public partial double Delay { get; set; }

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

    public sealed partial class Item : AODUniformityDTO.WindowItem, ICloneable<Item>
    {
        [ObservableProperty]
        public partial double Error { get; set; }

        [ObservableProperty]
        public partial bool IsOk { get; set; }

        public new Item Clone()
        {
            var clone = Guard.IsAssignableToTypeAndReturn<Item>(base.Clone());
            clone.Error = Error;
            clone.IsOk = IsOk;

            return clone;
        }
    }
}