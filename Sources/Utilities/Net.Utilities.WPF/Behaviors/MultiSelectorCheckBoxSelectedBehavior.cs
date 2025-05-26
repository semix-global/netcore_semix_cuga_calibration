using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.Behaviors;

public sealed class MultiSelectorCheckBoxSelectedBehavior : Behavior<CheckBox>
{
    #region 依赖属性

    public IList BindableSelectedItems
    {
        get => (IList)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    public static readonly DependencyProperty BindableSelectedItemsProperty =
        DependencyProperty.Register("BindableSelectedItems", typeof(IList), typeof(MultiSelectorCheckBoxSelectedBehavior), new PropertyMetadata(new List<(string, string)>()));

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
    }
}