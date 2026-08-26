using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Documents;
using Net.Utilities.WaferMap.WPF.Drawables;
using Net.Utilities.WaferMap.WPF.Editors;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMapTemplate : ObservableObject
{
    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial Rect TemplateROI { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;
}