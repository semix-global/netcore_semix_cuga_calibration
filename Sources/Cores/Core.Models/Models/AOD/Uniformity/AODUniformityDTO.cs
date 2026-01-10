using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
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
using Core.Utilities;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Models;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.Uniformity;

public sealed partial class AODUniformityDTO : CalibrationDtoBase, ICloneable<AODUniformityDTO>, IAdaptTo<CalibrationLaserAODUniformityItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private WindowItem _startWindowItem = new();

    [ObservableProperty]
    private WindowItem _stopWindowItem = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public bool IsReverse => StartWindowItem.ProjectMinPixel > StopWindowItem.ProjectMinPixel;

    [ObservableProperty]
    private WindowsItem _mappingWindowItem = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _forwardAndReverseScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

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

    partial void OnStartWindowItemChanged(WindowItem? oldValue, WindowItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshForwardAndReversePlot();
    }

    partial void OnStopWindowItemChanged(WindowItem? oldValue, WindowItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshForwardAndReversePlot();
    }

    partial void OnMappingWindowItemChanged(WindowsItem? oldValue, WindowsItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshForwardAndReversePlot();
    }

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
        var customGrid = new CustomGrid();
        ForwardAndReverseScatterPlotControl.Configure(customGrid, 4,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 2));
                customGrid.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        ForwardAndReverseScatterPlotControl.SetTitle(0, "Forward And Reverse Window(Y: Coefficient - X: sa)");
        ForwardAndReverseScatterPlotControl.SetTitle(1, "Forward And Reverse Horizontal Projects(Y: PMT Value(Log) - X: px)");
        ForwardAndReverseScatterPlotControl.SetTitle(2, "Mapping Window(Y: Coefficient - X: sa)");
        ForwardAndReverseScatterPlotControl.SetTitle(3, "Mapping Horizontal Projects(Y: PMT Value(Log) - X: px)");

        ScatterPlotControl.Configure(new Rows(), 3);

        ScatterPlotControl.SetTitle(0, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        ScatterPlotControl.SetTitle(1, "Window(Y: Coefficient - X: sa)");
        ScatterPlotControl.SetTitle(2, "Result Window(Y: Coefficient - X: sa)");
    }

    public AODUniformityDTO(IReadOnlyList<int> cibInformationChannelIds) : this()
    {
        ScatterPlotControls = [.. cibInformationChannelIds.Select(t => new KeyValuePair<int, IScatterPlotControl>(t, GetScatterPlotControl()))];
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

        void Refresh(WindowItem windowItem, string title, Color primaryColor, Color secondaryColor)
        {
            if (windowItem.Window.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    0,
                    title,
                    [.. windowItem.Window.Index().Select(t => new Point(t.Index, t.Item))],
                    primaryColor);

            if (windowItem.ImageHorizontalProjects.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    title,
                    [.. windowItem.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    primaryColor);

            if (windowItem.SmoothImageHorizontalProjects.Count > 0)
            {
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"{title} Smooth",
                    [.. windowItem.SmoothImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    secondaryColor);

                if (windowItem is WindowsItem windowItems)
                {
                    foreach (var (index, projectMinPixel) in windowItems.ProjectMinPixels.Index())
                    {
                        ForwardAndReverseScatterPlotControl.GetOrAddXLine(
                            1,
                            $"{title} Smooth Min Pixel: {index + 1}",
                            projectMinPixel,
                            secondaryColor);
                    }
                }
                else
                    ForwardAndReverseScatterPlotControl.GetOrAddXLine(
                        1,
                        $"{title} Smooth Min Pixel",
                        windowItem.ProjectMinPixel,
                        secondaryColor);
            }
        }
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);
            ScatterPlotControl.Clear(1);

            ScatterPlotControl.GetOrAddScatterLine(
                2,
                "Result",
                [.. Item.Window.Index().Select(t => new Point(t.Index, t.Item))],
                Colors.Red);

            foreach (var (i, itemItemData) in Item.Items.Index())
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    0,
                    $"{i + 1}: {Item.CIBInformation} Error: [{itemItemData.MinRate:0.###}, {itemItemData.MaxRate:0.###}]",
                    [.. itemItemData.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    i,
                    new Range(0, Item.Items.Count - 1));

                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"{i + 1}: {Item.CIBInformation}",
                    [.. itemItemData.Window.Index().Select(t => new Point(t.Index, t.Item))],
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

        scatterPlotControl.SetTitle("Horizontal Projects(Y: PMT Value(Log) - X: px)");
        scatterPlotControl.ToggleInvisibleLegendItem(false);

        return scatterPlotControl;
    }

    #region Mapper

    public AODUniformityDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        OpticsPolarizationModeEnumMeasurePowers = [.. OpticsPolarizationModeEnumMeasurePowers],
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
        OpticsPolarizationModeEnumMeasurePowers = [.. OpticsPolarizationModeEnumMeasurePowers.Select(t => new KeyValuePair<CgPolarizationTypeEnum, double>(t.Key.ToCgPolarizationTypeEnum(), t.Value))],
        OpticsPolarizationModeEnum = Item.OpticsPolarizationModeEnum.ToCgPolarizationTypeEnum(),
        Uniformities = [.. Item.Window],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public partial class WindowItem : ObservableObject, ICloneable<WindowItem>
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
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        public void CalculateProjectMinPixel(int yPixelTotalLength, int segmentCount, int segmentIndex)
        {
            var yPixelSegmentWidth = yPixelTotalLength / segmentCount;

            var (vYPixelStartIndex, _, vYPixelStopIndex) = Generate.LinearVShapeWindow(
                1d,
                1d,
                segmentIndex * yPixelSegmentWidth,
                yPixelSegmentWidth,
                yPixelTotalLength).Region;

            SmoothImageHorizontalProjects = [.. SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(ImageHorizontalProjects))];
            var (x, y) = Extremumor.FindMinima(
                Vector<double>.Build.DenseOfArray(Enumerable.Range(0, SmoothImageHorizontalProjects.Count).ToArray()),
                Vector<double>.Build.DenseOfEnumerable(SmoothImageHorizontalProjects));

            ProjectMinPixel = x
                .Select(t => (int)t)
                .Index()
                .Where(t => vYPixelStartIndex <= t.Item && t.Item <= vYPixelStopIndex)
                .Select(t => (X: t.Item, Y: y[t.Index]))
                .OrderBy(t => t.Y)
                .First()
                .X;
        }

        public WindowItem Clone() => new()
        {
            Window = [.. Window],
            ImageHorizontalProjects = [.. ImageHorizontalProjects],
            SmoothImageHorizontalProjects = [.. SmoothImageHorizontalProjects],
            ProjectMinPixel = ProjectMinPixel,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }

    public partial class WindowsItem : WindowItem, ICloneable<WindowsItem>
    {
        [ObservableProperty]
        private IReadOnlyList<int> _projectMinPixels = [];

        public void CalculateProjectMinPixels(int yPixelTotalLength, int segmentCount, int[] segmentIndexes)
        {
            var projectMinPixels = new int[segmentCount];

            var yPixelSegmentWidth = yPixelTotalLength / segmentCount;

            var regions = Generate.LinearVShapeWindow(
                1d,
                1d,
                [..segmentIndexes.Select(t => t * yPixelSegmentWidth)],
                yPixelSegmentWidth,
                yPixelTotalLength).Regions;

            SmoothImageHorizontalProjects = [.. SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(ImageHorizontalProjects))];
            var (x, y) = Extremumor.FindMinima(
                Vector<double>.Build.DenseOfArray(Enumerable.Range(0, SmoothImageHorizontalProjects.Count).ToArray()),
                Vector<double>.Build.DenseOfEnumerable(SmoothImageHorizontalProjects));

            foreach (var (index, (vYPixelStartIndex, _, vYPixelStopIndex)) in regions.Index())
            {
                projectMinPixels[index] = x
                    .Select(t => (int)t)
                    .Index()
                    .Where(t => vYPixelStartIndex <= t.Item && t.Item <= vYPixelStopIndex)
                    .Select(t => (X: t.Item, Y: y[t.Index]))
                    .OrderBy(t => t.Y)
                    .First()
                    .X;
            }

            ProjectMinPixels = projectMinPixels;
        }

        public new WindowsItem Clone()
        {
            var clone = GuardUtils.IsAssignableToType<WindowsItem>(base.Clone());
            clone.ProjectMinPixels = [..ProjectMinPixels];

            return clone;
        }
    }
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
    private IReadOnlyList<double> _window = [];

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
        Window = [.. Window]
    };

    #endregion Mapper

    public sealed partial class Item : AODUniformityDTO.WindowItem, ICloneable<Item>
    {
        [ObservableProperty]
        private double _minRate;

        [ObservableProperty]
        private double _maxRate;

        [ObservableProperty]
        private bool _isOk;

        public new Item Clone()
        {
            var clone = GuardUtils.IsAssignableToType<Item>(base.Clone());
            clone.MinRate = MinRate;
            clone.MaxRate = MaxRate;
            clone.IsOk = IsOk;

            return clone;
        }
    }
}