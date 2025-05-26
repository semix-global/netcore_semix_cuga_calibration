using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Management.Permission.UsersManagement;

[IOCAppService(ServiceType = typeof(UsersManagementUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class UsersManagementUserControl
{
    public UsersManagementUserControl()
    {
        InitializeComponent();
    }
}