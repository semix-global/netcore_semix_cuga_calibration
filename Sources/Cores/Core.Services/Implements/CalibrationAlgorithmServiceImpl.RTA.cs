using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.DarkField;
using Core.Services.Interfaces;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.Diagnostics;

namespace Core.Services.Implements;

public sealed partial class CalibrationAlgorithmServiceImpl
{
    public ICalibrationAlgorithmService.RTAScanSample[] GetRTAScanSamples(
        DarkFieldRawScanImageDTO rawScanImage,
        ICalibrationAlgorithmService.StageMapConfigDTO stageMapConfig,
        bool isForward,
        bool isRemoveLinear,
        Guid htmlLogUniqueId)
    {
        logger.LogHtmlInformation("Start", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            rawScanImage.RawImageFilePath,
            Config = new HtmlQuote(stageMapConfig.ToHtmlAnonymous()),
            isForward,
            isRemoveLinear,
        }), htmlLogUniqueId.LoggingHtml());

        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            using var rawImage = new Image(rawScanImage.RawImageFilePath);
            Guard.IsEqualTo(rawImage.Width, rawScanImage.Size.Height);
            Guard.IsEqualTo(rawImage.Height, rawScanImage.Size.Width);

            using var engine = new StageMapEngine(stageMapConfig.AdaptTo());
            using var result = engine.Process(rawImage);

            var arrHit = Guard.IsNotNullAndReturn(result.arr.hit);
            var yCount = arrHit.Width;
            var xCount = arrHit.Height;
            Guard.IsGreaterThan(xCount, yCount);

            var (realXMatrix, realYMatrix) = GetRealMatrix();

            #region 1. Spacing

            var xSpacingPoints = Generate.LinearRangeInt32(0, yCount - 1)
                .SelectMany(y => realXMatrix.Row(y).Index().Select(xTuple => new Point(xTuple.Index, xTuple.Item)))
                .Where(point => double.IsFinite(point.Y))
                .ToArray();
            var ySpacingPoints = Generate.LinearRangeInt32(0, xCount - 1)
                .SelectMany(x => realYMatrix.Column(x).Index().Select(yTuple => new Point(yTuple.Index, yTuple.Item)))
                .Where(point => double.IsFinite(point.Y))
                .ToArray();
            Guard.IsTrue(xSpacingPoints.Length >= 2 && xSpacingPoints.DistinctBy(t => t.X).Count() >= 2);
            Guard.IsTrue(ySpacingPoints.Length >= 2 && ySpacingPoints.DistinctBy(t => t.X).Count() >= 2);

            var (xSpacing, xSpacingIntercept, xSpacingRSquared, xSpacingPredicted) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. xSpacingPoints.Select(t => t.X)]),
                Vector<double>.Build.Dense([.. xSpacingPoints.Select(t => t.Y)]));
            var (ySpacing, ySpacingIntercept, ySpacingRSquared, ySpacingPredicted) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. ySpacingPoints.Select(t => t.X)]),
                Vector<double>.Build.Dense([.. ySpacingPoints.Select(t => t.Y)]));

            logger.LogHtmlInformation("1. Spacing", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                xSpacing,
                ySpacing,
                XFitPlot = new HtmlPlot2DLinesChart(
                [
                    ("Origin", [.. xSpacingPoints]),
                    ($"Fit {PolynomialCurve.ToString1(xSpacing, xSpacingIntercept, xSpacingRSquared, "0.##########")}", [.. xSpacingPoints.Index().Select(t => new Point(t.Item.X, xSpacingPredicted[t.Index]))])
                ], "X: index - Y: px"),
                YFitPlot = new HtmlPlot2DLinesChart(
                [
                    ("Origin", [.. ySpacingPoints]),
                    ($"Fit {PolynomialCurve.ToString1(ySpacing, ySpacingIntercept, ySpacingRSquared, "0.##########")}", [.. ySpacingPoints.Index().Select(t => new Point(t.Item.X, ySpacingPredicted[t.Index]))])
                ], "X: index - Y: px")
            }), htmlLogUniqueId.LoggingHtml());

            Guard.IsTrue(double.IsFinite(xSpacing) && xSpacing > 0d);
            Guard.IsTrue(double.IsFinite(ySpacing) && ySpacing > 0d);

            #endregion

            #region 2. Ideal

            var (xAnchor, xAnchorVector) = realXMatrix.EnumerateColumns().Index().First(t => t.Item.Any(double.IsFinite));
            var xAnchorVectorAverage = xAnchorVector.Where(double.IsFinite).Average();

            var (yAnchor, yAnchorVector) = realYMatrix.EnumerateRows().Index().First(t => t.Item.Any(double.IsFinite));
            var yAnchorVectorAverage = yAnchorVector.Where(double.IsFinite).Average();

            var idealXMatrix = Matrix<double>.Build.Dense(yCount, xCount, (_, x) => xAnchorVectorAverage + (x - xAnchor) * xSpacing);
            var idealYMatrix = Matrix<double>.Build.Dense(yCount, xCount, (y, _) => yAnchorVectorAverage + (y - yAnchor) * ySpacing);

            logger.LogHtmlInformation("2. Ideal", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                xCount,
                xAnchor,
                xAnchorVectorAverage,
                yCount,
                yAnchor,
                yAnchorVectorAverage
            }), htmlLogUniqueId.LoggingHtml());

            #endregion

            #region 3. Origin

            var originErrorXMatrix = CenterError(realXMatrix - idealXMatrix);
            var originErrorYMatrix = CenterError(realYMatrix - idealYMatrix);
            var originSamples = GetSamples(originErrorXMatrix, originErrorYMatrix);

            logger.LogHtmlInformation("3. Origin Error", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                XPlot3D = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, originErrorXMatrix, "X: scan px - Y: scan px - Z: error X px"),
                YPlot3D = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, originErrorYMatrix, "X: scan px - Y: scan px - Z: error Y px"),
                SamplesX = new HtmlPlot2DLinesChart([(string.Empty, [.. originSamples.Select(sample => new Point(sample.X, sample.Error.X))])], "X: scan px - Y: mean error X px"),
                SamplesY = new HtmlPlot2DLinesChart([(string.Empty, [.. originSamples.Select(sample => new Point(sample.X, sample.Error.Y))])], "X: scan px - Y: mean error Y px")
            }), htmlLogUniqueId.LoggingHtml());

            #endregion

            #region 4. Y Alignment

            var yAlignmentAngles = new List<double>();
            var yAlignmentFitLines = new List<(string Name, Point[] PointList)>();
            for (var y = 0; y < yCount; y++)
            {
                var points = realXMatrix
                    .Row(y)
                    .Index()
                    .Select(t => new Point(t.Item, realYMatrix[y, t.Index]))
                    .Where(point => double.IsFinite(point.X) && double.IsFinite(point.Y))
                    .ToArray();
                if (points.Length < 2 || points.DistinctBy(t => t.X).Count() < 2) continue;

                var (slope, intercept, rSquared, predicted) = PolynomialCurve.Fit1(
                    Vector<double>.Build.Dense([.. points.Select(point => point.X)]),
                    Vector<double>.Build.Dense([.. points.Select(point => point.Y)]));
                if (double.IsFinite(slope) == false) continue;

                yAlignmentAngles.Add(Math.Atan(slope));
                yAlignmentFitLines.Add(($"Row {y}: Origin", points));
                yAlignmentFitLines.Add(($"Row {y}: {PolynomialCurve.ToString1(slope, intercept, rSquared, "0.##########")}", [.. points.Index().Select(t => new Point(t.Item.X, predicted[t.Index]))]));
            }

            Guard.IsTrue(yAlignmentAngles.Count > 0);

            var yAlignmentAngle = yAlignmentAngles.Average();

            logger.LogHtmlInformation("4. Y Alignment", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                yAlignmentAngle,
                FitPlot = new HtmlPlot2DLinesChart([.. yAlignmentFitLines], "X: px - Y: px"),
            }), htmlLogUniqueId.LoggingHtml());

            Guard.IsTrue(double.IsFinite(yAlignmentAngle));

            var yAlignmentThetaMatrix = Matrix<double>.Build.DenseOfArray(new[,]
            {
                { Math.Cos(yAlignmentAngle), Math.Sin(yAlignmentAngle), 0d },
                { -Math.Sin(yAlignmentAngle), Math.Cos(yAlignmentAngle), 0d },
                { 0d, 0d, 1d }
            });

            for (var y = 0; y < yCount; y++)
            {
                for (var x = 0; x < xCount; x++)
                {
                    var temp = yAlignmentThetaMatrix * Vector<double>.Build.DenseOfArray([realXMatrix[y, x], realYMatrix[y, x], 1]);
                    realXMatrix[y, x] = temp[0];
                    realYMatrix[y, x] = temp[1];
                }
            }

            var removeYAlignmentErrorXMatrix = CenterError(realXMatrix - idealXMatrix);
            var removeYAlignmentErrorYMatrix = CenterError(realYMatrix - idealYMatrix);
            var removeYAlignmentSamples = GetSamples(removeYAlignmentErrorXMatrix, removeYAlignmentErrorYMatrix);
            var yAlignmentErrorXMatrix = originErrorXMatrix - removeYAlignmentErrorXMatrix;
            var yAlignmentErrorYMatrix = originErrorYMatrix - removeYAlignmentErrorYMatrix;

            logger.LogHtmlInformation("4.1. Y Alignment Error", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, yAlignmentErrorXMatrix, "X: scan px - Y: scan px - Z: error X px"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, yAlignmentErrorYMatrix, "X: scan px - Y: scan px - Z: error Y px"),
            }), htmlLogUniqueId.LoggingHtml());

            logger.LogHtmlInformation("4.2. Remove Y Alignment Error", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                XPlot3D = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, removeYAlignmentErrorXMatrix, "X: scan px - Y: scan px - Z: error X px"),
                YPlot3D = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, removeYAlignmentErrorYMatrix, "X: scan px - Y: scan px - Z: error Y px"),
                SamplesX = new HtmlPlot2DLinesChart([(string.Empty, [.. removeYAlignmentSamples.Select(sample => new Point(sample.X, sample.Error.X))])], "X: scan px - Y: mean error X px"),
                SamplesY = new HtmlPlot2DLinesChart([(string.Empty, [.. removeYAlignmentSamples.Select(sample => new Point(sample.X, sample.Error.Y))])], "X: scan px - Y: mean error Y px"),
            }), htmlLogUniqueId.LoggingHtml());

            #endregion

            #region 5. X Scale

            var xScalePoints = Generate.LinearRangeInt32(0, yCount - 1)
                .SelectMany(y => idealXMatrix.Row(y).Index().Select(xTuple => new Point(xTuple.Item, realXMatrix[y, xTuple.Index])))
                .Where(point => double.IsFinite(point.X) && double.IsFinite(point.Y))
                .ToArray();
            Guard.IsTrue(xScalePoints.Length >= 2 && xScalePoints.DistinctBy(t => t.X).Count() >= 2);

            var (xScaleSlope, xScaleIntercept, xScaleRSquared, xScalePredicted) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. xScalePoints.Select(t => t.X)]),
                Vector<double>.Build.Dense([.. xScalePoints.Select(t => t.Y)]));

            var xScaleCorrection = 1d / xScaleSlope;
            logger.LogHtmlInformation("5. X Scale", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                xScaleCorrection,
                FitPlot = new HtmlPlot2DLinesChart(
                [
                    ("Origin", [.. xScalePoints]),
                    ($"Fit {PolynomialCurve.ToString1(xScaleSlope, xScaleIntercept, xScaleRSquared, "0.##########")}", [.. xScalePoints.Index().Select(t => new Point(t.Item.X, xScalePredicted[t.Index]))])
                ], "X: px - Y: px"),
            }), htmlLogUniqueId.LoggingHtml());

            Guard.IsTrue(double.IsFinite(xScaleCorrection) && xScaleCorrection > 0d, "The inverse X scale must be finite and positive.");

            var scaleMatrix = Matrix<double>.Build.DenseOfArray(new[,]
            {
                { xScaleCorrection, 0d, 0d },
                { 0d, 1d, 0d },
                { 0d, 0d, 1d }
            });

            for (var x = 0; x < yCount; x++)
            {
                for (var y = 0; y < xCount; y++)
                {
                    var temp = scaleMatrix * Vector<double>.Build.DenseOfArray([realXMatrix[x, y], realYMatrix[x, y], 1]);
                    realXMatrix[x, y] = temp[0];
                    realYMatrix[x, y] = temp[1];
                }
            }

            var removeYAlignmentAndXScaleErrorXMatrix = CenterError(realXMatrix - idealXMatrix);
            var removeYAlignmentAndXScaleErrorYMatrix = CenterError(realYMatrix - idealYMatrix);
            var removeYAlignmentAndXScaleSamples = GetSamples(removeYAlignmentAndXScaleErrorXMatrix, removeYAlignmentAndXScaleErrorYMatrix);
            var xScaleErrorXMatrix = removeYAlignmentErrorXMatrix - removeYAlignmentAndXScaleErrorXMatrix;
            var xScaleErrorYMatrix = removeYAlignmentErrorYMatrix - removeYAlignmentAndXScaleErrorYMatrix;

            logger.LogHtmlInformation("5.1. X Scale Error", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                ErrorX = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, xScaleErrorXMatrix, "X: scan px - Y: scan px - Z: error X px"),
                ErrorY = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, xScaleErrorYMatrix, "X: scan px - Y: scan px - Z: error Y px"),
            }), htmlLogUniqueId.LoggingHtml());

            logger.LogHtmlInformation("5.2. Remove Y Alignment And X Scale Error", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                XPlot3D = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, removeYAlignmentAndXScaleErrorXMatrix, "X: scan px - Y: scan px - Z: error X px"),
                YPlot3D = ToHtmlPlot3DChart(idealXMatrix, idealYMatrix, removeYAlignmentAndXScaleErrorYMatrix, "X: scan px - Y: scan px - Z: error Y px"),
                SamplesX = new HtmlPlot2DLinesChart([(string.Empty, [.. removeYAlignmentAndXScaleSamples.Select(sample => new Point(sample.X, sample.Error.X))])], "X: scan px - Y: mean error X px"),
                SamplesY = new HtmlPlot2DLinesChart([(string.Empty, [.. removeYAlignmentAndXScaleSamples.Select(sample => new Point(sample.X, sample.Error.Y))])], "X: scan px - Y: mean error Y px"),
            }), htmlLogUniqueId.LoggingHtml());

            #endregion

            return isRemoveLinear ? removeYAlignmentAndXScaleSamples : originSamples;

            (Matrix<double> RealXMatrix, Matrix<double> RealYMatrix) GetRealMatrix()
            {
                var xMatrix = new double[yCount][];
                var yMatrix = new double[yCount][];

                for (var y = 0; y < yCount; y++)
                {
                    xMatrix[y] = new double[xCount];
                    yMatrix[y] = new double[xCount];
                }

                for (var x = 0; x < xCount; x++)
                {
                    var hitRow = arrHit.GetRow<double>(x);
                    for (var y = 0; y < yCount; y++)
                    {
                        var xPixel = hitRow[y * 2];
                        var yPixel = hitRow[y * 2 + 1];

                        xMatrix[y][x] = double.IsFinite(xPixel) ? xPixel : double.NaN;
                        yMatrix[y][x] = double.IsFinite(yPixel) ? yPixel : double.NaN;
                    }
                }

                return (Matrix<double>.Build.DenseOfRowArrays(xMatrix), Matrix<double>.Build.DenseOfRowArrays(yMatrix));
            }

            ICalibrationAlgorithmService.RTAScanSample[] GetSamples(Matrix<double> errorX, Matrix<double> errorY)
            {
                var samples = new ICalibrationAlgorithmService.RTAScanSample[xCount];

                for (var x = 0; x < xCount; x++)
                {
                    var averageX = errorX.Column(x).Average();
                    samples[x] = new ICalibrationAlgorithmService.RTAScanSample(
                        isForward ? idealXMatrix[0, x] : rawImage.Height - 1d - idealXMatrix[0, x], // 原生匹配位置按行列存储, 行号对应行扫 X 像素, 反向扫描按零基像素位置翻转.
                        new Vector(isForward ? averageX : -averageX /* 反向扫描误差也需要翻转 */, errorY.Column(x).Average()));
                }

                return samples;
            }

            static Matrix<double> CenterError(Matrix<double> errorMatrix)
            {
                var validErrors = errorMatrix.Enumerate().Where(double.IsFinite).ToArray();
                Guard.IsTrue(validErrors.Length > 0, "No finite grid errors are available.");

                var meanError = validErrors.Average();
                Guard.IsTrue(double.IsFinite(meanError), "The mean grid error must be finite.");

                return errorMatrix.Map(value => double.IsFinite(value) ? value - meanError : 0d, Zeros.Include);
            }
        }
        finally
        {
            logger.LogHtmlInformation("Stop", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                ElapsedMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds
            }), htmlLogUniqueId.LoggingHtml());
        }
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
                point3DMatrix[row, column] = new Point3D(idealXMatrix[row, column], idealYMatrix[row, column], errorMatrix[row, column]);
            }
        }

        return new HtmlPlot3DChart(point3DMatrix.ToArrayByRow(), title, HtmlPlot3DType.Surface);
    }
}