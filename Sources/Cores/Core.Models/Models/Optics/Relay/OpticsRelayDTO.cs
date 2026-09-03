using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.Extensions;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Net.Utilities.ScottPlot.Helper;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Optics.Relay;

[CacheVersion("1.0.0")]
public sealed partial class OpticsRelayDTO : CalibrationDTOBase<OpticsRelayDTO>, IAdaptTo<CalibrationOpticsRelay>
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<OpticsRelayDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OpticsRelayDTOXZItem> XZItems { get; set; } = [];

    [ObservableProperty]
    public partial double Slope { get; set; }

    [ObservableProperty]
    public partial double Intercept { get; set; }

    [ObservableProperty]
    public partial double RSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> FitRelayPoints { get; set; } = [];

    [ObservableProperty]
    public partial double XZSlope { get; set; }

    [ObservableProperty]
    public partial double XZIntercept { get; set; }

    [ObservableProperty]
    public partial double XZRSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> XZFitRelayPoints { get; set; } = [];

    [ObservableProperty]
    public partial double RelayMotorRatio { get; set; }

    [ObservableProperty]
    public partial double MinRelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double MaxRelayMotorAbsoluteValue { get; set; }

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource XZPlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<OpticsRelayDTOItem> oldValue, IReadOnlyList<OpticsRelayDTOItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

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

    partial void OnXZItemsChanged(IReadOnlyList<OpticsRelayDTOXZItem> oldValue, IReadOnlyList<OpticsRelayDTOXZItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

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
        PlotDataSource.Configure(new Columns(), 2);

        PlotDataSource.SetTitle(0, "Z Sync Quality(Y: Quality - X: ECS)");
        PlotDataSource.SetTitle(1, "Z Sync Relay(Y: ECS - X: mm)");

        XZPlotDataSource.SetTitle("X/Z Sync Relay(Y: ECS - X: mm)");
    }

    private void RefreshPlot()
    {
        try
        {
            var qualityScatterLines = PlotDataSource.GetOrAddScatterLines(0, Items.Count);
            var relayScatterLines = PlotDataSource.GetOrAddScatterLines(1, 2);

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
            PlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshXZPlot()
    {
        try
        {
            var relayScatterLines = XZPlotDataSource.GetOrAddScatterLines(3);

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
            XZPlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override OpticsRelayDTO Clone() => new()
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
    public partial double RelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Item> Qualitys { get; set; } = [];

    [ObservableProperty]
    public partial Item? MaxItem { get; set; }

    public OpticsRelayDTOItem Clone() => new()
    {
        RelayMotorAbsoluteValue = RelayMotorAbsoluteValue,
        Qualitys = [.. Qualitys.Select(t => t.Clone())],
        MaxItem = MaxItem?.Clone()
    };

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        public partial double ECS { get; set; }

        [ObservableProperty]
        public partial double Quality { get; set; }

        [ObservableProperty]
        public partial string ImageFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string RawImageFilePath { get; set; } = string.Empty;

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
    public partial double RelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial BestFocus BestFocus { get; set; } = new();

    public OpticsRelayDTOXZItem Clone() => new()
    {
        RelayMotorAbsoluteValue = RelayMotorAbsoluteValue,
        BestFocus = BestFocus.Clone()
    };
}