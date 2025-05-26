using System.Windows;
using System.Windows.Controls;

namespace WPF.CustomControl.UI.PermissionMenuTreeView;

public class PermissionMenuTreeView : TreeView
{
    public static readonly DependencyProperty IsCheckBoxEnabledProperty = DependencyProperty.Register(
        nameof(IsCheckBoxEnabled),
        typeof(int),
        typeof(PermissionMenuTreeView),
        new PropertyMetadata(0, OnMyPropertyChanged)
    );

    public int IsCheckBoxEnabled
    {
        get => (int)GetValue(IsCheckBoxEnabledProperty);
        set => SetValue(IsCheckBoxEnabledProperty, value);
    }

    static PermissionMenuTreeView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PermissionMenuTreeView), new FrameworkPropertyMetadata(typeof(PermissionMenuTreeView)));
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new PermissionMenuTreeViewItem();
    }

    private static void OnMyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }
}