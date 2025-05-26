using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.PmtGain;

[IOCAppService(ServiceType = typeof(LaserPmtGainCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPmtGainCalibrationUserControl
{
    public LaserPmtGainCalibrationUserControl()
    {
        InitializeComponent();
    }
}