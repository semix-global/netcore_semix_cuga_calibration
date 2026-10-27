using System.Diagnostics.CodeAnalysis;
using algocv_sharp;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Services.Interfaces;

public partial interface ICalibrationAlgorithmService
{
    /// <summary>
    /// 分析行扫原图, 转置格点矩阵并按扫描方向统一 X 像素坐标, 使用各方向全部有效格点整体拟合标准格点间距.
    /// </summary>
    /// <param name="rawScanImage">包含原图路径和尺寸的行扫图.</param>
    /// <param name="stageMapConfig">可绑定的格点识别配置.</param>
    /// <param name="isForward">是否正向扫描, 反向扫描时按原图扫描宽度翻转全部 X 像素坐标.</param>
    /// <param name="htmlLogUniqueId">拟合曲线, 参数和算法耗时对应的 HTML 日志标识.</param>
    /// <returns>按矩阵列顺序返回的标准 X 像素坐标与居中误差向量. 各方向仅使用有限值拟合和计算整体误差均值, 居中后将缺失误差补零并计算列均值.</returns>
    /// <exception cref="ArgumentException">有效格点不足或拟合格点间距无效.</exception>
    RTAScanSample[] GetRTAScanSamples(
        DarkFieldRawScanImageDTO rawScanImage,
        StageMapConfigDTO stageMapConfig,
        bool isForward,
        Guid htmlLogUniqueId);

    /// <summary>
    /// 可绑定的格点识别配置, 未显式覆盖的参数采用算法引擎默认值.
    /// </summary>
    public sealed partial class StageMapConfigDTO : ObservableObject, ICloneable<StageMapConfigDTO>, IAdaptTo<StageMapConfig>
    {
        /// <summary>
        /// 中值滤波核大小: 周期窗/每块条带/首格条带共用.
        /// </summary>
        [ObservableProperty]
        public partial int MedianKernelSize { get; set; }

        /// <summary>
        /// 周期分析窗起始行(全图行号).
        /// </summary>
        [ObservableProperty]
        public partial int PeriodRowStart { get; set; }

        /// <summary>
        /// 周期分析窗行数.
        /// </summary>
        [ObservableProperty]
        public partial int PeriodRowCount { get; set; }

        /// <summary>
        /// y 向投影算子(每行统计): "mean"/"std"/"range".
        /// </summary>
        [ObservableProperty]
        public partial string PeriodYOperator { get; set; }

        /// <summary>
        /// x 向投影算子(每列统计): "mean"/"std"/"range".
        /// </summary>
        [ObservableProperty]
        public partial string PeriodXOperator { get; set; }

        /// <summary>
        /// 频谱零填充倍数.
        /// </summary>
        [ObservableProperty]
        public partial int PeriodZeroPadding { get; set; }

        /// <summary>
        /// 每向返回的周期候选数上限.
        /// </summary>
        [ObservableProperty]
        public partial int PeriodTopN { get; set; }

        /// <summary>
        /// y 向每周期最少行数(短端界).
        /// </summary>
        [ObservableProperty]
        public partial double PeriodYMinPixels { get; set; }

        /// <summary>
        /// x 向每周期最少列数(短端界).
        /// </summary>
        [ObservableProperty]
        public partial double PeriodXMinPixels { get; set; }

        /// <summary>
        /// y 向窗内最少周期数(长端界).
        /// </summary>
        [ObservableProperty]
        public partial double PeriodYMinCycles { get; set; }

        /// <summary>
        /// x 向窗内最少周期数(长端界).
        /// </summary>
        [ObservableProperty]
        public partial double PeriodXMinCycles { get; set; }

        /// <summary>
        /// y 向谱峰抑制半径(谱索引).
        /// </summary>
        [ObservableProperty]
        public partial int PeriodYMinDistance { get; set; }

        /// <summary>
        /// x 向谱峰抑制半径(谱索引).
        /// </summary>
        [ObservableProperty]
        public partial int PeriodXMinDistance { get; set; }

        /// <summary>
        /// 头部内容检测窗行数.
        /// </summary>
        [ObservableProperty]
        public partial int DetectRowLimit { get; set; }

        /// <summary>
        /// 尾部内容检测窗行数.
        /// </summary>
        [ObservableProperty]
        public partial int DetectTailLimit { get; set; }

        /// <summary>
        /// 振荡带内下限系数.
        /// </summary>
        [ObservableProperty]
        public partial double DetectMinFraction { get; set; }

        /// <summary>
        /// 振荡带内上限系数.
        /// </summary>
        [ObservableProperty]
        public partial double DetectMaxFraction { get; set; }

        /// <summary>
        /// 连续带外判定系数(× T_y).
        /// </summary>
        [ObservableProperty]
        public partial double DetectNeedFraction { get; set; }

