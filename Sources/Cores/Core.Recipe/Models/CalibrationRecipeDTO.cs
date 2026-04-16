using CommunityToolkit.Mvvm.ComponentModel;
using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Recipe.Models;

public partial class CalibrationRecipeDTO : CalibrationRecipeDTOBase, ICloneable<CalibrationRecipeDTO>, IAdaptIn<CalibrationRecipeDTO, CalibrationRecipeDTO>
{
    [ObservableProperty]
    private WaferDTO _waferDTO = new();

    [ObservableProperty]
    private ReticleMarkDTO _reticleMarkDTO = new();

    public CalibrationRecipeDTO Clone() => new()
    {
        WaferDTO = WaferDTO.Clone(),
        ReticleMarkDTO = ReticleMarkDTO.Clone()
    };

    public CalibrationRecipeDTO AdaptIn(CalibrationRecipeDTO obj)
    {
        WaferDTO = new WaferDTO().AdaptIn(obj.WaferDTO);
        ReticleMarkDTO = new ReticleMarkDTO().AdaptIn(obj.ReticleMarkDTO);
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