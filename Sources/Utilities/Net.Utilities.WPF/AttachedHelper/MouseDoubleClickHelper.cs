using Net.Utilities.WPF.Helper;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Net.Utilities.WPF.AttachedHelper;

public sealed class MouseDoubleClickHelper
{
    #region 附加属性

    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(MouseDoubleClickHelper),
        new PropertyMetadata(CommandChanged)
    );

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
        "CommandParameter",
        typeof(object),
        typeof(MouseDoubleClickHelper),
        new PropertyMetadata(null)
    );

    public static ICommand? GetCommand(DependencyObject target)
    {
        return (ICommand?)target.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject target, ICommand value)
    {
        target.SetValue(CommandProperty, value);
    }

    public static object? GetCommandParameter(DependencyObject target)
    {
        return target.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(DependencyObject target, object value)
    {
        target.SetValue(CommandParameterProperty, value);
    }

    #endregion 附加属性

    private static void CommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not Control control) return;

        switch (e)
        {
            case { NewValue: not null, OldValue: null }:
                control.MouseDoubleClick -= OnMouseDoubleClick;
                control.MouseDoubleClick += OnMouseDoubleClick;
                break;

            case { NewValue: null, OldValue: not null }:
                control.MouseDoubleClick -= OnMouseDoubleClick;
                break;
        }
    }

    private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Control control) return;

        // 解决子元素响应了DoubleClick事件后, 父元素也响应了DoubleClick事件
        var clickedItem = control.InputHitTest(e.GetPosition(control)); // 获取鼠标指针命中了的元素
        if (clickedItem is not Visual dependencyObject) return;

        var findVisualAncestor = DependencyObjectHelper.FindVisualAncestor(dependencyObject, sender.GetType()); // 命中元素sender.GetType()相同的第一个元素 == sender
        if (findVisualAncestor != sender) return;

        var command = GetCommand(control);
        var commandParameter = GetCommandParameter(control);
        if (command is not null && command.CanExecute(commandParameter))
            command.Execute(commandParameter);

        e.Handled = true;
    }
}