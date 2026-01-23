using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.LineOrientationOffset;

[Permission]
[IOCAppService(ServiceType = typeof(LaserLineOrientationOffsetCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineOrientationOffsetCalibrationUserControl
{
    public LaserLineOrientationOffsetCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}