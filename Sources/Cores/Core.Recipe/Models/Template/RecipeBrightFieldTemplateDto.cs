using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Recipe.Models.Template;

public sealed partial class RecipeBrightFieldTemplateDto : RecipeTemplateDtoBase, ICloneable<RecipeBrightFieldTemplateDto>, IAdaptIn<RecipeBrightFieldTemplateDto, RecipeBrightFieldTemplateDto>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

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

    public RecipeBrightFieldTemplateDto AdaptIn(RecipeBrightFieldTemplateDto obj)
    {
        MicroscopeLensInformation = obj.MicroscopeLensInformation;
        TemplateId = obj.TemplateId;
        Remark = obj.Remark;
        MaskReticlePosition = obj.MaskReticlePosition;
        AlgorithmTemplateTypeEnum = obj.AlgorithmTemplateTypeEnum;
        TemplateImageFilePath = obj.TemplateImageFilePath;
        TemplateFilePath = obj.TemplateFilePath;
        AlgorithmTemplateSizeEnum = obj.AlgorithmTemplateSizeEnum;

        return this;
    }
}