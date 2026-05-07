using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Optics.Relay;

[CacheVersion("1.0.0")]
public sealed partial class OpticsRelayDTO : CalibrationDTOBase, ICloneable<OpticsRelayDTO>, IAdaptTo<CalibrationOpticsRelay>
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

    partial void OnXZItemsChanged(IReadOnlyList<OpticsRelayDTOXZItem>? oldValue, IReadOnlyList<OpticsRelayDTOXZItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshXZPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshXZPlot();
    }

    partial void OnXZSlopeChanged(double value) => RefreshXZPlot();

    partial void OnXZInterceptChanged(double value) => RefreshXZPlot();

    partial void OnXZRSquaredChanged(double value) => RefreshXZPlot();

    partial void OnXZFitRelayPointsChanged(IReadOnlyList<Point> value) => RefreshXZPlot();

    partial void OnRelayMotorRatioChanged(double value)
    {
        RefreshPlot();
        RefreshXZPlot();
    }

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsRelayDTO()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Z Sync Quality(Y: Quality - X: ECS)");
        ScatterPlotControl.SetTitle(1, "Z Sync Relay(Y: ECS - X: mm)");

        XZScatterPlotControl.SetTitle("X/Z Sync Relay(Y: ECS - X: mm)");
    }

    private void RefreshPlot()
    {
        try
        {
            var qualityScatterLines = ScatterPlotControl.GetOrAddScatterLines(0, Items.Count);
            var relayScatterLines = ScatterPlotControl.GetOrAddScatterLines(1, 2);

            var isNeedRefreshes = new bool[Items.Count];

            foreach (var (index, item) in Items.Index())
            {
                if (item.Qualitys.Count <= 0) continue;

                qualityScatterLines[index].Update(
                    $"{item.RelayMotorAbsoluteValue:0.###}(mm)",
                    [.. item.Qualitys.Select(t => new Point(t.ECS, t.Quality))],
                    Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));

                item.MaxItem = item.Qualitys.Maxima(t => t.Quality).First();

                isNeedRefreshes[index] = true;
            }

            if (isNeedRefreshes.All(b => b))
            {
                relayScatterLines[0].Update(
                    string.Empty,
                    [.. Items.Select(t => new Point(t.RelayMotorAbsoluteValue, Guard.IsNotNullAndReturn(t.MaxItem).ECS))],
                    Constants.Category10.GetColor(0));
            }

            relayScatterLines[1].Update(
                FitRelayPoints.Count > 0 ? $"{PolynomialCurve.ToString1(Slope, Intercept, RSquared, "0.######")}, Ratio = {RelayMotorRatio:0.###}" : string.Empty,
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
            var relayScatterLines = XZScatterPlotControl.GetOrAddScatterLines(3);

            relayScatterLines[0].Update(
                XZItems.Count > 0 ? "X Strehl Ratio" : string.Empty,
                [.. XZItems.Select(t => new Point(t.RelayMotorAbsoluteValue, t.BestFocus.BestXStrehlRatioECS))],
                Constants.Category10.GetColor(0));
            relayScatterLines[1].Update(
                XZItems.Count > 0 ? "Y Strehl Ratio" : string.Empty,
                [.. XZItems.Select(t => new Point(t.RelayMotorAbsoluteValue, t.BestFocus.BestYStrehlRatioECS))],
                Constants.Category10.GetColor(1));

            relayScatterLines[2].Update(
                XZFitRelayPoints.Count > 0 ? $"{PolynomialCurve.ToString1(XZSlope, XZIntercept, XZRSquared, "0.######")}, Ratio = {RelayMotorRatio:0.###}" : string.Empty,
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
    private BestFocus _bestFocus = new();

    public OpticsRelayDTOXZItem Clone() => new()
    {
        RelayMotorAbsoluteValue = RelayMotorAbsoluteValue,
        BestFocus = BestFocus.Clone()
    };
}