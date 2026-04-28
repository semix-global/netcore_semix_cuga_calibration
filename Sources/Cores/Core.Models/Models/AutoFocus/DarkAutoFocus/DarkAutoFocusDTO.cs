using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AutoFocus.DarkAutoFocus;

[CacheVersion("1.0.0")]
public sealed partial class DarkAutoFocusDTO : CalibrationDtoBase, ICloneable<DarkAutoFocusDTO>, IAdaptTo<CalibrationLaserAutoFocus>
{
    [ObservableProperty]
    private DarkAutoFocusCurrentDTO _currentADTO = new();

    [ObservableProperty]
    private DarkAutoFocusCurrentDTO _currentBDTO = new();

    public double CurrentA => CurrentADTO.ResultDTO.Current;
    public double Fa => CurrentADTO.ResultDTO.F;
    public double Na => CurrentADTO.ResultDTO.N;

    public double CurrentB => CurrentBDTO.ResultDTO.Current;
    public double Fb => CurrentBDTO.ResultDTO.F;
    public double Nb => CurrentBDTO.ResultDTO.N;

    [ObservableProperty]
    private double _lowCoefficient = 0.6d;

    [ObservableProperty]
    private double _highCoefficient = 1.5d;

    #region NSC

    #region NSC Gain

    [ObservableProperty]
    private DarkAutoFocusNSCDTO _nSCGainResultDTO = new();

    [ObservableProperty]
    private IReadOnlyList<DarkAutoFocusNSCDTO> _nSCGainDTOItems = [];

    #endregion

    #region NSC Profile

    [ObservableProperty]
    private bool _isNscUseMaxValue;

    [ObservableProperty]
    private bool _isNscUsePositiveSlope;

    [ObservableProperty]
    private double _originalSymmetryRatio;

    [ObservableProperty]
    private double _ecsToNmRange;

    [ObservableProperty]
    private double _nscStandard;

    [ObservableProperty]
    private DarkAutoFocusNSCDTO _nSCProfileResultDTO = new();

    #endregion NSC Profile

    #region ECS & Af Motor Relation

    [ObservableProperty]
    private double _ecsMotorPositionRelationSlope;

    [ObservableProperty]
    private double _ecsMotorPositionRelationIntercept;

    [ObservableProperty]
    private double _ecsMotorPositionRelationRSquare;

    [ObservableProperty]
    private double _minAFMotorAbsoluteValue;

    [ObservableProperty]
    private double _maxAFMotorAbsoluteValue;

    [ObservableProperty]
    private Point[] _eCSMotorOrigins = [];

    [ObservableProperty]
    private Point[] _fitECSMotorOrigins = [];

    partial void OnECSMotorOriginsChanged(Point[] value) => RefreshPlot();

    partial void OnFitECSMotorOriginsChanged(Point[] value) => RefreshPlot();

    #endregion

    #endregion NSC

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public DarkAutoFocusDTO()
    {
        ScatterPlotControl.Configure(new Columns());

        ScatterPlotControl.SetTitle(0, "Slope (Y: ECS - X: AF Motor)");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);

            if (ECSMotorOrigins.Length != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "Origins",
                    [.. ECSMotorOrigins],
                    0,
                    new Range(0, ECSMotorOrigins.Length - 1));

