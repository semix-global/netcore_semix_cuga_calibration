using CommunityToolkit.Diagnostics;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Python.Runtime;

namespace Net.Utilities.Calibration;

public static class PythonHelper
{
    public static void Initialize()
    {
        var pythonDllFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\Python314\python314.dll");
        var pythonHome = Path.GetDirectoryName(pythonDllFilePath);
        Guard.IsTrue(File.Exists(pythonDllFilePath));
        Guard.IsTrue(Directory.Exists(pythonHome));

        Runtime.PythonDLL = pythonDllFilePath;
        PythonEngine.PythonHome = pythonHome;

        PythonEngine.Initialize();
        _ = PythonEngine.BeginAllowThreads();
    }

    public static PyList ToPythonPointMatrix(Point[][] matrix)
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

    public static PyList ToPythonVectorMatrix(Vector[][] matrix)
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

    public static PyList ToPythonBooleanMatrix(bool[][] matrix)
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

    public static Vector[][] ToVectorMatrix(PyObject pyValues, int yLength, int xLength)
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

    public static bool[][] ToBoolMatrix(PyObject pyValues, int yLength, int xLength)
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

    public static double[] ToDoubles(PyObject pyValues)
    {
        using var values = new PyList(pyValues);
        var result = new double[values.Length()];

        for (var index = 0; index < result.Length; index++)
        {
            using var value = Guard.IsNotNullAndReturn(values[index]);

            result[index] = value.As<double>();
        }

        return result;
    }
}