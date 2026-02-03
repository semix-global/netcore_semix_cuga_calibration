using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Recipe.Wafer.WaferMap;

public sealed partial class WaferMapDataDto : ObservableValidator, ICloneable<WaferMapDataDto>
{
    [ObservableProperty]
    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Diameter: ")]
    private double _waferDiameter = 300000;

    [ObservableProperty]
    private double _edgeReduceDiePiichNumber;

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

    public int CellDiePicthRowNumber => (int)(WaferDiameter / DiePitchHeight);

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

    public WaferMapDataDto Clone() => new()
    {
        WaferDiameter = WaferDiameter,
        EdgeReduceDiePiichNumber = EdgeReduceDiePiichNumber,

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
}