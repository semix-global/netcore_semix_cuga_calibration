using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Recipe.Wafer;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Recipe.Template;

public partial class RecipeTemplateDtoBase : ObservableCacheBase
{
    [ObservableProperty]
    private int _templateId;

    [ObservableProperty]
    private string? _remark;

    [ObservableProperty]
    private Point _maskReticlePosition;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner;

    [ObservableProperty]
    private AlgorithmTemplateTypeEnum _algorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _algorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size256;
}