using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Net.Utilities.WPF.UI.Controls;

public class MagicStackPanel : StackPanel
{
    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
        nameof(ItemHeight),
        typeof(double),
        typeof(MagicStackPanel),
        new PropertyMetadata(0.0, ItemHeightPropertyChanged)
    );

    private readonly SolidColorBrush _color1 = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
    private readonly SolidColorBrush _color2 = new((Color)ColorConverter.ConvertFromString("#EEEEEE"));

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    private static void ItemHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (MagicStackPanel)d;
        panel.Children.Clear();
        panel.Height = panel.ItemHeight;

        var index = (int)panel.ItemHeight / 36;

        for (var i = 0; i < index; i++)
        {
            var border = new Border
            {
                Height = 36,
                Background = i % 2 == 0 ? panel._color1 : panel._color2
            };
            panel.Children.Add(border);
        }
    }
}