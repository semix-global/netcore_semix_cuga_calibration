using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.View;

[IOCAppService(ServiceType = typeof(EnableOpticsMagWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class EnableOpticsMagWindow
{
    public EnableOpticsMagWindow()
    {
        InitializeComponent();
    }
}