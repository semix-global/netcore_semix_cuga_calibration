using Net.Utilities.WPF.MVVM.Providers;
using System.ComponentModel;
using System.Windows;

namespace Net.Utilities.WPF.MVVM.AttachedHelper;

/// <summary>
/// Prism ViewModelLocator.cs
/// </summary>
public sealed class AutoWireViewModelHelper
{
    /// <summary>
    /// 自动注入DataContext
    /// </summary>
    public static readonly DependencyProperty IsAutoWireViewModelProperty = DependencyProperty.RegisterAttached(
        "IsAutoWireViewModel",
        typeof(bool?),
        typeof(AutoWireViewModelHelper),
        new FrameworkPropertyMetadata(
            null,
            OnIsAutoWireViewModelPropertyChangedCallback
        )
    );

    public static bool? GetIsAutoWireViewModel(DependencyObject obj)
    {
        return (bool?)obj.GetValue(IsAutoWireViewModelProperty);
    }

    public static void SetIsAutoWireViewModel(DependencyObject obj, bool value)
    {
        obj.SetValue(IsAutoWireViewModelProperty, value);
    }

    private static void OnIsAutoWireViewModelPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (DesignerProperties.GetIsInDesignMode(d)) return;

        if (d is not FrameworkElement frameworkElement || e.NewValue is not bool value) throw new ArgumentException($"Invalid IsAutoWireViewModel property value: {e.NewValue}");
        if (value == false) return;

        MVVMLocatorProvider.AutoWireViewModelChanged(frameworkElement);
    }
}