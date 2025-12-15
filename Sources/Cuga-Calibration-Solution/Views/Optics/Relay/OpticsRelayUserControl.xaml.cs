using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Optics.Relay;

[IOCAppService(ServiceType = typeof(OpticsRelayUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsRelayUserControl
{
    [Permission]
    public OpticsRelayUserControl()
    {
        InitializeComponent();
    }
}