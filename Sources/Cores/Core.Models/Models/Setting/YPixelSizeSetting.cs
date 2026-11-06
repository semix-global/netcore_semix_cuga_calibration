using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Models.Models.Setting;

/// <summary>
/// Y 像素尺寸设置聚合（仿 TemplateMatchSetting 范式）。
/// 算法库由流程 Cache（AlgorithmEngineTypeEnum）决定，此处不冗余存储；
/// 各算法库专属参数下沉到各自子对象，互不污染、便于扩展新库。
/// </summary>
public sealed partial class YPixelSizeSetting : ObservableObject
{
    /// <summary>AlgoCSharp 算法库专属参数</summary>
    public AlgoCSharpYPixelSizeSetting AlgoCSharp { get; set; } = new();

    /// <summary>Halcon 算法库当前无可开放参数，待后续引擎扩展时增设子对象</summary>

    public YPixelSizeSetting() => Restore(true);

    /// <summary>按属性声明顺序写回默认值；isSilent 为 false 时弹 Restored Ok</summary>
    [RelayCommand]
    private void Restore(bool isSilent)
    {
        AlgoCSharp.Restore();

        if (isSilent) return;
        HostApplication.GetRequiredService<IDialogWindowProvider>().ShowDialog("Restored Ok!");
    }

    public YPixelSizeSetting Clone() => new()
    {
        AlgoCSharp = AlgoCSharp.Clone()
    };
}

/// <summary>AlgoCSharp 算法库专属 Y 像素尺寸参数（字段与 AlgoCvYPixelSizeParams 一一对应，默认值同步）。</summary>
public sealed partial class AlgoCSharpYPixelSizeSetting : ObservableObject
{
    /// <summary>Y 向投影算子（每行统计），用于估 Ty</summary>
    [ObservableProperty]
    public partial AlgorithmPeriodOpEnum PeriodYOp { get; set; }

    /// <summary>X 向投影算子（每列统计），用于估 Tx</summary>
    [ObservableProperty]
    public partial AlgorithmPeriodOpEnum PeriodXOp { get; set; }

    /// <summary>投影谱 FFT 零填充长度（像素，0 为自动）</summary>
    [ObservableProperty]
    public partial int PeriodZeropad { get; set; }

    /// <summary>投影谱中挑前 n 个峰的个数上限</summary>
    [ObservableProperty]
    public partial int PeriodTopN { get; set; }

    /// <summary>Y 向单周期最少像素（Ty 下界）</summary>
    [ObservableProperty]
    public partial double PeriodYMinPx { get; set; }

    /// <summary>X 向单周期最少像素（Tx 下界）</summary>
    [ObservableProperty]
    public partial double PeriodXMinPx { get; set; }

    /// <summary>Y 向频域峰值间最小距离（抑制谱峰扎堆）</summary>
    [ObservableProperty]
    public partial int PeriodYMinDist { get; set; }

    /// <summary>X 向频域峰值间最小距离（抑制谱峰扎堆）</summary>
    [ObservableProperty]
    public partial int PeriodXMinDist { get; set; }

    /// <summary>Y 向至少出现的周期数（Ty 上界）</summary>
    [ObservableProperty]
    public partial double PeriodYMinCycles { get; set; }

    /// <summary>X 向至少出现的周期数（Tx 上界）</summary>
    [ObservableProperty]
    public partial double PeriodXMinCycles { get; set; }

    /// <summary>取中部行的比例（0~1，图像边缘噪声大）</summary>
    [ObservableProperty]
    public partial double MidFrac { get; set; }

    /// <summary>行亮斑与中值的最小比值</summary>
    [ObservableProperty]
    public partial double DetectMinFrac { get; set; }

    /// <summary>行亮斑与中值的最大比值</summary>
    [ObservableProperty]
    public partial double DetectMaxFrac { get; set; }

