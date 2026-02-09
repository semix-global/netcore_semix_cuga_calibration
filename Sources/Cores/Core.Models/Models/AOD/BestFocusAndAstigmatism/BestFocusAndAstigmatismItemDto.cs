using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;

namespace Core.Models.Models.AOD.BestFocusAndAstigmatism;

public partial class BestFocusAndAstigmatismDTO : CalibrationDtoBase, ICloneable<BestFocusAndAstigmatismDTO>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private OpticsApodizationModeEnum _apodizationModeEnum;

    [ObservableProperty]
    private double _spectralDensity;

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private ObservableCollection<BestFocusAndAstigmatismItemDto> _items = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public BestFocusAndAstigmatismDTO()
    {
        var customGrid = new CustomGrid();
        ScatterPlotControl.Configure(customGrid, 3,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 3, 1));
                customGrid.Set(plots[1], new GridCell(1, 0, 3, 1));
                customGrid.Set(plots[2], new GridCell(2, 0, 3, 1));
            });

        ScatterPlotControl.SetTitle(0, "(Y Focus)(Y: mm/MHz - X: Y Focus(ecs))");
        ScatterPlotControl.SetTitle(1, "(X Focus)(Y: mm/MHz - X: X Focus(ecs))");
        ScatterPlotControl.SetTitle(2, "(XY Focus Offset)(Y: mm/MHz - X: XY Focus Offset(ecs))");
    }

    partial void OnItemsChanged(ObservableCollection<BestFocusAndAstigmatismItemDto>? oldValue, ObservableCollection<BestFocusAndAstigmatismItemDto> newValue)
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
        ScatterPlotControl.Clear(0);
        ScatterPlotControl.Clear(1);
        ScatterPlotControl.Clear(2);

        if (Items.Count == 0) return;

        var channelItems = Items.First().ChannelItems;
        if (channelItems.Count == 0) return;

        foreach (var itemDto in Items)
        {
            foreach (var (index, channelItemDto) in itemDto.ChannelItems.Index())
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    0,
                    $"PMT:{channelItemDto.PmtId}-Channel:{channelItemDto.ChannelId}",
                    [
                        ..Items.Select(t =>
                            new Point
                            (
                                GuardUtils.IsNotNullAndReturn(t.SingleOrDefaultChannelItem(channelItemDto.PmtId, channelItemDto.ChannelId)).YBestFocusEcs,
                                1 / t.SpectralDensity
                            )
                        )
                    ],
                    index,
                    new ScottPlot.Range(0, channelItems.Count - 1));

                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"PMT:{channelItemDto.PmtId}-Channel:{channelItemDto.ChannelId}",
                    [
                        ..Items.Select(t =>
                            new Point
                            (
                                GuardUtils.IsNotNullAndReturn(t.SingleOrDefaultChannelItem(channelItemDto.PmtId, channelItemDto.ChannelId)).XBestFocusEcs,
                                1 / t.SpectralDensity
                            )
                        )
                    ],
                    index,
                    new ScottPlot.Range(0, channelItems.Count - 1));

                ScatterPlotControl.GetOrAddScatterLine(
                    2,
                    $"PMT:{channelItemDto.PmtId}-Channel:{channelItemDto.ChannelId}",
                    [
                        ..Items.Select(t =>
                            new Point
                            (
                                GuardUtils.IsNotNullAndReturn(t.SingleOrDefaultChannelItem(channelItemDto.PmtId, channelItemDto.ChannelId)).XYBestFocusOffsetEcs,
                                1 / t.SpectralDensity
                            )
                        )
                    ],
                    index,
                    new ScottPlot.Range(0, channelItems.Count - 1));
            }
        }

        ScatterPlotControl.AutoScaleRefresh();
    }

    public object ToFlatnessHtmlAnonymous() => new
    {
        ProductivityInformation,
        ApodizationModeEnum,
        AstigmatismBestSpectralDensity = SpectralDensity,
        AstigmatismBestChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        Plot = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
    };

    public BestFocusAndAstigmatismDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        ApodizationModeEnum = ApodizationModeEnum,
        SpectralDensity = SpectralDensity,
        GenerateChirpAODWaveformParam = GenerateChirpAODWaveformParam.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        Id = Id,
        Expiration = Expiration,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };
}

