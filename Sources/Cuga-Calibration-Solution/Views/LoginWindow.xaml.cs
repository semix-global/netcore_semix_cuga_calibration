using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views;

[IOCAppService(ServiceType = typeof(LoginWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class LoginWindow
{
    public LoginWindow()
    {
        InitializeComponent();
    }
}