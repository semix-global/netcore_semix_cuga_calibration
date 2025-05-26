using System.Windows;
using System.Windows.Controls.Primitives;

namespace Net.Utilities.WPF.UI.Controls;

public class ChevronSwitch : ToggleButton
{
    static ChevronSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ChevronSwitch), new FrameworkPropertyMetadata(typeof(ChevronSwitch)));
    }
}