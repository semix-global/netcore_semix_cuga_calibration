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
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _fitRelayPoints = [];

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

    partial void OnRelayMotorRatioChanged(double value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsRelayDTO()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Quality(Y: Quality - X: ECS)");
        ScatterPlotControl.SetTitle(1, "Relay(Y: ECS - X: mm)");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);
            ScatterPlotControl.Clear(1);

            var isNeedRefreshes = new bool[Items.Count];

            foreach (var (index, item) in Items.Index())
            {
                if (item.Qualitys.Count <= 0) continue;

                ScatterPlotControl.GetOrAddScatterLine(
                    0,
                    $"{item.RelayMotorAbsoluteValue:0.###}mm",
                    [.. item.Qualitys.Select(t => new Point(t.ECS, t.Quality))],
                    index,
                    new Range(0, Items.Count - 1));

                item.MaxItem = item.Qualitys.Maxima(t => t.Quality).First();

                isNeedRefreshes[index] = true;
            }

            if (isNeedRefreshes.All(b => b))
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    "Relay",
                    [.. Items.Select(t => new Point(t.RelayMotorAbsoluteValue, GuardUtils.IsNotNullAndReturn(t.MaxItem).ECS))],
                    Constants.Category10.GetColor(0));
            }

            if (FitRelayPoints.Count > 0)
                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"Fit Curve: y = {Slope:0.######}x + {Intercept:0.######} r^2 = {RSquared:0.######}), Ratio = {RelayMotorRatio:0.###}",
                    FitRelayPoints,
                    Constants.Category10.GetColor(1));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public OpticsRelayDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitRelayPoints = [.. FitRelayPoints],
        RelayMotorRatio = RelayMotorRatio,
        MinRelayMotorAbsoluteValue = MinRelayMotorAbsoluteValue,
        MaxRelayMotorAbsoluteValue = MinRelayMotorAbsoluteValue,
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
        MinRelayMotorAbsoluteValue = MinRelayMotorAbsoluteValue,
        MaxRelayMotorAbsoluteValue = MinRelayMotorAbsoluteValue,
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