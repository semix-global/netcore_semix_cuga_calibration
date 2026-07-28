using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.AutoFocus.GlobalFocusOffset;

[Permission]
[IOCAppService(ServiceType = typeof(AutoFocusGlobalFocusOffsetUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AutoFocusGlobalFocusOffsetUserControl
{
    public AutoFocusGlobalFocusOffsetUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}