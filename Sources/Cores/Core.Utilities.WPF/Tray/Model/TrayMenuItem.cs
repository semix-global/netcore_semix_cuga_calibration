using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Core.Utilities.WPF.Tray.Model;

public class TrayMenuItem
{
    public string Header { get; set; } = string.Empty;

    public ICommand? Command { get; set; }

    public ObservableCollection<TrayMenuItem> Children { get; set; } = [];
}