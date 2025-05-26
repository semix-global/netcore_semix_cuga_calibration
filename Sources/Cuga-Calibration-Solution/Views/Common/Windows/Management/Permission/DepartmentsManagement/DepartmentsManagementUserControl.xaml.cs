using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Management.Permission.DepartmentsManagement;

[IOCAppService(ServiceType = typeof(DepartmentsManagementUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class DepartmentsManagementUserControl
{
    public DepartmentsManagementUserControl()
    {
        InitializeComponent();
    }
}