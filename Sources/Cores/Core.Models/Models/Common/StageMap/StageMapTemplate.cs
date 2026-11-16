using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.StageMap;

public sealed partial class StageMapTemplate : ObservableObject, ICloneable<StageMapTemplate>
{
    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial int DisplayIndex { get; set; }

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial Rect TemplateROI { get; set; }

    [ObservableProperty]
    public partial string OriginImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    public StageMapTemplate Clone() => new()
    {
        DisplayIndex = DisplayIndex,
        FindBFMachinePosition = FindBFMachinePosition,
        TemplateROI = TemplateROI,
        OriginImageFilePath = OriginImageFilePath,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath
    };
}