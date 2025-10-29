using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.OpticalPowerMeter;

[IOCAppService(ServiceType = typeof(LaserOpticalPowerMeterUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserOpticalPowerMeterUserControl
{
    [Permission]
    public LaserOpticalPowerMeterUserControl()
    {
        InitializeComponent();
    }
}