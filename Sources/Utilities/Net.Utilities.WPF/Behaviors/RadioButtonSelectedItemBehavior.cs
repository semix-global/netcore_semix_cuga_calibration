using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Net.Utilities.WPF.Behaviors;

/// <summary>
/// todo: 只能用于固定的容器，ItemsControl如果变更了ItemsSource，需要重新加载[不推荐，频繁更新视图]
/// 允许<see cref="Panel"/>或<see cref="ItemsControl"/>具有类似<see cref="ComboBox"/>的行为
/// 具有要绑定到视图模型中的属性的<see cref="SelectedItem"/>依赖属性.
/// 容器里面的<see cref="RadioButton"/>不可变
/// </summary>
/// <example>
/// <code lang="xaml">
/// <![CDATA[
/// <StackPanel>
///     <i:Interaction.Behaviors>
///         <be:SelectedItemBehavior SelectedItem="{Binding SelectedItem}" />
///     </i:Interaction.Behaviors>
///     <RadioButton Tag="A" />
///     <RadioButton Tag="B" />
///     <RadioButton Tag="C" />
/// </StackPanel>
/// ]]>
/// </code>
/// </example>
public sealed class RadioButtonSelectedItemBehavior : Behavior<FrameworkElement>
{
    #region 依赖属性

    public required object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// 此<see cref="Panel"/>或<see cref="ItemsControl"/>中的<see cref="SelectedItem"/>
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem),
        typeof(object),
        typeof(RadioButtonSelectedItemBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChangedCallback)
    );

    private static void OnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // var behavior = Interaction.GetBehaviors(d).OfType<RadioButtonSelectedItemBehavior>().FirstOrDefault();
        // if (behavior is null) return;
        if (d is not RadioButtonSelectedItemBehavior behavior) return;
        foreach (var child in behavior.Children)
        {
            child.IsChecked = child.Tag.Equals(e.NewValue);
        }
    }

    #endregion 依赖属性

    /// <summary>
    /// 枚举<see cref="Behavior{T}.AssociatedObject"/>的所有<see cref="RadioButton"/>子级
    /// </summary>
    private IEnumerable<RadioButton> Children => AssociatedObject switch
    {
        Panel panel => panel.Children.OfType<RadioButton>(),
        ItemsControl control => Helper.DependencyObjectHelper.FindVisualDescendants<RadioButton>(control),
        _ => []
    };

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Loaded -= OnLoaded;
        foreach (var child in Children)
        {
            child.Checked -= OnRadioButtonChecked;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 只能用于固定的容器
        foreach (var child in Children)
        {
            child.Checked -= OnRadioButtonChecked;
            child.Checked += OnRadioButtonChecked;
            child.IsChecked = child.Tag.Equals(SelectedItem);
        }
    }

    private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
    {
        var radio = sender as RadioButton;
        SelectedItem = radio!.Tag;
        BindingOperations.GetBindingExpression(this, SelectedItemProperty)!.UpdateSource();
    }
}