using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.LineOrientationOffset;

[IOCAppService(ServiceType = typeof(LaserLineOrientationOffsetCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineOrientationOffsetCalibrationUserControl
{
    [Permission]
    public LaserLineOrientationOffsetCalibrationUserControl()
    {
        InitializeComponent();
    }
}