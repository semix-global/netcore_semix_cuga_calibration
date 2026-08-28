using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMapTemplate : ObservableObject
{
    [ObservableProperty]
    public partial int Index { get; set; } = 0;

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial Vector FindBFMachineVector { get; set; }

    [ObservableProperty]
    public partial Rect TemplateROI { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;
}