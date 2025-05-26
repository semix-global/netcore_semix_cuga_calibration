using System.Windows.Controls;

namespace Net.Utilities.WPF.Helper;

public static class TreeViewHelper
{
    public static void ExpandAllTreeView(TreeView item)
    {
        foreach (var subItem in item.Items) ExpandAllTreeViewItems((TreeViewItem)item.ItemContainerGenerator.ContainerFromItem(subItem));
    }

    public static void ExpandAllTreeViewItems(TreeViewItem item)
    {
        item.IsExpanded = true;

        foreach (var subItem in item.Items) ExpandAllTreeViewItems((TreeViewItem)item.ItemContainerGenerator.ContainerFromItem(subItem));
    }
}