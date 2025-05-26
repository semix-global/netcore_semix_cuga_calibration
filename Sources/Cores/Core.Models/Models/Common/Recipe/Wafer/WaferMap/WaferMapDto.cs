using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Common.Recipe.Wafer.WaferMap;

public sealed partial class WaferMapDto : ObservableCacheBase, ICloneable<WaferMapDto>
{
    [ObservableProperty]
    private WaferMapDataDto _waferMapData = new();

    [ObservableProperty]
    private WaferMapDieItemDto _originDieDto = new();

    [ObservableProperty]
    private WaferMapDieItemDto _originReticleDto = new();

    [ObservableProperty]
    private List<List<WaferMapDieItemDto>> _waferMapDieDtoItemList = [];

    [ObservableProperty]
    private List<List<WaferMapDieItemDto>> _waferMapReticleDieDtoItemList = [];

    public void WaferMapInitialization(Point originDieBrightPosition)
    {
        OriginDieDto = new()
        {
            WaferPosition = originDieBrightPosition,
            IsInWafer = true,
        };
        WaferMapDieDtoItemList =
        [
            ..Enumerable.Range(0, WaferMapData.CellDiePicthRowNumber).Select<int, List<WaferMapDieItemDto>>(_ => [.. Enumerable.Range(0, WaferMapData.CellDiePitchColumnNumber).Select(_ => new WaferMapDieItemDto())])
        ];

        WaferMapReticleDieDtoItemList =
        [
            ..Enumerable.Range(0, WaferMapData.ReticleRowNumber).Select<int, List<WaferMapDieItemDto>>(_ => [.. Enumerable.Range(0, WaferMapData.ReticleColumnNumber).Select(_ => new WaferMapDieItemDto())])
        ];
    }

    /// <summary>
    /// 生成数据
    /// </summary>
    public void GenerateMapByOriginDie()
    {
        var rowNumber = WaferMapData.CellDiePicthRowNumber;
        var columnNumber = WaferMapData.CellDiePitchColumnNumber;
        var rowCellHeight = WaferMapData.DiePitchHeight;
        var columnCellWidth = WaferMapData.DiePitchWidth;
        var diameter = WaferMapData.WaferDiameter;
        var centerPosition = OriginDieDto.WaferPosition;
        if (rowNumber < 1 || columnNumber < 1) ThrowHelper.ThrowArgumentOutOfRangeException("rowNumber or columnNumber must be greater than 1");

        // 中心点的索引
        var centerX = (columnNumber - 1) / 2;
        var centerY = (rowNumber - 1) / 2;
        for (var row = 0; row < rowNumber; row++)
        {
            for (var column = 0; column < columnNumber; column++)
            {
                // 计算点的坐标 (x向左, y向上)
                var x = (column - centerX) * columnCellWidth + centerPosition.X;
                var y = (row - centerY) * rowCellHeight + centerPosition.Y;
                var idealPoint = new Point(x, y);

                WaferMapDieDtoItemList[row][column].WaferPosition = idealPoint;
                WaferMapDieDtoItemList[row][column].RowIndex = row;
                WaferMapDieDtoItemList[row][column].ColumnIndex = column;

                WaferMapDieDtoItemList[row][column].IsInWafer = idealPoint.IsPointInCircle(centerPosition, diameter);
            }
        }

        OriginDieDto = WaferMapDieDtoItemList[centerY][centerX].Clone();

        rowNumber = WaferMapData.ReticleRowNumber;
        columnNumber = WaferMapData.ReticleColumnNumber;
        rowCellHeight = WaferMapData.ReticleHeight;
        columnCellWidth = WaferMapData.ReticleWidth;
        centerX = (columnNumber - 1) / 2;
        centerY = (rowNumber - 1) / 2;
        if (rowNumber < 1 || columnNumber < 1) ThrowHelper.ThrowArgumentOutOfRangeException("rowNumber or columnNumber must be greater than 1");

        for (var row = 0; row < rowNumber; row++)
        {
            for (var column = 0; column < columnNumber; column++)
            {
                // 计算点的坐标 (x向左, y向上)
                var x = (column - centerX) * columnCellWidth + centerPosition.X;
                var y = (row - centerY) * rowCellHeight + centerPosition.Y;
                var idealPoint = new Point(x, y);

                WaferMapReticleDieDtoItemList[row][column].WaferPosition = idealPoint;
                WaferMapReticleDieDtoItemList[row][column].RowIndex = row;
                WaferMapReticleDieDtoItemList[row][column].ColumnIndex = column;

                WaferMapReticleDieDtoItemList[row][column].IsInWafer = idealPoint.IsPointInCircle(centerPosition, diameter);
            }
        }

        OriginReticleDto = WaferMapReticleDieDtoItemList[centerY][centerX].Clone();
    }

    public WaferMapDto Clone() => new()
    {
        WaferMapData = WaferMapData.Clone(),
        OriginDieDto = OriginDieDto.Clone(),
        OriginReticleDto = OriginReticleDto.Clone(),
        WaferMapDieDtoItemList = [.. WaferMapDieDtoItemList.Select(t => t.Select(t => t.Clone()).ToList())],
        WaferMapReticleDieDtoItemList = [.. WaferMapReticleDieDtoItemList.Select(t => t.Select(t => t.Clone()).ToList())]
    };
}