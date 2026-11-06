using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Models.Models.Setting;

/// <summary>
/// BF 像素尺寸设置聚合（仿 TemplateMatchSetting / YPixelSizeSetting 范式）。
/// 算法库由流程 Cache（AlgorithmEngineTypeEnum）决定，此处不冗余存储；
/// 各算法库专属参数下沉到各自子对象，互不污染、便于扩展新库。
/// </summary>
public sealed partial class PixelSizeSetting : ObservableObject
{
    /// <summary>AlgoCSharp 算法库专属参数</summary>
    public AlgoCSharpPixelSizeSetting AlgoCSharp { get; set; } = new();

    /// <summary>Halcon 算法库当前无可开放参数，待后续引擎扩展时增设子对象</summary>

    public PixelSizeSetting() => Restore(true);

    /// <summary>按属性声明顺序写回默认值；isSilent 为 false 时弹 Restored Ok</summary>
    [RelayCommand]
    private void Restore(bool isSilent)
    {
        AlgoCSharp.Restore();

        if (isSilent) return;
        HostApplication.GetRequiredService<IDialogWindowProvider>().ShowDialog("Restored Ok!");
    }

    public PixelSizeSetting Clone() => new()
    {
        AlgoCSharp = AlgoCSharp.Clone()
    };
}

/// <summary>AlgoCSharp 算法库专属 BF 像素尺寸参数（字段与 AlgoCvPixelSizeParams 一一对应，默认值对齐 algolib BfPixelSizeConfig）。</summary>
public sealed partial class AlgoCSharpPixelSizeSetting : ObservableObject
{
    /// <summary>中心条带比例（竖条宽/横条高 = round_py(尺寸·该比例)）</summary>
    [ObservableProperty]
    public partial double RoiFrac { get; set; }

    /// <summary>条带投影算子</summary>
    [ObservableProperty]
    public partial AlgorithmPeriodOpEnum PeriodOp { get; set; }

    /// <summary>投影 1D 中值滤波半径（像素）</summary>
    [ObservableProperty]
    public partial int ProjMedianR { get; set; }

    /// <summary>投影轻高斯 sigma（像素），滤波半径取 round_py(3σ)</summary>
    [ObservableProperty]
    public partial double ProjGaussSigma { get; set; }

    /// <summary>相关带半宽（像素）。对齐 algolib BfPixelSizeConfig.period_half 默认 400；0 表示按掩膜规格自动选取扫描窗</summary>
    [ObservableProperty]
    public partial int PeriodHalf { get; set; }

    /// <summary>周期扫描最短格距</summary>
    [ObservableProperty]
    public partial int PeriodNMin { get; set; }

    /// <summary>周期扫描最长格距上限。对齐 algolib BfPixelSizeConfig.period_n_max 默认 400；0 表示按掩膜规格自动选取扫描窗</summary>
    [ObservableProperty]
    public partial int PeriodNMax { get; set; }

    /// <summary>局部极大入围门槛（相对最高分）</summary>
    [ObservableProperty]
    public partial double PeriodPeakFrac { get; set; }

    /// <summary>锚点平滑中值半径 / 周期</summary>
    [ObservableProperty]
    public partial double AnchorMedianFrac { get; set; }

    /// <summary>锚点平滑高斯 sigma / 周期</summary>
    [ObservableProperty]
    public partial double AnchorGaussFrac { get; set; }

    /// <summary>走步搜索窗下界 / 周期</summary>
    [ObservableProperty]
    public partial double AnchorLoFrac { get; set; }

    /// <summary>走步搜索窗上界 / 周期</summary>
    [ObservableProperty]
    public partial double AnchorHiFrac { get; set; }

    /// <summary>非极大抑制半径 / 周期</summary>
    [ObservableProperty]
    public partial double AnchorNmsFrac { get; set; }

    /// <summary>模板边长 / 周期（再取奇数）</summary>
    [ObservableProperty]
    public partial double TemplateSizeFrac { get; set; }

    /// <summary>模板匹配搜索半径（像素）</summary>
    [ObservableProperty]
    public partial int MatchRadiusPx { get; set; }

    /// <summary>TM_CCOEFF_NORMED 得分阈值</summary>
    [ObservableProperty]
    public partial double MatchScoreThresh { get; set; }

    /// <summary>拟合残差容差(px)，超者为异常点</summary>
    [ObservableProperty]
    public partial double FitJitter { get; set; }

    public AlgoCSharpPixelSizeSetting() => Restore();

    /// <summary>按属性声明顺序写回默认值</summary>
    public void Restore()
    {
        RoiFrac = 0.5;
        PeriodOp = AlgorithmPeriodOpEnum.Mean;
        ProjMedianR = 2;
        ProjGaussSigma = 1d;
        PeriodHalf = 400;
        PeriodNMin = 40;
        PeriodNMax = 400;
        PeriodPeakFrac = 0.9;
        AnchorMedianFrac = 0.05;
        AnchorGaussFrac = 0.035;
        AnchorLoFrac = 0.4;
        AnchorHiFrac = 1.8;
        AnchorNmsFrac = 0.25;
        TemplateSizeFrac = 0.6;
        MatchRadiusPx = 10;
        MatchScoreThresh = 0.85;
        FitJitter = 3d;
    }

    public AlgoCSharpPixelSizeSetting Clone() => new()
    {
        RoiFrac = RoiFrac,
        PeriodOp = PeriodOp,
        ProjMedianR = ProjMedianR,
        ProjGaussSigma = ProjGaussSigma,
        PeriodHalf = PeriodHalf,
        PeriodNMin = PeriodNMin,
        PeriodNMax = PeriodNMax,
        PeriodPeakFrac = PeriodPeakFrac,
        AnchorMedianFrac = AnchorMedianFrac,
        AnchorGaussFrac = AnchorGaussFrac,
        AnchorLoFrac = AnchorLoFrac,
        AnchorHiFrac = AnchorHiFrac,
        AnchorNmsFrac = AnchorNmsFrac,
        TemplateSizeFrac = TemplateSizeFrac,
        MatchRadiusPx = MatchRadiusPx,
        MatchScoreThresh = MatchScoreThresh,
        FitJitter = FitJitter
    };
}