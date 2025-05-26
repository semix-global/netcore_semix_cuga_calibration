using System.Windows;
using System.Windows.Controls;

namespace WPF.CustomControl.UI.DepartmentTreeView;

public class DepartmentTreeView : TreeView
{
    public static readonly DependencyProperty IsCheckBoxEnabledProperty = DependencyProperty.Register(
        nameof(IsCheckBoxEnabled),
        typeof(int),
        typeof(DepartmentTreeViewItem),
        new PropertyMetadata(0, OnMyPropertyChanged)
    );

    public int IsCheckBoxEnabled
    {
        get => (int)GetValue(IsCheckBoxEnabledProperty);
        set => SetValue(IsCheckBoxEnabledProperty, value);
    }

    static DepartmentTreeView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DepartmentTreeView), new FrameworkPropertyMetadata(typeof(DepartmentTreeView)));
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new DepartmentTreeViewItem();
    }

    private static void OnMyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }
}