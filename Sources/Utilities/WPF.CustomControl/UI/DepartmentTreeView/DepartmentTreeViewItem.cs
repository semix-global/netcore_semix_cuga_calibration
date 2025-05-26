using Local.SQL.DB.Providers.Models.Entities.DTO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPF.CustomControl.UI.DepartmentTreeView;

public class DepartmentTreeViewItem : TreeViewItem
{
    public static readonly DependencyProperty SelectionCommandProperty = DependencyProperty.Register(
        nameof(SelectionCommand),
        typeof(ICommand),
        typeof(DepartmentTreeViewItem),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty SettingCommandProperty = DependencyProperty.Register(
        nameof(SettingCommand),
        typeof(ICommand),
        typeof(DepartmentTreeViewItem),
        new PropertyMetadata(null)
    );

    static DepartmentTreeViewItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DepartmentTreeViewItem), new FrameworkPropertyMetadata(typeof(DepartmentTreeViewItem)));
    }

    public ICommand? SelectionCommand
    {
        get => (ICommand?)GetValue(SelectionCommandProperty);
        set => SetValue(SelectionCommandProperty, value);
    }

    public ICommand? SettingCommand
    {
        get => (ICommand?)GetValue(SettingCommandProperty);
        set => SetValue(SettingCommandProperty, value);
    }

    public DepartmentTreeViewItem()
    {
        MouseDoubleClick += DepartmentTreeViewItem_MouseDoubleClick;
        MouseLeftButtonUp += DepartmentTreeViewItem_MouseLeftButtonUp;
    }

    private void DepartmentTreeViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (DataContext is SysBaseDto item)
        {
            SelectionCommand?.Execute(item);
        }
    }

    private void DepartmentTreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (DataContext is SysBaseDto item)
        {
            SettingCommand?.Execute(item);
        }
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new DepartmentTreeViewItem();
    }
}