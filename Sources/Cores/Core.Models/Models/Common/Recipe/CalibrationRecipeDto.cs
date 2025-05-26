using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Recipe.Info;
using Core.Models.Models.Common.Recipe.Wafer;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Recipe;

public partial class CalibrationRecipeDto : CalibrationRecipeDtoBase, ICloneable<CalibrationRecipeDto>
{
    [ObservableProperty]
    private CalibrationRecipeInfoDto _calibrationRecipeInfoDto = new();

    [ObservableProperty]
    private WaferDto _waferDto = new();

    public CalibrationRecipeDto Clone() => new()
    {
        CalibrationRecipeInfoDto = CalibrationRecipeInfoDto.Clone(),
        WaferDto = WaferDto.Clone(),
    };
}