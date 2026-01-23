using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Optics.Relay;

[PermissionControl]
[IOCAppService(ServiceType = typeof(OpticsRelayUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsRelayUserControl
{
    public OpticsRelayUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}