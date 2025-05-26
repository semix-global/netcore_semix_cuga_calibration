using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPF.CustomControl.UI.Common;

public class MagicStackPanel : StackPanel
{
    private readonly SolidColorBrush _color = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
        nameof(ItemHeight),
        typeof(double),
        typeof(MagicStackPanel),
        new PropertyMetadata(0.0, ItemHeightPropertyChanged)
    );

    private static void ItemHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (MagicStackPanel)d;
        panel.Children.Clear();
        panel.Height = panel.ItemHeight;

        var index = (int)panel.ItemHeight / 36;

        for (var i = 0; i < index; i++)
        {
            var border = new Border { Height = 36, Background = panel._color };
            panel.Children.Add(border);
        }
    }
}