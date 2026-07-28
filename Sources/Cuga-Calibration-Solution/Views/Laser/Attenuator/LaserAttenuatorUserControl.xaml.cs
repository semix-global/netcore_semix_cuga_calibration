using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Laser.Attenuator;

[Permission]
[IOCAppService(ServiceType = typeof(LaserAttenuatorUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorUserControl
{
    public LaserAttenuatorUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}