using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Recipe.Models.Template;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Recipe.Models.Wafer.ReticleMask;

public sealed partial class ReticleMarkItemDto : ObservableObject, ICloneable<ReticleMarkItemDto>, IAdaptIn<ReticleMarkItemDto, ReticleMarkItemDto>
{
    [ObservableProperty]
    private int _maskIndex;

    [ObservableProperty]
    private string _remark = string.Empty;

    [ObservableProperty]
    private WaferMaskTypeEnum _reticleMaskTypeEnum = WaferMaskTypeEnum.DieCorner;

    [ObservableProperty]
    private Point _maskWaferCellPosition;

    [ObservableProperty]
    private RecipeBrightFieldTemplateDto _recipeBrightFieldTemplateDto = new();

    [ObservableProperty]
    private RecipeDarkFieldTemplateDto _recipeDarkFieldTemplateDto = new();

    public ReticleMarkItemDto Clone() => new()
    {
        MaskIndex = MaskIndex,
        Remark = Remark,
        ReticleMaskTypeEnum = ReticleMaskTypeEnum,
        MaskWaferCellPosition = MaskWaferCellPosition,
        RecipeBrightFieldTemplateDto = RecipeBrightFieldTemplateDto.Clone(),
        RecipeDarkFieldTemplateDto = RecipeDarkFieldTemplateDto.Clone()
    };

    public ReticleMarkItemDto AdaptIn(ReticleMarkItemDto obj)
    {
        MaskIndex = obj.MaskIndex;
        Remark = obj.Remark;
        ReticleMaskTypeEnum = obj.ReticleMaskTypeEnum;
        MaskWaferCellPosition = obj.MaskWaferCellPosition;
        RecipeBrightFieldTemplateDto = new RecipeBrightFieldTemplateDto().AdaptIn(obj.RecipeBrightFieldTemplateDto);
        RecipeDarkFieldTemplateDto = new RecipeDarkFieldTemplateDto().AdaptIn(obj.RecipeDarkFieldTemplateDto);

        return this;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ReticleMarkItemDto reticleMarkItemDto)
            return false;
        return reticleMarkItemDto.MaskIndex == MaskIndex
               && reticleMarkItemDto.Remark == Remark
               && reticleMarkItemDto.ReticleMaskTypeEnum == ReticleMaskTypeEnum;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine<int, string, WaferMaskTypeEnum>(MaskIndex, Remark, ReticleMaskTypeEnum);
    }
}