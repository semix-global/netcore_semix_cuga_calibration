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

namespace Core.Models.Models.Ads.YGains;

[CacheVersion("1.0.0")]
public sealed partial class AdsYGainsDTO : CalibrationDTOBase<AdsYGainsDTO>, IAdaptTo<CalibrationAdsYGainsItem>
{
    [ObservableProperty]
    public partial double Y1P0 { get; set; }

    [ObservableProperty]
    public partial double Y1P1 { get; set; }

    [ObservableProperty]
    public partial double Y1P2 { get; set; }

    [ObservableProperty]
    public partial double Y2P0 { get; set; }

    [ObservableProperty]
    public partial double Y2P1 { get; set; }

    [ObservableProperty]
    public partial double Y2P2 { get; set; }

    [ObservableProperty]
    public partial double Y3P0 { get; set; }

    [ObservableProperty]
    public partial double Y3P1 { get; set; }

    [ObservableProperty]
    public partial double Y3P2 { get; set; }

    [ObservableProperty]
    public partial double Y4P0 { get; set; }

    [ObservableProperty]
    public partial double Y4P1 { get; set; }

    [ObservableProperty]
    public partial double Y4P2 { get; set; }

    [ObservableProperty]
    public partial double Y5P0 { get; set; }

    [ObservableProperty]
    public partial double Y5P1 { get; set; }

    [ObservableProperty]
    public partial double Y5P2 { get; set; }

    [ObservableProperty]
    public partial double Y6P0 { get; set; }

    [ObservableProperty]
    public partial double Y6P1 { get; set; }

