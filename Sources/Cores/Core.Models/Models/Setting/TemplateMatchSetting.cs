using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Net.Utilities.Calibration;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Models.Models.Setting;

/// <summary>
/// 模板匹配设置聚合。
/// 算法库由全局 Setting（<see cref="AlgorithmEngineTypeEnum"/>）决定，此处不冗余存储；
/// 公共部分（模板尺寸）平铺，各算法库专属参数下沉到各自子对象，互不污染、便于扩展新库。
/// </summary>
public sealed partial class TemplateMatchSetting : ObservableObject
{
    /// <summary>模板尺寸（决定生成模板时的 ROI 大小），两算法库通用</summary>
    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum TemplateSize { get; set; }

    /// <summary>Halcon 算法库专属参数</summary>
    public HalconTemplateMatchSetting Halcon { get; set; } = new();

    /// <summary>AlgoCSharp 算法库专属参数</summary>
    public AlgoCSharpTemplateMatchSetting AlgoCSharp { get; set; } = new();

    public TemplateMatchSetting() => Restore(true);

    /// <summary>按属性声明顺序写回默认值；isSilent 为 false 时弹 Restored Ok</summary>
    [RelayCommand]
    private void Restore(bool isSilent)
    {
        TemplateSize = AlgorithmTemplateSizeEnum.Size256;
        Halcon.Restore();
        AlgoCSharp.Restore();

        if (isSilent) return;
        HostApplication.GetRequiredService<IDialogWindowProvider>().ShowDialog("Restored Ok!");
    }

    public TemplateMatchSetting Clone() => new()
    {
        TemplateSize = TemplateSize,
        Halcon = Halcon.Clone(),
        AlgoCSharp = AlgoCSharp.Clone()
    };
}

/// <summary>Halcon 算法库专属模板匹配参数。</summary>
public sealed partial class HalconTemplateMatchSetting : ObservableObject
{
    /// <summary>Halcon 模板类型</summary>
    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum TemplateType { get; set; }

    public HalconTemplateMatchSetting() => Restore();

    /// <summary>按属性声明顺序写回默认值</summary>
    public void Restore() => TemplateType = AlgorithmTemplateTypeEnum.Ncc;

    public HalconTemplateMatchSetting Clone() => new() { TemplateType = TemplateType };
}

/// <summary>AlgoCSharp 算法库专属模板匹配参数。</summary>
public sealed partial class AlgoCSharpTemplateMatchSetting : ObservableObject
{
    /// <summary>AlgoCSharp 匹配方法</summary>
    [ObservableProperty]
    public partial TemplateMatchMethodEnum Method { get; set; } = TemplateMatchMethodEnum.TmCcoeffNormed;

    /// <summary>AlgoCSharp 金字塔层级</summary>
    [ObservableProperty]
    public partial int PyramidLevels { get; set; } = 3;

    public AlgoCSharpTemplateMatchSetting() => Restore();

    /// <summary>按属性声明顺序写回默认值</summary>
    public void Restore()
    {
        Method = TemplateMatchMethodEnum.TmCcoeffNormed;
        PyramidLevels = 3;
    }

    public AlgoCSharpTemplateMatchSetting Clone() => new()
    {
        Method = Method,
        PyramidLevels = PyramidLevels
    };
}