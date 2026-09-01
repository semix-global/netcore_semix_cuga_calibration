using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.WPF.Extensions;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AutoFocus.DarkAutoFocus;

[CacheVersion("1.0.0")]
public sealed partial class DarkAutoFocusDTO : CalibrationDTOBase<DarkAutoFocusDTO>, IAdaptTo<CalibrationLaserAutoFocus>
{
    [ObservableProperty]
    public partial DarkAutoFocusCurrentDTO CurrentADTO { get; set; } = new();

    [ObservableProperty]
    public partial DarkAutoFocusCurrentDTO CurrentBDTO { get; set; } = new();

    public double CurrentA => CurrentADTO.ResultDTO.Current;
    public double Fa => CurrentADTO.ResultDTO.F;
    public double Na => CurrentADTO.ResultDTO.N;

    public double CurrentB => CurrentBDTO.ResultDTO.Current;
    public double Fb => CurrentBDTO.ResultDTO.F;
    public double Nb => CurrentBDTO.ResultDTO.N;

    [ObservableProperty]
    public partial double LowCoefficient { get; set; } = 0.6d;

    [ObservableProperty]
    public partial double HighCoefficient { get; set; } = 1.5d;

    #region NSC

    #region NSC Gain

    [ObservableProperty]
    public partial DarkAutoFocusNSCDTO NSCGainResultDTO { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<DarkAutoFocusNSCDTO> NSCGainDTOItems { get; set; } = [];

    #endregion

    #region NSC Profile

    [ObservableProperty]
    public partial bool IsNscUseMaxValue { get; set; }

    [ObservableProperty]
    public partial bool IsNscUsePositiveSlope { get; set; }

    [ObservableProperty]
    public partial double OriginalSymmetryRatio { get; set; }

    [ObservableProperty]
    public partial double EcsToNmRange { get; set; }

    [ObservableProperty]
    public partial double NscStandard { get; set; }

    [ObservableProperty]
    public partial DarkAutoFocusNSCDTO NSCProfileResultDTO { get; set; } = new();

    #endregion NSC Profile

    #region ECS & Af Motor Relation

    [ObservableProperty]
    public partial double AfMotor { get; set; }

    [ObservableProperty]
    public partial double EcsMotorPositionRelationSlope { get; set; }

    [ObservableProperty]
    public partial double EcsMotorPositionRelationIntercept { get; set; }

    [ObservableProperty]
    public partial double EcsMotorPositionRelationRSquare { get; set; }

    [ObservableProperty]
    public partial double MinAFMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double MaxAFMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial Point[] ECSMotorOrigins { get; set; } = [];

    [ObservableProperty]
    public partial Point[] FitECSMotorOrigins { get; set; } = [];

    partial void OnECSMotorOriginsChanged(Point[] value) => RefreshPlot();

    partial void OnFitECSMotorOriginsChanged(Point[] value) => RefreshPlot();

    partial void OnEcsMotorPositionRelationRSquareChanged(double value) => RefreshPlot();

    #endregion

    #endregion NSC

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public DarkAutoFocusDTO()
    {
        PlotDataSource.Configure(new Columns());

        PlotDataSource.SetTitle(0, "Slope (Y: ECS - X: AF Motor)");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear(0);

            if (ECSMotorOrigins.Length != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "Origins",
                    [.. ECSMotorOrigins],
                    0,
                    new Range(0, ECSMotorOrigins.Length - 1));

            if (FitECSMotorOrigins.Length > 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    $"F Fit Curve: y = {EcsMotorPositionRelationSlope:0.######}x + {EcsMotorPositionRelationIntercept:0.######} r^2 = {EcsMotorPositionRelationRSquare:0.######})",
                    [.. FitECSMotorOrigins],
                    1,
                    new Range(0, FitECSMotorOrigins.Length - 1));
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override DarkAutoFocusDTO Clone() => new()
    {
        CurrentADTO = CurrentADTO.Clone(),
        CurrentBDTO = CurrentBDTO.Clone(),
        LowCoefficient = LowCoefficient,
        HighCoefficient = HighCoefficient,
        IsNscUseMaxValue = IsNscUseMaxValue,
        IsNscUsePositiveSlope = IsNscUsePositiveSlope,
        OriginalSymmetryRatio = OriginalSymmetryRatio,
        EcsToNmRange = EcsToNmRange,
        NscStandard = NscStandard,
        NSCGainResultDTO = NSCGainResultDTO.Clone(),
        NSCProfileResultDTO = NSCProfileResultDTO.Clone(),
        NSCGainDTOItems = [.. NSCGainDTOItems.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        AfMotor = AfMotor,
        EcsMotorPositionRelationSlope = EcsMotorPositionRelationSlope,
        EcsMotorPositionRelationIntercept = EcsMotorPositionRelationIntercept,
        EcsMotorPositionRelationRSquare = EcsMotorPositionRelationRSquare,
        MinAFMotorAbsoluteValue = MinAFMotorAbsoluteValue,
        MaxAFMotorAbsoluteValue = MaxAFMotorAbsoluteValue,
        ECSMotorOrigins = [.. ECSMotorOrigins],
        FitECSMotorOrigins = [.. FitECSMotorOrigins],
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserAutoFocus AdaptTo() => new()
    {
        MiddleCurrentA = CurrentADTO.ResultDTO.Current,
        MiddleCurrentB = CurrentBDTO.ResultDTO.Current,
        LowCoefficient = LowCoefficient,
        HighCoefficient = HighCoefficient,
        NscGain = NSCGainResultDTO.NscGain,
        Slope = EcsMotorPositionRelationSlope,
        MinAFMotorAbsoluteValue = MinAFMotorAbsoluteValue,
        MaxAFMotorAbsoluteValue = MaxAFMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class DarkAutoFocusCurrentDTO : ObservableObject, ICloneable<DarkAutoFocusCurrentDTO>
{
    [ObservableProperty]
    public partial DarkAutoFocusCurrentDTOItem ResultDTO { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<DarkAutoFocusCurrentDTOItem> CurrentItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> FitCurrentPointsF { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> FitCurrentPointsN { get; set; } = [];

    [ObservableProperty]
    public partial double SlopeF { get; set; }

    [ObservableProperty]
    public partial double InterceptF { get; set; }

    [ObservableProperty]
    public partial double RSquaredF { get; set; }

    [ObservableProperty]
    public partial double SlopeN { get; set; }

    [ObservableProperty]
    public partial double InterceptN { get; set; }

    [ObservableProperty]
    public partial double RSquaredN { get; set; }

    [ObservableProperty]
    public partial Point FDomain { get; set; }

    [ObservableProperty]
    public partial Point NDomain { get; set; }

    [ObservableProperty]
    public partial Point CurrentDomain { get; set; }

    partial void OnCurrentItemsChanged(IReadOnlyList<DarkAutoFocusCurrentDTOItem> value) => RefreshPlot();

    partial void OnFitCurrentPointsFChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnFitCurrentPointsNChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnFDomainChanged(Point value) => RefreshPlot();

    partial void OnNDomainChanged(Point value) => RefreshPlot();

    partial void OnCurrentDomainChanged(Point value) => RefreshPlot();


#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public DarkAutoFocusCurrentDTO()
    {
        PlotDataSource.Configure(new Columns());

        PlotDataSource.SetTitle(0, "Current (Y: F/N - X: Current(mA))");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear(0);

            if (CurrentItems.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "F",
                    [.. CurrentItems.Select(t => new Point(t.Current, t.F))],
                    0,
                    new Range(0, CurrentItems.Count - 1));

            if (CurrentItems.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "N",
                    [.. CurrentItems.Select(t => new Point(t.Current, t.N))],
                    1,
                    new Range(0, CurrentItems.Count - 1));


            if (FitCurrentPointsF.Count > 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    $"F Fit Curve: y = {SlopeF:0.######}x + {InterceptF:0.######} r^2 = {RSquaredF:0.######})",
                    FitCurrentPointsF,
                    Constants.Category10.GetColor(1));

            if (FitCurrentPointsN.Count > 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    $"N Fit Curve: y = {SlopeN:0.######}x + {InterceptN:0.######} r^2 = {RSquaredN:0.######})",
                    FitCurrentPointsN,
                    Constants.Category10.GetColor(2));

            if (FDomain == Point.Origin || NDomain == Point.Origin) return;

            var yLines = PlotDataSource.GetOrAddXLines(0, 4);

            if (FDomain != Point.Origin)
            {
                yLines[0].Update(
                    "F Start",
                    FDomain.X,
                    Colors.Blue);
                yLines[1].Update(
                    "F Stop",
                    FDomain.Y,
                    Colors.Blue);
            }

            if (NDomain != Point.Origin)
            {
                yLines[2].Update(
                    "N Start",
                    NDomain.X,
                    Colors.Red);
                yLines[3].Update(
                    "N Stop",
                    NDomain.Y,
                    Colors.Red);
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public DarkAutoFocusCurrentDTO Clone() => new()
    {
        ResultDTO = ResultDTO.Clone(),
        CurrentItems = [.. CurrentItems.Select(t => t.Clone())],
        FitCurrentPointsF = [.. FitCurrentPointsF],
        SlopeF = SlopeF,
        InterceptF = InterceptF,
        RSquaredF = RSquaredF,
        FitCurrentPointsN = [.. FitCurrentPointsN],
        SlopeN = SlopeN,
        InterceptN = InterceptN,
        RSquaredN = RSquaredN,
        FDomain = FDomain,
        NDomain = NDomain,
        CurrentDomain = CurrentDomain
    };

    public object ToFlatnessHtmlAnonymous() => new
    {
        ResultDTO.Current,
        ResultDTO.F,
        ResultDTO.N,
        Plot = new HtmlContainer([.. PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
    };
}

public sealed partial class DarkAutoFocusCurrentDTOItem : ObservableObject, ICloneable<DarkAutoFocusCurrentDTOItem>
{
    [ObservableProperty]
    public partial double Current { get; set; }

    [ObservableProperty]
    public partial double F { get; set; }

    [ObservableProperty]
    public partial double N { get; set; }

    public DarkAutoFocusCurrentDTOItem Clone() => new()
    {
        Current = Current,
        F = F,
        N = N
    };
}

public sealed partial class DarkAutoFocusNSCDTO : ObservableObject, ICloneable<DarkAutoFocusNSCDTO>
{
    #region NscGain

    [ObservableProperty]
    public partial double NscOffset { get; set; }

    [ObservableProperty]
    public partial double NscGain { get; set; }

    [ObservableProperty]
    public partial double NscCurrentNscPerNm { get; set; }

    [ObservableProperty]
    public partial double NscCurrentSymmetryRatio { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<double> CalibrationEcs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> CalibrationNsc { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> CalibrationLvdt { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> CalibrationFa { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> CalibrationNa { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> CalibrationFb { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> CalibrationNb { get; set; } = [];

    [ObservableProperty]
    public partial Point[] CalibrationEcsNscPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point[] CalibrationEcsNscMaxMins { get; set; } = [];

    partial void OnCalibrationEcsChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnCalibrationNscChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnCalibrationLvdtChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnCalibrationFaChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnCalibrationNaChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnCalibrationFbChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnCalibrationNbChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnCalibrationEcsNscPointsChanged(Point[] value) => RefreshPlot();

    partial void OnCalibrationEcsNscMaxMinsChanged(Point[] value) => RefreshPlot();

    #endregion NscGain

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079


    public DarkAutoFocusNSCDTO()
    {
        PlotDataSource.Configure(new Rows(), 2);

        PlotDataSource.SetTitle(0, "Trace Buffers");
        PlotDataSource.SetTitle(1, "Ecs Nsc Curve And Slope");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear(0);
            PlotDataSource.Clear(1);

            if (CalibrationEcs.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "ECS",
                    [.. CalibrationEcs.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(0));

            if (CalibrationNsc.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "NSC",
                    [.. CalibrationNsc.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(1));

            if (CalibrationLvdt.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "LVDT",
                    [.. CalibrationLvdt.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(2));

            if (CalibrationFa.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "FA",
                    [.. CalibrationFa.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(3));

            if (CalibrationFb.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "FB",
                    [.. CalibrationFb.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(4));

            if (CalibrationNa.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "NA",
                    [.. CalibrationNa.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(5));

            if (CalibrationNb.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "NB",
                    [.. CalibrationNb.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(6));

            if (CalibrationEcsNscPoints.Length != 0)
                PlotDataSource.GetOrAddScatterLine(1,
                    "Ecs Nsc Curve",
                    [.. CalibrationEcsNscPoints],
                    Constants.Category10.GetColor(1));

            if (CalibrationEcsNscMaxMins.Length != 0)
                PlotDataSource.GetOrAddScatterLine(1,
                    "Ecs Nsc Slope",
                    [.. CalibrationEcsNscMaxMins],
                    Constants.Category10.GetColor(2));
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public DarkAutoFocusNSCDTO Clone() => new()
    {
        NscOffset = NscOffset,
        NscGain = NscGain,
        NscCurrentNscPerNm = NscCurrentNscPerNm,
        NscCurrentSymmetryRatio = NscCurrentSymmetryRatio,
        CalibrationEcs = [.. CalibrationEcs],
        CalibrationNsc = [.. CalibrationNsc],
        CalibrationLvdt = [.. CalibrationLvdt],
        CalibrationFa = [.. CalibrationFa],
        CalibrationNa = [.. CalibrationNa],
        CalibrationFb = [.. CalibrationFb],
        CalibrationNb = [.. CalibrationNb],
        CalibrationEcsNscPoints = [.. CalibrationEcsNscPoints],
        CalibrationEcsNscMaxMins = [.. CalibrationEcsNscMaxMins]
    };
}