public partial class BestFocusAndAstigmatismItemDto : ObservableObject, ICloneable<BestFocusAndAstigmatismItemDto>
{
    [ObservableProperty]
    private double _spectralDensity;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    [ObservableProperty]
    private IReadOnlyList<BestFocusAndAstigmatismChannelItemDto> _channelItems = [];

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    [ObservableProperty]
    private IReadOnlyList<(double Trigger, double XMachinePosition, double Ecs)> _traceBuffers = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    [ObservableProperty]
    private IReadOnlyList<double> _eCSInterpolationBuffers = [];

    partial void OnTraceBuffersChanged(IReadOnlyList<(double Trigger, double XMachinePosition, double Ecs)> value) => RefreshPlot();

    public int TriggerStartIndex { get; private set; }

    public int TriggerEndIndex { get; private set; }

    /// <summary>
    /// 线扫描频率，Lines/s
    /// </summary>
    [ObservableProperty]
    private double _lineScanRate;

    partial void OnLineScanRateChanged(double value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public BestFocusAndAstigmatismItemDto()
    {
        ScatterPlotControl.Configure(totalPlotCount: 2);
        ScatterPlotControl.SetTitle(0, "(TraceBuffer)(Y: value - X: times(ms))");
        ScatterPlotControl.SetTitle(1, "(TraceBuffer)(Y: ecs - X: x position(mm))");
    }


    public BestFocusAndAstigmatismChannelItemDto? SingleOrDefaultChannelItem(int pmtId, int channelId)
        => ChannelItems.SingleOrDefault(t => t.PmtId == pmtId && t.ChannelId == channelId);


    public IReadOnlyList<BestFocusAndAstigmatismChannelGroupItemDto> ChannelGroupItemDtoList
        =>
        [
            ..ChannelItems.GroupBy(t => t.ChannelId)
                .Select(t => new BestFocusAndAstigmatismChannelGroupItemDto()
                {
                    ChannelId = t.Key,
                    ChannelItems = [..t.Select(tt => tt)]
                })
                .ToList()
                .AsReadOnly()
        ];

    private void RefreshPlot()
    {
        if (TraceBuffers.Count == 0) return;

        var triggerBuffers = TraceBuffers.Select((t, i) => new Point(i, t.Trigger)).ToArray();
        var xMachinePositionBuffers = TraceBuffers.Select((t, i) => new Point(i, t.XMachinePosition)).ToArray();
        var ecsBuffers = TraceBuffers.Select((t, i) => new Point(i, t.Ecs)).ToArray();

        TriggerStartIndex = (int)triggerBuffers.First(t => t.Y > 0).X;
        TriggerEndIndex = (int)triggerBuffers.Last(t => t.Y > 0).X;

        if (TriggerStartIndex < 0 || TriggerEndIndex < 0) return;

        ScatterPlotControl.GetOrAddScatterMarkers(
            0,
            "Triggers",
            triggerBuffers,
            color: Constants.Category10.GetColor(1));

        ScatterPlotControl.GetOrAddScatterMarkers(
            0,
            "X Machine Positions",
            xMachinePositionBuffers,
            color: Constants.Category10.GetColor(2));

        ScatterPlotControl.GetOrAddScatterMarkers(
            0,
            "Ecs",
            ecsBuffers,
            color: Constants.Category10.GetColor(3));

        var scatterMarkersPlot1StartTrigger = ScatterPlotControl.GetOrAddScatterMarkers(
            0,
            "Start Trigger",
            [new Point(TriggerStartIndex, triggerBuffers[TriggerStartIndex].Y)],
            Constants.Category10.GetColor(4),
            markerShape: MarkerShape.Asterisk);
        scatterMarkersPlot1StartTrigger.MarkerSize = 20;

        var scatterMarkersPlot1EndTrigger = ScatterPlotControl.GetOrAddScatterMarkers(
            0,
            "End Trigger",
            [new Point(TriggerEndIndex, triggerBuffers[TriggerEndIndex].Y)],
            Constants.Category10.GetColor(5),
            markerShape: MarkerShape.Asterisk);
        scatterMarkersPlot1EndTrigger.MarkerSize = 20;

        ScatterPlotControl.GetOrAddScatterMarkers(
            1,
            "X&Z Sync",
            [.. TraceBuffers.Select(t => new Point(t.XMachinePosition, t.Ecs))],
            color: Constants.Category10.GetColor(1));

        var scatterMarkersPlot2StartTrigger = ScatterPlotControl.GetOrAddScatterMarkers(
            1,
            "Start Trigger",
            [new Point(xMachinePositionBuffers[TriggerStartIndex].Y, ecsBuffers[TriggerStartIndex].Y)],
            Constants.Category10.GetColor(2),
            markerShape: MarkerShape.Asterisk);
        scatterMarkersPlot2StartTrigger.MarkerSize = 20;

        var scatterMarkersPlot2EndTrigger = ScatterPlotControl.GetOrAddScatterMarkers(
            1,
            "End Trigger",
            [new Point(xMachinePositionBuffers[TriggerEndIndex].Y, ecsBuffers[TriggerEndIndex].Y)],
            Constants.Category10.GetColor(3),
            markerShape: MarkerShape.Asterisk);
        scatterMarkersPlot2EndTrigger.MarkerSize = 20;

        ScatterPlotControl.AutoScaleRefresh();
    }

    public object ToFlatnessHtmlAnonymous() => new
    {
        SpectralDensity,
        TriggerStartIndex,
        TriggerEndIndex,
        TraceBuffers = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
        ChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        ChirpAODWaveformProfiles = new HtmlTable([.. ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
    };

    public BestFocusAndAstigmatismItemDto Clone() => new()
    {
        SpectralDensity = SpectralDensity,
        TraceBuffers = [.. TraceBuffers],
        ECSInterpolationBuffers = [.. ECSInterpolationBuffers],
        LineScanRate = LineScanRate,
        TriggerStartIndex = TriggerStartIndex,
        TriggerEndIndex = TriggerEndIndex,
        ChannelItems = ChannelItems.Select(t => t.Clone()).ToList().AsReadOnly(),
        GenerateChirpAODWaveformParam = GenerateChirpAODWaveformParam.Clone()
    };
}

public sealed partial class BestFocusAndAstigmatismChannelGroupItemDto : ObservableObject
{
    [ObservableProperty]
    private int _channelId;

    public string ChannelName => $"Channel{ChannelId}";

    [ObservableProperty]
    private IReadOnlyList<BestFocusAndAstigmatismChannelItemDto> _channelItems = [];
}

public sealed partial class BestFocusAndAstigmatismChannelItemDto : ObservableObject, ICloneable<BestFocusAndAstigmatismChannelItemDto>
{
    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private int _channelId;

    [ObservableProperty]
    private double _xBestFocusEcs;

    [ObservableProperty]
    private double _yBestFocusEcs;

    [ObservableProperty]
    private double _xBestQuality;

    [ObservableProperty]
    private double _yBestQuality;

    public double XYBestFocusOffsetEcs => XBestFocusEcs - YBestFocusEcs;

    /// <summary>
    /// ecs buffer 插值后数据，（xPixel，ecs）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    [ObservableProperty]
    private IReadOnlyList<(int XPixel, double ECS)> _eCSInterpolationBuffers = [];

    /// <summary>
    /// 算法结果坐标集合
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<Point> _positions = [];

    /// <summary>
    /// 算法结果拟合结果
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<Point> _xFitPositions = [];

    /// <summary>
    /// 算法结果拟合结果
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<Point> _yFitPositions = [];

    /// <summary>
    /// 算法结果拟合结果
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<Point> _grayFitPositions = [];

    [ObservableProperty]
    private IReadOnlyList<double> _xQualitys = [];

    [ObservableProperty]
    private IReadOnlyList<double> _yQualitys = [];

    [ObservableProperty]
    private IReadOnlyList<double> _grayValues = [];

    [ObservableProperty]
    private string _rawFilePath = string.Empty;

    [ObservableProperty]
    private string _linearFilePath = string.Empty;

    [ObservableProperty]
    private string _originFilePath = string.Empty;

    partial void OnPositionsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnXQualitysChanged(IReadOnlyList<double> value) => RefreshPlot();


    partial void OnXBestFocusEcsChanged(double value) => RefreshPlot();

    partial void OnYBestFocusEcsChanged(double value) => RefreshPlot();

    partial void OnYQualitysChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnGrayValuesChanged(IReadOnlyList<double> value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    public BestFocusAndAstigmatismChannelItemDto()
    {
        var customGrid = new CustomGrid();
        ScatterPlotControl.Configure(customGrid, 4,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 2));
                customGrid.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        ScatterPlotControl.SetTitle(0, "(X Focus)(Y: X Quality - X: X(pix))");
        ScatterPlotControl.SetTitle(1, "(Y Focus)(Y: Y Quality - X: Y(pix))");
        ScatterPlotControl.SetTitle(2, "(Light)(Y: GrayValue - X: X(pix))");
        ScatterPlotControl.SetTitle(3, "(Interpolation)(Y: ECS - X: X(pix))");
    }

#pragma warning restore CS0657
#pragma warning restore IDE0079

    private void RefreshPlot()
    {
        // if (Positions.Count == 0 || XFitPositions.Count == 0 || YFitPositions.Count == 0 || XQualitys.Count == 0 || YQualitys.Count == 0 || GrayValues.Count == 0 || ECSInterpolationBuffers.Count == 0
        //     || XBestFocusEcs == 0 || YBestFocusEcs == 0) return;
        if (Positions.Count == 0 || XQualitys.Count == 0 || YQualitys.Count == 0 || GrayValues.Count == 0 || ECSInterpolationBuffers.Count == 0
            || XBestFocusEcs == 0 || YBestFocusEcs == 0) return;

        #region XBestFocus

        var xQualityPoints = Positions.Select((t, i) => new Point(t.X, XQualitys[i])).ToArray();
        ScatterPlotControl.GetOrAddScatterMarkers(
            0,
            "X Quality",
            xQualityPoints);
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

        #endregion

        #region YBestFocus

        var yQualityPoints = Positions.Select((t, i) => new Point(t.X, YQualitys[i])).ToArray();
        ScatterPlotControl.GetOrAddScatterMarkers(
            1,
            "Y Quality",
            [.. yQualityPoints]);
        ScatterPlotControl.GetOrAddScatterLine(
            1,
            "Fit Points",
            YFitPositions);

        if (YFitPositions.Count != 0)
        {
            var scatterMarkersY = ScatterPlotControl.GetOrAddScatterMarkers(
                1,
                "Y Best Focus",
                [YFitPositions.Maxima(t => t.Y).First()],
                color: Colors.Red,
                markerShape: MarkerShape.Asterisk);
            scatterMarkersY.MarkerSize = 25;
        }

        #endregion

        #region Light

        var lightPoints = Positions.Select((t, i) => new Point(t.X, GrayValues[i])).ToArray();
        ScatterPlotControl.GetOrAddScatterMarkers(
            2,
            "Gray Value",
            lightPoints);
        ScatterPlotControl.GetOrAddScatterLine(
            2,
            "Fit Points",
            GrayFitPositions);
        if (GrayFitPositions.Count != 0)
        {
            var scatterMarkersGray = ScatterPlotControl.GetOrAddScatterMarkers(
                2,
                "Gray Value ",
                [GrayFitPositions.Maxima(t => t.Y).First()],
                color: Colors.Red,
                markerShape: MarkerShape.Asterisk);
            scatterMarkersGray.MarkerSize = 25;
        }

        #endregion

        #region Interpolation

        var xBestFocusPoint = ECSInterpolationBuffers.Where(t => Math.Abs(t.ECS - XBestFocusEcs) < 1).First();
        ScatterPlotControl.GetOrAddScatterMarkers(
            3,
            "Interpolation",
            [.. ECSInterpolationBuffers.Select(t => new Point(t.XPixel, t.ECS))]);
        var interpolationScatterMarkersX = ScatterPlotControl.GetOrAddScatterMarkers(
            3,
            "X Best Focus",
            [new Point(xBestFocusPoint.XPixel, xBestFocusPoint.ECS)],
            color: Colors.Green,
            markerShape: MarkerShape.Asterisk);
        interpolationScatterMarkersX.MarkerSize = 25;

        var yBestFocusPoint = ECSInterpolationBuffers.Where(t => Math.Abs(t.ECS - YBestFocusEcs) < 1).First();
        var interpolationScatterMarkersY = ScatterPlotControl.GetOrAddScatterMarkers(
            3,
            "Y Best Focus",
            [new Point(yBestFocusPoint.XPixel, yBestFocusPoint.ECS)],
            color: Colors.Red,
            markerShape: MarkerShape.Asterisk);
        interpolationScatterMarkersY.MarkerSize = 25;

        #endregion

        ScatterPlotControl.AutoScaleRefresh();

        return;

        // 带状图均值算法
        (double x, double Quality) GetBestFocus(Point[] points)
        {
            Guard.IsNotEmpty(points);

            if (points.Length == 1)
                return (points[0].X, 0);

            var yMean = points.Average(p => p.Y);

            // 找到最接近Y均值的点
            // 如果有多个点具有相同的最小距离，计算它们的X坐标平均值
            var minDistance = double.MaxValue;
            var closestPoints = new List<Point>();

            foreach (var point in points)
            {
                var distance = Math.Abs(point.Y - yMean);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoints.Clear();
                    closestPoints.Add(point);
                }
                else if (distance - minDistance < Net.Utilities.Models.Constants.Tolerance)
                {
                    closestPoints.Add(point);
                }
            }

            // 计算最接近点的X坐标平均值
            var bestX = closestPoints.Average(p => p.X);

            return (bestX, yMean);
        }
    }

    public object ToFlatnessHtmlAnonymous() => new
    {
        PmtId,
        XBestFocusEcs,
        YBestFocusEcs,
        XBestQuality,
        YBestQuality,
        RawFilePath,
        Plot = new HtmlTab(new
        {
            XQualitysAnalysis = ScatterPlotControl.GetHtmlPlot2DLinesChart(0),
            YQualitysAnalysis = ScatterPlotControl.GetHtmlPlot2DLinesChart(1),
            LightAnalysis = ScatterPlotControl.GetHtmlPlot2DLinesChart(2),
            Interpolation = ScatterPlotControl.GetHtmlPlot2DLinesChart(3),
            OriginImage = new HtmlImage(OriginFilePath)
            // LinearImage = new HtmlImage(File.Exists(LinearFilePath) ? LinearFilePath : OriginFilePath)
        })
    };

    public BestFocusAndAstigmatismChannelItemDto Clone() => new()
    {
        PmtId = PmtId,
        ChannelId = ChannelId,
        XBestFocusEcs = XBestFocusEcs,
        YBestFocusEcs = YBestFocusEcs,
        ECSInterpolationBuffers = ECSInterpolationBuffers.ToList().AsReadOnly(),
        Positions = Positions.ToList().AsReadOnly(),
        XFitPositions = XFitPositions.ToList().AsReadOnly(),
        YFitPositions = YFitPositions.ToList().AsReadOnly(),
        XQualitys = XQualitys.ToList().AsReadOnly(),
        YQualitys = YQualitys.ToList().AsReadOnly(),
        GrayValues = GrayValues.ToList().AsReadOnly(),
        RawFilePath = RawFilePath,
        OriginFilePath = OriginFilePath,
        LinearFilePath = LinearFilePath
    };
}