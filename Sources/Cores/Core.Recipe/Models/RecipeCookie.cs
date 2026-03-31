using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Recipe.Models;

public sealed partial class RecipeCookie : ObservableObject
{
    /// <summary>
    /// 当前应用配方的数据库信息
    /// </summary>
    [ObservableProperty]
    private SysRecipeInformationDto _sysRecipeInformationDto = new();

    /// <summary>
    /// 校准当前应用配方
    /// </summary>
    [ObservableProperty]
    private CalibrationRecipeDTO _calibrationRecipeDto = new();

    /// <summary>
    /// 校准根据对准差值修正wafermap坐标后的配方
    /// </summary>
    [ObservableProperty]
    private CalibrationRecipeDTO _calibrationReviseRecipeDto = new();
}