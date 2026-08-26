using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
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

    public PyList ToPythonErrorMatrix()
    {
        var (rowCount, columnCount) = ErrorMatrix.GetRowColCount();
        var result = new PyList();

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRow = new PyList();
            for (var column = 0; column < columnCount; column++)
            {
                var vector = ErrorMatrix[row][column];

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

    public PyList ToPythonIdealMatrix()
    {
        var (rowCount, columnCount) = IdealMatrix.GetRowColCount();
        var result = new PyList();

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRow = new PyList();
            for (var column = 0; column < columnCount; column++)
            {
                var point = IdealMatrix[row][column];

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
        using var pyValueArray = pyValues.InvokeMethod("tolist");

        using var rows = new PyList(pyValueArray);
        var (rowCount, columnCount) = ErrorMatrix.GetRowColCount();
        Guard.IsEqualTo(rows.Length(), rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRowObject = Guard.IsNotNullAndReturn(rows[row]);
            using var pyRow = new PyList(pyRowObject);

            Guard.IsEqualTo(pyRow.Length(), columnCount);

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

                ErrorMatrix[row][column] = new Vector(x, y);
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
}