using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.LineOrientationOffset;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserLineOrientationOffsetCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineOrientationOffsetCalibrationUserControl
{
    public LaserLineOrientationOffsetCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}