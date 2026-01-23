using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.XYAstigmatism;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserXYAstigmatismCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXYAstigmatismCalibrationUserControl
{
    public LaserXYAstigmatismCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}