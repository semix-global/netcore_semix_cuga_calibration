using CommunityToolkit.Diagnostics;
using MathNet.Numerics;
using Net.Utilities.Calibration;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Python.Runtime;
using Core.Models.Models.Common.StageMap;

namespace Core.Algorithm.Models.StageMap;

public static class StageMapAlgorithm
{
    private static readonly string ClosedLoopCalibrationPythonScript = AssemblyHelper.GetEmbeddedResource("Assets.Python.closed_loop_calibration.py");

    extension(CugaCalibration.ViewModels.Common.Windows.Tools.Stage.StageMap @this)
    {
        #region 算法

        public void CalculateErrorMatrix()
        {
            using var _ = Py.GIL();
            using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
            using var process = module.GetAttr("process_first_measurement");

            using var pyResidual = @this.ToPythonErrorMatrix();
            using var pyDesiredPositions = @this.ToPythonIdealMatrix();
            using var pyMask = @this.ToPythonIsMatchMatrix();
            using var pyFillValue = 0d.ToPython();
            using var result = process.Invoke(pyResidual, pyDesiredPositions, pyMask, pyFillValue);

            @this.ApplyPythonErrorMatrix(result);
        }

        public bool CanRepeatAndCalculateErrorMatrix(IReadOnlyList<CugaCalibration.ViewModels.Common.Windows.Tools.Stage.StageMap> historyStageMaps, int stageMapRepeatTimes)
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

            using var pyDesiredPositions = @this.ToPythonIdealMatrix();
            using var pyTargetValidCount = stageMapRepeatTimes.ToPython();
            using var result = process.Invoke(pyResiduals, pyDesiredPositions, pyTargetValidCount, pyMasks);

            using var pyNeedMoreMeasurement = Guard.IsNotNullAndReturn(result[0]);
            using var pyResidualTable = Guard.IsNotNullAndReturn(result[1]);
            using var pyStage2ValidMask = Guard.IsNotNullAndReturn(result[2]);

            var needMoreMeasurement = pyNeedMoreMeasurement.As<bool>();

            @this.ApplyPythonErrorMatrix(pyResidualTable);
            @this.ApplyPythonIsMatchMatrix(pyStage2ValidMask);

            return needMoreMeasurement == false;
        }

        public void MergeErrorMatrix(CugaCalibration.ViewModels.Common.Windows.Tools.Stage.StageMap residualStageMap, int stageMapTemplateCount)
        {
            using var _ = Py.GIL();
            using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
            using var combine = module.GetAttr("combine_correction_tables");

            // 阶段 1 mask 表示初始修正值的来源, 阶段 2 mask 表示残差是否可参与合并.
            using var pyInitialCorrection = @this.ToPythonErrorMatrix();
            using var pyResidualTable = residualStageMap.ToPythonErrorMatrix();
            using var pyStage1Mask = @this.ToPythonIsMatchMatrix();
            using var pyStage2ValidMask = residualStageMap.ToPythonIsMatchMatrix();
            using var pyXGroupSize = stageMapTemplateCount.ToPython();
            using var result = combine.Invoke(pyInitialCorrection, pyResidualTable, pyStage1Mask, pyStage2ValidMask, pyXGroupSize);

            using var pyFinalCorrection = Guard.IsNotNullAndReturn(result[0]);
            using var pyInterpolationMask = Guard.IsNotNullAndReturn(result[1]);

            @this.ApplyPythonErrorMatrix(pyFinalCorrection);
            @this.ApplyPythonIsMatchMatrix(pyInterpolationMask);
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
            using var pyResidualTable = @this.ToPythonErrorMatrix();
            using var pyDesiredPositions = @this.ToPythonIdealMatrix();
            using var pyTargetPositions = PythonHelper.ToPythonPointMatrix(targetPoints);
            using var pySourceMask = @this.ToPythonIsMatchMatrix();
            using var pyTargetMask = PythonHelper.ToPythonBooleanMatrix(targetMask);
            using var result = interpolate.Invoke(pyResidualTable, pyDesiredPositions, pyTargetPositions, pySourceMask, pyTargetMask);

            return PythonHelper.ToVectorMatrix(result, yLength, xLength);
        }

        private PyList ToPythonIdealMatrix() => PythonHelper.ToPythonPointMatrix(@this.IdealMatrix);

        private PyList ToPythonErrorMatrix() => PythonHelper.ToPythonVectorMatrix(@this.ErrorMatrix);

        private PyList ToPythonIsMatchMatrix() => PythonHelper.ToPythonBooleanMatrix(@this.IsMatchMatrix);

        private void ApplyPythonErrorMatrix(PyObject pyValues)
        {
            var (yLength, xLength) = @this.ErrorMatrix.GetYXLength();

            var vectorMatrix = PythonHelper.ToVectorMatrix(pyValues, yLength, xLength);

            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    @this.ErrorMatrix[y][x] = vectorMatrix[y][x];
                }
            }
        }

        private void ApplyPythonIsMatchMatrix(PyObject pyValues)
        {
            var (yLength, xLength) = @this.ErrorMatrix.GetYXLength();

            var boolMatrix = PythonHelper.ToBoolMatrix(pyValues, yLength, xLength);

            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    @this.IsMatchMatrix[y][x] = boolMatrix[y][x];
                }
            }
        }

        #endregion

        #region Mapper

        public StageMapErrorDTO AdaptTo(Circle targetCircle)
        {
            var (yLength, xLength) = @this.IdealMatrix.GetYXLength();

            var xWidth = (@this.IdealMatrix[0][^1].X - @this.IdealMatrix[0][0].X) / (xLength - 1);
            var yHeight = (@this.IdealMatrix[^1][0].Y - @this.IdealMatrix[0][0].Y) / (yLength - 1);
            var startPoint = new Point(@this.IdealMatrix[0][0].X, @this.IdealMatrix[0][0].Y);

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

            var errors = @this.InterpolateErrors(points, targetCircle);

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

        #endregion
    }
}