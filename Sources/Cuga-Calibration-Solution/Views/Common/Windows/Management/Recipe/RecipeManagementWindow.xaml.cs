using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Management.Recipe;

[IOCAppService(ServiceType = typeof(RecipeManagementWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class RecipeManagementWindow
{
    public RecipeManagementWindow()
    {
        InitializeComponent();
    }
}