using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Management.Recipe;

[IOCAppService(ServiceType = typeof(RecipeSettingWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class RecipeSettingWindow
{
    public RecipeSettingWindow()
    {
        InitializeComponent();
    }
}