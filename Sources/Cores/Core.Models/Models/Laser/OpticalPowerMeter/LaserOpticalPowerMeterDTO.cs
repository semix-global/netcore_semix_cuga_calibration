using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interactivity.UserActionResponses;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;
using System.ComponentModel;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

[CacheVersion("1.0.0")]
public sealed partial class LaserOpticalPowerMeterDTO : CalibrationDTOBase<LaserOpticalPowerMeterDTO>, IAdaptTo<CalibrationLaserOpticalPower>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _maxCoefficient;

    [ObservableProperty]
    private IReadOnlyList<LaserOpticalPowerMeterDTOItem> _items = [];

    [ObservableProperty]
    private double _maxMeasurePower;

    [ObservableProperty]
    private Point _maxMeasurePowerPosition = Point.Origin;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<LaserOpticalPowerMeterDTOItem>? oldValue, IReadOnlyList<LaserOpticalPowerMeterDTOItem> newValue)
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

    public LaserOpticalPowerMeterDTO()
    {
        ScatterPlotControl.SetTitle("Map(Y: um - X: um - Z: mW)");

        ScatterPlotControl.Plot.HideAxesAndGrid();
        ScatterPlotControl.UserInputProcessor.UserActionResponses.Remove(ScatterPlotControl.UserInputProcessor.UserActionResponses.Single(t => t is MouseDragCrosshair));
        ScatterPlotControl.UserInputProcessor.UserActionResponses.Add(new MouseDragTextCrosshair(StandardMouseButtons.Left, StandardMouseButtons.Right)); // 右键拖动: 十字线
    }

    private void RefreshPlot()
    {
        try
        {
            lock (ScatterPlotControl.Plot.Sync) ScatterPlotControl.Plot.PlottableList.RemoveAll(t => t is Text);

            if (Items.Count <= 0) return;

            var items = Items.Where(t => double.IsNaN(t.MeasurePower) == false).ToArray();
            var maximumIndex = items.Length > 0 ? Vector<double>.Build.DenseOfEnumerable(items.Select(t => t.MeasurePower)).MaximumIndex() : 0;
            var measureMinPower = items.Length > 0 ? items.Min(t => t.MeasurePower) : 0;
            var measureMaxPower = items.Length > 0 ? items.Max(t => t.MeasurePower) : 0;

            foreach (var (index, laserOpticalPowerItemDto) in Items.Index())
            {
                var txt = new Text
                {
                    LabelText = $"{laserOpticalPowerItemDto.MeasurePower:00.00000}",
                    LabelBackgroundColor = Constants.Turbo.GetColor(laserOpticalPowerItemDto.MeasurePower, new Range(measureMinPower, measureMaxPower)),
                    LabelBorderColor = Colors.Transparent,
                    Location = new Coordinates(laserOpticalPowerItemDto.MeasurePosition.X, laserOpticalPowerItemDto.MeasurePosition.Y),
                    LabelFontSize = 12,
                    LabelPadding = 2,
                    LabelFontColor = Colors.White,
                    LabelAlignment = Alignment.MiddleCenter
                };

                lock (ScatterPlotControl.Plot.Sync) ScatterPlotControl.Plot.PlottableList.Add(txt);
                if (index != maximumIndex) continue;

                txt.LabelBorderColor = Colors.OrangeRed;
                txt.LabelBorderWidth = 5;
                txt.LabelPadding = 5;
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public HtmlPlot3DChart GetHtmlPlot3DChart(HtmlPlot3DType htmlPlot3DType) => new([.. Items.Select(t => new Point3D(t.MeasurePosition.X, t.MeasurePosition.Y, t.MeasurePower))], ScatterPlotControl.GetTitle(), htmlPlot3DType);

    #region Mapper

    public override LaserOpticalPowerMeterDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        MaxCoefficient = MaxCoefficient,
        Items = [.. Items.Select(x => x.Clone())],
        MaxMeasurePower = MaxMeasurePower,
        MaxMeasurePowerPosition = MaxMeasurePowerPosition,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserOpticalPower AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Coefficient = MaxCoefficient,
        MeasureMaxPower = MaxMeasurePower,
        MeasureMaxPowerPosition = MaxMeasurePowerPosition.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class LaserOpticalPowerMeterDTOItem : ObservableObject, ICloneable<LaserOpticalPowerMeterDTOItem>
{
    [ObservableProperty]
    private Point _measurePosition;

    [ObservableProperty]
    private double _measurePower;

    public LaserOpticalPowerMeterDTOItem Clone() => new()
    {
        MeasurePosition = MeasurePosition,
        MeasurePower = MeasurePower
    };
}