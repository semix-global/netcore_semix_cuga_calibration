using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.XTCCalibration;

[IOCAppService(ServiceType = typeof(LaserXTCCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXTCCalibrationUserControl
{
    public LaserXTCCalibrationUserControl()
    {
        InitializeComponent();
    }
}