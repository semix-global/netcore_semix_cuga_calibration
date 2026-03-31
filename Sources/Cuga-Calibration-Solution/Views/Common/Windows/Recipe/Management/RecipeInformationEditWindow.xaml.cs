using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Recipe.Management;

[IOCAppService(ServiceType = typeof(RecipeInformationEditWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class RecipeInformationEditWindow
{
    public RecipeInformationEditWindow()
    {
        InitializeComponent();
    }
}
