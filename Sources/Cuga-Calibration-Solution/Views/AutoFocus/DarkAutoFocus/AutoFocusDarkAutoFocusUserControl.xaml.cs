using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.AutoFocus.DarkAutoFocus;

[Permission]
[IOCAppService(ServiceType = typeof(AutoFocusDarkAutoFocusUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AutoFocusDarkAutoFocusUserControl
{
    public AutoFocusDarkAutoFocusUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}