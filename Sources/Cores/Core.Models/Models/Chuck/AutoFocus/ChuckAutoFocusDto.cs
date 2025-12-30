using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.AutoFocus;

public sealed partial class ChuckAutoFocusDto : CalibrationDtoBase, ICloneable<ChuckAutoFocusDto>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private int _rowNumber;

    [ObservableProperty]
    private int _columnNumber;

    [ObservableProperty]
    private double _chuckDiameter;

    [ObservableProperty]
    private double _columnCellWidth;

    [ObservableProperty]
    private double _rowCellHeight;

    [ObservableProperty]
    private List<ChuckAutoFocusItemDto> _map = [];

    #region Mapper

    public ChuckAutoFocusDto Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        RowNumber = RowNumber,
        ColumnNumber = ColumnNumber,
        ChuckDiameter = ChuckDiameter,
        ColumnCellWidth = ColumnCellWidth,
        RowCellHeight = RowCellHeight,
        Map = [.. Map.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}

public sealed partial class ChuckAutoFocusItemDto : ObservableCacheBase, ICloneable<ChuckAutoFocusItemDto>
{
    [ObservableProperty]
    private int _row;

    [ObservableProperty]
    private int _column;

    [ObservableProperty]
    private Point _position;

    [ObservableProperty]
    private double _ecsValue;

    [ObservableProperty]
    private bool _isInscribedSquareSide;

    #region Mapper

    public ChuckAutoFocusItemDto Clone() => new()
    {
        Row = Row,
        Column = Column,
        Position = Position,
        EcsValue = EcsValue,
        IsInscribedSquareSide = IsInscribedSquareSide,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}