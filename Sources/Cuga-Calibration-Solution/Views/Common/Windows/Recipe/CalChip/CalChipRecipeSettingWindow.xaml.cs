using CugaCalibration.ViewModels.Common.Windows.Recipe.CalChip;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System.Windows.Controls;

namespace CugaCalibration.Views.Common.Windows.Recipe.CalChip;

[IOCAppService(ServiceType = typeof(CalChipRecipeSettingWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class CalChipRecipeSettingWindow
{
    public CalChipRecipeSettingWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 阻止内层 Alignment TabControl 的 SelectionChanged 事件冒泡到外层 TabControl, 并按当前 Tab 切换明暗场显示
    /// </summary>
    private void AlignmentTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, sender) == false) return;
        e.Handled = true;

        if (sender is not TabControl tabControl || tabControl.SelectedItem is not TabItem tabItem) return;

        if (DataContext is not CalChipRecipeSettingViewModel viewModel) return;
    }
}