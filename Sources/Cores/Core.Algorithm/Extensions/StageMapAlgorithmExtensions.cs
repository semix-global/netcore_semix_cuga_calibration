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
        /// 外扩当前暗场标准网格以覆盖全部明场网格点, 保留暗场有效区域的误差, 其余区域插值明场误差.
        /// </summary>
        /// <param name="brightFieldStageMap">用于外扩区域插值的明场地图.</param>
        /// <param name="brightFieldTargetCircle">明场目标圆, 限定明场插值的有效区域.</param>
        /// <param name="darkFieldTargetCircle">暗场目标圆, 用于构建标准网格并限定保留暗场误差的有效区域.</param>
        /// <returns>外扩后的地图, 重叠区域优先使用暗场误差.</returns>
        public StageMap Expand(StageMap brightFieldStageMap, Circle brightFieldTargetCircle, Circle darkFieldTargetCircle)
        {
            var (darkFieldYLength, darkFieldXLength) = @this.IdealMatrix.GetYXLength();
            Guard.IsGreaterThan(darkFieldYLength, 1);
            Guard.IsGreaterThan(darkFieldXLength, 1);

            var (_, baseX, baseY, xStep, yStep, darkFieldRows) = @this.AdaptTo(darkFieldTargetCircle);
            var endX = baseX + (darkFieldXLength - 1) * xStep;
            var endY = baseY + (darkFieldYLength - 1) * yStep;

            var brightFieldPoints = brightFieldStageMap.IdealMatrix.SelectMany(row => row).ToArray();
            Guard.IsNotEmpty(brightFieldPoints);
            var minX = brightFieldPoints.Min(point => point.X);
            var maxX = brightFieldPoints.Max(point => point.X);
            var minY = brightFieldPoints.Min(point => point.Y);
            var maxY = brightFieldPoints.Max(point => point.Y);

            // 四个方向仅补足覆盖全部明场网格点所需的整行和整列.
            var leftColumnCount = (int)Math.Max(0d, Math.Ceiling((baseX - minX) / xStep));
            var rightColumnCount = (int)Math.Max(0d, Math.Ceiling((maxX - endX) / xStep));
            var bottomRowCount = (int)Math.Max(0d, Math.Ceiling((baseY - minY) / yStep));
            var topRowCount = (int)Math.Max(0d, Math.Ceiling((maxY - endY) / yStep));

            var xLength = darkFieldXLength + leftColumnCount + rightColumnCount;
            var yLength = darkFieldYLength + bottomRowCount + topRowCount;
            var darkFieldStartPoint = new Point(baseX, baseY);

            var idealMatrix = new Point[yLength][];
            var isInWaferMatrix = new bool[yLength][];
            var isMatchMatrix = new bool[yLength][];
            for (var y = 0; y < yLength; y++)
            {
                idealMatrix[y] = new Point[xLength];
                isInWaferMatrix[y] = new bool[xLength];
                isMatchMatrix[y] = new bool[xLength];
                for (var x = 0; x < xLength; x++)
                {
                    var point = darkFieldStartPoint + new Vector((x - leftColumnCount) * xStep, (y - bottomRowCount) * yStep);
                    var isInWafer = brightFieldTargetCircle.Contains(point);
                    idealMatrix[y][x] = point;
                    isInWaferMatrix[y][x] = isInWafer;
                    isMatchMatrix[y][x] = isInWafer;
                }
            }

            // 先插值明场误差, 再按原暗场标准网格的行列偏移保留暗场有效区域, 包括误差为零的点.
            var errors = brightFieldStageMap.InterpolateErrors(idealMatrix, brightFieldTargetCircle);
            for (var y = 0; y < darkFieldYLength; y++)
            {
                var targetY = y + bottomRowCount;
                for (var x = 0; x < darkFieldXLength; x++)
                {
                    var targetX = x + leftColumnCount;
                    if (darkFieldTargetCircle.Contains(idealMatrix[targetY][targetX]) == false) continue;

                    errors[targetY][targetX] = darkFieldRows[y].Cols[x].Error;
                    isInWaferMatrix[targetY][targetX] = true;
                    isMatchMatrix[targetY][targetX] = true;
                }
            }

            var result = new StageMap
            {
                IdealMatrix = idealMatrix,
                ErrorMatrix = errors,
                IsInWaferMatrix = isInWaferMatrix,
                IsMatchMatrix = isMatchMatrix
            };

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