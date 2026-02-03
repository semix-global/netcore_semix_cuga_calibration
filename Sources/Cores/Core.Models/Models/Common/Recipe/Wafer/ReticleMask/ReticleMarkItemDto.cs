using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Recipe.Template;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Recipe.Wafer.ReticleMask;

public sealed partial class ReticleMarkItemDto : ObservableObject, ICloneable<ReticleMarkItemDto>
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
        return HashCode.Combine(MaskIndex, Remark, ReticleMaskTypeEnum);
    }
}