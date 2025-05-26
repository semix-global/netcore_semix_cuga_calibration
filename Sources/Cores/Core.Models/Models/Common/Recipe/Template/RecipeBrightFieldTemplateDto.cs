using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Recipe.Template;

public sealed partial class RecipeBrightFieldTemplateDto : RecipeTemplateDtoBase, ICloneable<RecipeBrightFieldTemplateDto>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    public RecipeBrightFieldTemplateDto Clone() => new()
    {
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
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