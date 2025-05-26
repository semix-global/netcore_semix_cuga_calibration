using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Management;

[IOCAppService(ServiceType = typeof(SystemManagementWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class SystemManagementWindow
{
    public SystemManagementWindow()
    {
        InitializeComponent();
    }
}