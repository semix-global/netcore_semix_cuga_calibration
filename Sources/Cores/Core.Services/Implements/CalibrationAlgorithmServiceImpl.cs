using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Core.Models;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM;
using System.IO;
using Constants = Net.Utilities.Models.Constants;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationAlgorithmServiceImpl(
    ILogger<CalibrationAlgorithmServiceImpl> logger,
    CalibrationSetting calibrationSetting,
    AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private bool _isAlgorithmEngineInitialized = false;
    private readonly HAlgorithm.Algorithm _algorithm = new();

    public static readonly string ErrorImageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorImages");

    public void InitialAlgorithmEngine()
    {
        if (_isAlgorithmEngineInitialized) return;

        var options = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>();
        AlgoCv.InitAlgoCv(Path.Combine(options.Value.AlgorithmLogDirectory, $"{Constants.DateTimeFormat}"), options.Value.AlgorithmLogLevel);
        _isAlgorithmEngineInitialized = true;
    }

    public double GetQuality(BitmapImage image, Guid guid, AlgorithmEngineTypeEnum algorithmEngineTypeEnum)
    {
        try
        {
            return algorithmEngineTypeEnum switch
            {
                AlgorithmEngineTypeEnum.HAlgorithm => GetHAlgorithemQuality(image),
                AlgorithmEngineTypeEnum.AlgoCSharp => GetAlgoCVSharpQuality(image),
                _ => ThrowHelper.ThrowNotSupportedException<double>()
            };
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(GetQuality));
            LogAlgorithmError(ex, nameof(GetQuality), guid, errorImagePath);
            throw;
        }
    }

    private double GetHAlgorithemQuality(BitmapImage image)
    {
        using var hImage = image.ToHImage();
        _algorithm.LaplaceDefinition(hImage, out var meanTuple);
        using var _ = meanTuple;
        return meanTuple.D;
    }

    private double GetAlgoCVSharpQuality(BitmapImage image)
    {
        // Focus 评价默认 tenengrad 模式；BF 图为常规格式
        using var algoImage = image.ToAlgoCVImage();
        return FocusEngine.EvaluateFocus(algoImage, "tenengrad");
    }

    public double GetDarkFieldQuality(BitmapImage image, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            _algorithm.DarkLaplaceDefinition(hImage, out var meanTuple);
            using var _ = meanTuple;

            return meanTuple.D;
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(GetDarkFieldQuality));
            LogAlgorithmError(ex, nameof(GetDarkFieldQuality), guid, errorImagePath);
            throw;
        }
    }

    public (double XQuality, double YQuality) GetXyQuality(BitmapImage image, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            _algorithm.DarkDefinition(hImage, out var meanTupleY, out var meanTupleX);

            using var _1 = meanTupleX;
            using var _2 = meanTupleY;

            return (meanTupleX.D, meanTupleY.D);
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(GetXyQuality));
            LogAlgorithmError(ex, nameof(GetXyQuality), guid, errorImagePath);
            throw;
        }
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(BitmapImage image, Rect roiRect, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            using var roiImage = hImage.ToROI(roiRect);

            _algorithm.WuMTF(roiImage, out var mtfX, out var mtfY);

            using var _1 = mtfX;
            using var _2 = mtfY;

            return (mtfX.D, mtfY.D);
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(ModulationTransferFunction));
            LogAlgorithmError(ex, nameof(ModulationTransferFunction), guid, errorImagePath);
            throw;
        }
    }

    public BestFocus GetBestFocus(BitmapImage image, double startECS, double stopECS, AlgorithmEngineTypeEnum algorithmEngineTypeEnum, AlgorithmBestFocusTypeEnum algorithmBestFocusTypeEnum, Guid guid)
    {
        try
        {
            var bestFocus = algorithmEngineTypeEnum switch
            {
                AlgorithmEngineTypeEnum.HAlgorithm => GetHAlgorithemBestFocus(image),
                AlgorithmEngineTypeEnum.AlgoCSharp => GetAlgoCVSharpBestFocus(image, algorithmBestFocusTypeEnum),
                _ => ThrowHelper.ThrowNotSupportedException<BestFocus>()
            };

            bestFocus.BestXStrehlRatioECS = startECS + bestFocus.BestXStrehlRatioPoint.X / image.Size.Width * (stopECS - startECS);
            bestFocus.BestYStrehlRatioECS = startECS + bestFocus.BestYStrehlRatioPoint.X / image.Size.Width * (stopECS - startECS);
            bestFocus.IsAlgorithmOk = true;

            return bestFocus;
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(GetBestFocus));
            LogAlgorithmError(ex, nameof(GetBestFocus), guid, errorImagePath);
            throw;
        }
    }

    private BestFocus GetHAlgorithemBestFocus(BitmapImage image)
    {
        using var hImage = image.ToHImage();

        #region 算法调用

        _algorithm.STLR_kla(
            hImage,
            out var hvXListHTuple,
            out var hvXRatioMaxHTuple,
            out var hvXRatioMeanHTuple,
            out var hvXRatioMinHTuple,
            out var hvYRatioMaxHTuple,
            out var hvYRatioMeanHTuple,
            out var hvYRatioMinHTuple,
            out var hvXValuesHTuple,
            out var hvIndXHTuple,
            out var hvXMaxHTuple,
            out var hvYValuesHTuple,
            out var hvIndYHTuple,
            out var hvYMaxHTuple,
            out var hvPlotXHTuple,
            out var hvPlotYHTuple,
            out var xStrehlList,
            out var yStrehlList,
            out var hvRowBeginXHTuple,
            out var hvColBeginXHTuple,
            out var hvRowEndXHTuple,
            out var hvColEndXHTuple,
            out var hvKxHTuple,
            out var hvRowBeginYHTuple,
            out var hvColBeginYHTuple,
            out var hvRowEndYHTuple,
            out var hvColEndYHTuple,
            out var hvKyHTuple,
            out var hvPercentMeanHTuple);

        using var _0 = hvXListHTuple;
        using var _1 = hvXRatioMaxHTuple;
        using var _2 = hvXRatioMeanHTuple;
        using var _3 = hvXRatioMinHTuple;
        using var _4 = hvYRatioMaxHTuple;
        using var _5 = hvYRatioMeanHTuple;
        using var _6 = hvYRatioMinHTuple;
        using var _7 = hvXValuesHTuple;
        using var _8 = hvIndXHTuple;
        using var _9 = hvXMaxHTuple;
        using var _10 = hvYValuesHTuple;
        using var _11 = hvIndYHTuple;
        using var _12 = hvYMaxHTuple;
        using var _13 = hvPlotXHTuple;
        using var _14 = hvPlotYHTuple;
        using var _15 = hvRowBeginXHTuple;
        using var _16 = hvColBeginXHTuple;
        using var _17 = hvRowEndXHTuple;
        using var _18 = hvColEndXHTuple;
        using var _19 = hvKxHTuple;
        using var _20 = hvRowBeginYHTuple;
        using var _21 = hvColBeginYHTuple;
        using var _22 = hvRowEndYHTuple;
        using var _23 = hvColEndYHTuple;
        using var _24 = hvKyHTuple;
        using var _25 = hvPercentMeanHTuple;

        #endregion

        var xs = Generate.LinearRangeInt32(0, hvXListHTuple.Length - 1);
        var xFieldTiltPoints = Generate.LinearRangeInt32(0, hvPlotXHTuple.Length - 1).Select(t => new Point(t, hvPlotXHTuple[t].D)).ToArray();
        var (xFieldTiltFitSlope, xFieldTiltFitIntercept, xFieldTiltFitRSquared, xFieldTiltFitYPredicted) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. xFieldTiltPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. xFieldTiltPoints.Select(t => t.Y)]));
        var xFieldTiltFitPoints = xFieldTiltPoints.Index().Select(t => new Point(t.Item.X, xFieldTiltFitYPredicted[t.Index])).ToArray();

        var yFieldTiltPoints = Generate.LinearRangeInt32(0, hvPlotYHTuple.Length - 1).Select(t => new Point(t, hvPlotYHTuple[t].D)).ToArray();
        var (yFieldTiltFitSlope, yFieldTiltFitIntercept, yFieldTiltFitRSquared, yFieldTiltFitYPredicted) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. yFieldTiltPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. yFieldTiltPoints.Select(t => t.Y)]));
        var yFieldTiltFitPoints = yFieldTiltPoints.Index().Select(t => new Point(t.Item.X, yFieldTiltFitYPredicted[t.Index])).ToArray();

        var bestFocus = new BestFocus
        {
            XStrehlRatioPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvXRatioMeanHTuple[t].D))],
            XStrehlRatioFitPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvXValuesHTuple[t].D))],
            XStrehlRatioColumnPoints = [.. xs.Select<int, IReadOnlyList<Point>>(t => [new Point(hvXListHTuple[t].D, hvXRatioMinHTuple[t].D), new Point(hvXListHTuple[t].D, hvXRatioMaxHTuple[t].D)])],
            BestXStrehlRatioPoint = new Point(hvIndXHTuple.D, hvXMaxHTuple.D),
            XIntraRibbonFieldsPoints = [.. xStrehlList.Select<double[], IReadOnlyList<Point>>(t => [.. xs.Select(tt => new Point(hvXListHTuple[tt].D, t[tt]))])],
            XFieldTiltPoints = xFieldTiltPoints,
            XFieldTiltFitSlope = xFieldTiltFitSlope,
            XFieldTiltFitIntercept = xFieldTiltFitIntercept,
            XFieldTiltFitRSquared = xFieldTiltFitRSquared,
            XFieldTiltFitPoints = xFieldTiltFitPoints,
            YStrehlRatioPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvYRatioMeanHTuple[t].D))],
            YStrehlRatioFitPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvYValuesHTuple[t].D))],
            YStrehlRatioColumnPoints = [.. xs.Select<int, IReadOnlyList<Point>>(t => [new Point(hvXListHTuple[t].D, hvYRatioMinHTuple[t].D), new Point(hvXListHTuple[t].D, hvYRatioMaxHTuple[t].D)])],
            BestYStrehlRatioPoint = new Point(hvIndYHTuple.D, hvYMaxHTuple.D),
            YIntraRibbonFieldsPoints = [.. yStrehlList.Select<double[], IReadOnlyList<Point>>(t => [.. xs.Select(tt => new Point(hvXListHTuple[tt].D, t[tt]))])],
            YFieldTiltPoints = yFieldTiltPoints,
            YFieldTiltFitSlope = yFieldTiltFitSlope,
            YFieldTiltFitIntercept = yFieldTiltFitIntercept,
            YFieldTiltFitRSquared = yFieldTiltFitRSquared,
            YFieldTiltFitPoints = yFieldTiltFitPoints,
            SpotAreaPercentMean = hvPercentMeanHTuple.D
        };

        return bestFocus;
    }

    private BestFocus GetAlgoCVSharpBestFocus(BitmapImage image, AlgorithmBestFocusTypeEnum algorithmBestFocusTypeEnum)
    {
        // 输入为正向图，XAxis.Col 表示宽方向为 X 轴，输出矩阵与图像方向一致
        using var algoImage = image.ToAlgoCVImage();
        BestFocusEngineResult algoResult;

        switch (algorithmBestFocusTypeEnum)
        {
            case AlgorithmBestFocusTypeEnum.DSW065:
                using (var engine = new BestFocusEngine(new BestFocusConfig()))
                {
                    algoResult = engine.Process(algoImage, XAxis.Col);
                }

                break;

            case AlgorithmBestFocusTypeEnum.DSW:
                algoResult = BestFocusEngine.ProcessDSW163(algoImage, XAxis.Col);
                break;

            default:
                throw new NotSupportedException($"Algorithm Best Focus Type {algorithmBestFocusTypeEnum} is not supported.");
        }

        var bestfocus = ConvertToBestFocus(algoResult);
        algoResult.Dispose();
        return bestfocus;
    }

    public Size GetPixelSize(BitmapImage image, Size standardMaskSquareSize, Guid guid, AlgorithmEngineTypeEnum algorithmEngineTypeEnum, out BitmapImage drawingImage, out double angle)
    {
        try
        {
            switch (algorithmEngineTypeEnum)
            {
                case AlgorithmEngineTypeEnum.HAlgorithm:
                    return GetHAlgorithemPixelSize(image, standardMaskSquareSize, out drawingImage, out angle);

                case AlgorithmEngineTypeEnum.AlgoCSharp:
                    return GetAlgoCVSharpPixelSize(image, standardMaskSquareSize, out drawingImage, out angle);

                default:
                    throw new NotSupportedException($"Algorithm Type {algorithmEngineTypeEnum} is not supported.");
            }
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(GetPixelSize));
            LogAlgorithmError(ex, nameof(GetPixelSize), guid, errorImagePath);
            throw;
        }
    }

    private Size GetHAlgorithemPixelSize(BitmapImage image, Size standardMaskSquareSize, out BitmapImage drawingImage, out double angle)
    {
        using var hImage = image.ToHImage();
        _algorithm.CalculatePixSize(hImage, out var drawingImageObj, standardMaskSquareSize.Height, standardMaskSquareSize.Width, out var yTuple, out var xTuple, out var angleX);
        using var _1 = xTuple;
        using var _2 = yTuple;
        using var _3 = angleX;
        angle = angleX.D;

        using var drawingHImage = new HImage(drawingImageObj);
        drawingImage = drawingHImage.ToBitmapImage();
        return new Size(xTuple.D, yTuple.D);
    }

    /// <summary>
    /// AlgoCSharp 引擎（BfPixelSize 明场网格拟合）：显微 BF 图为常规格式（8bit 灰/彩色，jpg 直读），
    /// 桥直搬保持物理正向。
    /// </summary>
    private Size GetAlgoCVSharpPixelSize(BitmapImage image, Size standardMaskSquareSize, out BitmapImage drawingImage, out double angle)
    {
        using var algoImage = image.ToAlgoCVImage();

        // 按标准掩膜网格尺寸预估周期扫描窗（period_half/n_max 同值放宽；有效上限还受 2·half 铺 2 窗限制）：
        // Size100(5x，实测格距 146px)/Size50(10x，73px) 用默认 400；Size25(50x，365px) 贴默认上限，放宽到 500；Size10(100x/150x，289/431px) 放宽到 600。
        // period_n_max 不宜过大：超实际格距约 2 倍后周期估计可能锁到倍频（10x 帧 half=500+ 实测错锁 5 倍格距 364px）。
        var scanMax = standardMaskSquareSize.Width switch
        {
            >= 50 => 400,
            >= 25 => 500,
            _ => 600
        };

        var config = new BfPixelSizeConfig { period_half = scanMax, period_n_max = scanMax };
        using var engine = new BfPixelSizeEngine(config);
        using var result = engine.Process(algoImage);

        // hvc 为拟合格基矢：tx/ty = x/y 向格距(像素/格)；col_vec = j 向单格位移 (drow, dcol)。
        // pixel size = 物理格尺寸 / 格距；angle = 格向量相对 x 轴倾角(度)。
        angle = Math.Atan2(result.hvc.col_vec[0], result.hvc.col_vec[1]) * 180.0 / Math.PI;
        drawingImage = image.Copy(); // todo：实现绘制

        return new Size(standardMaskSquareSize.Width / result.hvc.tx, standardMaskSquareSize.Height / result.hvc.ty);
    }

    public double GetYPixelSize(BitmapImage image, double standardMaskSquareYSize, Guid guid, AlgorithmEngineTypeEnum algorithmEngineTypeEnum, out BitmapImage drawingImage, out Point[] yProjects, out int[] resultIndexs)
    {
        try
        {
            switch (algorithmEngineTypeEnum)
            {
                case AlgorithmEngineTypeEnum.HAlgorithm:
                    return GetHAlgorithemYPixelSize(image, standardMaskSquareYSize, out drawingImage, out yProjects, out resultIndexs);

                case AlgorithmEngineTypeEnum.AlgoCSharp:
                    return GetAlgoCVSharpYPixelSize(image, standardMaskSquareYSize, out drawingImage, out yProjects, out resultIndexs);

                default:
                    throw new NotSupportedException($"Algorithm Type {algorithmEngineTypeEnum} is not supported.");
            }
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(GetYPixelSize));
            LogAlgorithmError(ex, nameof(GetYPixelSize), guid, errorImagePath);
            throw;
        }
    }

    private double GetHAlgorithemYPixelSize(BitmapImage image, double standardMaskSquareYSize, out BitmapImage drawingImage, out Point[] yProjects, out int[] resultIndexes)
    {
        using var hImage = image.ToHImage();
        var data = hImage.RAW16BitsPerPixelToMatrix();
        var matrix = Matrix<double>.Build.DenseOfArray(data);
        var projectionYs = matrix.RowSums() / matrix.ColumnCount;

        // 使用AMPD算法找出波峰
        var signal = Vector<double>.Build.DenseOfEnumerable(projectionYs.Select(t => -t));
        var peaks = AutomaticMPeakDetection.Ampd(signal);

        var xDifferences = peaks
            .Zip(peaks.Skip(1), (prev, next) => (double)next - prev)
            .ToArray();
        var (indexes, filterXDifferences) = Filter.MAD(xDifferences);

        // 所有后一个减去前一个，得到差值, 然后取得均值
        var mean = filterXDifferences.Average();

        using var drawHImage = hImage.DrawLines(
            [
                .. indexes
                    .SelectMany(t => (int[])[t, t + 1])
                    .Distinct()
                    .OrderBy(t => t)
                    .Select(index => (new Point(0, peaks[index]), new Point(image.Width, peaks[index])))
            ],
            5);
        drawingImage = drawHImage.ToBitmapImage();

        yProjects = [.. projectionYs.Select((t, i) => new Point(i, t))];
        resultIndexes = [.. peaks];
        return standardMaskSquareYSize / mean;
    }

    /// <summary>
    /// AlgoCVSharp 计算 Y pixel size，输入为正向图，XAxis.Row（行方向为 X，默认口径），输出与图像方向一致
    /// </summary>
    private double GetAlgoCVSharpYPixelSize(BitmapImage image, double standardMaskSquareYSize, out BitmapImage drawingImage, out Point[] yProjects, out int[] resultIndexes)
    {
        using var algoImage = image.ToAlgoCVImage();
        using var engine = new DfPixelSizeEngine(new DfPixelSizeConfig());
        using var result = engine.Process(algoImage, XAxis.Row);

        // pairs.step_mean_c = 列向步长均值(像素/格)；Y pixel size = 物理格高 / 列向步长。
        // Y 投影曲线 = 每行灰度均值（与 HAlgorithm 版 projectionYs 同信号）；配对行位置 r0 对应 AMPD 峰位语义（竖线标记）。
        // 输入为正向图（XAxis.Row 为 DfPixelSize 默认口径），rowMean 按行号投影，r0 与 rowMean 下标一致
        var rowMean = new double[algoImage.Height];
        var pixels = algoImage.ToSpan<ushort>();
        for (var row = 0; row < algoImage.Height; row++)
        {
            long sum = 0;
            var offset = row * algoImage.Width;
            for (var col = 0; col < algoImage.Width; col++)
                sum += pixels[offset + col];

            rowMean[row] = (double)sum / algoImage.Width;
        }

        drawingImage = image.Copy();
        yProjects = [.. rowMean.Select((t, i) => new Point(i, t))];
        resultIndexes = [.. result.pairs.pairs.Select(t => (int)Math.Round(t.r0))];

        return standardMaskSquareYSize / result.pairs.step_mean_c;
    }

    public bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, BitmapImage image, string templateFilePath, Rect rect, Guid guid, out BitmapImage templateImage)
    {
        templateImage = BitmapImage.Random(2448, 2048, 10);

        try
        {
            using var hImage = image.ToHImage();
            using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
            var temp = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);
            DirectoryHelper.CreateFileDirectoryIfNotExists(temp);
            FileHelper.DeleteFileIfExists(temp);

            _algorithm.HCreateModelXY(scaleImageTo8Bit, out var templateImageObj, algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateFilePath, rect.X, rect.Y, rect.Width, rect.Height);

            using var drawingHImage = new HImage(templateImageObj);

            templateImage.Dispose();

            templateImage = drawingHImage.ToBitmapImage();

            return true;
        }
        catch (Exception ex)
        {
            templateImage.Dispose();
            templateImage = BitmapImage.Random(2448, 2048, 10);
            var errorImagePath = SaveErrorImage(image, guid, nameof(TryGenerateTemplate));
            LogAlgorithmError(ex, nameof(TryGenerateTemplate), guid, errorImagePath);
            throw;
        }
    }

    public bool TryReadTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, out HTuple templateId)
    {
        templateId = HalconFactory.EmptyHTuple;

        var temp = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);
        if (File.Exists(temp) == false) throw new FileNotFoundException(nameof(templateFilePath), temp);

        _algorithm.HReadModel(algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateFilePath, out templateId);

        return true;
    }

    public bool TryCleanTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HTuple templateId)
    {
        _algorithm.HClearModel(algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateId);

        return true;
    }

    public bool TryTemplateMatchToOffset(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, BitmapImage image, HTuple templateId, Guid guid, out Point markPoint, out Point offsetPoint, out double score, out double angle)
    {
        markPoint = Point.Origin;
        offsetPoint = Point.Origin;
        score = 0;
        angle = 0;

        try
        {
            using var hImage = image.ToHImage();
            using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
            _algorithm.HFindModel(scaleImageTo8Bit, algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateId, out var yHTuple, out var xHTuple, out var angleHTuple, out var scoreHTuple);
            using var _1 = yHTuple;
            using var _2 = xHTuple;
            using var _3 = angleHTuple;
            using var _4 = scoreHTuple;
            if (xHTuple.Length == 0 || yHTuple.Length == 0 || scoreHTuple.Length == 0 || angleHTuple.Length == 0) return false;

            var size = scaleImageTo8Bit.GetSize();
            markPoint = new Point(xHTuple.D, yHTuple.D);
            score = scoreHTuple.D;
            angle = angleHTuple.D;

            var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
            var tryGetMatchPosition = score >= templateMatchScoreThreshold;
            if (tryGetMatchPosition == false)
            {
                return false;
            }

            var offset = markPoint - (Point)((Size)size / 2d);
            offsetPoint = new Point(offset.X, -offset.Y);

            return true;
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(TryTemplateMatchToOffset));
            LogAlgorithmError(ex, nameof(TryTemplateMatchToOffset), guid, errorImagePath);
            throw;
        }
    }

    public (BitmapImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(BitmapImage hazeImage, BitmapImage shinyWaferImage, double rotateAngle, Guid guid)
    {
        try
        {
            using var hHazeImage = hazeImage.ToHImage();
            using var hShinyWaferImage = shinyWaferImage.ToHImage();
            _algorithm.CalculateTwoRegionCenter(hHazeImage, hShinyWaferImage, out var resultImage, rotateAngle, out var diameter, out var angle, out var dRow, out var dCol, out var row, out var col);

            using var drawingHImage = new HImage(resultImage);
            var drawingImage = drawingHImage.ToBitmapImage();

            return (drawingImage, diameter.D, angle.D, new Point(dCol.D, dRow.D), new Point(col.D, row.D));
        }
        catch (Exception ex)
        {
            var hazeErrorImagePath = SaveErrorImage(hazeImage, guid, $"{nameof(GetOpticsObjectiveYAngleResult)}_Haze");
            var shinyErrorImagePath = SaveErrorImage(shinyWaferImage, guid, $"{nameof(GetOpticsObjectiveYAngleResult)}_Shiny");
            logger.LogHtmlError(ex, $"{nameof(GetOpticsObjectiveYAngleResult)} Error", HtmlHeaderLevelEnum.Header3,
                new HtmlBullet(new
                {
                    HazeErrorImage = new HtmlImage(hazeErrorImagePath),
                    HazeImagePath = hazeErrorImagePath,
                    ShinyErrorImage = new HtmlImage(shinyErrorImagePath),
                    ShinyImagePath = shinyErrorImagePath,
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
            throw;
        }
    }

    public (double StartImageYPixel, double EndImageYPixel) GetOpticsROOSResult(BitmapImage image, out HImage drawImage)
    {
        using var hImage = image.ToHImage();
        _algorithm.FindLineYPosition(hImage, 1, out var resultTuple, out var drawImageObj);

        drawImage = new HImage(drawImageObj);
        var size = hImage.GetSize();

        return resultTuple.Length switch
        {
            0 => (0, size.Height),
            1 => resultTuple[0].D <= size.Height / 2d ? (resultTuple[0].D, size.Height) : (0, resultTuple[0].D),
            _ => (resultTuple[0].D, resultTuple[1].D)
        };
    }

    public Point GetChuckCenter(
        Point firstTopLeftPosition,
        Point secondTopLeftPosition,
        Point secondTopRightPosition,
        Point firstTopRightPosition,
        Point firstBottomLeftPosition,
        Point secondBottomLeftPosition,
        Point secondBottomRightPosition,
        Point firstBottomRightPosition)
    {
        double[] xArray = [firstTopLeftPosition.X, secondTopLeftPosition.X, secondTopRightPosition.X, firstTopRightPosition.X, firstBottomLeftPosition.X, secondBottomLeftPosition.X, secondBottomRightPosition.X, firstBottomRightPosition.X];
        double[] yArray = [firstTopLeftPosition.Y, secondTopLeftPosition.Y, secondTopRightPosition.Y, firstTopRightPosition.Y, firstBottomLeftPosition.Y, secondBottomLeftPosition.Y, secondBottomRightPosition.Y, firstBottomRightPosition.Y];
        _algorithm.WaferCenterCalculate(xArray, yArray, out var xHTuple, out var yHTuple);
        using var _1 = xHTuple;
        using var _2 = yHTuple;

        var centerX = xHTuple.D;
        var centerY = yHTuple.D;

        return new Point(centerX, centerY);
    }

    public bool CalculateChuckStageMapError(
        StageMapDto stageMapDto,
        bool isXOnlyGantryError,
        Guid guid,
        int calculateContainRowMinCount,
        int calculateContainColumnMinCount,
        double alignmentThreshold,
        double gantryThreshold,
        double scaleThreshold,
        double diameter)
    {
        var (idealXArray, idealYArray) = stageMapDto.GetIdealArray();
        var (realXArray, realYArray, isInWaferArray, templateMathIsOkArray) = stageMapDto.GetRealArray();

        var idealXMatrix = Matrix<double>.Build.DenseOfArray(idealXArray);
        var idealYMatrix = Matrix<double>.Build.DenseOfArray(idealYArray);
        var realXMatrix = Matrix<double>.Build.DenseOfArray(realXArray);
        var realYMatrix = Matrix<double>.Build.DenseOfArray(realYArray);
        var isInWaferMatrix = Matrix<double>.Build.DenseOfArray(isInWaferArray);
        var templateMathIsOkMatrix = Matrix<double>.Build.DenseOfArray(templateMathIsOkArray);

        var (isSuccess, errorXMatrix, errorYMatrix) = affineTransformation.CalculateMatrixError(
            idealXMatrix,
            idealYMatrix,
            realXMatrix,
            realYMatrix,
            isInWaferMatrix,
            templateMathIsOkMatrix,
            isXOnlyGantryError,
            guid,
            calculateContainRowMinCount: calculateContainRowMinCount,
            calculateContainColumnMinCount: calculateContainColumnMinCount,
            diameter: diameter,
            alignmentThreshold: alignmentThreshold,
            gantryThreshold: gantryThreshold,
            scaleThreshold: scaleThreshold
        );

        for (var row = 0; row < stageMapDto.RowNumber; row++)
        {
            for (var column = 0; column < stageMapDto.ColumnNumber; column++)
            {
                stageMapDto.ErrorMatrix[row][column] = new Point(errorXMatrix[row, column], errorYMatrix[row, column]);
            }
        }

        return isSuccess;
    }

    public StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId)
    {
        return affineTransformation.ExpandStageMapDto(baseStageMap, mergeStageMap, htmlLogUniqueId);
    }

    public double[] GetImageGrayYProjectionsPixels(BitmapImage image, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            _algorithm.LightSpot(hImage, out var yValue);

            return yValue.ToDArr();
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(GetImageGrayYProjectionsPixels));
            LogAlgorithmError(ex, nameof(GetImageGrayYProjectionsPixels), guid, errorImagePath);
            throw;
        }
    }

    public (Point CenterPosition, double Radius) FitCircle(IReadOnlyList<Point> points)
    {
        _algorithm.FindCircle(points.Select(t => t.X).ToList(), points.Select(t => t.Y).ToList(), out var yValue, out var xValue, out var radius);

        return (new Point(xValue.D, yValue.D), radius.D);
    }

    public ICalibrationAlgorithmService.RTAScanSample[] GetRTAScanSamples(DarkFieldRawScanImageDTO rawScanImage)
    {
        using var rawImage = new Image(rawScanImage.RawImageFilePath);
        Guard.IsEqualTo(rawImage.Width, rawScanImage.Size.Height);
        Guard.IsEqualTo(rawImage.Height, rawScanImage.Size.Width);

        using var engine = new StageMapEngine(new StageMapConfig());
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

                xMatrix[y][x] = double.IsFinite(yPixel) ? yPixel : double.NaN;
                yMatrix[y][x] = double.IsFinite(xPixel) ? xPixel : double.NaN;
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

        var (xSpacing, _, _, _) = PolynomialCurve.Fit1(
            Vector<double>.Build.DenseOfEnumerable(xPoints.Select(point => point.X)),
            Vector<double>.Build.DenseOfEnumerable(xPoints.Select(point => point.Y)));
        var (ySpacing, _, _, _) = PolynomialCurve.Fit1(
            Vector<double>.Build.DenseOfEnumerable(yPoints.Select(point => point.X)),
            Vector<double>.Build.DenseOfEnumerable(yPoints.Select(point => point.Y)));

        Guard.IsTrue(double.IsFinite(xSpacing) && xSpacing > 0d);
        Guard.IsTrue(double.IsFinite(ySpacing) && ySpacing > 0d);

        // 首个有效行和列只作为锚点, 标准矩阵保留跳过的空行和空列对应的索引偏移.
        var yAnchorRow = Array.FindIndex(yMatrix, pixels => pixels.Any(double.IsFinite));
        var yAnchor = yMatrix[yAnchorRow].Where(double.IsFinite).Average();

        var xAnchorColumn = Generate.LinearRangeInt32(0, xCount - 1).First(x => xMatrix.Any(pixels => double.IsFinite(pixels[x])));
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
                idealXMatrix[0, x],
                new Vector(errorXMatrix.Column(x).Average(), errorYMatrix.Column(x).Average()));
        }

        return samples;
    }

    #region BestFocus Algo

    private static BestFocus ConvertToBestFocus(BestFocusEngineResult result)
    {
        var xStrehlCols = Generate.LinearRangeInt32(0, result.metric_array_info_x.metric_array_cols - 1);
        var yStrehlCols = Generate.LinearRangeInt32(0, result.metric_array_info_y.metric_array_cols - 1);

        var xStrehlRows = Generate.LinearRangeInt32(0, result.metric_array_info_x.metric_array_rows - 1);
        var yStrehlRows = Generate.LinearRangeInt32(0, result.metric_array_info_y.metric_array_rows - 1);

        // X/YStrehlRatioFitPoints暂时不实现
        var bestFocus = new BestFocus
        {
            IsAlgorithmOk = true,
            XStrehlRatioPoints = GetStrehlRatioPoints(result.metric_array_info_x, true),
            XStrehlRatioColumnPoints = GetStrehlRatioColumnPoints(result.metric_array_info_x, true),
            XIntraRibbonFieldsPoints = GetIntraRibbonFieldsPoints(result.metric_array_info_x, true),
            YStrehlRatioPoints = GetStrehlRatioPoints(result.metric_array_info_y, false),
            YStrehlRatioColumnPoints = GetStrehlRatioColumnPoints(result.metric_array_info_y, false),
            YIntraRibbonFieldsPoints = GetIntraRibbonFieldsPoints(result.metric_array_info_y, false)
        };

        BuildDirectionData(result.metric_array_info_x, true);
        BuildDirectionData(result.metric_array_info_y, false);

        return bestFocus;

        IReadOnlyList<Point> GetStrehlRatioPoints(FocusMetricArrayInfo info, bool isX)
        {
            var cols = isX ? xStrehlCols : yStrehlCols;
            return [.. cols.Select(t => new Point(info.pixel_pos_per_col[t], info.median_metric_per_col[t]))];
        }

        IReadOnlyList<IReadOnlyList<Point>> GetStrehlRatioColumnPoints(FocusMetricArrayInfo info, bool isX)
        {
            var cols = isX ? xStrehlCols : yStrehlCols;
            return
            [
                .. cols.Select<int, IReadOnlyList<Point>>(t =>
                [
                    new Point(info.pixel_pos_per_col[t], info.min_metric_per_col[t]),
                    new Point(info.pixel_pos_per_col[t], info.max_metric_per_col[t])
                ])
            ];
        }

        IReadOnlyList<IReadOnlyList<Point>> GetIntraRibbonFieldsPoints(FocusMetricArrayInfo info, bool isX)
        {
            var rows = isX ? xStrehlRows : yStrehlRows;
            return
            [
                .. rows
                    .Select(rowIndex => info.metric_array_filtered.GetRow<float>(rowIndex).ToArray())
                    .Select<float[], IReadOnlyList<Point>>(cols => [.. cols.Zip(info.pixel_pos_per_col, (col, px) => new Point(px, col))])
            ];
        }

        void BuildDirectionData(FocusMetricArrayInfo info, bool isX)
        {
            Point[] fieldTiltPoints = [.. info.max_pixel_pos_per_row.Select((t, i) => new Point(i, t))];

            var (slope, intercept, rSquared, fieldTiltFitYPredicted) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. fieldTiltPoints.Select(t => t.X)]),
                Vector<double>.Build.Dense([.. fieldTiltPoints.Select(t => t.Y)]));

            var fieldTiltFitPoints = fieldTiltPoints.Select((p, i) => new Point(p.X, fieldTiltFitYPredicted[i])).ToArray();
            var bestPoint = new Point(info.max_metric_in_median_pixel_pos, info.max_metric_in_median);

            if (isX)
            {
                bestFocus.BestXStrehlRatioPoint = bestPoint;
                bestFocus.XFieldTiltPoints = fieldTiltPoints;
                bestFocus.XFieldTiltFitSlope = slope;
                bestFocus.XFieldTiltFitIntercept = intercept;
                bestFocus.XFieldTiltFitRSquared = rSquared;
                bestFocus.XFieldTiltFitPoints = fieldTiltFitPoints;
            }
            else
            {
                bestFocus.BestYStrehlRatioPoint = bestPoint;
                bestFocus.YFieldTiltPoints = fieldTiltPoints;
                bestFocus.YFieldTiltFitSlope = slope;
                bestFocus.YFieldTiltFitIntercept = intercept;
                bestFocus.YFieldTiltFitRSquared = rSquared;
                bestFocus.YFieldTiltFitPoints = fieldTiltFitPoints;
            }
        }
    }

    #endregion

    private string SaveErrorImage(BitmapImage image, Guid guid, string methodName)
    {
        var errorImagePath = Path.Combine(ErrorImageDirectory, $"{methodName}_{guid}.jpg");
        DirectoryHelper.CreateFileDirectoryIfNotExists(errorImagePath);
        image.SaveImage(errorImagePath);
        return errorImagePath;
    }

    private void LogAlgorithmError(Exception ex, string methodName, Guid guid, string? errorImagePath = null)
    {
        if (string.IsNullOrEmpty(errorImagePath))
        {
            logger.LogHtmlError(ex, $"{methodName} Error", HtmlHeaderLevelEnum.Header6,
                new HtmlBullet(new
                {
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
        }
        else
        {
            logger.LogHtmlError(ex, $"{methodName} Error", HtmlHeaderLevelEnum.Header6,
                new HtmlBullet(new
                {
                    ErrorImage = new HtmlImage(errorImagePath),
                    ImagePath = errorImagePath,
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
        }
    }
}