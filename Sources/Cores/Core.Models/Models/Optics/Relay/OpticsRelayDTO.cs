using System.ComponentModel;
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

namespace Core.Models.Models.Optics.Relay;

public sealed partial class OpticsRelayDTO : CalibrationDtoBase, ICloneable<OpticsRelayDTO>, IAdaptTo<CalibrationOpticsRelay>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private IReadOnlyList<OpticsRelayDTOItem> _items = [];

    [ObservableProperty]
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _fitRelayPoints = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

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

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsRelayDTO()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Quality(Y: Quality - X: ECS)");
        ScatterPlotControl.SetTitle(1, "Relay(Y: mm - X: ECS)");
    }

    private void RefreshPlot()
    {
        ScatterPlotControl.Clear(0);
        ScatterPlotControl.Clear(1);

        var isNeedRefreshes = new bool[Items.Count];

        foreach (var (index, item) in Items.Index())
        {
            if (item.Qualitys.Count <= 0) continue;

            ScatterPlotControl.GetOrAddScatterLine(
                0,
                $"{item.RelayMotorAbsoluteValue:0.###}",
                [.. item.Qualitys.Select(t => new Point(t.ECS, t.Quality))]);

            item.MaxItem = item.Qualitys.Maxima(t => t.Quality).First();

            isNeedRefreshes[index] = true;
        }

        if (isNeedRefreshes.All(b => b))
        {
            ScatterPlotControl.GetOrAddScatterLine(
                1,
                "Relay",
                [.. Items.Select(t => new Point(t.RelayMotorAbsoluteValue, GuardUtils.IsNotNullAndReturn(t.MaxItem).ECS))]);
        }

        if (FitRelayPoints.Count > 0)
            ScatterPlotControl.GetOrAddScatterLine(
                1,
                $"Fit Curve: y = {Slope:0.######}x + {Intercept:0.######} r^2 = {RSquared:0.######})",
                FitRelayPoints);

        ScatterPlotControl.AutoScaleRefresh();
    }

    #region Mapper

    public OpticsRelayDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Items = [..Items.Select(t => t.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitRelayPoints = [.. FitRelayPoints],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationOpticsRelay AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        Slope = Slope,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class OpticsRelayDTOItem : CalibrationCacheBase, ICloneable<OpticsRelayDTOItem>
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
        MaxItem = MaxItem?.Clone(),
        Id = Id,
        Expiration = Expiration
    };

    public sealed class Item : ICloneable<Item>
    {
        public double ECS { get; init; }

        public double Quality { get; set; }

        public string ImageFilePath { get; set; } = string.Empty;

        public Item Clone() => new()
        {
            ECS = ECS,
            Quality = Quality,
            ImageFilePath = ImageFilePath
        };
    }
}