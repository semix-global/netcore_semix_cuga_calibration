using System.Windows;
using System.Windows.Controls;

namespace WPF.CustomControl.UI.PermissionMenuTreeView;

public class PermissionMenuTreeViewItem : TreeViewItem
{
    static PermissionMenuTreeViewItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PermissionMenuTreeViewItem), new FrameworkPropertyMetadata(typeof(PermissionMenuTreeViewItem)));
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new PermissionMenuTreeViewItem();
    }
}