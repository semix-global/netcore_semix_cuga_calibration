using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace Core.Models.Models.CIB.MMD;

public sealed partial class CIBMMDDto : CalibrationDtoBase, ICloneable<CIBMMDDto>, IAdaptTo<CalibrationLaserCIBMMDItem>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<CIBMMDItemDto> _items = [];

    [ObservableProperty]
    private double _gainResidual;

    [ObservableProperty]
    private double _gainL2Norm;

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
    private IReadOnlyList<Point> _resultLogGainPoints = [];

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


    public CIBMMDDto()
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
        ScatterPlotControl.SetTitle(1, "Origin(Y: PMTValue - X: V)");
        ScatterPlotControl.SetTitle(2, "Gain(Y: Gain - X: V)");
        ScatterPlotControl.SetTitle(3, "LogGain(Y: LogGain - X: V)");
        ScatterPlotControl.SetTitle(4, "LogGain * 128 U12Bit(Y: LogGain * 128 U12Bit - X: Sense U14Bit)");
        ScatterPlotControl.SetTitle(5, "Gain S16Bit(Y: Gain S16Bit - X: LogGain * 128 U12Bit )");
    }

    public void RefreshPlot()
    {
        var items = Items.Where(t => double.IsNaN(t.MeasurePower) == false).ToArray();
        if (items.Length > 0)
        {
            ScatterPlotControl.GetOrAddScatterLine(
                0,
                "Attenuator",
                [.. items.Select(t => new Point(t.Coefficient, t.MeasurePower))]);

            foreach (var item in items)
            {
                var itemItems = item.Items.Where(t => double.IsNaN(t.PMTValue) == false).ToArray();
                if (itemItems.Length > 0)
                    ScatterPlotControl.GetOrAddScatterLine(
                        1,
                        $"{item.Coefficient}",
                        [.. itemItems.Select(t => new Point(t.Gain, t.PMTValue))]);
            }
        }

        if (GainPoints.Count > 0)
            ScatterPlotControl.GetOrAddScatterLine(
                2,
                $"Gain Residual: {GainResidual:0.000#} Gain Norm: {GainL2Norm:0.###}",
                GainPoints);

        if (OriginLogGainPoints.Count > 0)
        {
            ScatterPlotControl.Clear(3);
            ScatterPlotControl.GetOrAddScatterLine(
                3,
                $"Origin Curve Residual: {GainResidual:0.000#} GainL2Norm: {GainL2Norm:0.###}",
                OriginLogGainPoints);

            if (FitLogGainPoints.Count > 0)
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    3,
                    $"Fit Curve: y = {LogGainA2:0.######} + ({LogGainA1:0.######} - {LogGainA2:0.######}) / (1 + exp((x - {LogGainX0:0.######}) / {LogGainDx:0.######})) r^2 = {LogGainRSquared:0.######}",
                    FitLogGainPoints);
            }

            if (ResultLogGainPoints.Count > 0)
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    3,
                    $"Result Curve Residual: {GainResidual:0.000#} GainL2Norm: {GainL2Norm:0.###}",
                    ResultLogGainPoints);
            }
        }

        if (LogGainMul128U12BitPoints.Count > 0)
            ScatterPlotControl.GetOrAddScatterLine(
                4,
                $"Residual: {GainResidual:0.000#} GainL2Norm: {GainL2Norm:0.###}",
                LogGainMul128U12BitPoints);

        if (GainS16BitPoints.Count > 0)
            ScatterPlotControl.GetOrAddScatterLine(
                5,
                $"Residual: {GainResidual:0.000#} GainL2Norm: {GainL2Norm:0.###}",
                GainS16BitPoints);

        ScatterPlotControl.AutoScaleRefresh();
    }

    #region Mapper

    public CIBMMDDto Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        GainResidual = GainResidual,
        GainL2Norm = GainL2Norm,
        GainPoints = [.. GainPoints],
        OriginLogGainPoints = [.. OriginLogGainPoints],
        LogGainA1 = LogGainA1,
        LogGainA2 = LogGainA2,
        LogGainX0 = LogGainX0,
        LogGainDx = LogGainDx,
        LogGainRSquared = LogGainRSquared,
        FitLogGainPoints = [.. FitLogGainPoints],
        ResultLogGainPoints = [.. ResultLogGainPoints],
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

public sealed partial class CIBMMDItemDto : CalibrationCacheBase, ICloneable<CIBMMDItemDto>
{
    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private double _measurePower;

    [ObservableProperty]
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private double _protectedCount;

    public CIBMMDItemDto Clone() => new()
    {
        Coefficient = Coefficient,
        MeasurePower = MeasurePower,
        Items = [.. Items.Select(t => t.Clone())]
    };

    public sealed class Item : ICloneable<Item>
    {
        public double Gain { get; init; }

        public double PMTValue { get; set; }

        public Item Clone() => new()
        {
            Gain = Gain,
            PMTValue = PMTValue
        };
    }
}