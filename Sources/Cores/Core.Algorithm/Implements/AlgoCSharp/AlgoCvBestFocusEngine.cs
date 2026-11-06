using algocv_sharp;
using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.BestFocus;
using Core.Algorithm.Models.BestFocus;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.DarkField;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.AlgoCSharp;

/// <summary>
/// AlgoCSharp Best Focus 引擎（BestFocusEngine）
/// </summary>
public sealed class AlgoCvBestFocusEngine(ILogger<AlgoCvBestFocusEngine> logger) : IBestFocusEngine
{
    public AlgorithmResultRet<BestFocus> Excute(DarkFieldImageDTO darkFieldImage, BestFocusEngineParam param, Guid guid)
    {
        using var algoImage = RAWImageFactoryV3.CreateImage(darkFieldImage.RawImageFilePath, false);

        try
        {
            var bestFocus = ExcuteAlgoCv(algoImage, param.BestFocusType);
            bestFocus.BestXStrehlRatioECS = param.StartEcs + bestFocus.BestXStrehlRatioPoint.X / algoImage.Width * (param.StopEcs - param.StartEcs);
            bestFocus.BestYStrehlRatioECS = param.StartEcs + bestFocus.BestYStrehlRatioPoint.X / algoImage.Width * (param.StopEcs - param.StartEcs);
            bestFocus.IsAlgorithmOk = true;

            return AlgorithmResultRet.CreateSuccess(bestFocus);
        }
        catch (Exception ex)
        {
            using var errorImage = algoImage.ToBitmapImage(12);
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(errorImage, guid, nameof(Excute));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Excute), guid, errorImagePath);
            throw;
        }
    }

    private static BestFocus ExcuteAlgoCv(Image image, AlgorithmBestFocusTypeEnum algorithmBestFocusTypeEnum)
    {
        // 输入为原始方向的 log 域原图（竖图）
        // X 轴为列方向 (XAxis.Col), 输出矩阵转置（横向矩阵）
        BestFocusEngineResult algoResult;

        switch (algorithmBestFocusTypeEnum)
        {
            case AlgorithmBestFocusTypeEnum.DSW065:
                using (var engine = new BestFocusEngine(new BestFocusConfig()))
                {
                    algoResult = engine.Process(image, XAxis.Col);
                }

                break;

            case AlgorithmBestFocusTypeEnum.DSW:
                algoResult = BestFocusEngine.ProcessDSW163(image, XAxis.Col);
                break;

            default:
                throw new NotSupportedException($"Algorithm Best Focus Type {algorithmBestFocusTypeEnum} is not supported.");
        }

        var bestfocus = ConvertToBestFocus(algoResult);
        algoResult.Dispose();
        return bestfocus;
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

}
