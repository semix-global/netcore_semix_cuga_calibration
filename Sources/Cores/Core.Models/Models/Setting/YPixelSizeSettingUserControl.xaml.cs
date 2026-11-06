using Net.Utilities.Calibration;
using System.Windows;
using System.Windows.Controls;

namespace Core.Models.Models.Setting;

/// <summary>
/// Y 像素尺寸设置控件（仿 TemplateMatchSettingUserControl 范式）。
/// 由外部绑定设置对象（DataContext = YPixelSizeSetting）与流程引擎枚举（Engine），
/// 控件内部按算法库决定显示各库专属参数，不包含算法库判断逻辑在 ViewModel。
/// </summary>
public partial class YPixelSizeSettingUserControl : UserControl
{
    public static readonly DependencyProperty EngineProperty = DependencyProperty.Register(
        nameof(Engine),
        typeof(AlgorithmEngineTypeEnum),
        typeof(YPixelSizeSettingUserControl),
        new PropertyMetadata(AlgorithmEngineTypeEnum.HAlgorithm));

    public YPixelSizeSettingUserControl()
    {
        InitializeComponent();
    }

    /// <summary>流程算法库，决定显示哪些参数。</summary>
    public AlgorithmEngineTypeEnum Engine
    {
        get => (AlgorithmEngineTypeEnum)GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }
}
