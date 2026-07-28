using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

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