using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Models;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed partial class AddOrModifyRectROIDrawableInputOptions() : InputOptions<Unit>("Select Point")
{
    [ObservableProperty]
    public partial bool IsMultiple { get; set; }
}