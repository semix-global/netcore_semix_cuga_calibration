using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.OpticalPowerMeter;

[Permission]
[IOCAppService(ServiceType = typeof(LaserOpticalPowerMeterUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserOpticalPowerMeterUserControl
{
    public LaserOpticalPowerMeterUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}