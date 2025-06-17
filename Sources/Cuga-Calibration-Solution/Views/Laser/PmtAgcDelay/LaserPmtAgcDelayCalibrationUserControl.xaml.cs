using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.PmtAgcDelay;

[IOCAppService(ServiceType = typeof(LaserPmtAgcDelayCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPmtAgcDelayCalibrationUserControl
{
    [Permission]
    public LaserPmtAgcDelayCalibrationUserControl()
    {
        InitializeComponent();
    }
}