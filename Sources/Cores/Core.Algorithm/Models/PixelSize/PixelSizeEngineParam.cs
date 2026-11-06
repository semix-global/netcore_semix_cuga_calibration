using algocv_sharp;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Models.Setting;
using Net.Utilities.Calibration;

namespace Core.Algorithm.Models.PixelSize;

/// <summary>
/// BF 像素尺寸引擎参数
/// </summary>
public sealed class PixelSizeEngineParam
{
    /// <summary>
    /// 标准掩膜方块规格（枚举底层值即网格尺寸 um）
    /// </summary>
    public AlgorithmStandardMaskSquareSizeEnum StandardMaskSquareSize { get; init; }

    /// <summary>
    /// 算法库独有的参数
    /// </summary>
    public EngineParams? EngineParams { get; init; }

    public object ToHtmlAnonymous() => new
    {
        StandardMaskSquareSize,
        EngineParams = EngineParams?.ToHtmlAnonymous()
    };
}

/// <summary>
/// AlgoCv BF 像素尺寸引擎参数（转换层：不直接暴露底层 BfPixelSizeConfig 结构体）
/// </summary>
public sealed class AlgoCvPixelSizeParams : EngineParams
{
    /// <summary>
    /// 中心条带比例（竖条宽/横条高 = round_py(尺寸·该比例)）
    /// </summary>
    public double RoiFrac { get; init; } = 0.5;

    /// <summary>
    /// 条带投影算子
    /// </summary>
    public AlgorithmPeriodOpEnum PeriodOp { get; init; } = AlgorithmPeriodOpEnum.Mean;

    /// <summary>
    /// 投影 1D 中值滤波半径（像素）
    /// </summary>
    public int ProjMedianR { get; init; } = 2;

    /// <summary>
    /// 投影轻高斯 sigma（像素），滤波半径取 round_py(3σ)
    /// </summary>
    public double ProjGaussSigma { get; init; } = 1d;

    /// <summary>
    /// 相关带半宽（像素）。0 表示按掩膜规格自动选取扫描窗
    /// </summary>
    public int PeriodHalf { get; init; }

    /// <summary>
    /// 周期扫描最短格距
    /// </summary>
    public int PeriodNMin { get; init; } = 40;

    /// <summary>
    /// 周期扫描最长格距上限。0 表示按掩膜规格自动选取扫描窗
    /// </summary>
    public int PeriodNMax { get; init; }

    /// <summary>
    /// 局部极大入围门槛（相对最高分）
    /// </summary>
    public double PeriodPeakFrac { get; init; } = 0.9;

    /// <summary>
    /// 锚点平滑中值半径 / 周期
    /// </summary>
    public double AnchorMedianFrac { get; init; } = 0.05;

    /// <summary>
    /// 锚点平滑高斯 sigma / 周期
    /// </summary>
    public double AnchorGaussFrac { get; init; } = 0.035;

    /// <summary>
    /// 走步搜索窗下界 / 周期
    /// </summary>
    public double AnchorLoFrac { get; init; } = 0.4;

    /// <summary>
    /// 走步搜索窗上界 / 周期
    /// </summary>
    public double AnchorHiFrac { get; init; } = 1.8;

    /// <summary>
    /// 非极大抑制半径 / 周期
    /// </summary>
    public double AnchorNmsFrac { get; init; } = 0.25;

    /// <summary>
    /// 模板边长 / 周期（再取奇数）
    /// </summary>
    public double TemplateSizeFrac { get; init; } = 0.6;

    /// <summary>
    /// 模板匹配搜索半径（像素）
    /// </summary>
    public int MatchRadiusPx { get; init; } = 10;

    /// <summary>
    /// TM_CCOEFF_NORMED 得分阈值
    /// </summary>
    public double MatchScoreThresh { get; init; } = 0.85;

    /// <summary>
    /// 拟合残差容差(px)，超者为异常点
    /// </summary>
    public double FitJitter { get; init; } = 3d;

    /// <summary>
    /// 按掩膜规格预估周期扫描窗（档位沿自原服务实现：100/50→400、25→500、10→600）
    /// </summary>
    internal static int ResolvePeriodScanMax(AlgorithmStandardMaskSquareSizeEnum maskSize) => (int)maskSize switch
    {
        >= 50 => 400,
        >= 25 => 500,
        _ => 600
    };

