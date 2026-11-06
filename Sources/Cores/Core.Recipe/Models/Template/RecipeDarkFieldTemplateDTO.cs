using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Recipe.Models.Template;

public sealed partial class RecipeDarkFieldTemplateDTO : RecipeTemplateDTOBase, ICloneable<RecipeDarkFieldTemplateDTO>, IAdaptIn<RecipeDarkFieldTemplateDTO, RecipeDarkFieldTemplateDTO>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    public RecipeDarkFieldTemplateDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        TemplateId = TemplateId,
        Remark = Remark,
        MaskReticlePosition = MaskReticlePosition,
        TemplateMatchSetting = TemplateMatchSetting.Clone(),
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum
    };

    public RecipeDarkFieldTemplateDTO AdaptIn(RecipeDarkFieldTemplateDTO obj)
    {
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