            if (FitECSMotorOrigins.Length > 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    $"F Fit Curve: y = {EcsMotorPositionRelationSlope:0.######}x + {EcsMotorPositionRelationIntercept:0.######} r^2 = {EcsMotorPositionRelationRSquare:0.######})",
                    [.. FitECSMotorOrigins],
                    1,
                    new Range(0, FitECSMotorOrigins.Length - 1));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public DarkAutoFocusDTO Clone() => new()
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
        NSCProfileResultDTO = NSCProfileResultDTO.Clone(),
        NSCGainResultDTO = NSCGainResultDTO.Clone(),
        NSCGainDTOItems = [.. NSCGainDTOItems.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
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
    private DarkAutoFocusCurrentDTOItem _resultDTO = new();

    [ObservableProperty]
    private IReadOnlyList<DarkAutoFocusCurrentDTOItem> _currentItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _fitCurrentPointsF = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _fitCurrentPointsN = [];

    [ObservableProperty]
    private double _slopeF;

    [ObservableProperty]
    private double _interceptF;

    [ObservableProperty]
    private double _rSquaredF;

    [ObservableProperty]
    private double _slopeN;

    [ObservableProperty]
    private double _interceptN;

    [ObservableProperty]
    private double _rSquaredN;

    [ObservableProperty]
    private Point _fDomain;

    [ObservableProperty]
    private Point _nDomain;

    [ObservableProperty]
    private Point _currentDomain;

    partial void OnCurrentItemsChanged(IReadOnlyList<DarkAutoFocusCurrentDTOItem> value) => RefreshPlot();

    partial void OnFitCurrentPointsFChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnFitCurrentPointsNChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnFDomainChanged(Point value) => RefreshPlot();

    partial void OnNDomainChanged(Point value) => RefreshPlot();

    partial void OnCurrentDomainChanged(Point value) => RefreshPlot();


#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public DarkAutoFocusCurrentDTO()
    {
        ScatterPlotControl.Configure(new Columns());

        ScatterPlotControl.SetTitle(0, "Current (Y: F/N - X: Current(mA))");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);

            if (CurrentItems.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "F",
                    [.. CurrentItems.Select(t => new Point(t.Current, t.F))],
                    0,
                    new Range(0, CurrentItems.Count - 1));

            if (CurrentItems.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "N",
                    [.. CurrentItems.Select(t => new Point(t.Current, t.N))],
                    1,
                    new Range(0, CurrentItems.Count - 1));


            if (FitCurrentPointsF.Count > 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    $"F Fit Curve: y = {SlopeF:0.######}x + {InterceptF:0.######} r^2 = {RSquaredF:0.######})",
                    FitCurrentPointsF,
                    Constants.Category10.GetColor(1));

            if (FitCurrentPointsN.Count > 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    $"N Fit Curve: y = {SlopeN:0.######}x + {InterceptN:0.######} r^2 = {RSquaredN:0.######})",
                    FitCurrentPointsN,
                    Constants.Category10.GetColor(2));

            if (FDomain == Point.Origin || NDomain == Point.Origin) return;

            var yLines = ScatterPlotControl.GetOrAddXLines(0, 4);

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
            ScatterPlotControl.AutoScaleRefresh();
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
        NDomain = NDomain
    };

    public object ToFlatnessHtmlAnonymous() => new
    {
        ResultDTO.Current,
        ResultDTO.F,
        ResultDTO.N,
        FDomain,
        NDomain,
        Plot = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
    };
}

public sealed partial class DarkAutoFocusCurrentDTOItem : ObservableObject, ICloneable<DarkAutoFocusCurrentDTOItem>
{
    [ObservableProperty]
    private double _current;

    [ObservableProperty]
    private double _f;

    [ObservableProperty]
    private double _n;

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
    private double _nscOffset;

    [ObservableProperty]
    private double _nscGain;

    [ObservableProperty]
    private double _nscCurrentNscPerNm;

    [ObservableProperty]
    private double _nscCurrentSymmetryRatio;

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationEcs = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationNsc = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationLvdt = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationFa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationNa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationFb = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationNb = [];

    [ObservableProperty]
    private Point[] _calibrationEcsNscPoints = [];

    [ObservableProperty]
    private Point[] _calibrationEcsNscMaxMins = [];

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
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079


    public DarkAutoFocusNSCDTO()
    {
        ScatterPlotControl.Configure(new Rows(), 2);

        ScatterPlotControl.SetTitle(0, "Trace Buffers");
        ScatterPlotControl.SetTitle(1, "Ecs Nsc Curve And Slope");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);
            ScatterPlotControl.Clear(1);

            if (CalibrationEcs.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "ECS",
                    [.. CalibrationEcs.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(0));

            if (CalibrationNsc.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "NSC",
                    [.. CalibrationNsc.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(1));

            if (CalibrationLvdt.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "LVDT",
                    [.. CalibrationLvdt.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(2));

            if (CalibrationFa.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "FA",
                    [.. CalibrationFa.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(3));

            if (CalibrationFb.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "FB",
                    [.. CalibrationFb.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(4));

            if (CalibrationNa.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "NA",
                    [.. CalibrationNa.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(5));

            if (CalibrationNb.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "NB",
                    [.. CalibrationNb.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(6));

            if (CalibrationEcsNscPoints.Length != 0)
                ScatterPlotControl.GetOrAddScatterLine(1,
                    "Ecs Nsc Curve",
                    [.. CalibrationEcsNscPoints],
                    Constants.Category10.GetColor(1));

            if (CalibrationEcsNscMaxMins.Length != 0)
                ScatterPlotControl.GetOrAddScatterLine(1,
                    "Ecs Nsc Slope",
                    [.. CalibrationEcsNscMaxMins],
                    Constants.Category10.GetColor(2));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
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
        CalibrationLvdt = [.. CalibrationLvdt]
    };
}