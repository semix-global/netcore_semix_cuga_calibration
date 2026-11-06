using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.BestFocus;
using Core.Algorithm.Models.BestFocus;
using Core.Models.Models.Common.DarkField;
using HalconDotNet;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Halcon;

/// <summary>
/// Halcon Best Focus 引擎（STLR_kla）
/// </summary>
public sealed class HalconBestFocusEngine(ILogger<HalconBestFocusEngine> logger) : IBestFocusEngine
{
    private readonly HAlgorithm.Algorithm _algorithm = new();

    public AlgorithmResultRet<BestFocus> Excute(DarkFieldImageDTO darkFieldImage, BestFocusEngineParam param, Guid guid)
    {
        // Halcon 引擎 raw 底座：V2 直读 raw 产 Halcon 原生 HImage（世界口径正向图），替代旧“显示图 GetImage()→ToHImage”绕道
        using var hImage = RAWImageFactoryV2.CreateImage(darkFieldImage.RawImageFilePath, darkFieldImage.IsToLiner);
        hImage.GetImageSize(out int width, out int height);

        try
        {
            var bestFocus = ExcuteHAlgorithm(hImage);
            bestFocus.BestXStrehlRatioECS = param.StartEcs + bestFocus.BestXStrehlRatioPoint.X / width * (param.StopEcs - param.StartEcs);
            bestFocus.BestYStrehlRatioECS = param.StartEcs + bestFocus.BestYStrehlRatioPoint.X / width * (param.StopEcs - param.StartEcs);
            bestFocus.IsAlgorithmOk = true;

            return AlgorithmResultRet.CreateSuccess(bestFocus);
        }
        catch (Exception ex)
        {
            using var errorImage = hImage.ToBitmapImage(darkFieldImage.IsToLiner ? 16 : 12);
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(errorImage, guid, nameof(Excute));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Excute), guid, errorImagePath);
            throw;
        }
    }

    private BestFocus ExcuteHAlgorithm(HImage hImage)
    {

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

}
