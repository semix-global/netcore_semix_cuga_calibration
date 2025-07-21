using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Recipe.Template;

public sealed partial class RecipeBrightFieldTemplateDto : RecipeTemplateDtoBase, ICloneable<RecipeBrightFieldTemplateDto>
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    public RecipeBrightFieldTemplateDto Clone() => new()
    {
        MicroscopeMagnificationInfo = MicroscopeMagnificationInfo,
        TemplateId = TemplateId,
        Remark = Remark,
        MaskReticlePosition = MaskReticlePosition,
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum
    };
}