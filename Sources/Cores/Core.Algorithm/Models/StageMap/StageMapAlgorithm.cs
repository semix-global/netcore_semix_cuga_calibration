using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using HalconDotNet;
using Local.SQL.Cache.Providers.Services.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WaferMap.WPF.Primitives;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Python.Runtime;
using System.Collections;
using System.IO;
using System.Reflection;
using Constants = Net.Utilities.Models.Constants;

namespace Core.Algorithm.Models.StageMap;

public static class StageMapAlgorithm
{
    private static readonly string ClosedLoopCalibrationPythonScript = AssemblyHelper.GetEmbeddedResource("Assets.Python.closed_loop_calibration.py");

    private static void ProcessFirstMeasurement(StageMap stageMap)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_first_measurement");

        using var pyResidual = stageMap.ToPythonErrorMatrix();
        using var pyDesiredPositions = stageMap.ToPythonIdealMatrix();
        using var pyMask = stageMap.ToPythonIsMatchMatrix();
        using var pyFillValue = 0d.ToPython();
        using var result = process.Invoke(pyResidual, pyDesiredPositions, pyMask, pyFillValue);

        stageMap.ApplyPythonErrorMatrix(result);
    }

    private bool ProcessStage2Residuals(IReadOnlyList<StageMap> historyStageMaps, StageMap scanStageMap)
    {
        Guard.IsNotEmpty(historyStageMaps);

        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_stage2_residuals");

        using var pyResiduals = new PyList();
        using var pyMasks = new PyList();
        foreach (var stageMap in historyStageMaps)
        {
            using var pyResidual = stageMap.ToPythonErrorMatrix();
            using var pyMask = stageMap.ToPythonIsMatchMatrix();

            pyResiduals.Append(pyResidual);
            pyMasks.Append(pyMask);
        }

        using var pyDesiredPositions = scanStageMap.ToPythonIdealMatrix();
        using var pyTargetValidCount = Cache.StageMapRepeatTimes.ToPython();
        using var result = process.Invoke(pyResiduals, pyDesiredPositions, pyTargetValidCount, pyMasks);

        using var pyNeedMoreMeasurement = Guard.IsNotNullAndReturn(result[0]);
        using var pyResidualTable = Guard.IsNotNullAndReturn(result[1]);
        using var pyStage2ValidMask = Guard.IsNotNullAndReturn(result[2]);

        var needMoreMeasurement = pyNeedMoreMeasurement.As<bool>();

        scanStageMap.ApplyPythonErrorMatrix(pyResidualTable);
        scanStageMap.ApplyPythonIsMatchMatrix(pyStage2ValidMask);

        return needMoreMeasurement == false;
    }

    private void CombineCorrectionTables(StageMap residualStageMap)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var combine = module.GetAttr("combine_correction_tables");

        // 阶段 1 mask 表示初始修正值的来源, 阶段 2 mask 表示残差是否可参与合并.
        using var pyInitialCorrection = Cache.StageMap.ToPythonErrorMatrix();
        using var pyResidualTable = residualStageMap.ToPythonErrorMatrix();
        using var pyStage1Mask = Cache.StageMap.ToPythonIsMatchMatrix();
        using var pyStage2ValidMask = residualStageMap.ToPythonIsMatchMatrix();
        using var pyXGroupSize = Cache.StageMapTemplates.Length.ToPython();
        using var result = combine.Invoke(pyInitialCorrection, pyResidualTable, pyStage1Mask, pyStage2ValidMask, pyXGroupSize);

        using var pyFinalCorrection = Guard.IsNotNullAndReturn(result[0]);
        using var pyInterpolationMask = Guard.IsNotNullAndReturn(result[1]);

        Cache.StageMap.ApplyPythonErrorMatrix(pyFinalCorrection);
        Cache.StageMap.ApplyPythonIsMatchMatrix(pyInterpolationMask);
    }

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
}