using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.DOEAngle;

[IOCAppService(ServiceType = typeof(LaserDOEAngleCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class LaserDOEAngleCalibrationUserControl
{
    [Permission]
    public LaserDOEAngleCalibrationUserControl()
    {
        InitializeComponent();
    }
}