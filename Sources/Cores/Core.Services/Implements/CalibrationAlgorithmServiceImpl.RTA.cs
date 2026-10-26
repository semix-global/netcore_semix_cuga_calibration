using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.DarkField;
using Core.Services.Interfaces;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace Core.Services.Implements;

public sealed partial class CalibrationAlgorithmServiceImpl
{
    public ICalibrationAlgorithmService.RTAScanSample[] GetRTAScanSamples(
        DarkFieldRawScanImageDTO rawScanImage,
        ICalibrationAlgorithmService.StageMapConfigDTO stageMapConfig,
        bool isForward,
        Guid htmlLogUniqueId)
    {
        using var rawImage = new Image(rawScanImage.RawImageFilePath);
        Guard.IsEqualTo(rawImage.Width, rawScanImage.Size.Height);
        Guard.IsEqualTo(rawImage.Height, rawScanImage.Size.Width);

        using var engine = new StageMapEngine(stageMapConfig.AdaptTo());
        using var result = engine.Process(rawImage);

        Guard.IsNotNull(result.arr.hit);

        var yCount = result.arr.hit.Width;
        var xCount = result.arr.hit.Height;

        Guard.IsGreaterThan(xCount, yCount);

        var xMatrix = new double[yCount][];
        var yMatrix = new double[yCount][];

        for (var y = 0; y < yCount; y++)
        {
            xMatrix[y] = new double[xCount];
            yMatrix[y] = new double[xCount];
        }

        for (var x = 0; x < xCount; x++)
        {
            var hitRow = result.arr.hit.GetRow<double>(x);
            for (var y = 0; y < yCount; y++)
            {
                var xPixel = hitRow[y * 2];
                var yPixel = hitRow[y * 2 + 1];

                xMatrix[y][x] = double.IsFinite(xPixel) ? xPixel : double.NaN;
                yMatrix[y][x] = double.IsFinite(yPixel) ? yPixel : double.NaN;
            }
        }

        var xPoints = Generate.LinearRangeInt32(0, yCount - 1)
            .SelectMany(y => xMatrix[y].Index().Select(xTuple => new Point(xTuple.Index, xTuple.Item)))
            .Where(point => double.IsFinite(point.Y))
            .ToArray();
        var yPoints = Generate.LinearRangeInt32(0, xCount - 1)
            .SelectMany(x => yMatrix.Index().Select(yTuple => new Point(yTuple.Index, yTuple.Item[x])))
            .Where(point => double.IsFinite(point.Y))
            .ToArray();

        Guard.IsTrue(xPoints.Length >= 2);
        Guard.IsTrue(yPoints.Length >= 2);

        var (xSpacing, xIntercept, xRSquared, xPredicted) = PolynomialCurve.Fit1(
            Vector<double>.Build.DenseOfEnumerable(xPoints.Select(t => t.X)),
            Vector<double>.Build.DenseOfEnumerable(xPoints.Select(t => t.Y)));
        var (ySpacing, yIntercept, yRSquared, yPredicted) = PolynomialCurve.Fit1(
            Vector<double>.Build.DenseOfEnumerable(yPoints.Select(t => t.X)),
            Vector<double>.Build.DenseOfEnumerable(yPoints.Select(t => t.Y)));

        logger.LogHtmlInformation($"RTA {(isForward ? "Forward" : "Reverse")} Grid Spacing", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            rawScanImage.RawImageFilePath,
            Config = new HtmlQuote(stageMapConfig.ToHtmlAnonymous()),
            isForward,
            xSpacing,
            ySpacing,
            XFitPlot = new HtmlPlot2DLinesChart(
            [
                ("Origin", [.. xPoints]),
                ($"Fit {PolynomialCurve.ToString1(xSpacing, xIntercept, xRSquared, "0.##########")}", [.. xPoints.Index().Select(t => new Point(t.Item.X, xPredicted[t.Index]))])
            ], "X: grid index - Y: px"),
            YFitPlot = new HtmlPlot2DLinesChart(
            [
                ("Origin", [.. yPoints]),
                ($"Fit {PolynomialCurve.ToString1(ySpacing, yIntercept, yRSquared, "0.##########")}", [.. yPoints.Index().Select(t => new Point(t.Item.X, yPredicted[t.Index]))])
            ], "X: grid index - Y: px")
        }), htmlLogUniqueId.LoggingHtml());

        Guard.IsTrue(double.IsFinite(xSpacing) && xSpacing != 0d);
        Guard.IsTrue(double.IsFinite(ySpacing) && ySpacing > 0d);

        var yAnchorRow = Array.FindIndex(yMatrix, pixels => pixels.Any(double.IsFinite)); // 首个有效行和列只作为锚点, 标准矩阵保留跳过的空行和空列对应的索引偏移.
        var yAnchor = yMatrix[yAnchorRow].Where(double.IsFinite).Average();

        var xAnchorColumn = Generate.LinearRangeInt32(0, xCount - 1).First(x => xMatrix.Any(pixels => double.IsFinite(pixels[x]))); // 首个有效行和列只作为锚点, 标准矩阵保留跳过的空行和空列对应的索引偏移.
        var xAnchor = xMatrix.Select(pixels => pixels[xAnchorColumn]).Where(double.IsFinite).Average();

        var idealXMatrix = Matrix<double>.Build.Dense(yCount, xCount, (_, column) => xAnchor + (column - xAnchorColumn) * xSpacing);
        var idealYMatrix = Matrix<double>.Build.Dense(yCount, xCount, (row, _) => yAnchor + (row - yAnchorRow) * ySpacing);

        var errorXMatrix = Matrix<double>.Build.DenseOfRowArrays(xMatrix) - idealXMatrix;
        var errorYMatrix = Matrix<double>.Build.DenseOfRowArrays(yMatrix) - idealYMatrix;

        var meanErrorX = errorXMatrix.Enumerate().Where(double.IsFinite).Average();
        var meanErrorY = errorYMatrix.Enumerate().Where(double.IsFinite).Average();

        errorXMatrix = errorXMatrix.Map(value => double.IsFinite(value) ? value - meanErrorX : 0d);
        errorYMatrix = errorYMatrix.Map(value => double.IsFinite(value) ? value - meanErrorY : 0d);

        var samples = new ICalibrationAlgorithmService.RTAScanSample[xCount];

        for (var x = 0; x < xCount; x++)
        {
            samples[x] = new ICalibrationAlgorithmService.RTAScanSample(
                isForward ? idealXMatrix[0, x] : rawImage.Height - 1d - idealXMatrix[0, x], // 原生匹配位置按行列存储, 行号对应行扫 X 像素, 反向扫描按零基像素位置翻转.
                new Vector(errorXMatrix.Column(x).Average(), errorYMatrix.Column(x).Average()));
        }

        return samples;
    }
}