using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.StageMap;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace Core.Services.Implements;

/// <summary>
/// 仿射变化
/// </summary>
[IOCAppService(ServiceType = typeof(AffineTransformation), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class AffineTransformation(ILogger<AffineTransformation> logger)
{
    /// <summary>
    /// 计算StageMap误差矩阵
    /// </summary>
    /// <param name="idealXMatrix">理想X矩阵</param>
    /// <param name="idealYMatrix">理想Y矩阵</param>
    /// <param name="realXMatrix">实际X矩阵</param>
    /// <param name="realYMatrix">实际Y矩阵</param>
    /// <param name="isInWaferMatrix">是否在wafer内</param>
    /// <param name="templateMathIsOkMatrix">模板匹配是否成功矩阵</param>
    /// <param name="isXOnlyGantryError">X是否只包含gantry误差</param>
    /// <param name="htmlLogUniqueId">html记录日志的Id</param>
    /// <param name="calculateContainRowMinCount">算法行数包含最少行数</param>
    /// <param name="calculateContainColumnMinCount">算法列数包含最少列数</param>
    /// <param name="diameter">chuck直径</param>
    /// <param name="alignmentThreshold">对准精度</param>
    /// <param name="gantryThreshold">正交精度</param>
    /// <param name="scaleThreshold">比例精度</param>
    /// <returns>(误差矩阵X， 误差矩阵Y)</returns>
    public (bool IsSuccess, Matrix<double> ErrorXMatrix, Matrix<double> ErrorYMatrix) CalculateMatrixError(
        Matrix<double> idealXMatrix,
        Matrix<double> idealYMatrix,
        Matrix<double> realXMatrix,
        Matrix<double> realYMatrix,
        Matrix<double> isInWaferMatrix,
        Matrix<double> templateMathIsOkMatrix,
        bool isXOnlyGantryError,
        Guid htmlLogUniqueId,
        int calculateContainRowMinCount = 8,
        int calculateContainColumnMinCount = 8,
        double diameter = 300000d,
        double alignmentThreshold = 1.466d,
        double gantryThreshold = 1.466d,
        double scaleThreshold = 1.466d)
    {
        // 校验矩阵维度全部一致
        var rowCount = idealXMatrix.RowCount;
        var columnCount = idealXMatrix.ColumnCount;
        if (rowCount != idealYMatrix.RowCount || rowCount != realXMatrix.RowCount || rowCount != realYMatrix.RowCount || columnCount != idealYMatrix.ColumnCount || columnCount != realXMatrix.ColumnCount || columnCount != realYMatrix.ColumnCount)
            ThrowHelper.ThrowArgumentException("The matrix dimensions are inconsistent");
        if (calculateContainRowMinCount < 1 || rowCount < calculateContainRowMinCount)
            ThrowHelper.ThrowArgumentException("The calculateContainRowMinCount must be greater than 0 and less than or equal to rowCount");
        if (calculateContainColumnMinCount < 1 || columnCount < calculateContainColumnMinCount)
            ThrowHelper.ThrowArgumentException("The calculateContainColumnMinCount must be greater than 0 and less than or equal to columnCount");

        // isInWaferMatrix转换每一个数据bool类型，根据 calculateContainRowMinCount calculateContainColumnMinCount 计算满足的行数[minRow, MaxRow] 列数[minCol, MaxCol]
        var okRowIndexList = new List<int>();
        for (var row = 0; row < rowCount; row++)
        {
            if (isInWaferMatrix.Row(row).Select(Convert.ToBoolean).Count(t => t) < calculateContainColumnMinCount) continue;
            okRowIndexList.Add(row);
        }

        var okColumnIndexList = new List<int>();
        for (var column = 0; column < columnCount; column++)
        {
            if (isInWaferMatrix.Column(column).Select(Convert.ToBoolean).Count(t => t) < calculateContainRowMinCount) continue;
            okColumnIndexList.Add(column);
        }

        // okRowIndexList okColIndexList 为空，返回失败 , 里面的数据根据索引必须连续+1, 否则返回失败
        if (okRowIndexList.Count <= 0
            || okColumnIndexList.Count <= 0
            || okRowIndexList.SequenceEqual(Enumerable.Range(okRowIndexList[0], okRowIndexList.Count)) == false
            || okColumnIndexList.SequenceEqual(Enumerable.Range(okColumnIndexList[0], okColumnIndexList.Count)) == false)
            ThrowHelper.ThrowArgumentException(nameof(isInWaferMatrix));

        var minRowIndex = okRowIndexList[0];
        var maxRowIndex = okRowIndexList[^1];
        var minColumnIndex = okColumnIndexList[0];
        var maxColumnIndex = okColumnIndexList[^1];
        logger.LogHtmlInformation("Error Map Start", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            calculateRowMinCout = calculateContainRowMinCount,
            calculateColumnMinCount = calculateContainColumnMinCount,
            rowCount,
            columnCount,
            minRowIndex = minRowIndex + 1,
            maxRowIndex = maxRowIndex + 1,
            minColumnIndex = minColumnIndex + 1,
            maxColumnIndex = maxColumnIndex + 1,
            diameter = $"{diameter}um",
            alignmentThreshold = $"{alignmentThreshold}um",
            gantryThreshold = $"{gantryThreshold}um",
            scaleThreshold = $"{scaleThreshold}um"
        }), htmlLogUniqueId.LoggingHtml());

        #region 一. 原始误差矩阵

        var errorXOriginTempMatrix = realXMatrix - idealXMatrix;
        var errorYOriginTempMatrix = realYMatrix - idealYMatrix;

        // 移除统一偏差
        RemoveAverageTranslation(errorXOriginTempMatrix, errorYOriginTempMatrix, realXMatrix, realYMatrix);

        logger.LogHtmlInformation(
            "1. Origin",
            HtmlHeaderLevelEnum.Header4,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorXOriginTempMatrix, errorYOriginTempMatrix, "Origin Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorXOriginTempMatrix, "Error X"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorYOriginTempMatrix, "Error Y"),
                ErrorXRemoveAverageTranslation = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, realXMatrix - idealXMatrix, "Remove Average Translation Error X"),
                ErrorYRemoveAverageTranslation = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, realYMatrix - idealYMatrix, "Remove Average Translation Error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        #endregion 一. 原始误差矩阵

        /***********************************二. 去除实际矩阵坏点************************************************************/
        // 1. 去除Error坏点，标准为3σ原则
        // 2. 去除Error坏点后，重新计算不是坏点的平均值，用平均值替换坏点

        logger.LogHtmlInformation("2. Delete Bad Point", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

        #region 二.一. 去除X坏点

        var badXLogList = new List<(int Row, int Column, double BadValue, double NewValue, List<(string Name, Point[] PointList)> FitLine)>();
        var badXList = new List<(int Row, int Column)>();
        for (var row = 0; row < rowCount; row++)
        {
            var (isSuccess, minColIndexByRow, maxColIndexByRow, filterRow) = FilterRow(errorXOriginTempMatrix, row);
            if (isSuccess == false) continue;
            var average = filterRow.Average(); // 计算平均值
            var standardDeviation = filterRow.StandardDeviation(); // 计算标准差
            var badColumnList = new List<int>();
            var goodColumnList = new List<double>();
            var goodValueList = new List<double>();

            for (var column = minColIndexByRow; column <= maxColIndexByRow; column++)
            {
                if (Convert.ToBoolean(templateMathIsOkMatrix[row, column]) == false) // 模板匹配不成功，认为是坏点
                    badColumnList.Add(column);
                else if (Math.Abs(errorXOriginTempMatrix[row, column] - average) > 3 * standardDeviation) // 不符合3σ原则，认为是坏点
                    badColumnList.Add(column);
                else // 符合3σ原则，加入到行值列表
                {
                    goodColumnList.Add(column);
                    goodValueList.Add(errorXOriginTempMatrix[row, column]);
                }
            }

            if (badColumnList.Count <= 0) continue;
            if (goodValueList.Count < 2) // 无法行线性拟合
            {
                badXList.AddRange(badColumnList.Select(badColumn => (row, badColumn)));
                continue;
            }

            var xVector = Vector<double>.Build.DenseOfEnumerable(goodColumnList);
            var yVector = Vector<double>.Build.DenseOfEnumerable(goodValueList);
            var (k, b, rSquared, yPredicted) = PolynomialCurve.Fit1(xVector, yVector);
            foreach (var badColumn in badColumnList)
            {
                var badValue = errorXOriginTempMatrix[row, badColumn];
                errorXOriginTempMatrix[row, badColumn] = k * badColumn + b;
                badXLogList.Add((row, badColumn, badValue, errorXOriginTempMatrix[row, badColumn],
                [
                    ($"row {row}", ToPoints(xVector, yVector)),
                    ($"row {row}: y = {k:e3}x + {b:f3}, r^2 = {rSquared}", ToPoints(xVector, yPredicted))
                ]));
            }
        }

        foreach (var (badRow, badColumn) in badXList)
        {
            var (minRowIndexByColumn, maxRowIndexByColumn, filterColumn) = FilterColumn(errorXOriginTempMatrix, badColumn);
            var average = filterColumn.Average(); // 计算平均值
            var standardDeviation = filterColumn.StandardDeviation(); // 计算标准差
            var goodRowList = new List<double>();
            var goodValueList = new List<double>();

            for (var row = minRowIndexByColumn; row <= maxRowIndexByColumn; row++)
            {
                if (Math.Abs(errorXOriginTempMatrix[row, badColumn] - average) > 3 * standardDeviation) continue; // 符合3σ原则，加入到行值列表
                if (row == badRow) continue;

                goodRowList.Add(row);
                goodValueList.Add(errorXOriginTempMatrix[row, badColumn]);
            }

            if (goodValueList.Count < 2) // 无法行线性拟合
            {
                logger.LogHtmlWarning("2.1. X Warning", HtmlHeaderLevelEnum.Header5, new HtmlComment($"({badRow + 1}, {badColumn}), Dots less than 2 cannot be fitted"), htmlLogUniqueId.LoggingHtml());
                continue;
            }

            var xVector = Vector<double>.Build.DenseOfEnumerable(goodRowList);
            var yVector = Vector<double>.Build.DenseOfEnumerable(goodValueList);
            var (k, b, rSquared, yPredicted) = PolynomialCurve.Fit1(xVector, yVector);
            var badValue = errorXOriginTempMatrix[badRow, badColumn];
            errorXOriginTempMatrix[badRow, badColumn] = k * badRow + b;
            badXLogList.Add((badRow, badColumn, badValue, errorXOriginTempMatrix[badRow, badColumn],
            [
                ($"column {badColumn}", ToPoints(xVector, yVector)),
                ($"column {badColumn}: y = {k:e3}x + {b:f3}, r^2 = {rSquared}", ToPoints(xVector, yPredicted))
            ]));
        }

        logger.LogHtmlInformation(
            "2.1. X",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                BadX = new HtmlTable([
                    .. badXLogList.Select(t => new
                    {
                        Row = t.Row + 1,
                        Column = t.Column + 1,
                        t.BadValue,
                        t.NewValue,
                        FitReal = new HtmlPlot2DLinesChart([..t.FitLine], "unit: um")
                    })
                ]),
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorXOriginTempMatrix, errorYOriginTempMatrix, "Delete x bad point"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorXOriginTempMatrix, "Delete x bad point error x")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        #endregion 二.一. 去除X坏点

        #region 二.二. 去除Y坏点

        var badYLogList = new List<(int Row, int Column, double BadValue, double NewValue, List<(string Name, Point[] PointList)> FitLine)>();
        var badYList = new List<(int Row, int Column)>();
        for (var row = 0; row < rowCount; row++)
        {
            var (isSuccess, minColIndexByRow, maxColIndexByRow, filterRow) = FilterRow(errorYOriginTempMatrix, row);
            if (isSuccess == false) continue;
            var average = filterRow.Average(); // 计算平均值
            var standardDeviation = filterRow.StandardDeviation(); // 计算标准差
            var badColumnList = new List<int>();
            var goodColumnList = new List<double>();
            var goodValueList = new List<double>();

            for (var column = minColIndexByRow; column <= maxColIndexByRow; column++)
            {
                if (Convert.ToBoolean(templateMathIsOkMatrix[row, column]) == false) // 模板匹配不成功，认为是坏点
                    badColumnList.Add(column);
                else if (Math.Abs(errorYOriginTempMatrix[row, column] - average) > 3 * standardDeviation) // 不符合3σ原则，认为是坏点
                    badColumnList.Add(column);
                else // 符合3σ原则，加入到行值列表
                {
                    goodColumnList.Add(column);
                    goodValueList.Add(errorYOriginTempMatrix[row, column]);
                }
            }

            if (badColumnList.Count <= 0) continue;
            if (goodValueList.Count < 2) // 无法行线性拟合
            {
                badYList.AddRange(badColumnList.Select(badColumn => (row, badColumn)));
                continue;
            }

            var xVector = Vector<double>.Build.DenseOfEnumerable(goodColumnList);
            var yVector = Vector<double>.Build.DenseOfEnumerable(goodValueList);
            var (k, b, rSquared, yPredicted) = PolynomialCurve.Fit1(xVector, yVector);
            foreach (var badColumn in badColumnList)
            {
                var badValue = errorYOriginTempMatrix[row, badColumn];
                errorYOriginTempMatrix[row, badColumn] = k * badColumn + b;
                badYLogList.Add((row, badColumn, badValue, errorYOriginTempMatrix[row, badColumn],
                [
                    ($"row {row}", ToPoints(xVector, yVector)),
                    ($"row {row}: y = {k:e3}x + {b:f3}, r^2 = {rSquared}", ToPoints(xVector, yPredicted))
                ]));
            }
        }

        foreach (var (badRow, badColumn) in badYList)
        {
            var (minRowIndexByColumn, maxRowIndexByColumn, filterColumn) = FilterColumn(errorYOriginTempMatrix, badColumn);
            var average = filterColumn.Average(); // 计算平均值
            var standardDeviation = filterColumn.StandardDeviation(); // 计算标准差
            var goodRowList = new List<double>();
            var goodValueList = new List<double>();

            for (var row = minRowIndexByColumn; row <= maxRowIndexByColumn; row++)
            {
                if (Math.Abs(errorYOriginTempMatrix[row, badColumn] - average) > 3 * standardDeviation) continue; // 符合3σ原则，加入到行值列表
                if (row == badRow) continue;

                goodRowList.Add(row);
                goodValueList.Add(errorYOriginTempMatrix[row, badColumn]);
            }

            if (goodValueList.Count < 2) // 无法行线性拟合
            {
                logger.LogHtmlWarning("2.2. Y Warning", HtmlHeaderLevelEnum.Header5, new HtmlComment($"({badRow + 1}, {badColumn}), Dots less than 2 cannot be fitted"), htmlLogUniqueId.LoggingHtml());
                continue;
            }

            var xVector = Vector<double>.Build.DenseOfEnumerable(goodRowList);
            var yVector = Vector<double>.Build.DenseOfEnumerable(goodValueList);
            var (k, b, rSquared, yPredicted) = PolynomialCurve.Fit1(xVector, yVector);
            var badValue = errorYOriginTempMatrix[badRow, badColumn];
            errorYOriginTempMatrix[badRow, badColumn] = k * badRow + b;
            badYLogList.Add((badRow, badColumn, badValue, errorYOriginTempMatrix[badRow, badColumn],
            [
                ($"column {badColumn}", ToPoints(xVector, yVector)),
                ($"column {badColumn}: y = {k:e3}x + {b:f3}, r^2 = {rSquared}", ToPoints(xVector, yPredicted))
            ]));
        }

        logger.LogHtmlInformation(
            "2.2. Y",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                BadY = new HtmlTable([
                    .. badYLogList.Select(t => new
                    {
                        Row = t.Row + 1,
                        Column = t.Column + 1,
                        t.BadValue,
                        t.NewValue,
                        FitReal = new HtmlPlot2DLinesChart([..t.FitLine], "unit: um")
                    })
                ]),
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorXOriginTempMatrix, errorYOriginTempMatrix, "Delete y bad point"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorYOriginTempMatrix, "Delete y bad point error x")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        #endregion 二.二. 去除Y坏点

        #region 二.三. 用除坏点后的实际矩阵

        realXMatrix = idealXMatrix + errorXOriginTempMatrix;
        realYMatrix = idealYMatrix + errorYOriginTempMatrix;

        #endregion 二.三. 用除坏点后的实际矩阵

        /***********************************三. 旋转角度(去掉晶圆因为对准精度不够而带来的角度误差)校准理想矩阵************************************************************/
        // 1. 计算每一行的实际矩阵和理想矩阵的斜率
        // 2. 计算每一行的斜率差值，求平均值，得到旋转角度
        // 3. 旋转理想矩阵，使得理想矩阵和实际矩阵的斜率一致

        logger.LogHtmlInformation("3. Alignment", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

        #region 三.一. 计算理想矩阵和实际矩阵旋转角度

        var alignmentRealLineList = new List<(string Name, Point[] PointList)>();
        var alignmentIncludedDegreeAngleList = new List<Point>();
        var alignmentErrorList = new List<Point>();

        var thetaRotateVector = Vector<double>.Build.Dense(maxRowIndex - minRowIndex + 1);
        for (var row = minRowIndex; row <= maxRowIndex; row++)
        {
            var idealXRow = FilterRow(idealXMatrix, row).Result;
            var idealYRow = FilterRow(idealYMatrix, row).Result;
            var realXRow = FilterRow(realXMatrix, row).Result;
            var realYCol = FilterRow(realYMatrix, row).Result;
            var (k1, _, _, _) = PolynomialCurve.Fit1(idealXRow, idealYRow);
            var (k2, b2, rSquared2, yPredicted2) = PolynomialCurve.Fit1(realXRow, realYCol);
            alignmentRealLineList.Add(($"row {row}", ToPoints(realXRow, realYCol)));
            alignmentRealLineList.Add(($"row {row}: y = {k2:e3}x + {b2:f3}, r^2 = {rSquared2}", ToPoints(realXRow, yPredicted2)));

            thetaRotateVector[row - minRowIndex] = Math.TwoLineToIncludedRadianAngle(k2, k1); // 每行的夹角
            alignmentIncludedDegreeAngleList.Add(new Point(row, Math.RadianAngleToDegreeAngle(thetaRotateVector[row - minRowIndex])));
            alignmentErrorList.Add(new Point(row, diameter * Math.Tan(thetaRotateVector[row - minRowIndex])));
        }

        var isAlignmentSuccess = alignmentErrorList.All(t => Math.Abs(t.Y) < alignmentThreshold);
        var meanAlignmentTheta = thetaRotateVector.Average();

        var htmlBullet = new HtmlBullet(new
        {
            isAlignmentSuccess,
            meanThetaRotate = $"{Math.RadianAngleToDegreeAngle(meanAlignmentTheta):f10}°",
            FitReal = new HtmlPlot2DLinesChart([.. alignmentRealLineList], "unit: um"),
            alignmentIncludedDegreeAngleList = new HtmlPlot2DLinesChart([(nameof(alignmentIncludedDegreeAngleList), [.. alignmentIncludedDegreeAngleList])], "unit: °"),
            alignmentErrorList = new HtmlPlot2DLinesChart([(nameof(alignmentErrorList), [.. alignmentErrorList])], "unit: um")
        });

        if (isAlignmentSuccess)
        {
            logger.LogHtmlInformation(
                "3.1. Angle",
                HtmlHeaderLevelEnum.Header5,
                htmlBullet,
                htmlLogUniqueId.LoggingHtml()
            );
        }
        else
        {
            logger.LogHtmlError(
                "3.1. Angle Error",
                HtmlHeaderLevelEnum.Header5,
                htmlBullet,
                htmlLogUniqueId.LoggingHtml()
            );
        }

        #endregion 三.一. 计算理想矩阵和实际矩阵旋转角度

        #region 三.二. 旋转理想矩阵，使得理想矩阵和实际矩阵的斜率一致

        var alignmentThetaMatrix = Matrix<double>.Build.DenseOfArray(new[,]
        {
            { Math.Cos(meanAlignmentTheta), -Math.Sin(meanAlignmentTheta), 0 },
            { Math.Sin(meanAlignmentTheta), Math.Cos(meanAlignmentTheta), 0 },
            { 0, 0, 1 }
        });

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;

                var temp = alignmentThetaMatrix * Vector<double>.Build.DenseOfArray([idealXMatrix[row, column], idealYMatrix[row, column], 1]);
                idealXMatrix[row, column] = temp[0];
                idealYMatrix[row, column] = temp[1];
            }
        }

        logger.LogHtmlInformation(
            "3.2. Result",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, realXMatrix - idealXMatrix, realYMatrix - idealYMatrix, "Rotate Map"),
                ErrorXRemoveAverageTranslation = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, realXMatrix - idealXMatrix, "Remove Average Translation Rotate Error X"),
                ErrorYRemoveAverageTranslation = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, realYMatrix - idealYMatrix, "Remove Average Translation Rotate Error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        #endregion 三.二. 旋转理想矩阵，使得理想矩阵和实际矩阵的斜率一致

        /***********************************四. 正交角度(去掉晶圆因为生产精度不够gantry带来的角度误差)校准理想矩阵************************************************************/
        // 1. 计算每一列的实际矩阵和理想矩阵的斜率
        // 2. 计算每一列的斜率差值，求平均值，得到正交角度
        // 3. 补偿理想矩阵X，使得理想矩阵和实际矩阵的正交性一致

        logger.LogHtmlInformation("4. Gantry", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

        #region 四.一. 计算理想矩阵和实际矩阵正交角度

        var gantryLineList = new List<(string Name, Point[] PointList)>();
        var gantryIncludedDegreeAngleList = new List<Point>();
        var gantryErrorList = new List<Point>();

        var thetaGantryVector = Vector<double>.Build.Dense(maxColumnIndex - minColumnIndex + 1);
        for (var column = minColumnIndex; column <= maxColumnIndex; column++)
        {
            var idealYColumn = FilterColumn(idealYMatrix, column).Result;
            var idealXColumn = FilterColumn(idealXMatrix, column).Result;
            var realYColumn = FilterColumn(realYMatrix, column).Result;
            var realXColumn = FilterColumn(realXMatrix, column).Result;
            var (k1, _, _, _) = PolynomialCurve.Fit1(idealYColumn, idealXColumn);
            var (k2, b2, rSquared2, yPredicted2) = PolynomialCurve.Fit1(realYColumn, realXColumn);
            gantryLineList.Add(($"column {column}", ToPoints(realYColumn, realXColumn)));
            gantryLineList.Add(($"column {column}: y = {k2:e3}x + {b2:f3}, r^2 = {rSquared2}", ToPoints(realYColumn, yPredicted2)));

            thetaGantryVector[column - minColumnIndex] = Math.TwoLineToIncludedRadianAngle(k2, k1); // 每列的夹角

            gantryIncludedDegreeAngleList.Add(new Point(column, Math.RadianAngleToDegreeAngle(thetaGantryVector[column - minColumnIndex])));
            gantryErrorList.Add(new Point(column, diameter * Math.Tan(thetaGantryVector[column - minColumnIndex])));
        }

        var isGantrySuccess = gantryErrorList.All(t => Math.Abs(t.Y) < gantryThreshold);
        var meanGantryTheta = thetaGantryVector.Average();

        htmlBullet = new HtmlBullet(new
        {
            isGantrySuccess,
            meanThetaGantry = $"{Math.RadianAngleToDegreeAngle(meanGantryTheta):f10}°",
            Fit = new HtmlPlot2DLinesChart([.. gantryLineList], "unit: um"),
            gantryIncludedDegreeAngleList = new HtmlPlot2DLinesChart([(nameof(gantryIncludedDegreeAngleList), [.. gantryIncludedDegreeAngleList])], "unit: °"),
            gantryErrorList = new HtmlPlot2DLinesChart([(nameof(gantryErrorList), [.. gantryErrorList])], "unit: um")
        });

        if (isGantrySuccess)
        {
            logger.LogHtmlInformation(
                "4.1. Angle",
                HtmlHeaderLevelEnum.Header5,
                htmlBullet,
                htmlLogUniqueId.LoggingHtml()
            );
        }
        else
        {
            logger.LogHtmlError(
                "4.1. Angle Error",
                HtmlHeaderLevelEnum.Header5,
                htmlBullet,
                htmlLogUniqueId.LoggingHtml()
            );
        }

        #endregion 四.一. 计算理想矩阵和实际矩阵正交角度

        #region 四.二. 补偿理想矩阵X，使得理想矩阵和实际矩阵的正交性一致

        var errorGantryX = Matrix<double>.Build.Dense(rowCount, columnCount);

        var centerRow = (int)Math.Floor((rowCount - 1 + 0) / 2d);
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;

                errorGantryX[row, column] = (idealYMatrix[row, column] - idealYMatrix[centerRow, column]) * Math.Tan(meanGantryTheta);
                /*var columnIndex = column - minColumnIndex;
                errorGantryX[row, column] = (idealYMatrix[row, column] - idealYMatrix[centerRow, column])
                                            * ( columnIndex < 0 || columnIndex >= thetaGantryVector.Count
                                                ? Math.Tan(meanGantryTheta)
                                                : Math.Tan(thetaGantryVector[columnIndex]));*/
            }
        }

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;

                idealXMatrix[row, column] += errorGantryX[row, column];
            }
        }

        logger.LogHtmlInformation(
            "4.2. Gantry Error Map",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorGantryX, Matrix<double>.Build.SameAs(errorGantryX), "Scale Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorGantryX, "Gantry error X")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        logger.LogHtmlInformation(
            "4.3. Result",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, realXMatrix - idealXMatrix, realYMatrix - idealYMatrix, "Rotate Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, realXMatrix - idealXMatrix, "Rotate Error X"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, realYMatrix - idealYMatrix, "Rotate Error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        #endregion 四.二. 补偿理想矩阵X，使得理想矩阵和实际矩阵的正交性一致

        /***********************************五. 缩放系数(去掉编码器非线形误差)校准理想矩阵************************************************************/

        logger.LogHtmlInformation("5. Scale", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

        var errorXContainsScale = realXMatrix - idealXMatrix;
        var errorYContainsScale = realYMatrix - idealYMatrix;

        #region 五.一. 获取缩放系数

        var scaleXLineList = new List<(string Name, Point[] PointList)>();
        var scaleXSlopeList = new List<Point>();
        var scaleXErrorList = new List<Point>();
        var scaleYLineList = new List<(string Name, Point[] PointList)>();
        var scaleYSlopeList = new List<Point>();
        var scaleYErrorList = new List<Point>();

        var xScaleVector = Vector<double>.Build.Dense(maxRowIndex - minRowIndex + 1);
        for (var row = minRowIndex; row <= maxRowIndex; row++)
        {
            // 因为编码器非线形误差，所以不可能是y=x的关系, 所以可能会多走少走
            var idealXRow = FilterRow(idealXMatrix, row).Result;
            var realXRow = FilterRow(realXMatrix, row).Result;
            var (k, b, rSquared, yPredicted) = PolynomialCurve.Fit1(idealXRow, realXRow);
            scaleXLineList.Add(($"row {row}: y = {k:f10}x + {b:f3}, r^2 = {rSquared}", ToPoints(idealXRow, yPredicted)));

            xScaleVector[row - minRowIndex] = k;

            scaleXSlopeList.Add(new Point(row, k));
            scaleXErrorList.Add(new Point(row, diameter * (1 - k)));
        }

        var isScaleXSuccess = scaleXErrorList.All(t => Math.Abs(t.Y) < scaleThreshold);
        var meanScaleX = xScaleVector.Average();

        var yScaleVector = Vector<double>.Build.Dense(maxColumnIndex - minColumnIndex + 1);
        for (var column = minColumnIndex; column <= maxColumnIndex; column++)
        {
            var idealYColumn = FilterColumn(idealYMatrix, column).Result;
            var realYColumn = FilterColumn(realYMatrix, column).Result;
            var (k, b, rSquared, yPredicted) = PolynomialCurve.Fit1(idealYColumn, realYColumn);
            scaleYLineList.Add(($"column {column}: y = {k:f10}x + {b:f3}, r^2 = {rSquared}", ToPoints(idealYColumn, yPredicted)));

            yScaleVector[column - minColumnIndex] = k;

            scaleYSlopeList.Add(new Point(column, k));
            scaleYErrorList.Add(new Point(column, diameter * (1 - k)));
        }

        var isScaleYSuccess = scaleYErrorList.All(t => Math.Abs(t.Y) < scaleThreshold);
        var meanScaleY = yScaleVector.Average();

        htmlBullet = new HtmlBullet(new
        {
            isScaleXSuccess,
            isScaleYSuccess,
            meanScaleX = $"{meanScaleX:f10}",
            meanScaleY = $"{meanScaleY:f10}",
            FitX = new HtmlPlot2DLinesChart([.. scaleXLineList], "unit: um"),
            FitY = new HtmlPlot2DLinesChart([.. scaleYLineList], "unit: um"),
            scaleXSlopeList = new HtmlPlot2DLinesChart([(nameof(scaleXSlopeList), [.. scaleXSlopeList])], "unit: null"),
            scaleXErrorList = new HtmlPlot2DLinesChart([(nameof(scaleXErrorList), [.. scaleXErrorList])], "unit: um"),
            scaleYSlopeList = new HtmlPlot2DLinesChart([(nameof(scaleYSlopeList), [.. scaleYSlopeList])], "unit: null"),
            scaleYErrorList = new HtmlPlot2DLinesChart([(nameof(scaleYErrorList), [.. scaleYErrorList])], "unit: um")
        });

        if (isScaleXSuccess && isScaleYSuccess)
        {
            logger.LogHtmlInformation(
                "5.1. Angle",
                HtmlHeaderLevelEnum.Header5,
                htmlBullet,
                htmlLogUniqueId.LoggingHtml()
            );
        }
        else
        {
            logger.LogHtmlError(
                "5.1. Angle",
                HtmlHeaderLevelEnum.Header5,
                htmlBullet,
                htmlLogUniqueId.LoggingHtml()
            );
        }

        #endregion 五.一. 获取缩放系数

        #region 五.二. 缩放系数校准理想矩阵

        var scaleMatrix = Matrix<double>.Build.DenseOfArray(new[,]
        {
            { meanScaleX, 0, 0 },
            { 0, meanScaleY, 0 },
            { 0, 0, 1 }
        });

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;

                var temp = scaleMatrix * Vector<double>.Build.DenseOfArray([idealXMatrix[row, column], idealYMatrix[row, column], 1]);
                idealXMatrix[row, column] = temp[0];
                idealYMatrix[row, column] = temp[1];
            }
        }

        var errorX = realXMatrix - idealXMatrix;
        var errorY = realYMatrix - idealYMatrix;

        var errorScaleX = errorXContainsScale - errorX;
        var errorScaleY = errorYContainsScale - errorY;

        logger.LogHtmlInformation(
            "5.2.1. Scale Error Map",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorScaleX, errorScaleY, "Scale Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorScaleX, "Scale error X"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorScaleY, "Scale error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        logger.LogHtmlInformation(
            "5.2.2. Result",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorX, errorY, "Scale Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorX, "Scale error X"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorY, "Scale error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        #endregion 五.二. 缩放系数校准理想矩阵

        /***********************************六. 计算误差矩阵************************************************************/

        logger.LogHtmlInformation("6. Error Map", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

        #region 六.一. 误差矩阵平移坐标修正

        // 因为[二. 旋转角度(去掉晶圆因为对准精度不够而带来的角度)校准理想矩阵]不一定是按照原点旋转的，所以需要移除统一偏差
        RemoveAverageTranslation(errorX, errorY, errorX, errorY);

        logger.LogHtmlInformation(
            "6.1.1. Remove Average Translation Error Map",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorX, errorY, "Translation Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorX, "Remove Average Translation error X"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorY, "Remove Average Translation error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        /*errorX += errorScaleX;
        errorY += errorScaleY;

        logger.LogHtmlInformation(
            "6.1.2. Add Scale Error Map",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorX, errorY, "Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorX, "error X"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorY, "error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );*/

        #endregion 六.一. 误差矩阵平移坐标修正

        #region 六.二. 误差矩阵五次多项式拟合

        var polyErrorXLineList = new List<(string Name, Point[] PointList)>();
        var polyErrorYLineList = new List<(string Name, Point[] PointList)>();

        // 去掉因为模板匹配误差带来的误差
        for (var row = minRowIndex; row <= maxRowIndex; row++)
        {
            var (isSuccess, minColIndexByRow, maxColIndexByRow, errorXRow) = FilterRow(errorX, row);
            if (isSuccess == false) continue;
            var x = Vector<double>.Build.DenseOfEnumerable(Enumerable.Range(1, errorXRow.Count).Select(x => (double)x));

            var (p0, p1, p2, p3, p4, p5, rSquared, yPredicted) = PolynomialCurve.Fit5(x, errorXRow);
            polyErrorXLineList.Add(($"row {row}", ToPoints(x, errorXRow)));
            polyErrorXLineList.Add(($"row {row}: y = {p0} + {p1}*x + {p2}*x^2 + {p3}*x^3 + {p4}*x^4 + {p5}*x^5, r^2 = {rSquared}", ToPoints(x, yPredicted)));

            for (var column = minColIndexByRow; column <= maxColIndexByRow; column++)
            {
                if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;
                errorX[row, column] = yPredicted[column - minColIndexByRow];
            }
        }

        for (var row = minRowIndex; row <= maxRowIndex; row++)
        {
            var (isSuccess, minColIndexByRow, maxColIndexByRow, errorYRow) = FilterRow(errorY, row);
            if (isSuccess == false) continue;
            var x = Vector<double>.Build.DenseOfEnumerable(Enumerable.Range(1, errorYRow.Count).Select(x => (double)x));

            var (p0, p1, p2, p3, p4, p5, rSquared, yPredicted) = PolynomialCurve.Fit5(x, errorYRow);
            polyErrorYLineList.Add(($"row {row}", ToPoints(x, errorYRow)));
            polyErrorYLineList.Add(($"row {row}: y = {p0} + {p1}*x + {p2}*x^2 + {p3}*x^3 + {p4}*x^4 + {p5}*x^5, r^2 = {rSquared}", ToPoints(x, yPredicted)));

            for (var column = minColIndexByRow; column <= maxColIndexByRow; column++)
            {
                if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;

                errorY[row, column] = yPredicted[column - minColIndexByRow];
            }
        }

        logger.LogHtmlInformation(
            "6.2.1. Fit",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                FitX = new HtmlPlot2DLinesChart([.. polyErrorXLineList], "unit: um"),
                FitY = new HtmlPlot2DLinesChart([.. polyErrorYLineList], "unit: um")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        logger.LogHtmlInformation(
            "6.2.2. Fit Error Map",
            HtmlHeaderLevelEnum.Header5,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorX, errorY, "Fit Map"),
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorX, "Fit error X"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorY, "Fit error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        if (isXOnlyGantryError)
        {
            logger.LogHtmlInformation(
                "6.2.3. X Only Gantry",
                HtmlHeaderLevelEnum.Header5,
                new HtmlBullet(new
                {
                    VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(idealXMatrix, idealYMatrix, realXMatrix, realYMatrix, errorGantryX, errorY, "Fit Map"),
                    ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorGantryX, "Fit error X"),
                    ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, errorY, "Fit error Y")
                }),
                htmlLogUniqueId.LoggingHtml()
            );
        }

        #endregion 六.二. 误差矩阵五次多项式拟合

        return isXOnlyGantryError
            ? (isAlignmentSuccess && isGantrySuccess && isScaleXSuccess && isScaleYSuccess, errorGantryX, errorY)
            : (isAlignmentSuccess && isGantrySuccess && isScaleXSuccess && isScaleYSuccess, errorX, errorY);

        void RemoveAverageTranslation(Matrix<double> errorXMatrix, Matrix<double> errorYMatrix, Matrix<double> targetXMatrix, Matrix<double> targetYMatrix)
        {
            // 移除统一偏差
            var sumXTemp = 0d;
            var sumYTemp = 0d;
            var countTemp = 0d;

            for (var row = 0; row < rowCount; row++)
            {
                for (var column = 0; column < columnCount; column++)
                {
                    if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false
                        || Convert.ToBoolean(templateMathIsOkMatrix[row, column]) == false) continue;

                    sumXTemp += errorXMatrix[row, column];
                    sumYTemp += errorYMatrix[row, column];
                    countTemp++;
                }
            }

            var txTemp = sumXTemp / countTemp;
            var tyTemp = sumYTemp / countTemp;

            for (var row = 0; row < rowCount; row++)
            {
                for (var column = 0; column < columnCount; column++)
                {
                    if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;

                    targetXMatrix[row, column] -= txTemp;
                    targetYMatrix[row, column] -= tyTemp;
                }
            }
        }

        (bool isSuccess, int MinColIndexByRow, int MaxColIndexByRow, Vector<double> Result) FilterRow(Matrix<double> matrix, int row)
        {
            var okColumnIndexTempList = new List<int>();
            var array = matrix.Row(row).Where((_, column) =>
            {
                var boolean = Convert.ToBoolean(isInWaferMatrix[row, column]);
                if (boolean) okColumnIndexTempList.Add(column);
                return boolean;
            }).ToArray();

            if (okColumnIndexTempList.Count <= 0
                || okColumnIndexTempList.SequenceEqual(Enumerable.Range(okColumnIndexTempList[0], okColumnIndexTempList.Count)) == false)
                return (false, 0, 0, Vector<double>.Build.Dense(matrix.ColumnCount, 0d));

            return (true, okColumnIndexTempList[0], okColumnIndexTempList[^1], Vector<double>.Build.DenseOfArray(array));
        }

        (int MinRowIndexByCol, int MaxRowIndexByCol, Vector<double> Result) FilterColumn(Matrix<double> matrix, int column)
        {
            var okRowIndexTempList = new List<int>();
            var array = matrix.Column(column).Where((_, row) =>
            {
                var boolean = Convert.ToBoolean(isInWaferMatrix[row, column]);
                if (boolean) okRowIndexTempList.Add(row);
                return boolean;
            }).ToArray();

            if (okRowIndexTempList.Count <= 0
                || okRowIndexTempList.SequenceEqual(Enumerable.Range(okRowIndexTempList[0], okRowIndexTempList.Count)) == false)
                ThrowHelper.ThrowArgumentException(nameof(isInWaferMatrix));

            return (okRowIndexTempList[0], okRowIndexTempList[^1], Vector<double>.Build.DenseOfArray(array));
        }
    }

    /// <summary>
    /// 笛卡尔坐标系下, 扩展基StageMapDto, 并合并StageMap(将其包含在基里面)
    /// </summary>
    /// <param name="baseStageMap">需要扩展基的StageMapDto</param>
    /// <param name="mergeStageMap">需要合并的StageMapDto</param>
    /// <param name="htmlLogUniqueId">html记录日志的Id</param>
    /// <returns>扩展后的StageMapDto</returns>
    public StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId)
    {
        logger.LogHtmlInformation("Expand State Map Start", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            baseStageMapRowNumber = baseStageMap.RowNumber,
            baseStageMapColumnNumber = baseStageMap.ColumnNumber,
            baseStageMapRowCellHeight = baseStageMap.RowCellHeight,
            baseStageMapColumnCellWidth = baseStageMap.ColumnCellWidth,
            mergeStageMapRowNumber = mergeStageMap.RowNumber,
            mergeStageMapColumnNumber = mergeStageMap.ColumnNumber,
            mergeStageMapRowCellHeight = mergeStageMap.RowCellHeight,
            mergeStageMapColumnCellWidth = mergeStageMap.ColumnCellWidth
        }), htmlLogUniqueId.LoggingHtml());

        var (baseStageMapIdealXArray, baseStageMapIdealYArray) = baseStageMap.GetIdealArray();
        var (baseStageMapRealXArray, baseStageMapRealYArray, _, _) = baseStageMap.GetRealArray();
        var (baseStageMapErrorXMatrix, baseStageMapErrorYMatrix) = baseStageMap.GetErrorArray();

        logger.LogHtmlInformation(
            "1. Base Stage Map",
            HtmlHeaderLevelEnum.Header4,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(
                    Matrix<double>.Build.DenseOfArray(baseStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapRealXArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapRealYArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapErrorXMatrix),
                    Matrix<double>.Build.DenseOfArray(baseStageMapErrorYMatrix),
                    "Origin Map"),
                ErrorX = ToHtmlPlot3DChart(
                    Matrix<double>.Build.DenseOfArray(baseStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapErrorXMatrix),
                    "Error X"),
                ErrorY = ToHtmlPlot3DChart(
                    Matrix<double>.Build.DenseOfArray(baseStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(baseStageMapErrorYMatrix),
                    "Error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        var (mergeStageMapIdealXArray, mergeStageMapIdealYArray) = mergeStageMap.GetIdealArray();
        var (mergeStageMapRealXArray, mergeStageMapRealYArray, _, _) = mergeStageMap.GetRealArray();
        var (mergeStageMapErrorXMatrix, mergeStageMapErrorYMatrix) = mergeStageMap.GetErrorArray();

        logger.LogHtmlInformation(
            "2. merge Stage Map",
            HtmlHeaderLevelEnum.Header4,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(
                    Matrix<double>.Build.DenseOfArray(mergeStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapRealXArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapRealYArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapErrorXMatrix),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapErrorYMatrix),
                    "Origin Map"),
                ErrorX = ToHtmlPlot3DChart(
                    Matrix<double>.Build.DenseOfArray(mergeStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapErrorXMatrix),
                    "Error X"),
                ErrorY = ToHtmlPlot3DChart(
                    Matrix<double>.Build.DenseOfArray(mergeStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(mergeStageMapErrorYMatrix),
                    "Error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        // 计算扩展基的边界
        var baseStageMapLeftMinX = baseStageMap.IdealStageMapItemMatrix[0][0].Point.X;
        var baseStageMapRightMaxX = baseStageMap.IdealStageMapItemMatrix[0][^1].Point.X;
        var baseStageMapBottomMinY = baseStageMap.IdealStageMapItemMatrix[0][0].Point.Y;
        var baseStageMapTopMaxY = baseStageMap.IdealStageMapItemMatrix[^1][0].Point.Y;

        // 计算合并的边界
        var mergeStageMapLeftMinX = mergeStageMap.IdealStageMapItemMatrix[0][0].Point.X;
        var mergeStageMapRightMaxX = mergeStageMap.IdealStageMapItemMatrix[0][^1].Point.X;
        var mergeStageMapBottomMinY = mergeStageMap.IdealStageMapItemMatrix[0][0].Point.Y;
        var mergeStageMapTopMaxY = mergeStageMap.IdealStageMapItemMatrix[^1][0].Point.Y;

        // 确定扩展后的边界
        var expandedStageMapLeftMinX = Math.Min(mergeStageMapLeftMinX, baseStageMapLeftMinX);
        var expandedStageMapRightMaxX = Math.Max(mergeStageMapRightMaxX, baseStageMapRightMaxX);
        var expandedStageMapBottomMinY = Math.Min(mergeStageMapBottomMinY, baseStageMapBottomMinY);
        var expandedStageMapTopMaxY = Math.Max(mergeStageMapTopMaxY, baseStageMapTopMaxY);

        // 计算出需要扩展的行列数
        var expandLeftColumnCount = (int)Math.Ceiling((baseStageMapLeftMinX - expandedStageMapLeftMinX) / baseStageMap.ColumnCellWidth);
        Guard.IsGreaterThanOrEqualTo(expandLeftColumnCount, 0);
        var expandRightColumnCount = (int)Math.Ceiling((expandedStageMapRightMaxX - baseStageMapRightMaxX) / baseStageMap.ColumnCellWidth);
        Guard.IsGreaterThanOrEqualTo(expandRightColumnCount, 0);
        var expandBottomRowCount = (int)Math.Ceiling((baseStageMapBottomMinY - expandedStageMapBottomMinY) / baseStageMap.RowCellHeight);
        Guard.IsGreaterThanOrEqualTo(expandBottomRowCount, 0);
        var expandTopRowCount = (int)Math.Ceiling((expandedStageMapTopMaxY - baseStageMapTopMaxY) / baseStageMap.RowCellHeight);
        Guard.IsGreaterThanOrEqualTo(expandTopRowCount, 0);

        var expandColumnNumber = baseStageMap.ColumnNumber + expandLeftColumnCount + expandRightColumnCount;
        var expandRowNumber = baseStageMap.RowNumber + expandBottomRowCount + expandTopRowCount;
        // 计算出扩展后的左下角坐标
        var expandLeftX = baseStageMapLeftMinX - expandLeftColumnCount * baseStageMap.ColumnCellWidth;
        var expandBottomY = baseStageMapBottomMinY - expandBottomRowCount * baseStageMap.RowCellHeight;

        logger.LogHtmlInformation("3. Get Edges", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            baseStageMapLeftMinX,
            baseStageMapRightMaxX,
            baseStageMapBottomMinY,
            baseStageMapTopMaxY,
            mergeStageMapLeftMinX,
            mergeStageMapRightMaxX,
            mergeStageMapBottomMinY,
            mergeStageMapTopMaxY,
            expandedStageMapLeftMinX,
            expandedStageMapRightMaxX,
            expandedStageMapBottomMinY,
            expandedStageMapTopMaxY,
            expandLeftColumnCount,
            expandRightColumnCount,
            expandBottomRowCount,
            expandTopRowCount,
            expandLeftX,
            expandBottomY,
            expandRowNumber,
            expandColumnNumber
        }), htmlLogUniqueId.LoggingHtml());

        // 获取需要合并的StageMapDto的双线性二次插值数据
        var (mergeStageMapIdealMatrix, mergeStageMapIsInWaferMatrix, mergeStageMapErrorMatrix) = mergeStageMap.GetStageMapBilinearArray();
        var resultStageMap = new StageMapDto(expandRowNumber, expandColumnNumber, baseStageMap.RowCellHeight, baseStageMap.ColumnCellWidth);
        for (var row = 0; row < resultStageMap.RowNumber; row++)
        {
            for (var column = 0; column < resultStageMap.ColumnNumber; column++)
            {
                resultStageMap.IdealStageMapItemMatrix[row][column].Clear();

                var x = expandLeftX + column * resultStageMap.ColumnCellWidth;
                var y = expandBottomY + row * resultStageMap.RowCellHeight;
                var idealPoint = new Point(x, y);

                resultStageMap.IdealStageMapItemMatrix[row][column].Row = row;
                resultStageMap.IdealStageMapItemMatrix[row][column].Column = column;
                resultStageMap.IdealStageMapItemMatrix[row][column].Point = idealPoint;
                resultStageMap.IdealStageMapItemMatrix[row][column].IsInWafer = false;
                resultStageMap.RealMatrix[row][column] = idealPoint;
                resultStageMap.ErrorMatrix[row][column] = Point.Origin;

                // 在基中IdealStageMapItemList的行列号获取数据
                var baseStageMapRowIndex = row - expandBottomRowCount;
                var baseStageMapColumnIndex = column - expandLeftColumnCount;
                var baseStageMapItem = baseStageMap.IdealStageMapItemMatrix.ElementAtOrDefault(baseStageMapRowIndex)?.ElementAtOrDefault(baseStageMapColumnIndex);
                var baseReal = baseStageMap.RealMatrix.ElementAtOrDefault(baseStageMapRowIndex)?.ElementAtOrDefault(baseStageMapColumnIndex);
                var baseError = baseStageMap.ErrorMatrix.ElementAtOrDefault(baseStageMapRowIndex)?.ElementAtOrDefault(baseStageMapColumnIndex);
                if (baseStageMapItem is not null) // 如果基中有数据
                {
                    // 因为double会有精度损失. 序列化带来的精度损失 0.15e-10
                    Guard.IsLessThanOrEqualTo((baseStageMapItem.Point - (Vector)idealPoint).ToOriginLength, 1e-8, nameof(baseStageMapItem));
                    if (baseStageMapItem.IsInWafer) // 如果基中有数据且在晶圆内
                    {
                        Guard.IsNotNull(baseReal);
                        Guard.IsNotNull(baseError);
                        resultStageMap.IdealStageMapItemMatrix[row][column] = baseStageMapItem.Clone();
                        resultStageMap.RealMatrix[row][column] = baseReal.Value;
                        resultStageMap.ErrorMatrix[row][column] = baseError.Value;
                    }
                    else // 如果基中有数据但不在晶圆内
                    {
                        Bilinear(row, column, idealPoint);
                    }
                }
                else // 如果基中没有数据
                {
                    Bilinear(row, column, idealPoint);
                }
            }
        }

        var (resultStageMapIdealXArray, resultStageMapIdealYArray) = resultStageMap.GetIdealArray();
        var (resultStageMapRealXArray, resultStageMapRealYArray, _, _) = resultStageMap.GetRealArray();
        var (resultStageMapErrorXMatrix, resultStageMapErrorYMatrix) = resultStageMap.GetErrorArray();

        logger.LogHtmlInformation(
            "4. result Stage Map",
            HtmlHeaderLevelEnum.Header4,
            new HtmlBullet(new
            {
                VectorField = ToHtmlPlot2DErrorMapVectorFieldChart(
                    Matrix<double>.Build.DenseOfArray(resultStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapRealXArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapRealYArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapErrorXMatrix),
                    Matrix<double>.Build.DenseOfArray(resultStageMapErrorYMatrix),
                    "Origin Map"),
                ErrorX = ToHtmlPlot3DChart(
                    Matrix<double>.Build.DenseOfArray(resultStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapErrorXMatrix),
                    "Error X"),
                ErrorY = ToHtmlPlot3DChart(
                    Matrix<double>.Build.DenseOfArray(resultStageMapIdealXArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapIdealYArray),
                    Matrix<double>.Build.DenseOfArray(resultStageMapErrorYMatrix),
                    "Error Y")
            }),
            htmlLogUniqueId.LoggingHtml()
        );

        return resultStageMap;

        void Bilinear(int row, int column, Point idealPoint)
        {
            var tryBilinear = Interpolator.TryBilinear(mergeStageMapIdealMatrix, mergeStageMapIsInWaferMatrix, mergeStageMapErrorMatrix, idealPoint, out var error);
            if (tryBilinear)
            {
                resultStageMap.IdealStageMapItemMatrix[row][column].IsMatchOk = true;
                resultStageMap.IdealStageMapItemMatrix[row][column].IsInWafer = true;
                resultStageMap.RealMatrix[row][column] = idealPoint + (Vector)error;
                resultStageMap.ErrorMatrix[row][column] = error;
            }

            // 找最近的一个点代替
            var (length, rowIndex, columnIndex) = mergeStageMap.IdealStageMapItemMatrix
                .SelectMany((t, rowIndex) => t.Select((tt, columnIndex) => (Length: (tt.Point - (Vector)idealPoint).ToOriginLength, rowIndex, columnIndex)))
                .OrderBy(t => t.Length)
                .First();
            if (length < new Point(mergeStageMap.ColumnCellWidth, mergeStageMap.RowCellHeight).ToOriginLength)
            {
                resultStageMap.RealMatrix[row][column] = mergeStageMap.RealMatrix[rowIndex][columnIndex];
                resultStageMap.ErrorMatrix[row][column] = mergeStageMap.ErrorMatrix[rowIndex][columnIndex];
            }
        }
    }

    private static HtmlPlot2DErrorMapVectorFieldChart ToHtmlPlot2DErrorMapVectorFieldChart(
        Matrix<double> idealXMatrix,
        Matrix<double> idealYMatrix,
        Matrix<double> realXMatrix,
        Matrix<double> realYMatrix,
        Matrix<double> errorXMatrix,
        Matrix<double> errorYMatrix,
        string title
    )
    {
        var rowCount = idealXMatrix.RowCount;
        var columnCount = idealXMatrix.ColumnCount;
        var idealMatrix = new Point[rowCount, columnCount];
        var realMatrix = new Point[rowCount, columnCount];
        var errorMatrix = new Point[rowCount, columnCount];
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                idealMatrix[row, column] = new Point(idealXMatrix[row, column], idealYMatrix[row, column]);
                realMatrix[row, column] = new Point(realXMatrix[row, column], realYMatrix[row, column]);
                errorMatrix[row, column] = new Point(errorXMatrix[row, column], errorYMatrix[row, column]);
            }
        }

        return new HtmlPlot2DErrorMapVectorFieldChart(idealMatrix, realMatrix, errorMatrix, title);
    }

    private static HtmlPlot3DChart ToHtmlPlot3DChart(
        Matrix<double> idealXMatrix,
        Matrix<double> idealYMatrix,
        Matrix<double> errorMatrix,
        string title
    )
    {
        var rowCount = idealXMatrix.RowCount;
        var columnCount = idealXMatrix.ColumnCount;
        var point3DMatrix = new Point3D[rowCount, columnCount];
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                // if (Convert.ToBoolean(isInWaferMatrix[row, column]) == false) continue;

                point3DMatrix[row, column] = new Point3D(idealXMatrix[row, column], idealYMatrix[row, column], errorMatrix[row, column]);
            }
        }

        return new HtmlPlot3DChart(point3DMatrix.ToArrayByRow(), title, HtmlPlot3DType.Surface);
    }

    private static Point[] ToPoints(
        Vector<double> x,
        Vector<double> y
    )
    {
        var count = x.Count;
        var points = new Point[count];
        for (var i = 0; i < count; i++)
        {
            points[i] = new Point(x[i], y[i]);
        }

        return points;
    }
}