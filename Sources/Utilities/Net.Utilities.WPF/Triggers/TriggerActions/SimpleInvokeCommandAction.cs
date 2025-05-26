using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace Net.Utilities.WPF.Triggers.TriggerActions;

public sealed class SimpleInvokeCommandAction : TriggerAction<DependencyObject>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SimpleInvokeCommandAction), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(SimpleInvokeCommandAction), new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        var commandParameter = CommandParameter ?? parameter;
        if (Command is not null && Command.CanExecute(commandParameter))
        {
            Command.Execute(commandParameter);
        }
    }
}