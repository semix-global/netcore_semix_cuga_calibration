using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Recipe.Management;

[IOCAppService(ServiceType = typeof(RecipeManagementWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class RecipeManagementWindow
{
    public RecipeManagementWindow()
    {
        InitializeComponent();
    }
}