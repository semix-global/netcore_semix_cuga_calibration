using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interactivity.UserActionResponses;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerMeterDTO : CalibrationDtoBase, ICloneable<LaserOpticalPowerMeterDTO>, IAdaptTo<CalibrationLaserOpticalPower>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

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
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IPlotControl PlotControl => GuardUtils.IsAssignableToType<IPlotControl>(ScatterPlotControl);

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

        PlotControl.Plot.HideAxesAndGrid();
        PlotControl.UserInputProcessor.UserActionResponses.Remove(PlotControl.UserInputProcessor.UserActionResponses.Single(t => t is MouseDragCrosshair));
        PlotControl.UserInputProcessor.UserActionResponses.Add(new MouseDragTextCrosshair(StandardMouseButtons.Left, StandardMouseButtons.Right)); // 右键拖动: 十字线
    }

    private void RefreshPlot()
    {
        try
        {
            PlotControl.Plot.PlottableList.RemoveAll(t => t is Text);

            if (Items.Count <= 0) return;

            var maximumIndex = Vector<double>.Build.DenseOfEnumerable(Items.Select(t => t.MeasurePower)).MaximumIndex();

            var measureMinPower = Items.Min(t => t.MeasurePower);
            var measureMaxPower = Items.Max(t => t.MeasurePower);

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

                lock (PlotControl.Plot.Sync) PlotControl.Plot.PlottableList.Add(txt);
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

    public LaserOpticalPowerMeterDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
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
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.Default,
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