    /// <summary>行内满足亮斑判定的比例</summary>
    [ObservableProperty]
    public partial double DetectNeedFrac { get; set; }

    /// <summary>检测行时的外扩内缩系数</summary>
    [ObservableProperty]
    public partial double DetectInsetK { get; set; }

    /// <summary>锚点用中值滤波的窗口比例</summary>
    [ObservableProperty]
    public partial double AnchorMedianFrac { get; set; }

    /// <summary>锚点用高斯平滑的窗口比例</summary>
    [ObservableProperty]
    public partial double AnchorGaussFrac { get; set; }

    /// <summary>锚点与中值的最小比值</summary>
    [ObservableProperty]
    public partial double AnchorLoFrac { get; set; }

    /// <summary>锚点与中值的最大比值</summary>
    [ObservableProperty]
    public partial double AnchorHiFrac { get; set; }

    /// <summary>锚点非极大值抑制的窗口比例</summary>
    [ObservableProperty]
    public partial double AnchorNmsFrac { get; set; }

    /// <summary>同行配对时允许的半径（像素）</summary>
    [ObservableProperty]
    public partial int MatchRadiusPx { get; set; }

    /// <summary>同行配对的得分阈值</summary>
    [ObservableProperty]
    public partial double MatchScoreThresh { get; set; }

    /// <summary>锚点匹配的模板尺寸比例</summary>
    [ObservableProperty]
    public partial double TemplateSizeFrac { get; set; }

    public AlgoCSharpYPixelSizeSetting() => Restore();

    /// <summary>按属性声明顺序写回默认值</summary>
    public void Restore()
    {
        PeriodYOp = AlgorithmPeriodOpEnum.Mean;
        PeriodXOp = AlgorithmPeriodOpEnum.Mean;
        PeriodZeropad = 16;
        PeriodTopN = 4;
        PeriodYMinPx = 5d;
        PeriodXMinPx = 5d;
        PeriodYMinDist = 3;
        PeriodXMinDist = 3;
        PeriodYMinCycles = 6d;
        PeriodXMinCycles = 6d;
        MidFrac = 0.5;
        DetectMinFrac = 0.8;
        DetectMaxFrac = 1.5;
        DetectNeedFrac = 0.5;
        DetectInsetK = 1.5;
        AnchorMedianFrac = 0.05;
        AnchorGaussFrac = 0.035;
        AnchorLoFrac = 0.4;
        AnchorHiFrac = 1.8;
        AnchorNmsFrac = 0.25;
        MatchRadiusPx = 10;
        MatchScoreThresh = 0.75;
        TemplateSizeFrac = 0.6;
    }

    public AlgoCSharpYPixelSizeSetting Clone() => new()
    {
        PeriodYOp = PeriodYOp,
        PeriodXOp = PeriodXOp,
        PeriodZeropad = PeriodZeropad,
        PeriodTopN = PeriodTopN,
        PeriodYMinPx = PeriodYMinPx,
        PeriodXMinPx = PeriodXMinPx,
        PeriodYMinDist = PeriodYMinDist,
        PeriodXMinDist = PeriodXMinDist,
        PeriodYMinCycles = PeriodYMinCycles,
        PeriodXMinCycles = PeriodXMinCycles,
        MidFrac = MidFrac,
        DetectMinFrac = DetectMinFrac,
        DetectMaxFrac = DetectMaxFrac,
        DetectNeedFrac = DetectNeedFrac,
        DetectInsetK = DetectInsetK,
        AnchorMedianFrac = AnchorMedianFrac,
        AnchorGaussFrac = AnchorGaussFrac,
        AnchorLoFrac = AnchorLoFrac,
        AnchorHiFrac = AnchorHiFrac,
        AnchorNmsFrac = AnchorNmsFrac,
        MatchRadiusPx = MatchRadiusPx,
        MatchScoreThresh = MatchScoreThresh,
        TemplateSizeFrac = TemplateSizeFrac
    };
}