    internal BfPixelSizeConfig ToBfPixelSizeConfig(AlgorithmStandardMaskSquareSizeEnum maskSize)
    {
        var scanMax = ResolvePeriodScanMax(maskSize);

        return new()
        {
            roi_frac = RoiFrac,
            period_op = PeriodOp.ToAlgoCvPeriodOp(),
            proj_median_r = ProjMedianR,
            proj_gauss_sigma = ProjGaussSigma,
            period_half = PeriodHalf > 0 ? PeriodHalf : scanMax,
            period_n_min = PeriodNMin,
            period_n_max = PeriodNMax > 0 ? PeriodNMax : scanMax,
            period_peak_frac = PeriodPeakFrac,
            anchor_median_frac = AnchorMedianFrac,
            anchor_gauss_frac = AnchorGaussFrac,
            anchor_lo_frac = AnchorLoFrac,
            anchor_hi_frac = AnchorHiFrac,
            anchor_nms_frac = AnchorNmsFrac,
            template_size_frac = TemplateSizeFrac,
            match_radius_px = MatchRadiusPx,
            match_score_thresh = MatchScoreThresh,
            fit_jitter = FitJitter
        };
    }

    public override object ToHtmlAnonymous() => new
    {
        RoiFrac,
        PeriodOp,
        ProjMedianR,
        ProjGaussSigma,
        PeriodHalf,
        PeriodNMin,
        PeriodNMax,
        PeriodPeakFrac,
        AnchorMedianFrac,
        AnchorGaussFrac,
        AnchorLoFrac,
        AnchorHiFrac,
        AnchorNmsFrac,
        TemplateSizeFrac,
        MatchRadiusPx,
        MatchScoreThresh,
        FitJitter
    };
}

public static class PixelSizeEngineParamExtensions
{
    extension(AlgorithmEngineTypeEnum engine)
    {
        /// <summary>
        /// 按引擎枚举构造对应 BF 像素尺寸引擎参数（引擎专属参数取默认值，不读业务层设置）
        /// </summary>
        /// <param name="standardMaskSquareSize">标准掩膜方块规格（枚举底层值即网格尺寸 um）</param>
        public PixelSizeEngineParam ToPixelSizeEngineParam(AlgorithmStandardMaskSquareSizeEnum standardMaskSquareSize) => new()
        {
            StandardMaskSquareSize = standardMaskSquareSize,
            EngineParams = engine == AlgorithmEngineTypeEnum.AlgoCSharp ? new AlgoCvPixelSizeParams() : null
        };
    }

    extension(PixelSizeSetting setting)
    {
        /// <summary>
        /// 按传入引擎构造对应 BF 像素尺寸引擎参数（引擎专属参数由业务层 PixelSizeSetting 提供，错库参数由引擎入口 EngineParams.TryGet 硬失败兜底）
        /// </summary>
        /// <param name="standardMaskSquareSize">标准掩膜方块规格（枚举底层值即网格尺寸 um）</param>
        public PixelSizeEngineParam ToPixelSizeEngineParam(AlgorithmEngineTypeEnum engine, AlgorithmStandardMaskSquareSizeEnum standardMaskSquareSize) => new()
        {
            StandardMaskSquareSize = standardMaskSquareSize,
            EngineParams = engine == AlgorithmEngineTypeEnum.AlgoCSharp
                ? setting.AlgoCSharp.ToAlgoCvPixelSizeParams()
                : null
        };
    }
}

/// <summary>
/// 业务层设置 → 引擎参数转换（全字段拷贝）
/// </summary>
public static class AlgoCSharpPixelSizeSettingExtensions
{
    internal static AlgoCvPixelSizeParams ToAlgoCvPixelSizeParams(this AlgoCSharpPixelSizeSetting setting) => new()
    {
        RoiFrac = setting.RoiFrac,
        PeriodOp = setting.PeriodOp,
        ProjMedianR = setting.ProjMedianR,
        ProjGaussSigma = setting.ProjGaussSigma,
        PeriodHalf = setting.PeriodHalf,
        PeriodNMin = setting.PeriodNMin,
        PeriodNMax = setting.PeriodNMax,
        PeriodPeakFrac = setting.PeriodPeakFrac,
        AnchorMedianFrac = setting.AnchorMedianFrac,
        AnchorGaussFrac = setting.AnchorGaussFrac,
        AnchorLoFrac = setting.AnchorLoFrac,
        AnchorHiFrac = setting.AnchorHiFrac,
        AnchorNmsFrac = setting.AnchorNmsFrac,
        TemplateSizeFrac = setting.TemplateSizeFrac,
        MatchRadiusPx = setting.MatchRadiusPx,
        MatchScoreThresh = setting.MatchScoreThresh,
        FitJitter = setting.FitJitter
    };
}