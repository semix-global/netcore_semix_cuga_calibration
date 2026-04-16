using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Recipe.Models.Wafer.WaferMap;

public sealed partial class WaferMapDataDTO : ObservableValidator, ICloneable<WaferMapDataDTO>, IAdaptIn<WaferMapDataDTO, WaferMapDataDTO>
{
    [ObservableProperty]
    private Point _waferCircleCenter;

    [ObservableProperty]
    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Diameter: ")]
    private double _waferDiameter = 300000;

    [ObservableProperty]
    private double _edgeReduceDiePitchNumber;

    #region Die

    [ObservableProperty]
    private Point _waferOriginalDiePoint;

    [ObservableProperty]
    private Point _waferReticleOriginalDiePoint;

    [ObservableProperty]
    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Cell Die Width: ")]
    private double _cellDieWidth = 5100;

    [ObservableProperty]
    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Cell Die Height: ")]
    private double _cellDieHeight = 16600;

    [ObservableProperty]
    private double _dieScribeWidth;

    [ObservableProperty]
    private double _dieScribeHeight;

    public double DiePitchWidth => CellDieWidth + DieScribeWidth;

    public double DiePitchHeight => CellDieHeight + DieScribeHeight;

    public int CellDiePitchRowNumber => (int)(WaferDiameter / DiePitchHeight);

    public int CellDiePitchColumnNumber => (int)(WaferDiameter / DiePitchWidth);

    #endregion Die

    #region Reticle

    [ObservableProperty]
    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Die Width: ")]
    private double _reticleWidth = 5100;

    [ObservableProperty]
    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Die Height: ")]
    private double _reticleHeight = 16600;

    [ObservableProperty]
    private double _reticleScribeWidth;

    [ObservableProperty]
    private double _reticleScribeHeight;

    [ObservableProperty]
    private int _referenceDieRowNumber = 1;

    [ObservableProperty]
    private int _referenceDieColumnNumber = 1;

    public double ReticlePitchWidth => ReticleWidth + ReticleScribeWidth;

    public double ReticlePitchHeight => ReticleHeight + ReticleScribeHeight;

    public int ReticleRowNumber => (int)(WaferDiameter / ReticlePitchHeight);

    public int ReticleColumnNumber => (int)(WaferDiameter / ReticlePitchWidth);

    #endregion Reticle

    public WaferMapDataDTO Clone() => new()
    {
        WaferCircleCenter = WaferCircleCenter,
        WaferDiameter = WaferDiameter,
        EdgeReduceDiePitchNumber = EdgeReduceDiePitchNumber,

        WaferOriginalDiePoint = WaferOriginalDiePoint,
        WaferReticleOriginalDiePoint = WaferReticleOriginalDiePoint,

        CellDieWidth = CellDieWidth,
        CellDieHeight = CellDieHeight,
        DieScribeWidth = DieScribeWidth,
        DieScribeHeight = DieScribeHeight,

        ReticleWidth = ReticleWidth,
        ReticleHeight = ReticleHeight,
        ReticleScribeWidth = ReticleScribeWidth,
        ReticleScribeHeight = ReticleScribeHeight,

        ReferenceDieRowNumber = ReferenceDieRowNumber,
        ReferenceDieColumnNumber = ReferenceDieColumnNumber
    };

    public WaferMapDataDTO AdaptIn(WaferMapDataDTO obj)
    {
        WaferCircleCenter = obj.WaferCircleCenter;
        WaferDiameter = obj.WaferDiameter;
        EdgeReduceDiePitchNumber = obj.EdgeReduceDiePitchNumber;
        WaferOriginalDiePoint = obj.WaferOriginalDiePoint;
        WaferReticleOriginalDiePoint = obj.WaferReticleOriginalDiePoint;
        CellDieWidth = obj.CellDieWidth;
        CellDieHeight = obj.CellDieHeight;
        DieScribeWidth = obj.DieScribeWidth;
        DieScribeHeight = obj.DieScribeHeight;
        ReticleWidth = obj.ReticleWidth;
        ReticleHeight = obj.ReticleHeight;
        ReticleScribeWidth = obj.ReticleScribeWidth;
        ReticleScribeHeight = obj.ReticleScribeHeight;
        ReferenceDieRowNumber = obj.ReferenceDieRowNumber;
        ReferenceDieColumnNumber = obj.ReferenceDieColumnNumber;

        return this;
    }
}