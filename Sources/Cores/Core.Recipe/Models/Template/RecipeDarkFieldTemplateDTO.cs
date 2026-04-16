using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Recipe.Models.Template;

public sealed partial class RecipeDarkFieldTemplateDTO : RecipeTemplateDTOBase, ICloneable<RecipeDarkFieldTemplateDTO>, IAdaptIn<RecipeDarkFieldTemplateDTO, RecipeDarkFieldTemplateDTO>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

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
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum
    };

    public RecipeDarkFieldTemplateDTO AdaptIn(RecipeDarkFieldTemplateDTO obj)
    {
        ProductivityInformation = ProductivityInformation.AdaptIn(obj.ProductivityInformation);
        MicroscopeLensInformation = MicroscopeLensInformation.AdaptIn(obj.MicroscopeLensInformation);
        LaserLightInformation = LaserLightInformation.AdaptIn(obj.LaserLightInformation);
        CIBConfiguration = CIBConfiguration.AdaptIn(obj.CIBConfiguration);
        CIBInformation = CIBInformation.AdaptIn(obj.CIBInformation);
        OpticsConfiguration = OpticsConfiguration.AdaptIn(obj.OpticsConfiguration);
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