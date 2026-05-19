using CommunityToolkit.Mvvm.ComponentModel;
using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

[IOCAppService(ServiceType = typeof(RecipeReticleMarkViewUserControlViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class RecipeReticleMarkViewUserControlViewModel : ViewModelBase
{
    #region 属性

    [ObservableProperty]
    public partial WaferDTO WaferDTO { get; set; } = new();

    [ObservableProperty]
    private ObservableCollection<ReticleMarkDTOItem> _reticleMarkList = [];

    #endregion
}