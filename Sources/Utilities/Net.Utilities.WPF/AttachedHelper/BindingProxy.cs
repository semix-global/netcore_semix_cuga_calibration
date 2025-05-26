using System.Windows;

namespace Net.Utilities.WPF.AttachedHelper;

public class BindingProxy : Freezable
{
    #region Value

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(object),
        typeof(BindingProxy));

    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    #endregion Value

    protected override Freezable CreateInstanceCore()
    {
        return new BindingProxy();
    }
}