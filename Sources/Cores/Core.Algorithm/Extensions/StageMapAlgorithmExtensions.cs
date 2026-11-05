using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.StageMap;
using MathNet.Numerics;
using Net.Utilities.Calibration;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Python.Runtime;

namespace Core.Algorithm.Extensions;

public static class StageMapAlgorithmExtensions
{
    private static readonly string ClosedLoopCalibrationPythonScript = AssemblyHelper.GetEmbeddedResource("Assets.Python.closed_loop_calibration.py");

    extension(StageMap @this)
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

        public bool CanRepeatAndCalculateErrorMatrix(IReadOnlyList<StageMap> historyStageMaps, int stageMapRepeatTimes)
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

        public void MergeErrorMatrix(StageMap residualStageMap, int stageMapTemplateCount)
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

        /// <summary>
        /// 外扩当前暗场标准网格以覆盖全部明场网格点, 并插值明场误差.
        /// </summary>
        /// <param name="brightFieldStageMap">用于插值的明场地图.</param>
        /// <param name="brightFieldTargetCircle">明场目标圆, 限定插值有效区域.</param>
        /// <param name="darkFieldTargetCircle">构建暗场标准网格时使用的目标圆.</param>
        public StageMap Expand(StageMap brightFieldStageMap, Circle brightFieldTargetCircle, Circle darkFieldTargetCircle)
        {
            var (darkFieldYLength, darkFieldXLength) = @this.IdealMatrix.GetYXLength();
            Guard.IsGreaterThan(darkFieldYLength, 1);
            Guard.IsGreaterThan(darkFieldXLength, 1);

            // 使用暗场标准网格的原点, 步长和行列数.
            var standardStageMap = @this.AdaptTo(darkFieldTargetCircle);
            var brightFieldPoints = brightFieldStageMap.IdealMatrix.SelectMany(row => row).ToArray();
            Guard.IsNotEmpty(brightFieldPoints);

            var xStep = standardStageMap.XStep;
            var yStep = standardStageMap.YStep;
            var minX = brightFieldPoints.Min(point => point.X);
            var maxX = brightFieldPoints.Max(point => point.X);
            var minY = brightFieldPoints.Min(point => point.Y);
            var maxY = brightFieldPoints.Max(point => point.Y);
            var endX = standardStageMap.BaseX + (darkFieldXLength - 1) * xStep;
            var endY = standardStageMap.BaseY + (darkFieldYLength - 1) * yStep;

            // 四个方向仅补足覆盖全部明场网格点所需的整行和整列.
            var leftColumnCount = checked((int)Math.Max(0d, Math.Ceiling((standardStageMap.BaseX - minX) / xStep)));
            var rightColumnCount = checked((int)Math.Max(0d, Math.Ceiling((maxX - endX) / xStep)));
            var bottomRowCount = checked((int)Math.Max(0d, Math.Ceiling((standardStageMap.BaseY - minY) / yStep)));
            var topRowCount = checked((int)Math.Max(0d, Math.Ceiling((maxY - endY) / yStep)));
            var xLength = checked(darkFieldXLength + leftColumnCount + rightColumnCount);
            var yLength = checked(darkFieldYLength + bottomRowCount + topRowCount);
            var startPoint = new Point(standardStageMap.BaseX - leftColumnCount * xStep, standardStageMap.BaseY - bottomRowCount * yStep);

            var points = new Point[yLength][];
            var isInWaferMatrix = new bool[yLength][];
            var isMatchMatrix = new bool[yLength][];
            for (var y = 0; y < yLength; y++)
            {
                points[y] = new Point[xLength];
                isInWaferMatrix[y] = new bool[xLength];
                isMatchMatrix[y] = new bool[xLength];
                for (var x = 0; x < xLength; x++)
                {
                    var point = startPoint + new Vector(x * xStep, y * yStep);
                    var isInWafer = brightFieldTargetCircle.Contains(point);
                    points[y][x] = point;
                    isInWaferMatrix[y][x] = isInWafer;
                    isMatchMatrix[y][x] = isInWafer;
                }
            }

            // 只在明场目标圆内插值, 圆外点保持无效.
            var errors = brightFieldStageMap.InterpolateErrors(points, brightFieldTargetCircle);
            var result = new StageMap
            {
                IdealMatrix = points,
                ErrorMatrix = errors,
                IsInWaferMatrix = isInWaferMatrix,
                IsMatchMatrix = isMatchMatrix
            };
            result.Refresh();

            return result;
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
            (
                0,
                points[0][0].X,
                points[0][0].Y,
                xWidth,
                yHeight,
                [
                    .. Generate.LinearRangeInt32(0, yLength - 1)
                        .Select(row => new StageMapErrorRowDTO
                        (
                            row,
                            [
                                .. Generate.LinearRangeInt32(0, xLength - 1)
                                    .Select(column => new StageMapErrorColumnDTO
                                    (
                                        column,
                                        errors[row][column]
                                    ))
                            ]
                        ))
                ]
            );
        }

        #endregion
    }
}