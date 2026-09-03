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
using System.IO;
using System.Reflection;

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

            var vectorFieldList = new List<(int XIndex, int YIndex, Point Point, Vector Vector, bool? IsMatch)>();

            var (yLength, xLength) = IdealMatrix.GetYXLength();

            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    vectorFieldList.Add((x, y, IdealMatrix[y][x], ErrorMatrix[y][x], IsInWaferMatrix[y][x] ? IsMatchMatrix[y][x] : null));
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
        var (yLength, xLength) = ErrorMatrix.GetYXLength();

        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                ErrorMatrix[y][x] = Vector.Zero;
                IsMatchMatrix[y][x] = false;
            }
        }

        Refresh();
    }

    public StageMap Clone() => new()
    {
        IdealMatrix =
        [
            .. IdealMatrix.Select<Point[], Point[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ],
        ErrorMatrix =
        [
            .. ErrorMatrix.Select<Vector[], Vector[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ],
        IsInWaferMatrix =
        [
            .. IsInWaferMatrix.Select<bool[], bool[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ],
        IsMatchMatrix =
        [
            .. IsMatchMatrix.Select<bool[], bool[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ]
    };

    public PyList ToPythonIdealMatrix() => ToPythonPointMatrix(IdealMatrix);

    public PyList ToPythonErrorMatrix() => ToPythonVectorMatrix(ErrorMatrix);

    public PyList ToPythonIsMatchMatrix() => ToPythonBooleanMatrix(IsMatchMatrix);

    public void ApplyPythonErrorMatrix(PyObject pyValues)
    {
        var (yLength, xLength) = ErrorMatrix.GetYXLength();

        var vectorMatrix = ToVectorMatrix(pyValues, yLength, xLength);

        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                ErrorMatrix[y][x] = vectorMatrix[y][x];
            }
        }
    }

    public void ApplyPythonIsMatchMatrix(PyObject pyValues)
    {
        var (yLength, xLength) = ErrorMatrix.GetYXLength();

        var boolMatrix = ToBoolMatrix(pyValues, yLength, xLength);

        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                IsMatchMatrix[y][x] = boolMatrix[y][x];
            }
        }
    }

    public void SubtractInplace(StageMap other)
    {
        var (yLength, xLength) = ErrorMatrix.GetYXLength();
        var (otherYLength, otherXLength) = other.ErrorMatrix.GetYXLength();
        Guard.IsEqualTo(yLength, otherYLength);
        Guard.IsEqualTo(xLength, otherXLength);

        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                var targetError = ErrorMatrix[y][x];
                var scanError = other.ErrorMatrix[y][x];
                ErrorMatrix[y][x] = new Vector(targetError.X - scanError.X, targetError.Y - scanError.Y);
            }
        }
    }

    public Vector[][] InterpolateErrors(Point[][] targetPoints, Circle targetCircle)
    {
        var (yLength, xLength) = targetPoints.GetYXLength();
        bool[][] targetMask = [.. targetPoints.Select(row => new bool[row.Length])];
        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                targetMask[y][x] = targetCircle.Contains(targetPoints[y][x]);
            }
        }

        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var interpolate = module.GetAttr("interpolate_residual_table");
        using var pyResidualTable = ToPythonErrorMatrix();
        using var pyDesiredPositions = ToPythonIdealMatrix();
        using var pyTargetPositions = ToPythonPointMatrix(targetPoints);
        using var pySourceMask = ToPythonIsMatchMatrix();
        using var pyTargetMask = ToPythonBooleanMatrix(targetMask);
        using var result = interpolate.Invoke(pyResidualTable, pyDesiredPositions, pyTargetPositions, pySourceMask, pyTargetMask);


        return ToVectorMatrix(result, yLength, xLength);
    }

    public StageMapErrorDTO AdaptTo(Circle targetCircle)
    {
        var (yLength, xLength) = IdealMatrix.GetYXLength();

        var xWidth = (IdealMatrix[0][^1].X - IdealMatrix[0][0].X) / (xLength - 1);
        var yHeight = (IdealMatrix[^1][0].Y - IdealMatrix[0][0].Y) / (yLength - 1);
        var startPoint = new Point(IdealMatrix[0][0].X, IdealMatrix[0][0].Y);

        Guard.IsGreaterThan(xWidth, 0d);
        Guard.IsGreaterThan(yHeight, 0d);

        var points = new Point[yLength][];
        for (var y = 0; y < yLength; y++)
        {
            points[y] = new Point[xLength];
            for (var x = 0; x < xLength; x++)
            {
                points[y][x] = startPoint + new Vector(x * xWidth, y * yHeight);
            }
        }

        var errors = InterpolateErrors(points, targetCircle);

        return new StageMapErrorDTO
        {
            Zone = 0,
            BaseX = points[0][0].X,
            BaseY = points[0][0].Y,
            XStep = xWidth,
            YStep = yHeight,
            Rows =
            [
                .. Generate.LinearRangeInt32(0, yLength - 1)
                    .Select(row => new StageMapErrorRowDTO
                    {
                        Id = row,
                        Cols =
                        [
                            .. Generate.LinearRangeInt32(0, xLength - 1)
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
        var (yLength, xLength) = matrix.GetYXLength();
        var result = new PyList();

        for (var y = 0; y < yLength; y++)
        {
            using var pyRow = new PyList();

            for (var x = 0; x < xLength; x++)
            {
                var point = matrix[y][x];

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

    private static PyList ToPythonVectorMatrix(Vector[][] matrix)
    {
        var (yLength, xLength) = matrix.GetYXLength();
        var result = new PyList();

        for (var y = 0; y < yLength; y++)
        {
            using var pyRow = new PyList();

            for (var x = 0; x < xLength; x++)
            {
                var vector = matrix[y][x];

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

    private static PyList ToPythonBooleanMatrix(bool[][] matrix)
    {
        var (yLength, xLength) = matrix.GetYXLength();
        var result = new PyList();

        for (var y = 0; y < yLength; y++)
        {
            using var pyRow = new PyList();

            for (var x = 0; x < xLength; x++)
            {
                using var pyValue = matrix[y][x].ToPython();
                pyRow.Append(pyValue);
            }

            result.Append(pyRow);
        }

        return result;
    }

    private static Vector[][] ToVectorMatrix(PyObject pyValues, int yLength, int xLength)
    {
        using var pyValueArray = pyValues.InvokeMethod("tolist");

        using var rows = new PyList(pyValueArray);
        Guard.IsEqualTo(rows.Length(), yLength);

        var result = new Vector[yLength][];
        for (var y = 0; y < yLength; y++)
        {
            using var pyRowObject = Guard.IsNotNullAndReturn(rows[y]);
            using var pyRow = new PyList(pyRowObject);

            Guard.IsEqualTo(pyRow.Length(), xLength);

            result[y] = new Vector[xLength];
            for (var x = 0; x < xLength; x++)
            {
                using var pyVectorObject = Guard.IsNotNullAndReturn(pyRow[x]);
                using var pyVector = new PyList(pyVectorObject);
                Guard.IsEqualTo(pyVector.Length(), 2);

                using var pyX = Guard.IsNotNullAndReturn(pyVector[0]);
                using var pyY = Guard.IsNotNullAndReturn(pyVector[1]);

                var xValue = pyX.As<double>();
                var yValue = pyY.As<double>();

                Guard.IsFalse(double.IsInfinity(xValue) || double.IsInfinity(yValue));
                Guard.IsFalse(double.IsNaN(xValue) || double.IsNaN(yValue));

                result[y][x] = new Vector(xValue, yValue);
            }
        }

        return result;
    }

    private static bool[][] ToBoolMatrix(PyObject pyValues, int yLength, int xLength)
    {
        using var pyValueArray = pyValues.InvokeMethod("tolist");

        using var rows = new PyList(pyValueArray);
        Guard.IsEqualTo(rows.Length(), yLength);

        var result = new bool[yLength][];
        for (var y = 0; y < yLength; y++)
        {
            using var pyRowObject = Guard.IsNotNullAndReturn(rows[y]);
            using var pyRow = new PyList(pyRowObject);

            Guard.IsEqualTo(pyRow.Length(), xLength);

            result[y] = new bool[xLength];
            for (var x = 0; x < xLength; x++)
            {
                using var pyBool = Guard.IsNotNullAndReturn(pyRow[x]);
                result[y][x] = pyBool.As<bool>();
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