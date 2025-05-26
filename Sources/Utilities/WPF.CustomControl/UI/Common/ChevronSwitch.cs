using System.Windows;
using System.Windows.Controls.Primitives;

namespace WPF.CustomControl.UI.Common;

public class ChevronSwitch : ToggleButton
{
    static ChevronSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ChevronSwitch), new FrameworkPropertyMetadata(typeof(ChevronSwitch)));
    }
}