        /// <summary>
        /// 投影平滑核长系数(× T, 奇数化).
        /// </summary>
        [ObservableProperty]
        public partial double BandSmoothFraction { get; set; }

        /// <summary>
        /// 亮带搜索半窗系数(× T).
        /// </summary>
        [ObservableProperty]
        public partial double BandWindowFraction { get; set; }

        /// <summary>
        /// 边缘伪峰剔除系数(× T).
        /// </summary>
        [ObservableProperty]
        public partial double BandEdgeFraction { get; set; }

        /// <summary>
        /// 模板尺寸系数(× T).
        /// </summary>
        [ObservableProperty]
        public partial double TemplateFraction { get; set; }

        /// <summary>
        /// 锚点行亮带下标(0 起).
        /// </summary>
        [ObservableProperty]
        public partial int TemplateAnchorRow { get; set; }

        /// <summary>
        /// 锚点列亮带下标(0 起).
        /// </summary>
        [ObservableProperty]
        public partial int TemplateAnchorColumn { get; set; }

        /// <summary>
        /// 局部搜索余量 ±px.
        /// </summary>
        [ObservableProperty]
        public partial int MatchMargin { get; set; }

        /// <summary>
        /// NCC 分数阈值.
        /// </summary>
        [ObservableProperty]
        public partial double MatchThreshold { get; set; }

        /// <summary>
        /// 换模板阈值(末 3 行均分低于此值即换).
        /// </summary>
        [ObservableProperty]
        public partial double MatchRefreshThreshold { get; set; }

        /// <summary>
        /// 拟合残差容差(px).
        /// </summary>
        [ObservableProperty]
        public partial double FitJitter { get; set; }

        /// <summary>
        /// 拟合内点比例下限.
        /// </summary>
        [ObservableProperty]
        public partial double FitMatchRatio { get; set; }

        /// <summary>
        /// 换模板 delta 的 trimmed mean 截尾比例.
        /// </summary>
        [ObservableProperty]
        public partial double DeltaTrim { get; set; }

        /// <summary>
        /// 首格定位搜索行数.
        /// </summary>
        [ObservableProperty]
        public partial int OriginSearchRows { get; set; }

        /// <summary>
        /// 流式分块高度(行).
        /// </summary>
        [ObservableProperty]
        public partial int StreamBlockRows { get; set; }

        public StageMapConfigDTO() => Restore(true);

        [RelayCommand]
        [MemberNotNull(nameof(PeriodYOperator), nameof(PeriodXOperator))]
        private void Restore(bool isSilent)
        {
            var config = new StageMapConfig
            {
                period_row_start = 1000,
                period_row_count = 2000,
                period_y_min_cycles = 5d,
                period_x_min_cycles = 2d,
                detect_row_limit = 30000,
                detect_tail_limit = 30000
            };

            MedianKernelSize = config.median_ksize;
            PeriodRowStart = config.period_row_start;
            PeriodRowCount = config.period_row_count;
            PeriodYOperator = config.period_y_op;
            PeriodXOperator = config.period_x_op;
            PeriodZeroPadding = config.period_zeropad;
            PeriodTopN = config.period_top_n;
            PeriodYMinPixels = config.period_y_min_px;
            PeriodXMinPixels = config.period_x_min_px;
            PeriodYMinCycles = config.period_y_min_cycles;
            PeriodXMinCycles = config.period_x_min_cycles;
            PeriodYMinDistance = config.period_y_min_dist;
            PeriodXMinDistance = config.period_x_min_dist;
            DetectRowLimit = config.detect_row_limit;
            DetectTailLimit = config.detect_tail_limit;
            DetectMinFraction = config.detect_min_frac;
            DetectMaxFraction = config.detect_max_frac;
            DetectNeedFraction = config.detect_need_frac;
            BandSmoothFraction = config.band_smooth_frac;
            BandWindowFraction = config.band_win_frac;
            BandEdgeFraction = config.band_edge_frac;
            TemplateFraction = config.tpl_frac;
            TemplateAnchorRow = config.tpl_anchor_row;
            TemplateAnchorColumn = config.tpl_anchor_col;
            MatchMargin = config.match_margin;
            MatchThreshold = config.match_thr;
            MatchRefreshThreshold = config.match_refresh_thr;
            FitJitter = config.fit_jitter;
            FitMatchRatio = config.fit_match_ratio;
            DeltaTrim = config.delta_trim;
            OriginSearchRows = config.origin_search_rows;
            StreamBlockRows = config.stream_block_rows;

            if (isSilent) return;

            HostApplication.GetRequiredService<IDialogWindowProvider>().ShowDialog("Restored Ok!");
        }

