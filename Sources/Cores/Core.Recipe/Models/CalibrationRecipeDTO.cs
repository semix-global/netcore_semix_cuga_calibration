using CommunityToolkit.Mvvm.ComponentModel;
using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Recipe.Models;

public partial class CalibrationRecipeDTO : CalibrationRecipeDtoBase, ICloneable<CalibrationRecipeDTO>, IAdaptIn<CalibrationRecipeDTO, CalibrationRecipeDTO>
{
    [ObservableProperty]
    private WaferDto _waferDto = new();

    [ObservableProperty]
    private ReticleMarkDto _reticleMarkDto = new();

    public CalibrationRecipeDTO Clone() => new()
    {
        WaferDto = WaferDto.Clone(),
        ReticleMarkDto = ReticleMarkDto.Clone()
    };

    public CalibrationRecipeDTO AdaptIn(CalibrationRecipeDTO obj)
    {
        WaferDto = new WaferDto().AdaptIn(obj.WaferDto);
        ReticleMarkDto = new ReticleMarkDto().AdaptIn(obj.ReticleMarkDto);
        CreatedUserId = obj.CreatedUserId;
        CreatedUserName = obj.CreatedUserName;
        Id = obj.Id;
        CreatedTime = obj.CreatedTime;
        ModifiedTime = obj.ModifiedTime;
        Expiration = obj.Expiration;
        IsDeleted = obj.IsDeleted;
        return this;
    }
}