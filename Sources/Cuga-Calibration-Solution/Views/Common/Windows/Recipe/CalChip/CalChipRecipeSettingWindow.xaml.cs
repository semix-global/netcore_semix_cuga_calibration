using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Recipe.CalChip;

[IOCAppService(ServiceType = typeof(CalChipRecipeSettingWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class CalChipRecipeSettingWindow
{
    public CalChipRecipeSettingWindow()
    {
        InitializeComponent();
    }
}


