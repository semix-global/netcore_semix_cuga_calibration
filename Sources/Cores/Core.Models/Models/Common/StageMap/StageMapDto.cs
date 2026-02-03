using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Cuga.Data.DataStruct.Stage;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.IO;
using System.Text;

namespace Core.Models.Models.Common.StageMap;

/// <summary>
/// StageMap坐标(笛卡尔坐标系)
/// </summary>
public sealed partial class StageMapDto : ObservableObject, ICloneable<StageMapDto>, IAdaptTo<Wcf.Models.Chuck.StageMap>
{
    [ObservableProperty]
    private StageMapItemDto[][] _idealStageMapItemMatrix = [];

    [ObservableProperty]
    private Point[][] _realMatrix = [];

    [ObservableProperty]
    private Point[][] _errorMatrix = [];

    [ObservableProperty]
    private int _rowNumber;

    [ObservableProperty]
    private int _columnNumber;

    [ObservableProperty]
    private double _columnCellWidth;

    [ObservableProperty]
    private double _rowCellHeight;

    [ObservableProperty]
    private string _idealCsvFilePath = string.Empty;

    [ObservableProperty]
    private string _realCsvFilePath = string.Empty;

    [ObservableProperty]
    private string _realIsInWaferOkCsvFilePath = string.Empty;

    [ObservableProperty]
    private string _realIsMatchOkCsvFilePath = string.Empty;

    [ObservableProperty]
    private string _errorCsvFilePath = string.Empty;

    public StageMapDto() : this(0, 0, 0, 0)
    {
    }

    public StageMapDto(int rowNumber, int columnNumber, double rowHeight, double columnWidth)
    {
        if (rowNumber < 0 || columnNumber < 0) ThrowHelper.ThrowArgumentOutOfRangeException("rowNumber or columnNumber must be greater than 0");
        if (columnWidth < 0 || rowHeight < 0) ThrowHelper.ThrowArgumentOutOfRangeException("columnWidth or rowHeight must be greater than 0");

        IdealStageMapItemMatrix =
        [
            ..Enumerable.Range(0, rowNumber).Select<int, StageMapItemDto[]>(_ =>
            [
                .. Enumerable.Range(0, columnNumber).Select(_ => new StageMapItemDto())
            ])
        ];

        RealMatrix =
        [
            ..Enumerable.Range(0, rowNumber).Select<int, Point[]>(_ =>
            [
                .. Enumerable.Range(0, columnNumber).Select(_ => Point.Origin)
            ])
        ];

        ErrorMatrix =
        [
            ..Enumerable.Range(0, rowNumber).Select<int, Point[]>(_ =>
            [
                .. Enumerable.Range(0, columnNumber).Select(_ => Point.Origin)
            ])
        ];

        RowNumber = rowNumber;
        ColumnNumber = columnNumber;
        ColumnCellWidth = columnWidth;
        RowCellHeight = rowHeight;
    }

