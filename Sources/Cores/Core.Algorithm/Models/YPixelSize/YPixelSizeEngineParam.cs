using algocv_sharp;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Models.Setting;
using Net.Utilities.Calibration;

namespace Core.Algorithm.Models.YPixelSize;

public sealed class YPixelSizeEngineParam
{
    /// <summary>
    /// 标准掩膜方块规格（枚举底层值即 Y 尺寸 um，2026-10-24 用户裁定由裸 double 收敛为封闭枚举）
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
/// AlgoCv Y 像素尺寸引擎参数（转换层：不直接暴露底层 DfPixelSizeConfig 结构体）
/// </summary>
public sealed class AlgoCvYPixelSizeParams : EngineParams
{
    /// <summary>
    /// Y 向投影算子（每行统计），用于估 Ty
    /// </summary>
    public AlgorithmPeriodOpEnum PeriodYOp { get; init; } = AlgorithmPeriodOpEnum.Mean;

    /// <summary>
    /// X 向投影算子（每列统计），用于估 Tx
    /// </summary>
    public AlgorithmPeriodOpEnum PeriodXOp { get; init; } = AlgorithmPeriodOpEnum.Mean;

    /// <summary>
    /// 投影谱 FFT 零填充长度（像素，0 为自动）
    /// </summary>
    public int PeriodZeropad { get; init; } = 16;

    /// <summary>
    /// 投影谱中挑前 n 个峰的个数上限
    /// </summary>
    public int PeriodTopN { get; init; } = 4;

    /// <summary>
    /// Y 向单周期最少像素（Ty 下界）
    /// </summary>
    public double PeriodYMinPx { get; init; } = 5.0;

    /// <summary>
    /// X 向单周期最少像素（Tx 下界）
    /// </summary>
    public double PeriodXMinPx { get; init; } = 5.0;

    /// <summary>
    /// Y 向频域峰值间最小距离（抑制谱峰扎堆）
    /// </summary>
    public int PeriodYMinDist { get; init; } = 3;

    /// <summary>
    /// X 向频域峰值间最小距离（抑制谱峰扎堆）
    /// </summary>
    public int PeriodXMinDist { get; init; } = 3;

    /// <summary>
    /// Y 向至少出现的周期数（Ty 上界）
    /// </summary>
    public double PeriodYMinCycles { get; init; } = 6.0;

    /// <summary>
    /// X 向至少出现的周期数（Tx 上界）
    /// </summary>
    public double PeriodXMinCycles { get; init; } = 6.0;

    /// <summary>
    /// 取中部行的比例（0~1，图像边缘噪声大）
    /// </summary>
    public double MidFrac { get; init; } = 0.5;

    /// <summary>
    /// 行亮斑与中值的最小比值
    /// </summary>
    public double DetectMinFrac { get; init; } = 0.8;

    /// <summary>
    /// 行亮斑与中值的最大比值
    /// </summary>
    public double DetectMaxFrac { get; init; } = 1.5;

    /// <summary>
    /// 行内满足亮斑判定的比例
    /// </summary>
    public double DetectNeedFrac { get; init; } = 0.5;

    /// <summary>
    /// 检测行时的外扩内缩系数
    /// </summary>
    public double DetectInsetK { get; init; } = 1.5;

    /// <summary>
    /// 锚点用中值滤波的窗口比例
    /// </summary>
    public double AnchorMedianFrac { get; init; } = 0.05;

    /// <summary>
    /// 锚点用高斯平滑的窗口比例
    /// </summary>
    public double AnchorGaussFrac { get; init; } = 0.035;

    /// <summary>
    /// 锚点与中值的最小比值
    /// </summary>
    public double AnchorLoFrac { get; init; } = 0.4;

    /// <summary>
    /// 锚点与中值的最大比值
    /// </summary>
    public double AnchorHiFrac { get; init; } = 1.8;

    /// <summary>
    /// 锚点非极大值抑制的窗口比例
    /// </summary>
    public double AnchorNmsFrac { get; init; } = 0.25;

    /// <summary>
    /// 同行配对时允许的半径（像素）
    /// </summary>
    public int MatchRadiusPx { get; init; } = 10;

    /// <summary>
    /// 同行配对的得分阈值
    /// </summary>
    public double MatchScoreThresh { get; init; } = 0.75;

    /// <summary>
    /// 锚点匹配的模板尺寸比例
    /// </summary>
    public double TemplateSizeFrac { get; init; } = 0.6;

