using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Recipe.Models.Template;

public sealed partial class RecipeBrightFieldTemplateDTO : RecipeTemplateDTOBase, ICloneable<RecipeBrightFieldTemplateDTO>, IAdaptIn<RecipeBrightFieldTemplateDTO, RecipeBrightFieldTemplateDTO>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    public RecipeBrightFieldTemplateDTO Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        TemplateId = TemplateId,
        Remark = Remark,
        MaskReticlePosition = MaskReticlePosition,
        TemplateMatchSetting = TemplateMatchSetting.Clone(),
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum
    };

    public RecipeBrightFieldTemplateDTO AdaptIn(RecipeBrightFieldTemplateDTO obj)
    {
        MicroscopeLensInformation = obj.MicroscopeLensInformation;
        TemplateId = obj.TemplateId;
        Remark = obj.Remark;
        MaskReticlePosition = obj.MaskReticlePosition;
        TemplateMatchSetting = obj.TemplateMatchSetting.Clone();
        TemplateImageFilePath = obj.TemplateImageFilePath;
        TemplateFilePath = obj.TemplateFilePath;
        AlgorithmTemplateSizeEnum = obj.AlgorithmTemplateSizeEnum;

        return this;
    }
}