    /// <summary>
    /// 以当前点为起始点生成数据
    /// </summary>
    /// <param name="startPosition">当前点的位置</param>
    /// <param name="centerPointOfCircle">圆心</param>
    /// <param name="diameter">圆直径</param>
    public void GenerateByStartPosition(Point startPosition, Point centerPointOfCircle, double diameter)
    {
        if (RowNumber < 1 || ColumnNumber < 1) ThrowHelper.ThrowArgumentOutOfRangeException("rowNumber or columnNumber must be greater than 1");

        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                IdealStageMapItemMatrix[row][column].Clear();

                var x = startPosition.X + column * ColumnCellWidth;
                var y = startPosition.Y + row * RowCellHeight;
                var idealPoint = new Point(x, y);

                IdealStageMapItemMatrix[row][column].Row = row;
                IdealStageMapItemMatrix[row][column].Column = column;
                IdealStageMapItemMatrix[row][column].Point = idealPoint;

                var isPointInCircle = new Circle(centerPointOfCircle, diameter / 2d).Contains(idealPoint);
                IdealStageMapItemMatrix[row][column].IsInWafer = isPointInCircle;

                RealMatrix[row][column] = idealPoint;
                ErrorMatrix[row][column] = Point.Origin;
            }
        }
    }

    /// <summary>
    /// 以当前点为中心生成数据
    /// </summary>
    /// <param name="startPosition">当前点的位置</param>
    /// <param name="centerPointOfCircle">圆心</param>
    /// <param name="diameter">圆直径</param>
    public void GenerateByCenterPosition(Point startPosition, Point centerPointOfCircle, double diameter)
    {
        if (RowNumber < 1 || ColumnNumber < 1) ThrowHelper.ThrowArgumentOutOfRangeException("rowNumber or columnNumber must be greater than 1");

        // 中心点的索引
        var centerX = (ColumnNumber - 1) / 2;
        var centerY = (RowNumber - 1) / 2;

        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                IdealStageMapItemMatrix[row][column].Clear();

                var x = (column - centerX) * ColumnCellWidth + startPosition.X;
                var y = (row - centerY) * RowCellHeight + startPosition.Y;
                var idealPoint = new Point(x, y);

                IdealStageMapItemMatrix[row][column].Row = row;
                IdealStageMapItemMatrix[row][column].Column = column;
                IdealStageMapItemMatrix[row][column].Point = idealPoint;

                var isPointInCircle = new Circle(centerPointOfCircle, diameter / 2d).Contains(idealPoint);
                IdealStageMapItemMatrix[row][column].IsInWafer = isPointInCircle;

                RealMatrix[row][column] = idealPoint;
                ErrorMatrix[row][column] = Point.Origin;
            }
        }
    }

    /// <summary>
    /// 重置校准数据
    /// </summary>
    public void Reset()
    {
        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                IdealStageMapItemMatrix[row][column].Reset();
                RealMatrix[row][column] = IdealStageMapItemMatrix[row][column].Point;
                ErrorMatrix[row][column] = Point.Origin;
            }
        }
    }

    #region 读取数据

    public (double[,] XArray, double[,] YArray) GetIdealArray()
    {
        // 创建二维数组
        var x = new double[RowNumber, ColumnNumber];
        var y = new double[RowNumber, ColumnNumber];

        // 填充数组
        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                var point = IdealStageMapItemMatrix[row][column].Point;
                x[row, column] = point.X;
                y[row, column] = point.Y;
            }
        }

        return (x, y);
    }

    public (double[,] XArray, double[,] YArray, double[,] isInWaferArray, double[,] templateMathIsOkArray) GetRealArray()
    {
        // 创建二维数组
        var x = new double[RowNumber, ColumnNumber];
        var y = new double[RowNumber, ColumnNumber];
        var isInWaferArray = new double[RowNumber, ColumnNumber];
        var templateMathIsOk = new double[RowNumber, ColumnNumber];

        // 填充数组
        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                var point = RealMatrix[row][column];
                x[row, column] = point.X;
                y[row, column] = point.Y;
                isInWaferArray[row, column] = Convert.ToDouble(IdealStageMapItemMatrix[row][column].IsInWafer);
                templateMathIsOk[row, column] = Convert.ToDouble(IdealStageMapItemMatrix[row][column].IsMatchOk);
            }
        }

        return (x, y, isInWaferArray, templateMathIsOk);
    }

    public (double[,] XArray, double[,] YArray) GetErrorArray()
    {
        // 创建二维数组
        var x = new double[RowNumber, ColumnNumber];
        var y = new double[RowNumber, ColumnNumber];

        // 填充数组
        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                var point = ErrorMatrix[row][column];
                x[row, column] = point.X;
                y[row, column] = point.Y;
            }
        }

        return (x, y);
    }

    public (Point[,] IdealMatrix, bool[,] IsInWaferMatrix, Point[,] ErrorMatrix) GetStageMapBilinearArray()
    {
        var idealMatrix = new Point[RowNumber, ColumnNumber];
        var isInWaferMatrix = new bool[RowNumber, ColumnNumber];
        var errorMatrix = new Point[RowNumber, ColumnNumber];

        // 填充数组
        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                idealMatrix[row, column] = IdealStageMapItemMatrix[row][column].Point;
                isInWaferMatrix[row, column] = IdealStageMapItemMatrix[row][column].IsInWafer;
                errorMatrix[row, column] = ErrorMatrix[row][column];
            }
        }

        return (idealMatrix, isInWaferMatrix, errorMatrix);
    }

    #endregion 读取数据

    #region 保存

    /// <summary>
    /// 将理想矩阵坐标写入到csv文件中
    /// </summary>
    /// <param name="filepath">文件路径</param>
    public void SaveIdealCsv(string filepath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filepath);
        FileHelper.DeleteFileIfExists(filepath);

        var sb = new StringBuilder();

        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                var point = IdealStageMapItemMatrix[row][column].Point;
                sb.Append($"{point.X:f8}^{point.Y:f8}");
                if (column != ColumnNumber - 1) sb.Append(',');
                else sb.AppendLine();
            }
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    /// <summary>
    /// 将实际矩阵写入到csv文件中
    /// </summary>
    /// <param name="filepath">文件路径</param>
    public void SaveRealCsv(string filepath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filepath);
        FileHelper.DeleteFileIfExists(filepath);

        var sb = new StringBuilder();

        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                var point = RealMatrix[row][column];
                sb.Append($"{point.X:f8}^{point.Y:f8}");
                if (column != ColumnNumber - 1) sb.Append(',');
                else sb.AppendLine();
            }
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    /// <summary>
    /// 将实际矩阵写入到csv文件中
    /// </summary>
    /// <param name="filepath">文件路径</param>
    public void SaveIsInWaferOkCsv(string filepath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filepath);
        FileHelper.DeleteFileIfExists(filepath);

        var sb = new StringBuilder();

        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                sb.Append($"{Convert.ToDouble(IdealStageMapItemMatrix[row][column].IsInWafer)}");
                if (column != ColumnNumber - 1) sb.Append(',');
                else sb.AppendLine();
            }
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    /// <summary>
    /// 将实际矩阵写入到csv文件中
    /// </summary>
    /// <param name="filepath">文件路径</param>
    public void SaveIsMatchOkCsv(string filepath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filepath);
        FileHelper.DeleteFileIfExists(filepath);

        var sb = new StringBuilder();

        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                sb.Append($"{Convert.ToDouble(IdealStageMapItemMatrix[row][column].IsMatchOk)}");
                if (column != ColumnNumber - 1) sb.Append(',');
                else sb.AppendLine();
            }
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    /// <summary>
    /// 将实际矩阵写入到csv文件中
    /// </summary>
    /// <param name="filepath">文件路径</param>
    public void SaveErrorCsv(string filepath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filepath);
        FileHelper.DeleteFileIfExists(filepath);

        var sb = new StringBuilder();

        for (var row = 0; row < RowNumber; row++)
        {
            for (var column = 0; column < ColumnNumber; column++)
            {
                var point = ErrorMatrix[row][column];
                sb.Append($"{point.X:f8}^{point.Y:f8}");
                if (column != ColumnNumber - 1) sb.Append(',');
                else sb.AppendLine();
            }
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    #endregion 保存

    #region Mapper

    public StageMapDto Clone() => new()
    {
        IdealStageMapItemMatrix =
        [
            .. IdealStageMapItemMatrix.Select<StageMapItemDto[], StageMapItemDto[]>(t =>
            [
                .. t.Select(tt => tt.Clone())
            ])
        ],
        RealMatrix =
        [
            .. RealMatrix.Select<Point[], Point[]>(t =>
            [
                .. t
            ])
        ],
        ErrorMatrix =
        [
            .. ErrorMatrix.Select<Point[], Point[]>(t =>
            [
                .. t
            ])
        ],
        RowNumber = RowNumber,
        ColumnNumber = ColumnNumber,
        ColumnCellWidth = ColumnCellWidth,
        RowCellHeight = RowCellHeight,
        IdealCsvFilePath = IdealCsvFilePath,
        RealCsvFilePath = RealCsvFilePath,
        RealIsMatchOkCsvFilePath = RealIsMatchOkCsvFilePath,
        ErrorCsvFilePath = ErrorCsvFilePath
    };

    public Wcf.Models.Chuck.StageMap AdaptTo() => new()
    {
        IdealBasePoint = (IdealStageMapItemMatrix.ElementAtOrDefault(0)?.ElementAtOrDefault(0)?.Point ?? Point.Origin).ToCgPoint(),
        RowNumber = RowNumber,
        ColumnNumber = ColumnNumber,
        ColumnCellWidth = ColumnCellWidth,
        RowCellHeight = RowCellHeight,
        ErrorMatrix =
        [
            .. ErrorMatrix.Select<Point[], CgPoint[]>(t =>
            [
                ..t.Select(tt => tt.ToCgPoint())
            ])
        ]
    };

    #endregion Mapper
}