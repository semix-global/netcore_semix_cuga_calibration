using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Management.Permission.MenusManagement;

[IOCAppService(ServiceType = typeof(MenusManagementUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class MenusManagementUserControl
{
    public MenusManagementUserControl()
    {
        InitializeComponent();
    }
}