using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Optics.GlobalFieldTilt;

public sealed partial class GlobalFieldTiltDTO : CalibrationDtoBase, ICloneable<GlobalFieldTiltDTO>, IAdaptTo<CalibrationLaserDOEAngle>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private IReadOnlyList<GlobalFieldTiltDTOItem> _items = [];

    [ObservableProperty]
    private GlobalFieldTiltDTOItem? _resultItem;

    partial void OnItemsChanged(IReadOnlyList<GlobalFieldTiltDTOItem>? oldValue, IReadOnlyList<GlobalFieldTiltDTOItem> newValue)
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

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079


    public GlobalFieldTiltDTO()
    {
        ScatterPlotControl.Configure(new Columns());

        ScatterPlotControl.SetTitle(0, "Summary(Y: Global Field Tilt(ECS) - X: DOE Pos)");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear();

            ScatterPlotControl.GetOrAddScatterLine(
                0,
                "Origin",
                [.. Items.Select(t => new Point(t.AppliedDOEPos, t.GlobalFieldTiltError))],
                Constants.Category10.GetColor(1));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public GlobalFieldTiltDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ResultItem = ResultItem?.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserDOEAngle AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        DOEAngle = ResultItem?.AppliedDOEPos ?? 0
    };
}

public sealed partial class GlobalFieldTiltDTOItem : ObservableObject, ICloneable<GlobalFieldTiltDTOItem>
{
    [ObservableProperty]
    private double _appliedDOEPos;

    [ObservableProperty]
    private double _reviseDOEPos;

    [ObservableProperty]
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private double _globalFieldTiltError;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [ObservableProperty]
    private IReadOnlyList<Point> _originPoints = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [ObservableProperty]
    private IReadOnlyList<Point> _fitPoints = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [ObservableProperty]
    private IReadOnlyList<Item> _bestFocusChannelItems = [];

    public GlobalFieldTiltDTOItem()
    {
        ScatterPlotControl.Configure(new Columns());

        ScatterPlotControl.SetTitle(0, "Global Field Tilt(Y: Focus(ECS) - X: PMT(um))");
    }

    partial void OnBestFocusChannelItemsChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
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

    partial void OnOriginPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    private void RefreshPlot()
    {
        try
        {
            if (OriginPoints.Count == 0) return;

            ScatterPlotControl.Clear(0);
            ScatterPlotControl.GetOrAddScatterLine(
                0,
                "Origin",
                OriginPoints,
                0,
                new Range(0, OriginPoints.Count - 1));

            if (FitPoints.Count > 0)
                ScatterPlotControl.GetOrAddScatterLine(
                    0,
                    $"Fit Curve: y = {Slope:0.######}x + {Intercept:0.######} r^2 = {RSquared:0.######}",
                    FitPoints,
                    Constants.Category10.GetColor(1));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public object ToFlatnessHtmlAnonymous() => new
    {
        AppliedDOEPos,
        ReviseDOEPos,
        Slope,
        Intercept,
        RSquared,
        GlobalFieldTiltEcsError = GlobalFieldTiltError,
        Analysis = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
        MultiLightBestFocusResult = new HtmlTable([.. BestFocusChannelItems.Select(t => t.ToFlatnessHtmlAnonymous())])
    };

    public GlobalFieldTiltDTOItem Clone() => new()
    {
        AppliedDOEPos = AppliedDOEPos,
        ReviseDOEPos = ReviseDOEPos,
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        GlobalFieldTiltError = GlobalFieldTiltError,
        OriginPoints = [.. OriginPoints],
        BestFocusChannelItems = [.. BestFocusChannelItems.Select(t => t.Clone())]
    };

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        private int _pmtId;

        [ObservableProperty]
        private int _channelId;

        [ObservableProperty]
        private double _xBestFocusEcs;

        [ObservableProperty]
        private double _xBestFocusQuality;

        [ObservableProperty]
        private IReadOnlyList<Point> _xQualitys = [];

        /// <summary>
        /// 算法结果拟合结果
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<Point> _xFitPositions = [];

        partial void OnXFitPositionsChanged(IReadOnlyList<Point> value) => RefreshPlot();

        partial void OnXQualitysChanged(IReadOnlyList<Point> value) => RefreshPlot();

        partial void OnXBestFocusEcsChanged(double value) => RefreshPlot();

        [ObservableProperty]
        private string _rawFilePath = string.Empty;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _linearFilePath = string.Empty;

#pragma warning disable IDE0079
#pragma warning disable CS0657

        [ObservableProperty]
        [property: Newtonsoft.Json.JsonIgnore]
        [property: System.Text.Json.Serialization.JsonIgnore]
        [property: System.Xml.Serialization.XmlIgnore]
        private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        public Item()
        {
            ScatterPlotControl.Configure(new Columns());
            ScatterPlotControl.SetTitle(0, "(X Focus)(Y: X Quality - X: X(pix/ECS))");
        }

#pragma warning restore CS0657
#pragma warning restore IDE0079

        private void RefreshPlot()
        {
            ScatterPlotControl.GetOrAddScatterMarkers(
                0,
                "X Quality",
                XQualitys);
            ScatterPlotControl.GetOrAddScatterLine(
                0,
                "Fit Points",
                XFitPositions);

            if (XFitPositions.Count != 0)
            {
                var scatterMarkersX = ScatterPlotControl.GetOrAddScatterMarkers(
                    0,
                    "X Best Focus",
                    [XFitPositions.Maxima(t => t.Y).First()],
                    color: Colors.Red,
                    markerShape: MarkerShape.Asterisk);
                scatterMarkersX.MarkerSize = 25;
            }

            ScatterPlotControl.AutoScaleRefresh();
        }

        public object ToFlatnessHtmlAnonymous() => new
        {
            PmtId,
            ChannelId,
            XBestFocusEcs,
            XBestFocusQuality,
            RawFilePath,
            Plot = new HtmlTab(new
            {
                Analysis = new HtmlContainer(ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()),
                OriginImage = new HtmlImage(FilePath)
                // LinearImage = new HtmlImage(File.Exists(LinearFilePath) ? LinearFilePath : OriginFilePath)
            })
        };

        public Item Clone() => new()
        {
            PmtId = PmtId,
            ChannelId = ChannelId,
            XBestFocusEcs = XBestFocusEcs,
            XBestFocusQuality = XBestFocusQuality,
            XFitPositions = [.. XFitPositions],
            RawFilePath = RawFilePath,
            FilePath = FilePath,
            LinearFilePath = LinearFilePath
        };
    }
}