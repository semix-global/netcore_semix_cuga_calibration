using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Attributes.DataAnnotations;
using Net.Utilities.Constants;
using Net.Utilities.Enums.Maths;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Recipe.Wafer.WaferMap;

public sealed partial class WaferMapDataDto : ObservableCacheBase, ICloneable<WaferMapDataDto>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CellDiePicthRowNumber), nameof(CellDiePitchColumnNumber))]
    [Comparison(1000d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Diameter: ")]
    private double _waferDiameter = 300000;

    [ObservableProperty]
    private double _edgeReduceDiePiichNumber;

    #region Scribe Lines

    [ObservableProperty]
    private int _scribeLinesWidth;

    [ObservableProperty]
    private int _scribeLinesHeight = 0;

    #endregion Scribe Lines

    #region Die Pitch

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CellDiePitchColumnNumber), nameof(ReticleWidth), nameof(ReticleHeight),
        nameof(ReticleColumnNumber))]
    [Comparison(1000d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Cell Die Width: ")]
    private double _diePitchWidth = 5100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CellDiePicthRowNumber), nameof(ReticleWidth), nameof(ReticleHeight),
        nameof(ReticleRowNumber))]
    [Comparison(1000d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Cell Die Height: ")]
    private double _diePitchHeight = 16600;

    public int CellDiePicthRowNumber => (int)(WaferDiameter / DiePitchHeight);

    public int CellDiePitchColumnNumber => (int)(WaferDiameter / DiePitchWidth);

    #endregion Die Pitch

    #region Die Value

    public double CellDieWidth => DiePitchWidth - ScribeLinesWidth;

    public double CellDieHeight => DiePitchHeight - ScribeLinesHeight;

    #endregion Die Value

    #region Reticle

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReticleHeight), nameof(ReticleRowNumber))]
    private int _referenceDieRowNumber = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReticleWidth), nameof(ReticleColumnNumber))]
    private int _referenceDieColumnNumber = 1;

    public double ReticleWidth => ReferenceDieColumnNumber * DiePitchWidth;

    public double ReticleHeight => ReferenceDieRowNumber * DiePitchHeight;

    public int ReticleRowNumber => CellDiePicthRowNumber / ReferenceDieRowNumber;

    public int ReticleColumnNumber => CellDiePitchColumnNumber / ReferenceDieColumnNumber;

    public WaferMapDataDto Clone() => new()
    {
        WaferDiameter = WaferDiameter,
        EdgeReduceDiePiichNumber = EdgeReduceDiePiichNumber,
        ScribeLinesWidth = ScribeLinesWidth,
        ScribeLinesHeight = ScribeLinesHeight,
        DiePitchWidth = DiePitchWidth,
        DiePitchHeight = DiePitchHeight,
        ReferenceDieRowNumber = ReferenceDieRowNumber,
        ReferenceDieColumnNumber = ReferenceDieColumnNumber,
    };

    public override bool Equals(object? obj)
    {
        return obj is WaferMapDataDto dto &&
               CellDiePicthRowNumber == dto.CellDiePicthRowNumber &&
               CellDiePitchColumnNumber == dto.CellDiePitchColumnNumber &&
               ReticleWidth - dto.ReticleWidth < ConstantHelper.Tolerance &&
               ReticleHeight - dto.ReticleHeight - dto.ReticleWidth < ConstantHelper.Tolerance &&
               ReticleRowNumber - dto.ReticleRowNumber - dto.ReticleWidth < ConstantHelper.Tolerance &&
               ReticleColumnNumber == dto.ReticleColumnNumber &&
               WaferDiameter - dto.WaferDiameter - dto.ReticleWidth < ConstantHelper.Tolerance &&
               DiePitchWidth - dto.DiePitchWidth - dto.ReticleWidth < ConstantHelper.Tolerance &&
               DiePitchHeight - dto.DiePitchHeight - dto.ReticleWidth < ConstantHelper.Tolerance &&
               ReferenceDieRowNumber == dto.ReferenceDieRowNumber &&
               ReferenceDieColumnNumber == dto.ReferenceDieColumnNumber;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(CellDiePicthRowNumber);
        hash.Add(CellDiePitchColumnNumber);
        hash.Add(ReticleWidth);
        hash.Add(ReticleHeight);
        hash.Add(ReticleRowNumber);
        hash.Add(ReticleColumnNumber);
        hash.Add(WaferDiameter);
        hash.Add(DiePitchWidth);
        hash.Add(DiePitchHeight);
        hash.Add(ReferenceDieRowNumber);
        hash.Add(ReferenceDieColumnNumber);
        return hash.ToHashCode();
    }

    #endregion Reticle
}