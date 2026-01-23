using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.Attenuator;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserAttenuatorUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorUserControl
{
    public LaserAttenuatorUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}