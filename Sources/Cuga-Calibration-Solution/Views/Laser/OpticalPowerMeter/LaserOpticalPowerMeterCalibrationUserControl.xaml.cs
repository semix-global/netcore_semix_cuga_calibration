using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.OpticalPowerMeter;

[IOCAppService(ServiceType = typeof(LaserOpticalPowerMeterCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserOpticalPowerMeterCalibrationUserControl
{
    [Permission]
    public LaserOpticalPowerMeterCalibrationUserControl()
    {
        InitializeComponent();
    }
}