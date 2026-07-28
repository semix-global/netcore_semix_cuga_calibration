using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Optics.Relay;

[Permission]
[IOCAppService(ServiceType = typeof(OpticsRelayUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsRelayUserControl
{
    public OpticsRelayUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}