        public StageMapConfigDTO Clone() => new()
        {
            MedianKernelSize = MedianKernelSize,
            PeriodRowStart = PeriodRowStart,
            PeriodRowCount = PeriodRowCount,
            PeriodYOperator = PeriodYOperator,
            PeriodXOperator = PeriodXOperator,
            PeriodZeroPadding = PeriodZeroPadding,
            PeriodTopN = PeriodTopN,
            PeriodYMinPixels = PeriodYMinPixels,
            PeriodXMinPixels = PeriodXMinPixels,
            PeriodYMinCycles = PeriodYMinCycles,
            PeriodXMinCycles = PeriodXMinCycles,
            PeriodYMinDistance = PeriodYMinDistance,
            PeriodXMinDistance = PeriodXMinDistance,
            DetectRowLimit = DetectRowLimit,
            DetectTailLimit = DetectTailLimit,
            DetectMinFraction = DetectMinFraction,
            DetectMaxFraction = DetectMaxFraction,
            DetectNeedFraction = DetectNeedFraction,
            BandSmoothFraction = BandSmoothFraction,
            BandWindowFraction = BandWindowFraction,
            BandEdgeFraction = BandEdgeFraction,
            TemplateFraction = TemplateFraction,
            TemplateAnchorRow = TemplateAnchorRow,
            TemplateAnchorColumn = TemplateAnchorColumn,
            MatchMargin = MatchMargin,
            MatchThreshold = MatchThreshold,
            MatchRefreshThreshold = MatchRefreshThreshold,
            FitJitter = FitJitter,
            FitMatchRatio = FitMatchRatio,
            DeltaTrim = DeltaTrim,
            OriginSearchRows = OriginSearchRows,
            StreamBlockRows = StreamBlockRows
        };

        public StageMapConfig AdaptTo() => new()
        {
            median_ksize = MedianKernelSize,
            period_row_start = PeriodRowStart,
            period_row_count = PeriodRowCount,
            period_y_op = PeriodYOperator,
            period_x_op = PeriodXOperator,
            period_zeropad = PeriodZeroPadding,
            period_top_n = PeriodTopN,
            period_y_min_px = PeriodYMinPixels,
            period_x_min_px = PeriodXMinPixels,
            period_y_min_cycles = PeriodYMinCycles,
            period_x_min_cycles = PeriodXMinCycles,
            period_y_min_dist = PeriodYMinDistance,
            period_x_min_dist = PeriodXMinDistance,
            detect_row_limit = DetectRowLimit,
            detect_tail_limit = DetectTailLimit,
            detect_min_frac = DetectMinFraction,
            detect_max_frac = DetectMaxFraction,
            detect_need_frac = DetectNeedFraction,
            band_smooth_frac = BandSmoothFraction,
            band_win_frac = BandWindowFraction,
            band_edge_frac = BandEdgeFraction,
            tpl_frac = TemplateFraction,
            tpl_anchor_row = TemplateAnchorRow,
            tpl_anchor_col = TemplateAnchorColumn,
            match_margin = MatchMargin,
            match_thr = MatchThreshold,
            match_refresh_thr = MatchRefreshThreshold,
            fit_jitter = FitJitter,
            fit_match_ratio = FitMatchRatio,
            delta_trim = DeltaTrim,
            origin_search_rows = OriginSearchRows,
            stream_block_rows = StreamBlockRows
        };

        public object ToHtmlAnonymous() => new
        {
            MedianKernelSize,
            PeriodRowStart,
            PeriodRowCount,
            PeriodYOperator,
            PeriodXOperator,
            PeriodZeroPadding,
            PeriodTopN,
            PeriodYMinPixels,
            PeriodXMinPixels,
            PeriodYMinCycles,
            PeriodXMinCycles,
            PeriodYMinDistance,
            PeriodXMinDistance,
            DetectRowLimit,
            DetectTailLimit,
            DetectMinFraction,
            DetectMaxFraction,
            DetectNeedFraction,
            BandSmoothFraction,
            BandWindowFraction,
            BandEdgeFraction,
            TemplateFraction,
            TemplateAnchorRow,
            TemplateAnchorColumn,
            MatchMargin,
            MatchThreshold,
            MatchRefreshThreshold,
            FitJitter,
            FitMatchRatio,
            DeltaTrim,
            OriginSearchRows,
            StreamBlockRows
        };
    }

    /// <summary>
    /// RTAScanSample 结构体, 包含标准 X 像素坐标, 扫描时间和居中误差向量.
    /// </summary>
    /// <param name="X">标准位置px</param>
    /// <param name="Error">误差px</param>
    public readonly record struct RTAScanSample(double X, Vector Error);
}