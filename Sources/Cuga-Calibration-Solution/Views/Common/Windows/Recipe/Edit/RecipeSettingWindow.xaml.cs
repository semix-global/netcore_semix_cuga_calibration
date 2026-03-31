using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System.Windows.Controls;

namespace CugaCalibration.Views.Common.Windows.Recipe.Edit;

[IOCAppService(ServiceType = typeof(RecipeSettingWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class RecipeSettingWindow
{
    public RecipeSettingWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 阻止内层 Alignment TabControl 的 SelectionChanged 事件冒泡到外层 TabControl，
    /// 避免触发外层的 ParamTypeChangedCommand 并污染 EditRecipeTypeName。
    /// </summary>
    private void AlignmentTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender)) return;
        e.Handled = true;
    }
}