using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.OpticalPowerMeter;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserOpticalPowerMeterUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserOpticalPowerMeterUserControl
{
    public LaserOpticalPowerMeterUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}