    internal DfPixelSizeConfig ToDfPixelSizeConfig() => new()
    {
        period_y_op = PeriodYOp.ToAlgoCvPeriodOp(),
        period_x_op = PeriodXOp.ToAlgoCvPeriodOp(),
        period_zeropad = PeriodZeropad,
        period_top_n = PeriodTopN,
        period_y_min_px = PeriodYMinPx,
        period_x_min_px = PeriodXMinPx,
        period_y_min_dist = PeriodYMinDist,
        period_x_min_dist = PeriodXMinDist,
        period_y_min_cycles = PeriodYMinCycles,
        period_x_min_cycles = PeriodXMinCycles,
        mid_frac = MidFrac,
        detect_min_frac = DetectMinFrac,
        detect_max_frac = DetectMaxFrac,
        detect_need_frac = DetectNeedFrac,
        detect_inset_k = DetectInsetK,
        anchor_median_frac = AnchorMedianFrac,
        anchor_gauss_frac = AnchorGaussFrac,
        anchor_lo_frac = AnchorLoFrac,
        anchor_hi_frac = AnchorHiFrac,
        anchor_nms_frac = AnchorNmsFrac,
        match_radius_px = MatchRadiusPx,
        match_score_thresh = MatchScoreThresh,
        template_size_frac = TemplateSizeFrac
    };

    public override object ToHtmlAnonymous() => new
    {
        PeriodYOp,
        PeriodXOp,
        PeriodZeropad,
        PeriodTopN,
        PeriodYMinPx,
        PeriodXMinPx,
        PeriodYMinDist,
        PeriodXMinDist,
        PeriodYMinCycles,
        PeriodXMinCycles,
        MidFrac,
        DetectMinFrac,
        DetectMaxFrac,
        DetectNeedFrac,
        DetectInsetK,
        AnchorMedianFrac,
        AnchorGaussFrac,
        AnchorLoFrac,
        AnchorHiFrac,
        AnchorNmsFrac,
        MatchRadiusPx,
        MatchScoreThresh,
        TemplateSizeFrac
    };
}

public static class YPixelSizeEngineParamExtensions
{
    extension(AlgorithmEngineTypeEnum engine)
    {
        /// <summary>
        /// 按引擎枚举构造对应 Y 像素尺寸引擎参数（引擎专属参数取默认值，不读业务层设置）
        /// </summary>
        /// <param name="standardMaskSquareSize">标准掩膜方块规格（枚举底层值即 Y 尺寸 um）</param>
        public YPixelSizeEngineParam ToYPixelSizeEngineParam(AlgorithmStandardMaskSquareSizeEnum standardMaskSquareSize) => new()
        {
            StandardMaskSquareSize = standardMaskSquareSize,
            EngineParams = engine == AlgorithmEngineTypeEnum.AlgoCSharp ? new AlgoCvYPixelSizeParams() : null
        };
    }

    extension(YPixelSizeSetting setting)
    {
        /// <summary>
        /// 按传入引擎构造对应 Y 像素尺寸引擎参数（引擎专属参数由业务层 YPixelSizeSetting 提供，错库参数由引擎入口 EngineParams.TryGet 硬失败兜底）
        /// </summary>
        /// <param name="standardMaskSquareSize">标准掩膜方块规格（枚举底层值即 Y 尺寸 um）</param>
        public YPixelSizeEngineParam ToYPixelSizeEngineParam(AlgorithmEngineTypeEnum engine, AlgorithmStandardMaskSquareSizeEnum standardMaskSquareSize) => new()
        {
            StandardMaskSquareSize = standardMaskSquareSize,
            EngineParams = engine == AlgorithmEngineTypeEnum.AlgoCSharp
                ? setting.AlgoCSharp.ToAlgoCvYPixelSizeParams()
                : null
        };
    }
}

/// <summary>
/// 业务层设置 → 引擎参数转换（全字段拷贝，仿 TemplateMatchSettingExtensions.ToEngineParams 模式）
/// </summary>
public static class AlgoCSharpYPixelSizeSettingExtensions
{
    internal static AlgoCvYPixelSizeParams ToAlgoCvYPixelSizeParams(this AlgoCSharpYPixelSizeSetting setting) => new()
    {
        PeriodYOp = setting.PeriodYOp,
        PeriodXOp = setting.PeriodXOp,
        PeriodZeropad = setting.PeriodZeropad,
        PeriodTopN = setting.PeriodTopN,
        PeriodYMinPx = setting.PeriodYMinPx,
        PeriodXMinPx = setting.PeriodXMinPx,
        PeriodYMinDist = setting.PeriodYMinDist,
        PeriodXMinDist = setting.PeriodXMinDist,
        PeriodYMinCycles = setting.PeriodYMinCycles,
        PeriodXMinCycles = setting.PeriodXMinCycles,
        MidFrac = setting.MidFrac,
        DetectMinFrac = setting.DetectMinFrac,
        DetectMaxFrac = setting.DetectMaxFrac,
        DetectNeedFrac = setting.DetectNeedFrac,
        DetectInsetK = setting.DetectInsetK,
        AnchorMedianFrac = setting.AnchorMedianFrac,
        AnchorGaussFrac = setting.AnchorGaussFrac,
        AnchorLoFrac = setting.AnchorLoFrac,
        AnchorHiFrac = setting.AnchorHiFrac,
        AnchorNmsFrac = setting.AnchorNmsFrac,
        MatchRadiusPx = setting.MatchRadiusPx,
        MatchScoreThresh = setting.MatchScoreThresh,
        TemplateSizeFrac = setting.TemplateSizeFrac
    };
}