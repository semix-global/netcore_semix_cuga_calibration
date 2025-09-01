using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Recipe.Template;

public sealed partial class RecipeBrightFieldTemplateDto : RecipeTemplateDtoBase, ICloneable<RecipeBrightFieldTemplateDto>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation =  MicroscopeLensInformation.Default;

    public RecipeBrightFieldTemplateDto Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        TemplateId = TemplateId,
        Remark = Remark,
        MaskReticlePosition = MaskReticlePosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum
    };
}