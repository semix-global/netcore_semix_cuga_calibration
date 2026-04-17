using Core.Models.Models.Common.Cookies;
using Net.Utilities.WPF.MVVM;
using System.Windows;

namespace Core.Models.Models.Common.Pattern;

public partial class OpticsConfigurationUserControl
{
    public ApplicationCookie ApplicationCookie => HostApplication.GetRequiredService<ApplicationCookie>();

    public static readonly DependencyProperty IsEnableOpticsApodizationModeProperty = DependencyProperty.Register(
        nameof(IsEnableOpticsApodizationMode),
        typeof(bool),
        typeof(OpticsConfigurationUserControl),
        new PropertyMetadata(true, OnIsEnableOpticsApodizationModeChanged));

    private static void OnIsEnableOpticsApodizationModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OpticsConfigurationUserControl control && e.NewValue is bool value) control.IsEnableOpticsApodizationMode = value;
    }

    public bool IsEnableOpticsApodizationMode
    {
        get => (bool)GetValue(IsEnableOpticsApodizationModeProperty);
        set => SetValue(IsEnableOpticsApodizationModeProperty, value);
    }

    public OpticsConfigurationUserControl()
    {
        InitializeComponent();
    }
}