using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Setting;
using Net.Utilities.Models.Geometries;

namespace Core.Recipe.Models.Template;

public partial class RecipeTemplateDTOBase : ObservableObject
{
    [ObservableProperty]
    public partial int TemplateId { get; set; }

    [ObservableProperty]
    public partial string? Remark { get; set; }

    [ObservableProperty]
    public partial Point MaskReticlePosition { get; set; }
    [ObservableProperty]
    public partial TemplateMatchSetting TemplateMatchSetting { get; set; } = new();

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum AlgorithmTemplateSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;
}