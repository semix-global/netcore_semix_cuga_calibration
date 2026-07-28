using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.AutoFocus.FAFBCompensation;

[Permission]
[IOCAppService(ServiceType = typeof(AutoFocusFAFBCompensationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AutoFocusFAFBCompensationUserControl
{
    public AutoFocusFAFBCompensationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}