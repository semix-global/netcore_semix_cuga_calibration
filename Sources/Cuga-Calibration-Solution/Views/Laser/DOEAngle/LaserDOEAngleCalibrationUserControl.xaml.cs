using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.DOEAngle;

[Permission]
[IOCAppService(ServiceType = typeof(LaserDOEAngleCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class LaserDOEAngleCalibrationUserControl
{
    public LaserDOEAngleCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}