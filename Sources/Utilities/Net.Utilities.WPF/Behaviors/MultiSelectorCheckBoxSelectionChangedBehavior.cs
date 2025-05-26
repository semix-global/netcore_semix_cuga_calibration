using Microsoft.Xaml.Behaviors;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Net.Utilities.WPF.Behaviors;

public sealed class MultiSelectorCheckBoxSelectionChangedBehavior : Behavior<CheckBox>
{
    #region 依赖属性

    public List<(string, string)> BindableSelectedItems
    {
        get => (List<(string, string)>)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    // 定义可绑定的命令属性 
    public ICommand SelectionChangedCommand
    {
        get => (ICommand)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public static readonly DependencyProperty SelectionChangedCommandProperty =
        DependencyProperty.Register(nameof(SelectionChangedCommand), typeof(ICommand), typeof(MultiSelectorCheckBoxSelectionChangedBehavior));

    public static readonly DependencyProperty BindableSelectedItemsProperty =
        DependencyProperty.Register("BindableSelectedItems", typeof(List<(string, string)>), typeof(MultiSelectorCheckBoxSelectionChangedBehavior), new PropertyMetadata(new List<(string, string)>()));

    #endregion 依赖属性

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Checked += OnSelectionChanged;
        AssociatedObject.Unchecked += OnSelectionChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Checked -= OnSelectionChanged;
        AssociatedObject.Unchecked -= OnSelectionChanged;
    }

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        //if (BindableSelectedItems is null)
        //{
        //    BindableSelectedItems =new List<string>();
        //}
        if (sender is CheckBox checkbox)
        {
            if (checkbox.IsChecked == true)
            {
                if (!BindableSelectedItems.Contains((checkbox.Tag.ToString(), checkbox.Content.ToString())))
                {
                    BindableSelectedItems.Add((checkbox.Tag.ToString(), checkbox.Content.ToString()));
                }
            }
            else
            {
                if (BindableSelectedItems.Contains((checkbox.Tag.ToString(), checkbox.Content.ToString())))
                {
                    BindableSelectedItems.Remove((checkbox.Tag.ToString(), checkbox.Content.ToString()));
                }
            }
        }

        if (SelectionChangedCommand.CanExecute(new ObservableCollection<(string, string)>(BindableSelectedItems)))
        {
            SelectionChangedCommand.Execute(new ObservableCollection<(string, string)>(BindableSelectedItems));
        }
        //SelectionChangedCommand.Execute(string.Empty);
    }
}