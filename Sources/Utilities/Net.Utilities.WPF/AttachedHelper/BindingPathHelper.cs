using System.Windows;

namespace Net.Utilities.WPF.AttachedHelper;

public class BindingPathHelper : DependencyObject
{
    public static readonly DependencyProperty TargetPropertyProperty =
        DependencyProperty.RegisterAttached(
            "TargetProperty",
            typeof(DependencyProperty),
            typeof(BindingPathHelper),
            new PropertyMetadata(default(DependencyProperty))
        );

    public static DependencyProperty GetTargetProperty(DependencyObject obj)
    {
        var targetProperty = (DependencyProperty)obj.GetValue(TargetPropertyProperty);
        return targetProperty;
    }

    public static void SetTargetProperty(DependencyObject obj, DependencyProperty value) =>
        obj.SetValue(TargetPropertyProperty, value);
}