using Core.Models.Enums.Algorithm;
using System.Windows;
using System.Windows.Controls;

namespace Core.Models.Models.Setting;

/// <summary>
/// 模板匹配设置控件。由外部绑定模板设置对象（DataContext = TemplateMatchSetting）与全局算法库（Engine），
/// 控件内部按算法库决定显示 Halcon / AlgoCSharp 各自的参数，不包含算法库判断逻辑在 ViewModel。
/// </summary>
public partial class TemplateMatchSettingUserControl : UserControl
{
    public static readonly DependencyProperty EngineProperty = DependencyProperty.Register(
        nameof(Engine),
        typeof(AlgorithmEngineTypeEnum),
        typeof(TemplateMatchSettingUserControl),
        new PropertyMetadata(AlgorithmEngineTypeEnum.HAlgorithm));

    public TemplateMatchSettingUserControl()
    {
        InitializeComponent();
    }

    /// <summary>全局算法库，决定显示哪些参数。</summary>
    public AlgorithmEngineTypeEnum Engine
    {
        get => (AlgorithmEngineTypeEnum)GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }
}