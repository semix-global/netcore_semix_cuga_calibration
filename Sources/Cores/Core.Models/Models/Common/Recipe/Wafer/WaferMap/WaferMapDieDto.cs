using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Recipe.Wafer.WaferMap;

public sealed partial class WaferMapDieItemDto : ObservableCacheBase, ICloneable<WaferMapDieItemDto>
{
    [ObservableProperty]
    private int _rowIndex;

    [ObservableProperty]
    private int _columnIndex;

    [ObservableProperty]
    private Point _waferPosition;

    [ObservableProperty]
    private bool _isInWafer;

    public WaferMapDieItemDto Clone() => new()
    {
        RowIndex = RowIndex,
        ColumnIndex = ColumnIndex,
        WaferPosition = WaferPosition,
        IsInWafer = IsInWafer
    };
}