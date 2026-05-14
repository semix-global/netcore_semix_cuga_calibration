using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
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

[CacheVersion("1.0.0")]
public sealed partial class GlobalFieldTiltDTO : CalibrationDTOBase<GlobalFieldTiltDTO>, IAdaptTo<CalibrationLaserDOEAngle>
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<GlobalFieldTiltDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial GlobalFieldTiltDTOItem? ResultItem { get; set; }

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
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

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

    public override GlobalFieldTiltDTO Clone() => new()
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
    public partial double AppliedDOEPos { get; set; }

    [ObservableProperty]
    public partial double ReviseDOEPos { get; set; }

    [ObservableProperty]
    public partial double Slope { get; set; }

    [ObservableProperty]
    public partial double Intercept { get; set; }

    [ObservableProperty]
    public partial double RSquared { get; set; }

    [ObservableProperty]
    public partial double GlobalFieldTiltError { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Point> OriginPoints { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Point> FitPoints { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Item> BestFocusChannelItems { get; set; } = [];

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
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

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
        public partial int PmtId { get; set; }

        [ObservableProperty]
        public partial int ChannelId { get; set; }

        [ObservableProperty]
        public partial double XBestFocusEcs { get; set; }

        [ObservableProperty]
        public partial double XBestFocusQuality { get; set; }

        [ObservableProperty]
        public partial IReadOnlyList<Point> XQualitys { get; set; } = [];

        /// <summary>
        /// 算法结果拟合结果
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<Point> XFitPositions { get; set; } = [];

        partial void OnXFitPositionsChanged(IReadOnlyList<Point> value) => RefreshPlot();

        partial void OnXQualitysChanged(IReadOnlyList<Point> value) => RefreshPlot();

        partial void OnXBestFocusEcsChanged(double value) => RefreshPlot();

        [ObservableProperty]
        public partial string RawFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string FilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string LinearFilePath { get; set; } = string.Empty;

#pragma warning disable IDE0079
#pragma warning disable CS0657

        [ObservableProperty]
        [Newtonsoft.Json.JsonIgnore]
        public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

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