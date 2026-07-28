using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.CIB.LineOrientationOffset;

[Permission]
[IOCAppService(ServiceType = typeof(CIBLineOrientationOffsetUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLineOrientationOffsetUserControl
{
    public CIBLineOrientationOffsetUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}