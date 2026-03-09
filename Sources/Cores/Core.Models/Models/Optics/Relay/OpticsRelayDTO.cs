using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.ScottPlot.WPF.Plottables;
using ScottPlot;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Optics.Relay;

public sealed partial class OpticsRelayDTO : CalibrationDtoBase, ICloneable<OpticsRelayDTO>, IAdaptTo<CalibrationOpticsRelay>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private IReadOnlyList<OpticsRelayDTOItem> _items = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsRelayDTOXZItem> _xZItems = [];

    [ObservableProperty]
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _fitRelayPoints = [];

    [ObservableProperty]
    private double _xZSlope;

    [ObservableProperty]
    private double _xZIntercept;

    [ObservableProperty]
    private double _xZRSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _xZFitRelayPoints = [];

    [ObservableProperty]
    private double _relayMotorRatio;

    [ObservableProperty]
    private double _minRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _maxRelayMotorAbsoluteValue;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _xZScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<OpticsRelayDTOItem>? oldValue, IReadOnlyList<OpticsRelayDTOItem> newValue)
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

    partial void OnSlopeChanged(double value) => RefreshPlot();

    partial void OnInterceptChanged(double value) => RefreshPlot();

    partial void OnRSquaredChanged(double value) => RefreshPlot();

    partial void OnFitRelayPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnRelayMotorRatioChanged(double value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsRelayDTO()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Quality(Y: Quality - X: ECS)");
        ScatterPlotControl.SetTitle(1, "Z Relay(Y: ECS - X: mm)");

        XZScatterPlotControl.SetTitle("X/Z Relay(Y: ECS - X: mm)");
    }

    private void RefreshPlot()
    {
        try
        {
            var qualityScatterLines = ScatterPlotControl.GetOrAddScatterLines(0, 1);
            var relayScatterLines = ScatterPlotControl.GetOrAddScatterLines(1, 2);

            var isNeedRefreshes = new bool[Items.Count];

            foreach (var (index, item) in Items.Index())
            {
                if (item.Qualitys.Count <= 0) continue;

                qualityScatterLines[0].Update(
                    $"{item.RelayMotorAbsoluteValue:0.###}(mm)",
                    [.. item.Qualitys.Select(t => new Point(t.ECS, t.Quality))],
                    Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));

                item.MaxItem = item.Qualitys.Maxima(t => t.Quality).First();

                isNeedRefreshes[index] = true;
            }

            if (isNeedRefreshes.All(b => b))
            {
                relayScatterLines[0].Update(
                    "Relay",
                    [.. Items.Select(t => new Point(t.RelayMotorAbsoluteValue, GuardUtils.IsNotNullAndReturn(t.MaxItem).ECS))],
                    Constants.Category10.GetColor(0));
            }

            relayScatterLines[1].Update(
                $"{PolynomialCurve.ToString1(Slope, Intercept, RSquared, "0.######")}, Ratio = {RelayMotorRatio:0.###}",
                FitRelayPoints,
                Constants.Category10.GetColor(1));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    private void RefreshXZPlot()
    {
        try
        {
            var relayScatterLines = XZScatterPlotControl.GetOrAddScatterLines(4);

            relayScatterLines[0].Update(
                "X Strehl Ratio",
                [.. XZItems.Select(t => new Point(t.RelayMotorAbsoluteValue, t.BestXStrehlRatioECS))],
                Constants.Category10.GetColor(0));
            relayScatterLines[1].Update(
                "Y Strehl Ratio",
                [.. XZItems.Select(t => new Point(t.RelayMotorAbsoluteValue, t.BestYStrehlRatioECS))],
                Constants.Category10.GetColor(1));
            relayScatterLines[2].Update(
                "Gray",
                [.. XZItems.Select(t => new Point(t.RelayMotorAbsoluteValue, t.BestGrayECS))],
                Constants.Category10.GetColor(2));

            relayScatterLines[3].Update(
                $"{PolynomialCurve.ToString1(XZSlope, XZIntercept, XZRSquared, "0.######")}, Ratio = {RelayMotorRatio:0.###}",
                XZFitRelayPoints,
                Constants.Category10.GetColor(3));
        }
        finally
        {
            XZScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public OpticsRelayDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        XZItems = [.. XZItems.Select(t => t.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitRelayPoints = [.. FitRelayPoints],
        XZSlope = XZSlope,
        XZIntercept = XZIntercept,
        XZRSquared = XZRSquared,
        XZFitRelayPoints = [.. XZFitRelayPoints],
        RelayMotorRatio = RelayMotorRatio,
        MinRelayMotorAbsoluteValue = MinRelayMotorAbsoluteValue,
        MaxRelayMotorAbsoluteValue = MaxRelayMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationOpticsRelay AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        Slope = XZSlope,
        MinRelayMotorAbsoluteValue = MinRelayMotorAbsoluteValue,
        MaxRelayMotorAbsoluteValue = MaxRelayMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class OpticsRelayDTOItem : ObservableObject, ICloneable<OpticsRelayDTOItem>
{
    [ObservableProperty]
    private double _relayMotorAbsoluteValue;

    [ObservableProperty]
    private IReadOnlyList<Item> _qualitys = [];

    [ObservableProperty]
    private Item? _maxItem;

    public OpticsRelayDTOItem Clone() => new()
    {
        RelayMotorAbsoluteValue = RelayMotorAbsoluteValue,
        Qualitys = [.. Qualitys.Select(t => t.Clone())],
        MaxItem = MaxItem?.Clone()
    };

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        private double _eCS;

        [ObservableProperty]
        private double _quality;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        public Item Clone() => new()
        {
            ECS = ECS,
            Quality = Quality,
            ImageFilePath = ImageFilePath,
            RawImageFilePath = RawImageFilePath
        };
    }
}

public sealed partial class OpticsRelayDTOXZItem : ObservableObject, ICloneable<OpticsRelayDTOXZItem>
{
    [ObservableProperty]
    private double _relayMotorAbsoluteValue;

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<Point> _xStrehlRatioPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _xStrehlRatioFitPoints = [];

    [ObservableProperty]
    private Point _bestXStrehlRatioPoint;

    [ObservableProperty]
    private double _bestXStrehlRatioECS;

    [ObservableProperty]
    private IReadOnlyList<Point> _yStrehlRatioPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _yStrehlRatioFitPoints = [];

    [ObservableProperty]
    private Point _bestYStrehlRatioPoint;

    [ObservableProperty]
    private double _bestYStrehlRatioECS;

    [ObservableProperty]
    private IReadOnlyList<Point> _grayPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _grayFitPoints = [];

    [ObservableProperty]
    private Point _bestGrayPoint;

    [ObservableProperty]
    private double _bestGrayECS;

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestXStrehlRatioXPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestXStrehlRatioXPSFFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestXStrehlRatioYPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestXStrehlRatioYPSFFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestYStrehlRatioXPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestYStrehlRatioXPSFFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestYStrehlRatioYPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestYStrehlRatioYPSFFitPoints = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _xStrehlRatioScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _yStrehlRatioScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _grayScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    public OpticsRelayDTOXZItem()
    {
        XStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        XStrehlRatioScatterPlotControl.SetTitle(0, "X Strehl Ratio(Y: Strehl Ratio - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(1, "X PSF(Y: Gray - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(2, "Y PSF(Y: Gray - X: px)");

        YStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        YStrehlRatioScatterPlotControl.SetTitle(0, "Y Strehl Ratio(Y: Strehl Ratio - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(1, "X PSF(Y: Gray - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(2, "Y PSF(Y: Gray - X: px)");

        GrayScatterPlotControl.SetTitle("Gray(Y: Gray - X: px)");
    }

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnXStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnXStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestXStrehlRatioPointChanged(Point value) => RefreshPlot();

    partial void OnBestXStrehlRatioECSChanged(double value) => RefreshPlot();

    partial void OnYStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnYStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestYStrehlRatioPointChanged(Point value) => RefreshPlot();

    partial void OnBestYStrehlRatioECSChanged(double value) => RefreshPlot();

    partial void OnGrayPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnGrayFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestGrayPointChanged(Point value) => RefreshPlot();

    partial void OnBestGrayECSChanged(double value) => RefreshPlot();

    partial void OnBestXStrehlRatioXPSFPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnBestXStrehlRatioXPSFFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestXStrehlRatioYPSFPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnBestXStrehlRatioYPSFFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestYStrehlRatioXPSFPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnBestYStrehlRatioXPSFFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestYStrehlRatioYPSFPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnBestYStrehlRatioYPSFFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    private void RefreshPlot()
    {
        try
        {
            RefreshBase(XStrehlRatioScatterPlotControl, XStrehlRatioPoints, XStrehlRatioFitPoints, BestXStrehlRatioPoint, BestXStrehlRatioECS);
            Refresh(XStrehlRatioScatterPlotControl, 1, BestXStrehlRatioXPSFPoints, BestXStrehlRatioXPSFFitPoints);
            Refresh(XStrehlRatioScatterPlotControl, 2, BestXStrehlRatioYPSFPoints, BestXStrehlRatioYPSFFitPoints);

            RefreshBase(YStrehlRatioScatterPlotControl, YStrehlRatioPoints, YStrehlRatioFitPoints, BestYStrehlRatioPoint, BestYStrehlRatioECS);
            Refresh(YStrehlRatioScatterPlotControl, 1, BestYStrehlRatioXPSFPoints, BestYStrehlRatioXPSFFitPoints);
            Refresh(YStrehlRatioScatterPlotControl, 2, BestYStrehlRatioYPSFPoints, BestYStrehlRatioYPSFFitPoints);

            RefreshBase(GrayScatterPlotControl, GrayPoints, GrayFitPoints, BestGrayPoint, BestGrayECS);
        }
        finally
        {
            XStrehlRatioScatterPlotControl.AutoScaleRefresh();
            YStrehlRatioScatterPlotControl.AutoScaleRefresh();
            GrayScatterPlotControl.AutoScaleRefresh();

            XStrehlRatioScatterPlotControl.Plot.Axes.SetLimitsY(0.05d, 0.3d);
            YStrehlRatioScatterPlotControl.Plot.Axes.SetLimitsY(0.05d, 0.3d);

            foreach (var plot in XStrehlRatioScatterPlotControl.Multiplot.GetPlots().Skip(1).Concat(YStrehlRatioScatterPlotControl.Multiplot.GetPlots().Skip(1)))
            {
                var fitPoints = plot.GetPlottables().OfType<ScatterLine>().SingleOrDefault()?.ScatterSourcePoints.Points ?? [];

                if (fitPoints.Count > 0)
                {
                    var xes = fitPoints.Select(t => t.X).ToArray();

                    var max = xes.Max();
                    var min = xes.Min();
                    var length = max - min;
                    var middle = (max + min) / 2d;

                    plot.Axes.SetLimitsX(middle - length / 8d, middle + length / 8d);
                }
            }
        }

        return;

        void RefreshBase(IScatterPlotControl scatterPlotControl, IReadOnlyList<Point> points, IReadOnlyList<Point> fitPoints, Point bestPoint, double bestECS)
        {
            var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkerses(0, 2);

            scatterMarkers[0].Update(string.Empty, points, Colors.Gray, MarkerShape.FilledCircle);
            scatterMarkers[1].Update($"Best ECS {bestECS:0.###} ECS", [bestPoint], Colors.Red, MarkerShape.FilledSquare);
            scatterMarkers[1].MarkerSize = 20;

            var scatterLines = scatterPlotControl.GetOrAddScatterLines(0, 1);
            scatterLines[0].Update(string.Empty, fitPoints, Colors.Green);
            scatterLines[0].LineWidth = 2;
            scatterLines[0].MarkerSize = 5;
            scatterLines[0].MarkerColor = Colors.DarkGreen;
        }

        void Refresh(IScatterPlotControl scatterPlotControl, int plotIndex, IReadOnlyList<IReadOnlyList<Point>> points, IReadOnlyList<Point> fitPoints)
        {
            scatterPlotControl.Clear(plotIndex);

            var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkerses(plotIndex, points.Count);

            foreach (var (index, temp) in points.Index())
            {
                scatterMarkers[index].Update(string.Empty, temp, Colors.Gray, MarkerShape.FilledCircle);
            }

            var scatterLines = scatterPlotControl.GetOrAddScatterLines(plotIndex, 1);
            scatterLines[0].Update(string.Empty, fitPoints, Colors.Green);
            scatterLines[0].LineWidth = 2;
            scatterLines[0].MarkerSize = 5;
            scatterLines[0].MarkerColor = Colors.DarkGreen;
        }
    }

    public OpticsRelayDTOXZItem Clone() => new()
    {
        RelayMotorAbsoluteValue = RelayMotorAbsoluteValue,
        ImageFilePath = ImageFilePath,
        RawImageFilePath = RawImageFilePath,
        XStrehlRatioPoints = [.. XStrehlRatioPoints],
        XStrehlRatioFitPoints = [.. XStrehlRatioFitPoints],
        BestXStrehlRatioPoint = BestXStrehlRatioPoint,
        BestXStrehlRatioECS = BestXStrehlRatioECS,
        YStrehlRatioPoints = [.. YStrehlRatioPoints],
        YStrehlRatioFitPoints = [.. YStrehlRatioFitPoints],
        BestYStrehlRatioPoint = BestYStrehlRatioPoint,
        BestYStrehlRatioECS = BestYStrehlRatioECS,
        GrayPoints = [.. GrayPoints],
        GrayFitPoints = [.. GrayFitPoints],
        BestGrayPoint = BestGrayPoint,
        BestGrayECS = BestGrayECS,
        BestXStrehlRatioXPSFPoints = [.. BestXStrehlRatioXPSFPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestXStrehlRatioXPSFFitPoints = [.. BestXStrehlRatioXPSFFitPoints],
        BestXStrehlRatioYPSFPoints = [.. BestXStrehlRatioYPSFPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestXStrehlRatioYPSFFitPoints = [.. BestXStrehlRatioYPSFFitPoints],
        BestYStrehlRatioXPSFPoints = [.. BestYStrehlRatioXPSFPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestYStrehlRatioXPSFFitPoints = [.. BestYStrehlRatioXPSFFitPoints],
        BestYStrehlRatioYPSFPoints = [.. BestYStrehlRatioYPSFPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestYStrehlRatioYPSFFitPoints = [.. BestYStrehlRatioYPSFFitPoints]
    };
}