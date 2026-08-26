using System.IO;
using System.Reflection;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.StageMap;
using MathNet.Numerics;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Newtonsoft.Json;
using Python.Runtime;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMap : ObservableObject, ICloneable<StageMap>
{
    private static readonly string ClosedLoopCalibrationPythonScript = GetEmbeddedResource("closed_loop_calibration.py");

    public Point[][] IdealMatrix { get; set; } = [];

    public Vector[][] ErrorMatrix { get; set; } = [];

    public bool[][] IsInWaferMatrix { get; set; } = [];

    public bool[][] IsMatchMatrix { get; set; } = [];

    [JsonIgnore]
    [ObservableProperty]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    public void Refresh()
    {
        try
        {
            var vectorFields = PlotDataSource.GetOrAddVectorFields(1);

            var vectorFieldList = new List<(Point Point, Vector Vector)>();

            var (rowCount, columnCount) = IdealMatrix.GetRowColCount();

            for (var row = 0; row < rowCount; row++)
            {
                for (var column = 0; column < columnCount; column++)
                {
                    if (IsInWaferMatrix[row][column] == false) continue;

                    vectorFieldList.Add((IdealMatrix[row][column], ErrorMatrix[row][column]));
                }
            }

            vectorFields[0].Update(string.Empty, vectorFieldList);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public void Reset()
    {
        var (rowCount, columnCount) = ErrorMatrix.GetRowColCount();

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                ErrorMatrix[row][column] = Vector.Zero;
                IsMatchMatrix[row][column] = false;
            }
        }

        Refresh();
    }

    public StageMap Clone() => new()
    {
        IdealMatrix = JaggedArrayExtensions.Clone(IdealMatrix),
        ErrorMatrix = JaggedArrayExtensions.Clone(ErrorMatrix),
        IsInWaferMatrix = JaggedArrayExtensions.Clone(IsInWaferMatrix),
        IsMatchMatrix = JaggedArrayExtensions.Clone(IsMatchMatrix)
    };

    public PyList ToPythonIdealMatrix() => ToPythonPointMatrix(IdealMatrix);

    public PyList ToPythonErrorMatrix() => ToPythonVectorMatrix(ErrorMatrix);

    public void ApplyPythonErrorMatrix(PyObject pyValues)
    {
        var (rowCount, columnCount) = ErrorMatrix.GetRowColCount();

        var vectorMatrix = ToVectorMatrix(pyValues, rowCount, columnCount);

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                ErrorMatrix[row][column] = vectorMatrix[row][column];
            }
        }
    }

    public void SubtractInplace(StageMap other)
    {
        var (rowCount, columnCount) = ErrorMatrix.GetRowColCount();
        var (scanRowCount, scanColumnCount) = other.ErrorMatrix.GetRowColCount();
        Guard.IsEqualTo(scanRowCount, rowCount);
        Guard.IsEqualTo(scanColumnCount, columnCount);

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                var targetError = ErrorMatrix[row][column];
                var scanError = other.ErrorMatrix[row][column];
                ErrorMatrix[row][column] = new Vector(targetError.X - scanError.X, targetError.Y - scanError.Y);
            }
        }
    }

    public Vector[][] InterpolateErrors(Point[][] targetPoints)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var interpolate = module.GetAttr("interpolate_residual_table");
        using var pyResidualTable = ToPythonErrorMatrix();
        using var pyDesiredPositions = ToPythonIdealMatrix();
        using var pyTargetPositions = ToPythonPointMatrix(targetPoints);
        using var result = interpolate.Invoke(pyResidualTable, pyDesiredPositions, pyTargetPositions);

        var (rowCount, columnCount) = targetPoints.GetRowColCount();

        return ToVectorMatrix(result, rowCount, columnCount);
    }

    public StageMapErrorDTO AdaptTo()
    {
        var (yCount, xCount) = IdealMatrix.GetRowColCount();

        var isReverseX = IdealMatrix[0][0].X > IdealMatrix[0][1].X;

        var xWidth = isReverseX
            ? (IdealMatrix[0][0].X - IdealMatrix[0][^1].X) / (xCount - 1)
            : (IdealMatrix[0][^1].X - IdealMatrix[0][0].X) / (xCount - 1);
        var yHeight = (IdealMatrix[0][^1].Y - IdealMatrix[0][0].Y) / (yCount - 1);
        var startPoint = isReverseX
            ? new Point(IdealMatrix[0][^1].X, IdealMatrix[0][^1].Y)
            : new Point(IdealMatrix[0][0].X, IdealMatrix[0][0].Y);

        Guard.IsGreaterThan(xWidth, 0d);
        Guard.IsGreaterThan(yHeight, 0d);

        var points = new Point[yCount][];
        for (var y = 0; y < yCount; y++)
        {
            points[y] = new Point[xCount];
            for (var x = 0; x < xCount; x++)
            {
                points[y][x] = startPoint + new Vector(x * xWidth, y * yHeight);
            }
        }

        var errors = InterpolateErrors(points);

        return new StageMapErrorDTO
        {
            Zone = 0,
            BaseX = points[0][0].X,
            BaseY = points[0][0].Y,
            XStep = xWidth,
            YStep = yHeight,
            Rows =
            [
                .. Generate.LinearRangeInt32(0, yCount - 1)
                    .Select(row => new StageMapErrorRowDTO
                    {
                        Id = row,
                        Cols =
                        [
                            .. Generate.LinearRangeInt32(0, xCount - 1)
                                .Select(column => new StageMapErrorColumnDTO
                                {
                                    Id = column,
                                    Error = errors[row][column]
                                })
                        ]
                    })
            ]
        };
    }

    private static PyList ToPythonPointMatrix(Point[][] matrix)
    {
        var (rowCount, columnCount) = matrix.GetRowColCount();
        var result = new PyList();

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRow = new PyList();
            for (var column = 0; column < columnCount; column++)
            {
                var point = matrix[row][column];

                using var pyPoint = new PyList();
                using var pyX = point.X.ToPython();
                using var pyY = point.Y.ToPython();

                pyPoint.Append(pyX);
                pyPoint.Append(pyY);
                pyRow.Append(pyPoint);
            }

            result.Append(pyRow);
        }

        return result;
    }

    public static PyList ToPythonVectorMatrix(Vector[][] matrix)
    {
        var (rowCount, columnCount) = matrix.GetRowColCount();
        var result = new PyList();

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRow = new PyList();
            for (var column = 0; column < columnCount; column++)
            {
                var vector = matrix[row][column];

                using var pyVector = new PyList();
                using var pyX = vector.X.ToPython();
                using var pyY = vector.Y.ToPython();

                pyVector.Append(pyX);
                pyVector.Append(pyY);
                pyRow.Append(pyVector);
            }

            result.Append(pyRow);
        }

        return result;
    }

    public static Vector[][] ToVectorMatrix(PyObject pyValues, int rowCount, int columnCount)
    {
        using var pyValueArray = pyValues.InvokeMethod("tolist");

        using var rows = new PyList(pyValueArray);
        Guard.IsEqualTo(rows.Length(), rowCount);

        var result = new Vector[rowCount][];
        for (var row = 0; row < rowCount; row++)
        {
            using var pyRowObject = Guard.IsNotNullAndReturn(rows[row]);
            using var pyRow = new PyList(pyRowObject);

            Guard.IsEqualTo(pyRow.Length(), columnCount);

            result[row] = new Vector[columnCount];
            for (var column = 0; column < columnCount; column++)
            {
                using var pyVectorObject = Guard.IsNotNullAndReturn(pyRow[column]);
                using var pyVector = new PyList(pyVectorObject);
                Guard.IsEqualTo(pyVector.Length(), 2);

                using var pyX = Guard.IsNotNullAndReturn(pyVector[0]);
                using var pyY = Guard.IsNotNullAndReturn(pyVector[1]);

                var x = pyX.As<double>();
                var y = pyY.As<double>();

                Guard.IsFalse(double.IsInfinity(x) || double.IsInfinity(y));
                Guard.IsFalse(double.IsNaN(x) || double.IsNaN(y));

                result[row][column] = new Vector(x, y);
            }
        }

        return result;
    }

    private static string GetEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().SingleOrDefault(t => t.EndsWith($".Assets.Python.{fileName}", StringComparison.OrdinalIgnoreCase));
        Guard.IsNotNull(resourceName);

        using var stream = assembly.GetManifestResourceStream(resourceName);
        Guard.IsNotNull(stream);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}