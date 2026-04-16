using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Recipe.Models.Template;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Recipe.Models.Wafer.ReticleMask;

public sealed partial class ReticleMarkDTOItem : ObservableObject, ICloneable<ReticleMarkDTOItem>, IAdaptIn<ReticleMarkDTOItem, ReticleMarkDTOItem>
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
    private RecipeBrightFieldTemplateDTO _recipeBrightFieldTemplateDTO = new();

    [ObservableProperty]
    private RecipeDarkFieldTemplateDTO _recipeDarkFieldTemplateDTO = new();

    public ReticleMarkDTOItem Clone() => new()
    {
        MaskIndex = MaskIndex,
        Remark = Remark,
        ReticleMaskTypeEnum = ReticleMaskTypeEnum,
        MaskWaferCellPosition = MaskWaferCellPosition,
        RecipeBrightFieldTemplateDTO = RecipeBrightFieldTemplateDTO.Clone(),
        RecipeDarkFieldTemplateDTO = RecipeDarkFieldTemplateDTO.Clone()
    };

    public ReticleMarkDTOItem AdaptIn(ReticleMarkDTOItem obj)
    {
        MaskIndex = obj.MaskIndex;
        Remark = obj.Remark;
        ReticleMaskTypeEnum = obj.ReticleMaskTypeEnum;
        MaskWaferCellPosition = obj.MaskWaferCellPosition;
        RecipeBrightFieldTemplateDTO = new RecipeBrightFieldTemplateDTO().AdaptIn(obj.RecipeBrightFieldTemplateDTO);
        RecipeDarkFieldTemplateDTO = new RecipeDarkFieldTemplateDTO().AdaptIn(obj.RecipeDarkFieldTemplateDTO);

        return this;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ReticleMarkDTOItem reticleMarkItemDto)
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