    [ObservableProperty]
    public partial double Y6P2 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsYGainsDTOItem[] BestY1Y2Y3Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsYGainsDTOItem[] BestY4Y5Y6Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsYGainsDTOItem[] CalibrateItems { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AdsYGainsDTOItem[] VerifyItems { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource Y1Y2Y3PlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource Y4Y5Y6PlotDataSource { get; set; } = new PlotDataSource();

    partial void OnBestY1Y2Y3ItemsChanged(AdsYGainsDTOItem[] value) => RefreshY1Y2Y3Plot();

    partial void OnBestY4Y5Y6ItemsChanged(AdsYGainsDTOItem[] value) => RefreshY4Y5Y6Plot();

    public AdsYGainsDTO()
    {
        Y1Y2Y3PlotDataSource.Configure();
        Y1Y2Y3PlotDataSource.SetTitle(0, "Y1Y2Y3Plot(Y: Y1/Y2/Y3 Value - X: Speed(mm/s))");
        Y1Y2Y3PlotDataSource.ToggleLegend(0, true);

        Y4Y5Y6PlotDataSource.Configure();
        Y4Y5Y6PlotDataSource.SetTitle(0, "Y4Y5Y6Plot(Y: Y4/Y5/Y6 Value - X: Speed(mm/s))");
        Y4Y5Y6PlotDataSource.ToggleLegend(0, true);
    }

    private void RefreshY1Y2Y3Plot()
    {
        try
        {
            var speeds = BestY1Y2Y3Items.Select(t => t.SpeedYValue).ToList();
            var xSpeed = Vector<double>.Build.DenseOfEnumerable(speeds);
            var y1 = Vector<double>.Build.DenseOfEnumerable(BestY1Y2Y3Items.Select(t => (double)t.Y1OrY4));
            var y2 = Vector<double>.Build.DenseOfEnumerable(BestY1Y2Y3Items.Select(t => (double)t.Y2OrY5));
            var y3 = Vector<double>.Build.DenseOfEnumerable(BestY1Y2Y3Items.Select(t => (double)t.Y3OrY6));

            (Y1P0, Y1P1, Y1P2, var rSquared1, var yPredicted1) = PolynomialCurve.Fit2(xSpeed, y1);
            (Y2P0, Y2P1, Y2P2, var rSquared2, var yPredicted2) = PolynomialCurve.Fit2(xSpeed, y2);
            (Y3P0, Y3P1, Y3P2, var rSquared3, var yPredicted3) = PolynomialCurve.Fit2(xSpeed, y3);

            Point[] ToPoints(IReadOnlyList<double> x, Vector<double> y) => Enumerable.Range(0, x.Count).Select(i => new Point(x[i], y[i])).ToArray();
            var plot = new[] { y1, y2, y3 }.Select(y => ToPoints(speeds, y)).ToArray();
            var smooth = new[] { yPredicted1, yPredicted2, yPredicted3 }.Select(y => ToPoints(speeds, y)).ToArray();

            Y1Y2Y3PlotDataSource.GetOrAddScatterLine(0, $"Y1", plot[0]);
            Y1Y2Y3PlotDataSource.GetOrAddScatterLine(0, $"Y2", plot[1]);
            Y1Y2Y3PlotDataSource.GetOrAddScatterLine(0, $"Y3", plot[2]);
            Y1Y2Y3PlotDataSource.GetOrAddScatterLine(0, $"SmoothY1: " + PolynomialCurve.ToString2(Y1P0, Y1P1, Y1P2, rSquared1, "0.########"), smooth[0]);
            Y1Y2Y3PlotDataSource.GetOrAddScatterLine(0, $"SmoothY2: " + PolynomialCurve.ToString2(Y2P0, Y2P1, Y2P2, rSquared2, "0.########"), smooth[1]);
            Y1Y2Y3PlotDataSource.GetOrAddScatterLine(0, $"SmoothY3: " + PolynomialCurve.ToString2(Y3P0, Y3P1, Y3P2, rSquared3, "0.########"), smooth[2]);
        }
        finally
        {
            Y1Y2Y3PlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshY4Y5Y6Plot()
    {
        try
        {
            var speeds = BestY4Y5Y6Items.Select(t => t.SpeedYValue).ToList();
            var xSpeed = Vector<double>.Build.DenseOfEnumerable(speeds);
            var y1 = Vector<double>.Build.DenseOfEnumerable(BestY4Y5Y6Items.Select(t => (double)t.Y1OrY4));
            var y2 = Vector<double>.Build.DenseOfEnumerable(BestY4Y5Y6Items.Select(t => (double)t.Y2OrY5));
            var y3 = Vector<double>.Build.DenseOfEnumerable(BestY4Y5Y6Items.Select(t => (double)t.Y3OrY6));

            (Y4P0, Y4P1, Y4P2, var rSquared1, var yPredicted1) = PolynomialCurve.Fit2(xSpeed, y1);
            (Y5P0, Y5P1, Y5P2, var rSquared2, var yPredicted2) = PolynomialCurve.Fit2(xSpeed, y2);
            (Y6P0, Y6P1, Y6P2, var rSquared3, var yPredicted3) = PolynomialCurve.Fit2(xSpeed, y3);

            Point[] ToPoints(IReadOnlyList<double> x, Vector<double> y) => Enumerable.Range(0, x.Count).Select(i => new Point(x[i], y[i])).ToArray();
            var plot = new[] { y1, y2, y3 }.Select(y => ToPoints(speeds, y)).ToArray();
            var smooth = new[] { yPredicted1, yPredicted2, yPredicted3 }.Select(y => ToPoints(speeds, y)).ToArray();

            Y4Y5Y6PlotDataSource.GetOrAddScatterLine(0, $"Y4", plot[0]);
            Y4Y5Y6PlotDataSource.GetOrAddScatterLine(0, $"Y5", plot[1]);
            Y4Y5Y6PlotDataSource.GetOrAddScatterLine(0, $"Y6", plot[2]);
            Y4Y5Y6PlotDataSource.GetOrAddScatterLine(0, $"SmoothY4: " + PolynomialCurve.ToString2(Y4P0, Y4P1, Y4P2, rSquared1, "0.########"), smooth[0]);
            Y4Y5Y6PlotDataSource.GetOrAddScatterLine(0, $"SmoothY5: " + PolynomialCurve.ToString2(Y5P0, Y5P1, Y5P2, rSquared2, "0.########"), smooth[1]);
            Y4Y5Y6PlotDataSource.GetOrAddScatterLine(0, $"SmoothY6: " + PolynomialCurve.ToString2(Y6P0, Y6P1, Y6P2, rSquared3, "0.########"), smooth[2]);
        }
        finally
        {
            Y4Y5Y6PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override AdsYGainsDTO Clone() => new()
    {
        Y1P0 = Y1P0,
        Y1P1 = Y1P1,
        Y1P2 = Y1P2,
        Y2P0 = Y2P0,
        Y2P1 = Y2P1,
        Y2P2 = Y2P2,
        Y3P0 = Y3P0,
        Y3P1 = Y3P1,
        Y3P2 = Y3P2,
        Y4P0 = Y4P0,
        Y4P1 = Y4P1,
        Y4P2 = Y4P2,
        Y5P0 = Y5P0,
        Y5P1 = Y5P1,
        Y5P2 = Y5P2,
        Y6P0 = Y6P0,
        Y6P1 = Y6P1,
        Y6P2 = Y6P2,
        BestY1Y2Y3Items = [.. BestY1Y2Y3Items],
        BestY4Y5Y6Items = [.. BestY4Y5Y6Items],
        CalibrateItems = [.. CalibrateItems],
        VerifyItems = [.. VerifyItems],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAdsYGainsItem AdaptTo() => new()
    {
        PositiveY1P1 = Y1P2,
        PositiveY1P2 = Y1P1,
        PositiveY1P3 = Y1P0,
        PositiveY2P1 = Y2P2,
        PositiveY2P2 = Y2P1,
        PositiveY2P3 = Y2P0,
        PositiveY3P1 = Y3P2,
        PositiveY3P2 = Y3P1,
        PositiveY3P3 = Y3P0,
        NegativeY4P1 = Y4P2,
        NegativeY4P2 = Y4P1,
        NegativeY4P3 = Y4P0,
        NegativeY5P1 = Y5P2,
        NegativeY5P2 = Y5P1,
        NegativeY5P3 = Y5P0,
        NegativeY6P1 = Y6P2,
        NegativeY6P2 = Y6P1,
        NegativeY6P3 = Y6P0,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public sealed partial class AdsYGainsDTOItem : ObservableObject, ICloneable<AdsYGainsDTOItem>
    {
        [ObservableProperty]
        public partial bool IsPositive { get; set; }

        [ObservableProperty]
        public partial double SpeedYValue { get; set; }

        [ObservableProperty]
        public partial int Y1OrY4 { get; set; }

        [ObservableProperty]
        public partial int Y2OrY5 { get; set; }

        [ObservableProperty]
        public partial int Y3OrY6 { get; set; }

        [ObservableProperty]
        public partial Point[] PlotZ1OrZ4 { get; set; } = [];

        [ObservableProperty]
        public partial Point[] PlotZ2OrZ5 { get; set; } = [];

        [ObservableProperty]
        public partial Point[] PlotZ3OrZ6 { get; set; } = [];

        [ObservableProperty]
        public partial Point[] Speeds { get; set; } = [];

        [ObservableProperty]
        public partial bool IsUpwardZ1OrZ4 { get; set; }

        [ObservableProperty]
        public partial bool IsUpwardZ2OrZ5 { get; set; }

        [ObservableProperty]
        public partial bool IsUpwardZ3OrZ6 { get; set; }

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

        partial void OnPlotZ1OrZ4Changed(Point[] value) => RefreshPlot();

        partial void OnPlotZ2OrZ5Changed(Point[] value) => RefreshPlot();

        partial void OnPlotZ3OrZ6Changed(Point[] value) => RefreshPlot();

        partial void OnSpeedsChanged(Point[] value) => RefreshPlot();

        partial void OnPlotHChanged(Point[] value) => RefreshHrpPlot();

        partial void OnPlotRChanged(Point[] value) => RefreshHrpPlot();

        partial void OnPlotPChanged(Point[] value) => RefreshHrpPlot();

        public AdsYGainsDTOItem()
        {
            ZPlotDataSource.Configure();
            ZPlotDataSource.SetTitle(0, "PlotZ(Y: Z1/Z2/Z3(um) Speed(mm/s) - X: Time(ms))");
            ZPlotDataSource.ToggleLegend(0, true);

            HrpPlotDataSource.Configure();
            HrpPlotDataSource.SetTitle(0, "PlotHRP(Y: Height Roll Pitch Value(um) - X: Time(ms))");
            HrpPlotDataSource.ToggleLegend(0, true);
        }

        private void RefreshPlot()
        {
            var (index1, index2, index3) = IsPositive ? ("1", "2", "3") : ("4", "5", "6");
            try
            {
                ZPlotDataSource.GetOrAddScatterLine(0, $"Z{index1}", PlotZ1OrZ4);
                ZPlotDataSource.GetOrAddScatterLine(0, $"Z{index2}", PlotZ2OrZ5);
                ZPlotDataSource.GetOrAddScatterLine(0, $"Z{index3}", PlotZ3OrZ6);

                ZPlotDataSource.GetOrAddScatterLine(0, $"Smooth{index1}", Filter.MovMean([.. PlotZ1OrZ4], 501));
                ZPlotDataSource.GetOrAddScatterLine(0, $"Smooth{index2}", Filter.MovMean([.. PlotZ2OrZ5], 501));
                ZPlotDataSource.GetOrAddScatterLine(0, $"Smooth{index3}", Filter.MovMean([.. PlotZ3OrZ6], 501));

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

        public AdsYGainsDTOItem Clone() => new()
        {
            IsPositive = IsPositive,
            SpeedYValue = SpeedYValue,
            Y1OrY4 = Y1OrY4,
            Y2OrY5 = Y2OrY5,
            Y3OrY6 = Y3OrY6,
            PlotZ1OrZ4 = [.. PlotZ1OrZ4],
            PlotZ2OrZ5 = [.. PlotZ2OrZ5],
            PlotZ3OrZ6 = [.. PlotZ3OrZ6],
            Speeds = [.. Speeds],
            IsUpwardZ1OrZ4 = IsUpwardZ1OrZ4,
            IsUpwardZ2OrZ5 = IsUpwardZ2OrZ5,
            IsUpwardZ3OrZ6 = IsUpwardZ3OrZ6,
            PlotH = [.. PlotH],
            PlotR = [.. PlotR],
            PlotP = [.. PlotP],
        };
    }
}