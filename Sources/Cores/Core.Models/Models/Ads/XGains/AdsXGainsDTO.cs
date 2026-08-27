using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Ads;
using Local.SQL.Cache.Providers.Bases;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;

namespace Core.Models.Models.Ads.XGains;

[CacheVersion("1.0.0")]
public sealed partial class AdsXGainsDTO : CalibrationDTOBase<AdsXGainsDTO>, IAdaptTo<CalibrationAdsXGainsItem>
{
    [ObservableProperty]
    public partial double X1P0 { get; set; }

    [ObservableProperty]
    public partial double X1P1 { get; set; }

    [ObservableProperty]
    public partial double X1P2 { get; set; }

    [ObservableProperty]
    public partial double X2P0 { get; set; }

    [ObservableProperty]
    public partial double X2P1 { get; set; }

    [ObservableProperty]
    public partial double X2P2 { get; set; }

    [ObservableProperty]
    public partial double X3P0 { get; set; }

    [ObservableProperty]
    public partial double X3P1 { get; set; }

    [ObservableProperty]
    public partial double X3P2 { get; set; }

    [ObservableProperty]
    public partial double X4P0 { get; set; }

    [ObservableProperty]
    public partial double X4P1 { get; set; }

    [ObservableProperty]
    public partial double X4P2 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsXGainsDTOItem[] BestX1X2Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsXGainsDTOItem[] BestX3X4Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsXGainsDTOItem[] CalibrateItems { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsXGainsDTOItem[] VerifyItems { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource X1X2PlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource X3X4PlotDataSource { get; set; } = new PlotDataSource();

    partial void OnBestX1X2ItemsChanged(AdsXGainsDTOItem[] value) => RefreshX1X2Plot();

    partial void OnBestX3X4ItemsChanged(AdsXGainsDTOItem[] value) => RefreshX3X4Plot();

    public AdsXGainsDTO()
    {
        X1X2PlotDataSource.Configure();
        X1X2PlotDataSource.SetTitle(0, "X1X2Plot(Y: X1/X2 Value - X: Speed(mm/s))");
        X1X2PlotDataSource.ToggleLegend(0, true);

        X3X4PlotDataSource.Configure();
        X3X4PlotDataSource.SetTitle(0, "X3X4Plot(Y: X3/X4 Value - X: Speed(mm/s))");
        X3X4PlotDataSource.ToggleLegend(0, true);
    }

    private void RefreshX1X2Plot()
    {
        try
        {
            var speeds = BestX1X2Items.Select(t => t.SpeedXValue).ToList();
            var xSpeed = Vector<double>.Build.DenseOfEnumerable(speeds);
            var y1 = Vector<double>.Build.DenseOfEnumerable(BestX1X2Items.Select(t => (double)t.X1OrX3));
            var y2 = Vector<double>.Build.DenseOfEnumerable(BestX1X2Items.Select(t => (double)t.X2OrX4));

            (X1P0, X1P1, X1P2, var rSquared1, var yPredicted1) = PolynomialCurve.Fit2(xSpeed, y1);
            (X2P0, X2P1, X2P2, var rSquared2, var yPredicted2) = PolynomialCurve.Fit2(xSpeed, y2);

            Point[] ToPoints(IReadOnlyList<double> x, Vector<double> y) => Enumerable.Range(0, x.Count).Select(i => new Point(x[i], y[i])).ToArray();
            var plot = new[] { y1, y2 }.Select(y => ToPoints(speeds, y)).ToArray();
            var smooth = new[] { yPredicted1, yPredicted2 }.Select(y => ToPoints(speeds, y)).ToArray();

            X1X2PlotDataSource.GetOrAddScatterLine(0, $"X1", plot[0]);
            X1X2PlotDataSource.GetOrAddScatterLine(0, $"X2", plot[1]);
            X1X2PlotDataSource.GetOrAddScatterLine(0, $"SmoothX1: " + PolynomialCurve.ToString2(X1P0, X1P1, X1P2, rSquared1, "0.########"), smooth[0]);
            X1X2PlotDataSource.GetOrAddScatterLine(0, $"SmoothX2: " + PolynomialCurve.ToString2(X2P0, X2P1, X2P2, rSquared2, "0.########"), smooth[1]);
        }
        finally
        {
            X1X2PlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshX3X4Plot()
    {
        try
        {
            var speeds = BestX3X4Items.Select(t => t.SpeedXValue).ToList();
            var xSpeed = Vector<double>.Build.DenseOfEnumerable(speeds);
            var y1 = Vector<double>.Build.DenseOfEnumerable(BestX3X4Items.Select(t => (double)t.X1OrX3));
            var y2 = Vector<double>.Build.DenseOfEnumerable(BestX3X4Items.Select(t => (double)t.X2OrX4));

            (X3P0, X3P1, X3P2, var rSquared1, var yPredicted1) = PolynomialCurve.Fit2(xSpeed, y1);
            (X4P0, X4P1, X4P2, var rSquared2, var yPredicted2) = PolynomialCurve.Fit2(xSpeed, y2);

            Point[] ToPoints(IReadOnlyList<double> x, Vector<double> y) => Enumerable.Range(0, x.Count).Select(i => new Point(x[i], y[i])).ToArray();
            var plot = new[] { y1, y2 }.Select(y => ToPoints(speeds, y)).ToArray();
            var smooth = new[] { yPredicted1, yPredicted2 }.Select(y => ToPoints(speeds, y)).ToArray();

            X3X4PlotDataSource.GetOrAddScatterLine(0, $"X3", plot[0]);
            X3X4PlotDataSource.GetOrAddScatterLine(0, $"X4", plot[1]);
            X3X4PlotDataSource.GetOrAddScatterLine(0, $"SmoothX3: " + PolynomialCurve.ToString2(X3P0, X3P1, X3P2, rSquared1, "0.########"), smooth[0]);
            X3X4PlotDataSource.GetOrAddScatterLine(0, $"SmoothX4: " + PolynomialCurve.ToString2(X4P0, X4P1, X4P2, rSquared2, "0.########"), smooth[1]);
        }
        finally
        {
            X3X4PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override AdsXGainsDTO Clone() => new()
    {
        X1P0 = X1P0,
        X1P1 = X1P1,
        X1P2 = X1P2,
        X2P0 = X2P0,
        X2P1 = X2P1,
        X2P2 = X2P2,
        X3P0 = X3P0,
        X3P1 = X3P1,
        X3P2 = X3P2,
        X4P0 = X4P0,
        X4P1 = X4P1,
        X4P2 = X4P2,
        BestX1X2Items = [.. BestX1X2Items],
        BestX3X4Items = [.. BestX3X4Items],
        CalibrateItems = [.. CalibrateItems],
        VerifyItems = [.. VerifyItems],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAdsXGainsItem AdaptTo() => new()
    {
        PositiveX1P3 = X1P0,
        PositiveX1P2 = X1P1,
        PositiveX1P1 = X1P2,
        PositiveX2P3 = X2P0,
        PositiveX2P2 = X2P1,
        PositiveX2P1 = X2P2,
        NegativeX3P3 = X3P0,
        NegativeX3P2 = X3P1,
        NegativeX3P1 = X3P2,
        NegativeX4P3 = X4P0,
        NegativeX4P2 = X4P1,
        NegativeX4P1 = X4P2,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public sealed partial class AdsXGainsDTOItem : ObservableObject, ICloneable<AdsXGainsDTOItem>
    {
        [ObservableProperty]
        public partial bool IsPositive { get; set; }

        [ObservableProperty]
        public partial double SpeedXValue { get; set; }

        [ObservableProperty]
        public partial int X1OrX3 { get; set; }

        [ObservableProperty]
        public partial int X2OrX4 { get; set; }

        [ObservableProperty]
        public partial Point[] PlotZ1OrZ3 { get; set; } = [];

        [ObservableProperty]
        public partial Point[] PlotZ2OrZ4 { get; set; } = [];

        [ObservableProperty]
        public partial Point[] Speeds { get; set; } = [];

        [ObservableProperty]
        public partial bool IsUpwardZ1OrZ3 { get; set; }

        [ObservableProperty]
        public partial bool IsUpwardZ2OrZ4 { get; set; }

        public double H => PlotH.Any() ? PlotH.Max(t => Math.Abs(t.Y)) : 0d;

        public double R => PlotR.Any() ? PlotR.Max(t => Math.Abs(t.Y)) : 0d;

        public double P => PlotP.Any() ? PlotP.Max(t => Math.Abs(t.Y)) : 0d;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(H))]
        public partial Point[] PlotH { get; set; } = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(R))]
        public partial Point[] PlotR { get; set; } = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(P))]
        public partial Point[] PlotP { get; set; } = [];

        [ObservableProperty]
        public partial IPlotDataSource ZPlotDataSource { get; set; } = new PlotDataSource();

        [ObservableProperty]
        public partial IPlotDataSource HrpPlotDataSource { get; set; } = new PlotDataSource();

        partial void OnPlotZ1OrZ3Changed(Point[] value) => RefreshPlot();

        partial void OnPlotZ2OrZ4Changed(Point[] value) => RefreshPlot();

        partial void OnSpeedsChanged(Point[] value) => RefreshPlot();

        partial void OnPlotHChanged(Point[] value) => RefreshHrpPlot();

        partial void OnPlotRChanged(Point[] value) => RefreshHrpPlot();

        partial void OnPlotPChanged(Point[] value) => RefreshHrpPlot();

        public AdsXGainsDTOItem()
        {
            ZPlotDataSource.Configure();
            ZPlotDataSource.SetTitle(0, "PlotZ(Y: Z1/Z2(um) Speed(mm/s) - X: Time(ms))");
            ZPlotDataSource.ToggleLegend(0, true);

            HrpPlotDataSource.Configure();
            HrpPlotDataSource.SetTitle(0, "PlotHRP(Y: Height Roll Pitch Value(um) - X: Time(ms))");
            HrpPlotDataSource.ToggleLegend(0, true);
        }

        private void RefreshPlot()
        {
            var (index1, index2) = IsPositive ? ("1", "2") : ("3", "4");
            try
            {
                ZPlotDataSource.GetOrAddScatterLine(0, $"Z{index1}", PlotZ1OrZ3);
                ZPlotDataSource.GetOrAddScatterLine(0, $"Z{index2}", PlotZ2OrZ4);

                ZPlotDataSource.GetOrAddScatterLine(0, $"Smooth{index1}", Filter.MovMean([.. PlotZ1OrZ3], 501));
                ZPlotDataSource.GetOrAddScatterLine(0, $"Smooth{index2}", Filter.MovMean([.. PlotZ2OrZ4], 501));

                ZPlotDataSource.GetOrAddScatterLine(0, $"Speed", Speeds);
            }
            finally
            {
                ZPlotDataSource.AutoScaleRefresh();
            }
        }

        private void RefreshHrpPlot()
        {
            try
            {
                HrpPlotDataSource.GetOrAddScatterLine(0, $"H", PlotH);
                HrpPlotDataSource.GetOrAddScatterLine(0, $"R", PlotR);
                HrpPlotDataSource.GetOrAddScatterLine(0, $"P", PlotP);
            }
            finally
            {
                HrpPlotDataSource.AutoScaleRefresh();
            }
        }

        public AdsXGainsDTOItem Clone() => new()
        {
            SpeedXValue = SpeedXValue,
            IsPositive = IsPositive,
            X1OrX3 = X1OrX3,
            X2OrX4 = X2OrX4,
            PlotZ1OrZ3 = [.. PlotZ1OrZ3],
            PlotZ2OrZ4 = [.. PlotZ2OrZ4],
            Speeds = [.. Speeds],
            IsUpwardZ1OrZ3 = IsUpwardZ1OrZ3,
            IsUpwardZ2OrZ4 = IsUpwardZ2OrZ4,
            PlotH = [.. PlotH],
            PlotR = [.. PlotR],
            PlotP = [.. PlotP],
        };
    }
}