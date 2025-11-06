using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.View;

[IOCAppService(ServiceType = typeof(EnableProductiveInformationWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class EnableProductiveInformationWindow
{
    public EnableProductiveInformationWindow()
    {
        InitializeComponent();
    }
}