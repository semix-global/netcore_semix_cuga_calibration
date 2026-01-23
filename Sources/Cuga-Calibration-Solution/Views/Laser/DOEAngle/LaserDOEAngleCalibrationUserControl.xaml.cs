using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.DOEAngle;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserDOEAngleCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class LaserDOEAngleCalibrationUserControl
{
    public LaserDOEAngleCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}