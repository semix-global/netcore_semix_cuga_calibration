using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;

namespace Core.Models.Models.CIB.MMD;

public sealed partial class CIBMMDDTO : CalibrationDtoBase, ICloneable<CIBMMDDTO>, IAdaptTo<CalibrationLaserCIBMMDItem>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<CIBMMDGainRelationshipDTO> _gainRelationships = [];

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDTOItem> _items = [];

    [ObservableProperty]
    private double _gainRSquared;

    [ObservableProperty]
    private double _gainResidual;

    [ObservableProperty]
    private IReadOnlyList<Point> _gainPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _originLogGainPoints = [];

    [ObservableProperty]
    private double _logGainA1;

    [ObservableProperty]
    private double _logGainA2;

    [ObservableProperty]
    private double _logGainX0;

    [ObservableProperty]
    private double _logGainDx;

    [ObservableProperty]
    private double _logGainRSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _fitLogGainPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _logGainMul128U12BitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _gainS16BitPoints = [];

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

    partial void OnItemsChanged(IReadOnlyList<CIBMMDDTOItem>? oldValue, IReadOnlyList<CIBMMDDTOItem> newValue)
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

    partial void OnGainRSquaredChanged(double value) => RefreshPlot();

    partial void OnGainResidualChanged(double value) => RefreshPlot();

    partial void OnGainPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnOriginLogGainPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnLogGainA1Changed(double value) => RefreshPlot();

    partial void OnLogGainA2Changed(double value) => RefreshPlot();

    partial void OnLogGainX0Changed(double value) => RefreshPlot();

    partial void OnLogGainDxChanged(double value) => RefreshPlot();

    partial void OnLogGainRSquaredChanged(double value) => RefreshPlot();

    partial void OnFitLogGainPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnLogGainMul128U12BitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnGainS16BitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBMMDDTO()
    {
        var customGrid = new CustomGrid();
        ScatterPlotControl.Configure(customGrid, 6,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 3, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 3, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 3, 2));
                customGrid.Set(plots[3], new GridCell(1, 1, 3, 2));
                customGrid.Set(plots[4], new GridCell(2, 0, 3, 2));
                customGrid.Set(plots[5], new GridCell(2, 1, 3, 2));
            });

        ScatterPlotControl.SetTitle(0, "Origin(Y: mW - X: Coefficient)");
        ScatterPlotControl.SetTitle(1, "Origin(Y: PMT Value(DC) - X: V)");
        ScatterPlotControl.SetTitle(2, "Gain(Y: Gain - X: V)");
        ScatterPlotControl.SetTitle(3, "LogGain(Y: LogGain - X: V)");
        ScatterPlotControl.SetTitle(4, "LogGain * 128 U12Bit(Y: LogGain * 128 U12Bit - X: Sense U14Bit)");
        ScatterPlotControl.SetTitle(5, "Gain S16Bit(Y: Gain S16Bit - X: LogGain * 128 U12Bit )");
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterMarkerses = ScatterPlotControl.GetOrAddScatterMarkerses(0, Items.Count > 0 ? 1 : 0);
            scatterMarkerses.ElementAtOrDefault(0)?.Update(
                string.Empty,
                [.. Items.Select(t => new Point(t.Coefficient, t.MeasurePower))],
                Constants.Category10.GetColor(0));

            var temps = (from item in Items
                         let itemItems = item.Items.Where(t => double.IsNaN(t.PMTValue) == false).ToArray()
                         where itemItems.Length > 0
                         select new
                         {
                             LegendText = $"{item.Coefficient:0.###}",
                             Points = itemItems.Select(t => new Point(t.Gain, t.PMTValue)).ToArray()
                         }
                ).ToArray();

            var scatterLines = ScatterPlotControl.GetOrAddScatterLines(1, temps.Length);

            foreach (var (index, temp) in temps.Index())
            {
                scatterLines[index].Update(temp.LegendText, temp.Points, Constants.Category10.GetColor(index));
            }

            scatterLines = ScatterPlotControl.GetOrAddScatterLines(2, GainPoints.Count > 0 ? 1 : 0);
            scatterLines.ElementAtOrDefault(0)?.Update(
                $"Gain r^2: {GainRSquared:0.000#} Gain Residual: {GainResidual:0.###}",
                GainPoints,
                Constants.Category10.GetColor(0));

            scatterLines = ScatterPlotControl.GetOrAddScatterLines(3, (OriginLogGainPoints.Count > 0 ? 1 : 0) + (FitLogGainPoints.Count > 0 ? 1 : 0));
            scatterLines.ElementAtOrDefault(0)?.Update(
                $"Origin Curve Gain r^2: {GainRSquared:0.000#} Gain Residual: {GainResidual:0.###}",
                OriginLogGainPoints,
                Constants.Category10.GetColor(0));
            scatterLines.ElementAtOrDefault(1)?.Update(
                BoltzmannCurve.ToString(LogGainA1, LogGainA2, LogGainX0, LogGainDx, LogGainRSquared, "0.######"),
                FitLogGainPoints,
                Constants.Category10.GetColor(1));

            scatterLines = ScatterPlotControl.GetOrAddScatterLines(4, LogGainMul128U12BitPoints.Count > 0 ? 1 : 0);
            scatterLines.ElementAtOrDefault(0)?.Update(
                $"Gain r^2: {GainRSquared:0.000#} Gain Residual: {GainResidual:0.###}",
                LogGainMul128U12BitPoints,
                Constants.Category10.GetColor(0));

            scatterLines = ScatterPlotControl.GetOrAddScatterLines(5, GainS16BitPoints.Count > 0 ? 1 : 0);
            scatterLines.ElementAtOrDefault(0)?.Update(
                $"Gain r^2: {GainRSquared:0.000#} Gain Residual: {GainResidual:0.###}",
                GainS16BitPoints,
                Constants.Category10.GetColor(0));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public CIBMMDDTO Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        GainRelationships = [.. GainRelationships.Select(t => t.Clone())],
        Items = [.. Items.Select(t => t.Clone())],
        GainRSquared = GainRSquared,
        GainResidual = GainResidual,
        GainPoints = [.. GainPoints],
        OriginLogGainPoints = [.. OriginLogGainPoints],
        LogGainA1 = LogGainA1,
        LogGainA2 = LogGainA2,
        LogGainX0 = LogGainX0,
        LogGainDx = LogGainDx,
        LogGainRSquared = LogGainRSquared,
        FitLogGainPoints = [.. FitLogGainPoints],
        LogGainMul128U12BitPoints = [.. LogGainMul128U12BitPoints],
        GainS16BitPoints = [.. GainS16BitPoints],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserCIBMMDItem AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        ChannelId = CIBInformation.ChannelId,
        LogGainMul128U12Bits = [.. LogGainMul128U12BitPoints.Select(t => t.Y)],
        GainS16Bits = [.. GainS16BitPoints.Select(t => t.Y)],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class CIBMMDDTOItem : ObservableObject, ICloneable<CIBMMDDTOItem>
{
    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private double _measurePower;

    [ObservableProperty]
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    private double _protectedOverflowProtectedPMTValueCount;

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

    public CIBMMDDTOItem Clone() => new()
    {
        Coefficient = Coefficient,
        MeasurePower = MeasurePower,
        Items = [.. Items.Select(t => t.Clone())]
    };

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        private double _gain;

        [ObservableProperty]
        private double _pMTValue;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        public Item Clone() => new()
        {
            Gain = Gain,
            PMTValue = PMTValue,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }
}