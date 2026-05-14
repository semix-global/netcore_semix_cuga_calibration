using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot.MultiplotLayouts;

namespace Core.Models.Models.Laser.Attenuator;

[CacheVersion("1.0.0")]
public sealed partial class LaserAttenuatorDTO : CalibrationDTOBase<LaserAttenuatorDTO>, IAdaptTo<CalibrationAttenuatorObj>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double WaitTime { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> MeasurePowerPoints { get; set; } = [];

    [ObservableProperty]
    public partial double MaxMeasurePower { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> AttenuatorPoints { get; set; } = [];

    [ObservableProperty]
    public partial double P0 { get; set; }

    [ObservableProperty]
    public partial double P1 { get; set; }

    [ObservableProperty]
    public partial double P2 { get; set; }

    [ObservableProperty]
    public partial double P3 { get; set; }

    [ObservableProperty]
    public partial double RSquared { get; set; }

    [ObservableProperty]
    public partial double SaturationCoefficient { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> FitAttenuatorPoints { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnMeasurePowerPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnMaxMeasurePowerChanged(double value) => RefreshPlot();

    partial void OnAttenuatorPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnP0Changed(double value) => RefreshPlot();

    partial void OnP1Changed(double value) => RefreshPlot();

    partial void OnP2Changed(double value) => RefreshPlot();

    partial void OnP3Changed(double value) => RefreshPlot();

    partial void OnRSquaredChanged(double value) => RefreshPlot();

    partial void OnFitAttenuatorPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public LaserAttenuatorDTO()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Measure Power(Y: mW - X: Coefficient)");
        ScatterPlotControl.SetTitle(1, "Laser Attenuator(Y: Rate - X: Coefficient)");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(1);

            if (MeasurePowerPoints.Count > 0)
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    0,
                    "Measure Power",
                    MeasurePowerPoints,
                    Constants.Category10.GetColor(0));
            }

            if (AttenuatorPoints.Count > 0)
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"Max Measure Power = {MaxMeasurePower:0.######}(mW)",
                    AttenuatorPoints,
                    Constants.Category10.GetColor(0));
            }

            if (FitAttenuatorPoints.Count > 0)
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"Fit Curve: y = {P0:0.######} + {P1:0.######}x + {P2:0.######}x^2 + {P3:0.######}x^3 r^2 = {RSquared:0.######}",
                    FitAttenuatorPoints,
                    Constants.Category10.GetColor(0));
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override LaserAttenuatorDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        WaitTime = WaitTime,
        MeasurePowerPoints = [.. MeasurePowerPoints],
        MaxMeasurePower = MaxMeasurePower,
        AttenuatorPoints = [.. AttenuatorPoints],
        P0 = P0,
        P1 = P1,
        P2 = P2,
        P3 = P3,
        RSquared = RSquared,
        SaturationCoefficient = SaturationCoefficient,
        FitAttenuatorPoints = [.. FitAttenuatorPoints],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAttenuatorObj AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        MaxCoefficientAverageMeasurePower = MaxMeasurePower,
        CoefficientMeasurePowerPoints = [.. MeasurePowerPoints.Select(t => t.ToCgPoint())],
        CoefficientMeasurePowerRatePoints = [.. AttenuatorPoints.Select(t => t.ToCgPoint())],
        P0 = P0,
        P1 = P1,
        P2 = P2,
        P3 = P3,
        RSquared = RSquared,
        SaturationCoefficient = SaturationCoefficient,
        CoefficientFitMeasurePowerRatePoints = [.. FitAttenuatorPoints.Select(t => t.ToCgPoint())],
        WaitTime = WaitTime,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified
    };

    #endregion Mapper
}