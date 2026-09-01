using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace Core.Utilities.WPF.Tray.Model;

public class TrayOptions
{
    public string ToolTip { get; set; } = string.Empty;

    public ImageSource? Icon { get; set; }

    public ObservableCollection<TrayMenuItem> MenuItems { get; set; } = [];

    public object? DataContext { get; set; }

    public ContextMenu? ContextMenu { get; set; }
}