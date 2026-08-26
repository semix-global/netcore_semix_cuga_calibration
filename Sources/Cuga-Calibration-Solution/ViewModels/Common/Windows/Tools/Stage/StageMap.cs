using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.StageMap;
using Cuga.Data.DataStruct.Stage;
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

public sealed partial class StageMap : ObservableObject, ICloneable<StageMap>, IAdaptTo<CgErrorMapDto>
{
    public Point[][] IdealMatrix { get; set; } = [];

    public Vector[][] ErrorMatrix { get; set; } = [];

    public bool[][] IsInWaferMatrix { get; set; } = [];

    public bool[][] IsMatchMatrix { get; set; } = [];

    public int TemplateCount { get; set; } = 1;

    public double ColumnCellWidth { get; set; }

    public double RowCellHeight { get; set; }

    private static readonly string ClosedLoopCalibrationPythonScript = GetEmbeddedResource("closed_loop_calibration.py");

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
        IsMatchMatrix = JaggedArrayExtensions.Clone(IsMatchMatrix),
        TemplateCount = TemplateCount,
        ColumnCellWidth = ColumnCellWidth,
        RowCellHeight = RowCellHeight
    };

    public Vector GetError(Point point)
    {
        var (desiredPositions, residualTable) = CreateInterpolationSource();

        return InterpolateErrors(residualTable, desiredPositions, [[point]])[0][0];
    }

    public CgErrorMapDto AdaptTo()
    {
        var (desiredPositions, residualTable) = CreateInterpolationSource();
        var (rowCount, columnCount) = desiredPositions.GetRowColCount();

        Guard.IsGreaterThan(ColumnCellWidth, 0d);
        Guard.IsGreaterThan(RowCellHeight, 0d);

        var interpolatedErrors = InterpolateErrors(residualTable, desiredPositions, desiredPositions);
        var result = new CgErrorMapDto
        {
            Zone = 0,
            BaseX = desiredPositions[0][0].X,
            BaseY = desiredPositions[0][0].Y,
            XStep = ColumnCellWidth,
            YStep = RowCellHeight
        };

        for (var row = 0; row < rowCount; row++)
        {
            var resultRow = new CgErrorMapRowDto { Id = row };

            for (var column = 0; column < columnCount; column++)
            {
                var matrixColumn = column * TemplateCount;
                var error = IsInWaferMatrix[row][matrixColumn]
                    ? interpolatedErrors[row][column]
                    : Vector.Zero;

                resultRow.Cols.Add(new CgErrorMapColDto
                {
                    Id = column,
                    Location = new CgPoint(error.X, error.Y)
                });
            }

            result.Rows.Add(resultRow);
        }

        return result;
    }

    public PyList ToPythonErrorMatrix()
        => ToPythonErrorMatrix(ErrorMatrix);

    private static PyList ToPythonErrorMatrix(Vector[][] matrix)
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

    public PyList ToPythonIdealMatrix() => ToPythonPointMatrix(IdealMatrix);

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

    public void ApplyPythonErrorMatrix(PyObject pyValues)
    {
        var (rowCount, columnCount) = ErrorMatrix.GetRowColCount();
        var values = ParsePythonErrorMatrix(pyValues, rowCount, columnCount);

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
                ErrorMatrix[row][column] = values[row][column];
        }
    }

    private (Point[][] DesiredPositions, Vector[][] ResidualTable) CreateInterpolationSource()
    {
        var (rowCount, matrixColumnCount) = IdealMatrix.GetRowColCount();
        var (errorRowCount, errorColumnCount) = ErrorMatrix.GetRowColCount();
        var (isInWaferRowCount, isInWaferColumnCount) = IsInWaferMatrix.GetRowColCount();
        var (isMatchRowCount, isMatchColumnCount) = IsMatchMatrix.GetRowColCount();

        Guard.IsGreaterThan(rowCount, 0);
        Guard.IsGreaterThan(matrixColumnCount, 0);
        Guard.IsGreaterThan(TemplateCount, 0);
        Guard.IsEqualTo(matrixColumnCount % TemplateCount, 0);
        Guard.IsEqualTo(errorRowCount, rowCount);
        Guard.IsEqualTo(errorColumnCount, matrixColumnCount);
        Guard.IsEqualTo(isInWaferRowCount, rowCount);
        Guard.IsEqualTo(isInWaferColumnCount, matrixColumnCount);
        Guard.IsEqualTo(isMatchRowCount, rowCount);
        Guard.IsEqualTo(isMatchColumnCount, matrixColumnCount);

        var columnCount = matrixColumnCount / TemplateCount;
        var desiredPositions = new Point[rowCount][];
        var residualTable = new Vector[rowCount][];

        for (var row = 0; row < rowCount; row++)
        {
            desiredPositions[row] = new Point[columnCount];
            residualTable[row] = new Vector[columnCount];

            for (var column = 0; column < columnCount; column++)
            {
                var matrixColumn = column * TemplateCount;
                desiredPositions[row][column] = IdealMatrix[row][matrixColumn];

                if (IsInWaferMatrix[row][matrixColumn] == false)
                {
                    residualTable[row][column] = Vector.Zero;
                    continue;
                }

                var errorX = 0d;
                var errorY = 0d;
                var validCount = 0;

                for (var markerIndex = 0; markerIndex < TemplateCount; markerIndex++)
                {
                    if (IsMatchMatrix[row][matrixColumn + markerIndex] == false) continue;

                    var error = ErrorMatrix[row][matrixColumn + markerIndex];
                    errorX += error.X;
                    errorY += error.Y;
                    validCount++;
                }

                residualTable[row][column] = validCount == 0
                    ? Vector.Zero
                    : new Vector(errorX / validCount, errorY / validCount);
            }
        }

        return (desiredPositions, residualTable);
    }

    private Vector[][] InterpolateErrors(Vector[][] residualTable, Point[][] desiredPositions, Point[][] targetPoints)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var interpolate = module.GetAttr("interpolate_residual_table");
        using var pyResidualTable = ToPythonErrorMatrix(residualTable);
        using var pyDesiredPositions = ToPythonPointMatrix(desiredPositions);
        using var pyTargetPositions = ToPythonPointMatrix(targetPoints);
        using var result = interpolate.Invoke(pyResidualTable, pyDesiredPositions, pyTargetPositions);

        var (rowCount, columnCount) = targetPoints.GetRowColCount();
        return ParsePythonErrorMatrix(result, rowCount, columnCount);
    }

    private static Vector[][] ParsePythonErrorMatrix(PyObject pyValues, int rowCount, int columnCount)
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
}