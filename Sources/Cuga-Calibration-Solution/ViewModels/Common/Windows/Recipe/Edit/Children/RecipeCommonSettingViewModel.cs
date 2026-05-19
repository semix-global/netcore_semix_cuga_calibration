using CommunityToolkit.Mvvm.ComponentModel;
using Core.Recipe.Models;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

[IOCAppService(ServiceType = typeof(RecipeCommonSettingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeCommonSettingViewModel : ViewModelBase
{
    /// <summary>
    /// 当前编辑中的配方副本（由主 VM 在 Loaded 时赋值）
    /// </summary>
    [ObservableProperty]
    private CalibrationRecipeDTO _editDTO = new();

    public void Initialize(CalibrationRecipeDTO dto)
    {
        EditDTO = dto;
    }
}