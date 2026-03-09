using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Recipe.Template;

public sealed partial class RecipeDarkFieldTemplateDto : RecipeTemplateDtoBase, ICloneable<RecipeDarkFieldTemplateDto>
{
    /*[ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;*/

    public RecipeDarkFieldTemplateDto Clone() => new()
    {
        /*OpticsMagTypeEnum = OpticsMagTypeEnum,
        StageSpeedEnum = StageSpeedEnum,*/
        TemplateId = TemplateId,
        Remark = Remark,
        MaskReticlePosition = MaskReticlePosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum
    };
}