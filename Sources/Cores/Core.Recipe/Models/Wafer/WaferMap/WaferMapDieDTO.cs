using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Recipe.Models.Wafer.WaferMap;

public sealed partial class WaferMapDieDTO : ObservableObject, ICloneable<WaferMapDieDTO>
{
    [ObservableProperty]
    private int _rowIndex;

    [ObservableProperty]
    private int _columnIndex;

    [ObservableProperty]
    private Point _waferPosition;

    [ObservableProperty]
    private bool _isInWafer;

    public WaferMapDieDTO Clone() => new()
    {
        RowIndex = RowIndex,
        ColumnIndex = ColumnIndex,
        WaferPosition = WaferPosition,
        IsInWafer = IsInWafer
    };
}