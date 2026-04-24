using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Recipe.Models;

public sealed partial class RecipeCookie : ObservableObject
{
    /// <summary>
    /// 当前应用配方的数据库信息
    /// </summary>
    [ObservableProperty]
    private SysRecipeInformationDTO _sysRecipeInformationDTO = new();

    /// <summary>
    /// 校准当前应用配方
    /// </summary>
    // [property:DefaultCache]
    [ObservableProperty]
    private CalibrationRecipeDTO _calibrationRecipeDto = new();
}