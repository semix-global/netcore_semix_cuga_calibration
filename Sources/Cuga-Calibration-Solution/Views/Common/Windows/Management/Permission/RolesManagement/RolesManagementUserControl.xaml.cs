using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Management.Permission.RolesManagement;

[IOCAppService(ServiceType = typeof(RolesManagementUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class RolesManagementUserControl
{
    public RolesManagementUserControl()
    {
        InitializeComponent();
    }
}