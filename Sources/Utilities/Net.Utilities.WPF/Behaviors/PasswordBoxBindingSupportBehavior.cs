using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.Behaviors;

/// <summary>
/// 此行为允许 PasswordBox 的 Password 可绑定
/// </summary>
public class PasswordBoxBindingSupportBehavior : Behavior<PasswordBox>
{
    public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
        nameof(Password),
        typeof(string),
        typeof(PasswordBoxBindingSupportBehavior),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChangedCallback)
    );

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    private static void OnPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var associatedObject = (sender as PasswordBoxBindingSupportBehavior)?.AssociatedObject;
        if (associatedObject == null)
            return;

        var newValue = e.NewValue as string;
        if (associatedObject.Password != newValue)
            associatedObject.Password = newValue ?? string.Empty;
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        OnPropertyChangedCallback(this, new DependencyPropertyChangedEventArgs(PasswordProperty, null, Password));

        AssociatedObject.PasswordChanged -= OnPasswordChanged;
        AssociatedObject.PasswordChanged += OnPasswordChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.PasswordChanged -= OnPasswordChanged;
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox) throw new ArgumentNullException(nameof(sender));

        if (Password != passwordBox.Password)
            Password = passwordBox.Password;
    }
}