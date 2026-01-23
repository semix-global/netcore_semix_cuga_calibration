using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.PixelSize;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserPixelSizeCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPixelSizeCalibrationUserControl
{
    public LaserPixelSizeCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}